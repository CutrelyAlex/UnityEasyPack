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

        // 分离有持续时间的Buff和永久Buff
        private readonly List<Buff> _timedBuffs = new List<Buff>();
        private readonly List<Buff> _permanentBuffs = new List<Buff>();

        // 缓存需要触发的Buff
        private readonly List<Buff> _triggeredBuffs = new List<Buff>();

        // 模块执行缓存
        private readonly List<BuffModule> _moduleCache = new List<BuffModule>();

        // 快速查找索引
        private readonly Dictionary<string, List<Buff>> _buffsByID = new Dictionary<string, List<Buff>>();
        private readonly Dictionary<string, List<Buff>> _buffsByTag = new Dictionary<string, List<Buff>>();
        private readonly Dictionary<string, List<Buff>> _buffsByLayer = new Dictionary<string, List<Buff>>();

        // Buff位置索引
        private readonly Dictionary<Buff, int> _buffPositions = new Dictionary<Buff, int>();
        private readonly Dictionary<Buff, int> _timedBuffPositions = new Dictionary<Buff, int>();
        private readonly Dictionary<Buff, int> _permanentBuffPositions = new Dictionary<Buff, int>();
        // 批量移除缓存
        private readonly HashSet<Buff> _buffsToRemove = new HashSet<Buff>();
        private readonly List<int> _removalIndices = new List<int>();


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

                _buffPositions[buff] = _allBuffs.Count;
                _allBuffs.Add(buff);

                // 根据持续时间分类
                if (buff.DurationTimer > 0)
                {
                    _timedBuffs.Add(buff);
                }
                else
                {
                    _permanentBuffs.Add(buff);
                }

                // 添加到索引
                AddBuffToIndexes(buff);

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
        /// 将Buff添加到各种索引中
        /// </summary>
        private void AddBuffToIndexes(Buff buff)
        {
            // 添加到ID索引
            if (!_buffsByID.TryGetValue(buff.BuffData.ID, out var idList))
            {
                idList = new List<Buff>();
                _buffsByID[buff.BuffData.ID] = idList;
            }
            idList.Add(buff);

            // 添加到标签索引
            if (buff.BuffData.Tags != null)
            {
                foreach (var tag in buff.BuffData.Tags)
                {
                    if (!_buffsByTag.TryGetValue(tag, out var tagList))
                    {
                        tagList = new List<Buff>();
                        _buffsByTag[tag] = tagList;
                    }
                    tagList.Add(buff);
                }
            }

            // 添加到层级索引
            if (buff.BuffData.Layers != null)
            {
                foreach (var layer in buff.BuffData.Layers)
                {
                    if (!_buffsByLayer.TryGetValue(layer, out var layerList))
                    {
                        layerList = new List<Buff>();
                        _buffsByLayer[layer] = layerList;
                    }
                    layerList.Add(buff);
                }
            }
        }

        /// <summary>
        /// 移除指定目标的所有Buff
        /// </summary>
        public BuffManager RemoveAllBuffs(object target)
        {
            if (_targetToBuffs.TryGetValue(target, out List<Buff> buffs))
            {
                // 批量移除优化
                foreach (var buff in buffs)
                {
                    _buffsToRemove.Add(buff);
                }

                // 执行批量移除
                BatchRemoveBuffs();
            }
            return this;
        }

        /// <summary>
        /// 从各种索引中移除Buff
        /// </summary>
        private void RemoveBuffFromIndexes(Buff buff)
        {
            // 从ID索引中移除
            if (_buffsByID.TryGetValue(buff.BuffData.ID, out var idList))
            {
                FastRemoveFromList(idList, buff);
                if (idList.Count == 0)
                {
                    _buffsByID.Remove(buff.BuffData.ID);
                }
            }

            // 从标签索引中移除
            if (buff.BuffData.Tags != null)
            {
                foreach (var tag in buff.BuffData.Tags)
                {
                    if (_buffsByTag.TryGetValue(tag, out var tagList))
                    {
                        FastRemoveFromList(tagList, buff);
                        if (tagList.Count == 0)
                        {
                            _buffsByTag.Remove(tag);
                        }
                    }
                }
            }

            // 从层级索引中移除
            if (buff.BuffData.Layers != null)
            {
                foreach (var layer in buff.BuffData.Layers)
                {
                    if (_buffsByLayer.TryGetValue(layer, out var layerList))
                    {
                        FastRemoveFromList(layerList, buff);
                        if (layerList.Count == 0)
                        {
                            _buffsByLayer.Remove(layer);
                        }
                    }
                }
            }
        }


        /// <summary>
        /// 批量移除Buff
        /// </summary>
        private void BatchRemoveBuffs()
        {
            if (_buffsToRemove.Count == 0)
                return;

            // 执行移除回调
            foreach (var buff in _buffsToRemove)
            {
                buff.OnRemove?.Invoke(buff);
                ExecuteBuffModules(buff, BuffCallBackType.OnRemove);
            }

            // 批量从主列表移除
            BatchRemoveFromList(_allBuffs, _buffPositions, _buffsToRemove);

            // 批量从时间列表移除
            BatchRemoveFromList(_timedBuffs, _timedBuffPositions, _buffsToRemove);

            // 批量从永久列表移除
            BatchRemoveFromList(_permanentBuffs, _permanentBuffPositions, _buffsToRemove);

            // 从目标索引移除
            var targetGroups = _buffsToRemove.GroupBy(b => b.Target);
            foreach (var group in targetGroups)
            {
                if (_targetToBuffs.TryGetValue(group.Key, out List<Buff> targetBuffs))
                {
                    foreach (var buff in group)
                    {
                        FastRemoveFromList(targetBuffs, buff);
                    }

                    if (targetBuffs.Count == 0)
                    {
                        _targetToBuffs.Remove(group.Key);
                    }
                }
            }

            // 从快速索引移除
            foreach (var buff in _buffsToRemove)
            {
                RemoveBuffFromIndexes(buff);
            }

            _buffsToRemove.Clear();
        }


        /// <summary>
        /// 批量从列表中移除元素
        /// </summary>
        private void BatchRemoveFromList(List<Buff> list, Dictionary<Buff, int> positions, HashSet<Buff> itemsToRemove)
        {
            if (list.Count == 0 || itemsToRemove.Count == 0)
                return;

            _removalIndices.Clear();

            // 收集需要移除的索引
            foreach (var item in itemsToRemove)
            {
                if (positions.TryGetValue(item, out int index))
                {
                    _removalIndices.Add(index);
                }
            }

            if (_removalIndices.Count == 0)
                return;

            // 排序索引，从高到低移除
            _removalIndices.Sort((a, b) => b.CompareTo(a));

            // 批量移除
            foreach (int index in _removalIndices)
            {
                if (index < list.Count)
                {
                    Buff removedBuff = list[index];
                    int lastIndex = list.Count - 1;

                    if (index != lastIndex)
                    {
                        // 将最后一个元素移到当前位置
                        Buff lastBuff = list[lastIndex];
                        list[index] = lastBuff;
                        positions[lastBuff] = index;
                    }

                    // 移除最后一个元素
                    list.RemoveAt(lastIndex);
                    positions.Remove(removedBuff);
                }
            }
        }

        /// <summary>
        /// 快速从列表中移除元素
        /// </summary>
        private void FastRemoveFromList(List<Buff> list, Buff item)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == item)
                {
                    // 将最后一个元素移到当前位置，然后移除最后一个元素
                    list[i] = list[list.Count - 1];
                    list.RemoveAt(list.Count - 1);
                    break;
                }
            }
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

            _buffsToRemove.Add(buff);
            BatchRemoveBuffs();

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
                    _buffsToRemove.Add(buff);
                    BatchRemoveBuffs();
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
                foreach (var buff in buffs.Where(b => b.BuffData.HasTag(tag)))
                {
                    _buffsToRemove.Add(buff);
                }
                BatchRemoveBuffs();
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
                foreach (var buff in buffs.Where(b => b.BuffData.InLayer(layer)))
                {
                    _buffsToRemove.Add(buff);
                }
                BatchRemoveBuffs();
            }
            return this;
        }


        /// <summary>
        /// 移除所有指定ID的Buff（所有目标）
        /// </summary>
        public BuffManager RemoveAllBuffsByID(string buffID)
        {
            if (_buffsByID.TryGetValue(buffID, out var buffs))
            {
                foreach (var buff in buffs)
                {
                    _buffsToRemove.Add(buff);
                }
                BatchRemoveBuffs();
            }
            return this;
        }

        /// <summary>
        /// 移除所有带有特定标签的Buff（所有目标）
        /// </summary>
        public BuffManager RemoveAllBuffsByTag(string tag)
        {
            if (_buffsByTag.TryGetValue(tag, out var buffs))
            {
                foreach (var buff in buffs)
                {
                    _buffsToRemove.Add(buff);
                }
                BatchRemoveBuffs();
            }
            return this;
        }

        /// <summary>
        /// 移除所有特定层级的Buff（所有目标）
        /// </summary>
        public BuffManager RemoveAllBuffsByLayer(string layer)
        {
            if (_buffsByLayer.TryGetValue(layer, out var buffs))
            {
                foreach (var buff in buffs)
                {
                    _buffsToRemove.Add(buff);
                }
                BatchRemoveBuffs();
            }
            return this;
        }

        /// <summary>
        /// 立即执行所有待移除的Buff
        /// </summary>
        public BuffManager FlushPendingRemovals()
        {
            BatchRemoveBuffs();
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

        /// <summary>
        /// 获取指定目标上带有特定标签的所有Buff
        /// </summary>
        public List<Buff> GetBuffsByTag(object target, string tag)
        {
            if (_targetToBuffs.TryGetValue(target, out List<Buff> buffs))
            {
                return buffs.Where(b => b.BuffData.HasTag(tag)).ToList();
            }
            return new List<Buff>();
        }

        /// <summary>
        /// 获取指定目标上特定层级的所有Buff
        /// </summary>
        public List<Buff> GetBuffsByLayer(object target, string layer)
        {
            if (_targetToBuffs.TryGetValue(target, out List<Buff> buffs))
            {
                return buffs.Where(b => b.BuffData.InLayer(layer)).ToList();
            }
            return new List<Buff>();
        }

        /// <summary>
        /// 获取所有指定ID的Buff（所有目标）
        /// </summary>
        public List<Buff> GetAllBuffsByID(string buffID)
        {
            if (_buffsByID.TryGetValue(buffID, out var buffs))
            {
                return new List<Buff>(buffs);
            }
            return new List<Buff>();
        }

        /// <summary>
        /// 获取所有带有特定标签的Buff（所有目标）
        /// </summary>
        public List<Buff> GetAllBuffsByTag(string tag)
        {
            if (_buffsByTag.TryGetValue(tag, out var buffs))
            {
                return new List<Buff>(buffs);
            }
            return new List<Buff>();
        }

        /// <summary>
        /// 获取所有特定层级的Buff（所有目标）
        /// </summary>
        public List<Buff> GetAllBuffsByLayer(string layer)
        {
            if (_buffsByLayer.TryGetValue(layer, out var buffs))
            {
                return new List<Buff>(buffs);
            }
            return new List<Buff>();
        }

        /// <summary>
        /// 检查是否存在指定ID的Buff（任何目标）
        /// </summary>
        public bool HasBuffByID(string buffID)
        {
            return _buffsByID.ContainsKey(buffID) && _buffsByID[buffID].Count > 0;
        }

        /// <summary>
        /// 检查是否存在带有特定标签的Buff（任何目标）
        /// </summary>
        public bool HasBuffByTag(string tag)
        {
            return _buffsByTag.ContainsKey(tag) && _buffsByTag[tag].Count > 0;
        }

        /// <summary>
        /// 检查是否存在特定层级的Buff（任何目标）
        /// </summary>
        public bool HasBuffByLayer(string layer)
        {
            return _buffsByLayer.ContainsKey(layer) && _buffsByLayer[layer].Count > 0;
        }


        #endregion

        #region 更新
        // <summary>
        /// 更新所有Buff
        /// </summary>
        public BuffManager Update(float deltaTime)
        {
            _removedBuffs.Clear();
            _triggeredBuffs.Clear();

            // 只更新有时间限制的Buff
            UpdateTimedBuffs(deltaTime);

            // 分别处理永久Buff的触发
            UpdatePermanentBuffs(deltaTime);

            // 批量触发Buff
            BatchTriggerBuffs();

            // 批量执行更新回调
            BatchUpdateBuffs();

            // 移除过期的Buff
            foreach (var buff in _removedBuffs)
            {
                InternalRemoveBuff(buff);
            }

            return this;
        }

        /// <summary>
        /// 更新有时间限制的Buff
        /// </summary>
        private void UpdateTimedBuffs(float deltaTime)
        {
            for (int i = _timedBuffs.Count - 1; i >= 0; i--)
            {
                var buff = _timedBuffs[i];

                // 更新持续时间
                buff.DurationTimer -= deltaTime;
                if (buff.DurationTimer <= 0)
                {
                    _removedBuffs.Add(buff);
                    continue;
                }

                // 检查触发间隔
                buff.TriggerTimer -= deltaTime;
                if (buff.TriggerTimer <= 0)
                {
                    buff.TriggerTimer = buff.BuffData.TriggerInterval;
                    _triggeredBuffs.Add(buff);
                }
            }
        }

        /// <summary>
        /// 更新永久Buff
        /// </summary>
        private void UpdatePermanentBuffs(float deltaTime)
        {
            for (int i = 0; i < _permanentBuffs.Count; i++)
            {
                var buff = _permanentBuffs[i];

                // 检查触发间隔
                buff.TriggerTimer -= deltaTime;
                if (buff.TriggerTimer <= 0)
                {
                    buff.TriggerTimer = buff.BuffData.TriggerInterval;
                    _triggeredBuffs.Add(buff);
                }
            }
        }

        /// <summary>
        /// 批量触发Buff
        /// </summary>
        private void BatchTriggerBuffs()
        {
            foreach (var buff in _triggeredBuffs)
            {
                buff.OnTrigger?.Invoke(buff);
                ExecuteBuffModules(buff, BuffCallBackType.OnTick);
            }
        }

        /// <summary>
        /// 批量执行更新回调
        /// </summary>
        private void BatchUpdateBuffs()
        {
            // 处理有时间限制的Buff
            foreach (var buff in _timedBuffs)
            {
                if (!_removedBuffs.Contains(buff))
                {
                    buff.OnUpdate?.Invoke(buff);
                    ExecuteBuffModules(buff, BuffCallBackType.OnUpdate);
                }
            }

            // 处理永久Buff
            foreach (var buff in _permanentBuffs)
            {
                buff.OnUpdate?.Invoke(buff);
                ExecuteBuffModules(buff, BuffCallBackType.OnUpdate);
            }
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
            if (buff.BuffData.BuffModules == null || buff.BuffData.BuffModules.Count == 0)
                return;

            // 使用缓存列表
            _moduleCache.Clear();

            var modules = buff.BuffData.BuffModules;
            for (int i = 0; i < modules.Count; i++)
            {
                if (modules[i].ShouldExecute(callBackType, customCallbackName))
                {
                    _moduleCache.Add(modules[i]);
                }
            }

            // 插排
            for (int i = 1; i < _moduleCache.Count; i++)
            {
                var key = _moduleCache[i];
                int j = i - 1;

                while (j >= 0 && _moduleCache[j].Priority < key.Priority)
                {
                    _moduleCache[j + 1] = _moduleCache[j];
                    j--;
                }
                _moduleCache[j + 1] = key;
            }

            // 执行模块
            for (int i = 0; i < _moduleCache.Count; i++)
            {
                _moduleCache[i].Execute(buff, callBackType, customCallbackName, parameters);
            }
        }

        #endregion
    }
}