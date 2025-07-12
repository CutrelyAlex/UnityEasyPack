using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EasyPack
{
    /// <summary>
    /// 只负责Buff的生命周期管理和事件，不直接操作IProperty。
    /// Buff的应用与移除由BuffHandle负责。
    /// </summary>
    public class BuffManager
    {
        private readonly Dictionary<object, List<Buff>> _targetToBuffs = new Dictionary<object, List<Buff>>();
        private readonly List<Buff> _allBuffs = new List<Buff>();
        private readonly List<Buff> _removedBuffs = new List<Buff>();

        #region BUFF管理
        /// <summary>
        /// 创建一个新的Buff
        /// </summary>
        public Buff AddBuff(BuffData buffData, GameObject creator, GameObject target)
        {
            if (buffData == null)
                return null;

            // 将Buff添加到管理列表中
            if (!_targetToBuffs.TryGetValue(target, out List<Buff> buffs))
            {
                buffs = new List<Buff>();
                _targetToBuffs[target] = buffs;
            }

            // 检查已有相同ID的Buff
            Buff existingBuff = buffs.FirstOrDefault(b => b.BuffData.ID == buffData.ID);
            if (existingBuff != null)
            {
                // 处理已存在的Buff的持续时间
                switch (buffData.BuffSuperpositionStrategy)
                {
                    case BuffSuperpositionDurationType.Add:
                        // 叠加持续时间
                        existingBuff.DurationTimer += buffData.Duration;
                        break;
                    case BuffSuperpositionDurationType.ResetThenAdd:
                        // 重置持续时间后再叠加
                        existingBuff.DurationTimer = 2 * buffData.Duration;
                        break;
                    case BuffSuperpositionDurationType.Reset:
                        existingBuff.DurationTimer = buffData.Duration;
                        // 重置持续时间   
                        break;
                    case BuffSuperpositionDurationType.Keep:
                        break;
                }

                // 处理堆叠数
                switch (buffData.BuffSuperpositionStacksStrategy)
                {
                    case BuffSuperpositionStacksType.Add:
                        // 叠加堆叠数
                        AddStackToBuff(existingBuff);
                        break;
                    case BuffSuperpositionStacksType.ResetThenAdd:
                        // 重置堆叠数后再叠加
                        existingBuff.CurrentStacks = 1;
                        AddStackToBuff(existingBuff);
                        break;
                    case BuffSuperpositionStacksType.Reset:
                        existingBuff.CurrentStacks = 1;
                        break;
                    case BuffSuperpositionStacksType.Keep:
                        break;
                }

                return existingBuff; // 返回已存在的Buff，不添加新Buff
            }
            else
            {
                // 创建新的Buff
                Buff buff = new()
                {
                    BuffData = buffData,
                    Creator = creator,
                    Target = target,
                    DurationTimer = buffData.Duration > 0 ? buffData.Duration : -1f,
                    TriggerTimer = buffData.TriggerInterval,
                    CurrentStacks = 1 // 确保初始堆叠为1
                };

                // 添加新的Buff
                buffs.Add(buff);
                _allBuffs.Add(buff);

                // 执行Buff创建回调
                buff.OnCreate?.Invoke(buff);
                ExecuteBuffModules(buff, BuffCallBackType.OnCreate);

                // 如果Buff需要在创建时触发一次
                if (buffData.TriggerOnCreate)
                {
                    buff.OnTrigger?.Invoke(buff);
                    ExecuteBuffModules(buff, BuffCallBackType.OnTick);
                }

                return buff;
            }
        }

        /// <summary>
        /// 移除指定目标的所有Buff
        /// </summary>
        public BuffManager RemoveAllBuffs(object target)
        {
            if (_targetToBuffs.TryGetValue(target, out List<Buff> buffs))
            {
                foreach (var buff in buffs.ToList())
                {
                    RemoveBuff(buff);
                }
            }
            return this;
        }

        /// <summary>
        /// 移除指定的Buff
        /// </summary>
        public BuffManager RemoveBuff(Buff buff)
        {
            if (buff == null)
                return this;

            switch (buff.BuffData.BuffRemoveStrategy)
            {
                case BuffRemoveType.All:
                    // 完全移除Buff
                    InternalRemoveBuff(buff);
                    break;

                case BuffRemoveType.OneStack:
                    // 减少一层
                    ReduceStackFromBuff(buff);
                    break;

                case BuffRemoveType.Manual:
                    break;
            }

            return this;
        }

        /// <summary>
        /// 从管理列表中移除Buff
        /// </summary>
        private BuffManager InternalRemoveBuff(Buff buff)
        {
            if (buff == null)
                return this;

            buff.OnRemove?.Invoke(buff);
            ExecuteBuffModules(buff, BuffCallBackType.OnRemove);

            _allBuffs.Remove(buff);

            if (_targetToBuffs.TryGetValue(buff.Target, out List<Buff> buffs))
            {
                buffs.Remove(buff);
                if (buffs.Count == 0)
                {
                    _targetToBuffs.Remove(buff.Target);
                }
            }
            return this;
        }

        #endregion

        #region 删除操作
        /// <summary>
        /// 移除指定目标上指定ID的Buff
        /// </summary>
        public BuffManager RemoveBuffByID(object target, string buffID)
        {
            if (_targetToBuffs.TryGetValue(target, out List<Buff> buffs))
            {
                Buff buff = buffs.FirstOrDefault(b => b.BuffData.ID == buffID);
                if (buff != null)
                {
                    RemoveBuff(buff);
                }
            }
            return this;
        }

        /// <summary>
        /// 移除指定目标上带有特定标签的所有Buff
        /// </summary>
        public BuffManager RemoveBuffsByTag(object target, string tag)
        {
            if (_targetToBuffs.TryGetValue(target, out List<Buff> buffs))
            {
                foreach (var buff in buffs.Where(b => b.BuffData.HasTag(tag)).ToList())
                {
                    RemoveBuff(buff);
                }
            }
            return this;
        }

        /// <summary>
        /// 移除指定目标上特定层级的所有Buff
        /// </summary>
        public BuffManager RemoveBuffsByLayer(object target, string layer)
        {
            if (_targetToBuffs.TryGetValue(target, out List<Buff> buffs))
            {
                foreach (var buff in buffs.Where(b => b.BuffData.InLayer(layer)).ToList())
                {
                    RemoveBuff(buff);
                }
            }
            return this;
        }
        #endregion

        #region 查询操作
        /// <summary>
        /// 检查指定目标是否有特定ID的Buff
        /// </summary>
        public bool HasBuff(object target, string buffID)
        {
            if (_targetToBuffs.TryGetValue(target, out List<Buff> buffs))
            {
                return buffs.Any(b => b.BuffData.ID == buffID);
            }
            return false;
        }

        /// <summary>
        /// 获取指定目标特定ID的Buff
        /// </summary>
        public Buff GetBuff(object target, string buffID)
        {
            if (_targetToBuffs.TryGetValue(target, out List<Buff> buffs))
            {
                return buffs.FirstOrDefault(b => b.BuffData.ID == buffID);
            }
            return null;
        }

        /// <summary>
        /// 获取指定目标上所有的Buff
        /// </summary>
        public List<Buff> GetAllBuffs(object target)
        {
            if (_targetToBuffs.TryGetValue(target, out List<Buff> buffs))
            {
                return new List<Buff>(buffs);
            }
            return new List<Buff>();
        }
        #endregion

        #region 更新
        /// <summary>
        /// 更新所有Buff
        /// </summary>
        public BuffManager Update(float deltaTime)
        {
            _removedBuffs.Clear();

            foreach (var buff in _allBuffs)
            {
                // 更新持续时间
                if (buff.DurationTimer > 0)
                {
                    buff.DurationTimer -= deltaTime;
                    if (buff.DurationTimer <= 0)
                    {
                        _removedBuffs.Add(buff);
                        continue;
                    }
                }

                // 更新触发间隔
                buff.TriggerTimer -= deltaTime;
                if (buff.TriggerTimer <= 0)
                {
                    buff.TriggerTimer = buff.BuffData.TriggerInterval;
                    buff.OnTrigger?.Invoke(buff);
                    ExecuteBuffModules(buff, BuffCallBackType.OnTick);
                }

                // 执行Buff更新回调
                buff.OnUpdate?.Invoke(buff);
                ExecuteBuffModules(buff, BuffCallBackType.OnUpdate);
            }

            // 移除过期的Buff
            foreach (var buff in _removedBuffs)
            {
                InternalRemoveBuff(buff);
            }

            return this;
        }

        #endregion

        #region 层级操作
        /// <summary>
        /// 给Buff增加层，并检查是否超过最大层数
        /// </summary>
        private BuffManager AddStackToBuff(Buff buff, int stack = 1)
        {
            if (buff == null || buff.CurrentStacks >= buff.BuffData.MaxStacks)
                return this;
            
            buff.CurrentStacks += stack;
            buff.CurrentStacks = Mathf.Min(buff.CurrentStacks, buff.BuffData.MaxStacks);

            buff.OnAddStack?.Invoke(buff);
            ExecuteBuffModules(buff, BuffCallBackType.OnAddStack);

            return this;
        }

        /// <summary>
        /// 从Buff减少层，如果减至0则移除
        /// </summary>
        private BuffManager ReduceStackFromBuff(Buff buff, int stack = 1)
        {
            if (buff == null || buff.CurrentStacks <= 1)
            {
                InternalRemoveBuff(buff);
                return this;
            }

            buff.CurrentStacks -= stack;
            if(buff.CurrentStacks <= 0)
            {
                InternalRemoveBuff(buff);
                return this;
            }

            buff.OnReduceStack?.Invoke(buff);
            ExecuteBuffModules(buff, BuffCallBackType.OnReduceStack);
            return this;
        }

        #endregion

        #region 模块执行
        /// <summary>
        /// 执行Buff上对应类型的所有模块
        /// </summary>
        private void ExecuteBuffModules(Buff buff, BuffCallBackType callBackType, string customCallbackName = "", params object[] parameters)
        {
            var modules = buff.BuffData.BuffModules
                .Where(m => m.ShouldExecute(callBackType, customCallbackName))
                .OrderByDescending(m => m.Priority);

            foreach (var module in modules)
            {
                module.Execute(buff, callBackType, customCallbackName, parameters);
            }
        }
        #endregion
    }
}