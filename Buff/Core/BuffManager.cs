using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EasyPack
{
    /// <summary>
    /// ֻ����Buff���������ڹ�����¼�����ֱ�Ӳ���IProperty��
    /// Buff��Ӧ�����Ƴ���BuffHandle����
    /// </summary>
    public class BuffManager
    {
        private readonly Dictionary<object, List<Buff>> _targetToBuffs = new Dictionary<object, List<Buff>>();
        private readonly List<Buff> _allBuffs = new List<Buff>();
        private readonly List<Buff> _removedBuffs = new List<Buff>();

        #region BUFF����
        /// <summary>
        /// ����һ���µ�Buff
        /// </summary>
        public Buff AddBuff(BuffData buffData, GameObject creator, GameObject target)
        {
            if (buffData == null)
                return null;

            // ��Buff��ӵ������б���
            if (!_targetToBuffs.TryGetValue(target, out List<Buff> buffs))
            {
                buffs = new List<Buff>();
                _targetToBuffs[target] = buffs;
            }

            // ���������ͬID��Buff
            Buff existingBuff = buffs.FirstOrDefault(b => b.BuffData.ID == buffData.ID);
            if (existingBuff != null)
            {
                // �����Ѵ��ڵ�Buff�ĳ���ʱ��
                switch (buffData.BuffSuperpositionStrategy)
                {
                    case BuffSuperpositionDurationType.Add:
                        // ���ӳ���ʱ��
                        existingBuff.DurationTimer += buffData.Duration;
                        break;
                    case BuffSuperpositionDurationType.ResetThenAdd:
                        // ���ó���ʱ����ٵ���
                        existingBuff.DurationTimer = 2 * buffData.Duration;
                        break;
                    case BuffSuperpositionDurationType.Reset:
                        existingBuff.DurationTimer = buffData.Duration;
                        // ���ó���ʱ��   
                        break;
                    case BuffSuperpositionDurationType.Keep:
                        break;
                }

                // ����ѵ���
                switch (buffData.BuffSuperpositionStacksStrategy)
                {
                    case BuffSuperpositionStacksType.Add:
                        // ���Ӷѵ���
                        AddStackToBuff(existingBuff);
                        break;
                    case BuffSuperpositionStacksType.ResetThenAdd:
                        // ���öѵ������ٵ���
                        existingBuff.CurrentStacks = 1;
                        AddStackToBuff(existingBuff);
                        break;
                    case BuffSuperpositionStacksType.Reset:
                        existingBuff.CurrentStacks = 1;
                        break;
                    case BuffSuperpositionStacksType.Keep:
                        break;
                }

                return existingBuff; // �����Ѵ��ڵ�Buff���������Buff
            }
            else
            {
                // �����µ�Buff
                Buff buff = new()
                {
                    BuffData = buffData,
                    Creator = creator,
                    Target = target,
                    DurationTimer = buffData.Duration > 0 ? buffData.Duration : -1f,
                    TriggerTimer = buffData.TriggerInterval,
                    CurrentStacks = 1 // ȷ����ʼ�ѵ�Ϊ1
                };

                // ����µ�Buff
                buffs.Add(buff);
                _allBuffs.Add(buff);

                // ִ��Buff�����ص�
                buff.OnCreate?.Invoke(buff);
                ExecuteBuffModules(buff, BuffCallBackType.OnCreate);

                // ���Buff��Ҫ�ڴ���ʱ����һ��
                if (buffData.TriggerOnCreate)
                {
                    buff.OnTrigger?.Invoke(buff);
                    ExecuteBuffModules(buff, BuffCallBackType.OnTick);
                }

                return buff;
            }
        }

        /// <summary>
        /// �Ƴ�ָ��Ŀ�������Buff
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
        /// �Ƴ�ָ����Buff
        /// </summary>
        public BuffManager RemoveBuff(Buff buff)
        {
            if (buff == null)
                return this;

            switch (buff.BuffData.BuffRemoveStrategy)
            {
                case BuffRemoveType.All:
                    // ��ȫ�Ƴ�Buff
                    InternalRemoveBuff(buff);
                    break;

                case BuffRemoveType.OneStack:
                    // ����һ��
                    ReduceStackFromBuff(buff);
                    break;

                case BuffRemoveType.Manual:
                    break;
            }

            return this;
        }

        /// <summary>
        /// �ӹ����б����Ƴ�Buff
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

        #region ɾ������
        /// <summary>
        /// �Ƴ�ָ��Ŀ����ָ��ID��Buff
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
        /// �Ƴ�ָ��Ŀ���ϴ����ض���ǩ������Buff
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
        /// �Ƴ�ָ��Ŀ�����ض��㼶������Buff
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

        #region ��ѯ����
        /// <summary>
        /// ���ָ��Ŀ���Ƿ����ض�ID��Buff
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
        /// ��ȡָ��Ŀ���ض�ID��Buff
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
        /// ��ȡָ��Ŀ�������е�Buff
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

        #region ����
        /// <summary>
        /// ��������Buff
        /// </summary>
        public BuffManager Update(float deltaTime)
        {
            _removedBuffs.Clear();

            foreach (var buff in _allBuffs)
            {
                // ���³���ʱ��
                if (buff.DurationTimer > 0)
                {
                    buff.DurationTimer -= deltaTime;
                    if (buff.DurationTimer <= 0)
                    {
                        _removedBuffs.Add(buff);
                        continue;
                    }
                }

                // ���´������
                buff.TriggerTimer -= deltaTime;
                if (buff.TriggerTimer <= 0)
                {
                    buff.TriggerTimer = buff.BuffData.TriggerInterval;
                    buff.OnTrigger?.Invoke(buff);
                    ExecuteBuffModules(buff, BuffCallBackType.OnTick);
                }

                // ִ��Buff���»ص�
                buff.OnUpdate?.Invoke(buff);
                ExecuteBuffModules(buff, BuffCallBackType.OnUpdate);
            }

            // �Ƴ����ڵ�Buff
            foreach (var buff in _removedBuffs)
            {
                InternalRemoveBuff(buff);
            }

            return this;
        }

        #endregion

        #region �㼶����
        /// <summary>
        /// ��Buff���Ӳ㣬������Ƿ񳬹�������
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
        /// ��Buff���ٲ㣬�������0���Ƴ�
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

        #region ģ��ִ��
        /// <summary>
        /// ִ��Buff�϶�Ӧ���͵�����ģ��
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