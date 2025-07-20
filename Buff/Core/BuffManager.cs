using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EasyPack
{
    /// <summary>
    /// Buff�������ڹ�����������Buff�Ĵ��������¡��Ƴ��Ͳ�ѯ
    /// ��ֱ�Ӳ���IProperty��Buff��Ӧ�����Ƴ���BuffHandle����
    /// </summary>
    public class BuffManager
    {
        #region �������ݽṹ

        // ��Ҫ�洢�ṹ
        private readonly Dictionary<object, List<Buff>> _targetToBuffs = new Dictionary<object, List<Buff>>();
        private readonly List<Buff> _allBuffs = new List<Buff>();
        private readonly List<Buff> _removedBuffs = new List<Buff>();

        // ���������ڷ����Buff�б�
        private readonly List<Buff> _timedBuffs = new List<Buff>();
        private readonly List<Buff> _permanentBuffs = new List<Buff>();

        // ����ѭ������
        private readonly List<Buff> _triggeredBuffs = new List<Buff>();
        private readonly List<BuffModule> _moduleCache = new List<BuffModule>();

        // ���ٲ�������
        private readonly Dictionary<string, List<Buff>> _buffsByID = new Dictionary<string, List<Buff>>();
        private readonly Dictionary<string, List<Buff>> _buffsByTag = new Dictionary<string, List<Buff>>();
        private readonly Dictionary<string, List<Buff>> _buffsByLayer = new Dictionary<string, List<Buff>>();

        // λ���������ڿ����Ƴ�
        private readonly Dictionary<Buff, int> _buffPositions = new Dictionary<Buff, int>();
        private readonly Dictionary<Buff, int> _timedBuffPositions = new Dictionary<Buff, int>();
        private readonly Dictionary<Buff, int> _permanentBuffPositions = new Dictionary<Buff, int>();

        // �����Ƴ��Ż�
        private readonly HashSet<Buff> _buffsToRemove = new HashSet<Buff>();
        private readonly List<int> _removalIndices = new List<int>();

        #endregion

        #region Buff���������

        /// <summary>
        /// ����������µ�Buff�������ظ�ID�ĵ��Ӳ���
        /// </summary>
        public Buff CreateBuff(BuffData buffData, GameObject creator, GameObject target)
        {
            if (buffData == null)
                return null;

            if (!_targetToBuffs.TryGetValue(target, out List<Buff> buffs))
            {
                buffs = new List<Buff>();
                _targetToBuffs[target] = buffs;
            }

            // ����Ƿ������ͬID��Buff����������߼�
            Buff existingBuff = buffs.FirstOrDefault(b => b.BuffData.ID == buffData.ID);
            if (existingBuff != null)
            {
                // �������ʱ����Ӳ���
                switch (buffData.BuffSuperpositionStrategy)
                {
                    case BuffSuperpositionDurationType.Add:
                        existingBuff.DurationTimer += buffData.Duration;
                        break;
                    case BuffSuperpositionDurationType.ResetThenAdd:
                        existingBuff.DurationTimer = 2 * buffData.Duration;
                        break;
                    case BuffSuperpositionDurationType.Reset:
                        existingBuff.DurationTimer = buffData.Duration;
                        break;
                    case BuffSuperpositionDurationType.Keep:
                        break;
                }

                // ����ѵ������Ӳ���
                switch (buffData.BuffSuperpositionStacksStrategy)
                {
                    case BuffSuperpositionStacksType.Add:
                        IncreaseBuffStacks(existingBuff);
                        break;
                    case BuffSuperpositionStacksType.ResetThenAdd:
                        existingBuff.CurrentStacks = 1;
                        IncreaseBuffStacks(existingBuff);
                        break;
                    case BuffSuperpositionStacksType.Reset:
                        existingBuff.CurrentStacks = 1;
                        break;
                    case BuffSuperpositionStacksType.Keep:
                        break;
                }

                return existingBuff;
            }
            else
            {
                // ����ȫ�µ�Buffʵ��
                Buff buff = new()
                {
                    BuffData = buffData,
                    Creator = creator,
                    Target = target,
                    DurationTimer = buffData.Duration > 0 ? buffData.Duration : -1f,
                    TriggerTimer = buffData.TriggerInterval,
                    CurrentStacks = 1
                };

                // ��ӵ����ֹ����б������
                buffs.Add(buff);
                _buffPositions[buff] = _allBuffs.Count;
                _allBuffs.Add(buff);

                // ���ݳ���ʱ�����洢
                if (buff.DurationTimer > 0)
                {
                    _timedBuffs.Add(buff);
                }
                else
                {
                    _permanentBuffs.Add(buff);
                }

                RegisterBuffInIndexes(buff);

                // ִ�д����ص�
                buff.OnCreate?.Invoke(buff);
                InvokeBuffModules(buff, BuffCallBackType.OnCreate);

                // ������ʱ��������
                if (buffData.TriggerOnCreate)
                {
                    buff.OnTrigger?.Invoke(buff);
                    InvokeBuffModules(buff, BuffCallBackType.OnTick);
                }

                return buff;
            }
        }

        /// <summary>
        /// ��Buff��ӵ����ٲ���������
        /// </summary>
        private void RegisterBuffInIndexes(Buff buff)
        {
            // ��ӵ�ID����
            if (!_buffsByID.TryGetValue(buff.BuffData.ID, out var idList))
            {
                idList = new List<Buff>();
                _buffsByID[buff.BuffData.ID] = idList;
            }
            idList.Add(buff);

            // ��ӵ���ǩ����
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

            // ��ӵ��㼶����
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

        #endregion

        #region �ѵ�����

        /// <summary>
        /// ����Buff�ѵ��������������ֵ
        /// </summary>
        private BuffManager IncreaseBuffStacks(Buff buff, int stack = 1)
        {
            if (buff == null || buff.CurrentStacks >= buff.BuffData.MaxStacks)
                return this;

            buff.CurrentStacks += stack;
            buff.CurrentStacks = Mathf.Min(buff.CurrentStacks, buff.BuffData.MaxStacks);

            buff.OnAddStack?.Invoke(buff);
            InvokeBuffModules(buff, BuffCallBackType.OnAddStack);

            return this;
        }

        /// <summary>
        /// ����Buff�ѵ�����Ϊ0ʱ�Ƴ�Buff
        /// </summary>
        private BuffManager DecreaseBuffStacks(Buff buff, int stack = 1)
        {
            if (buff == null || buff.CurrentStacks <= 1)
            {
                QueueBuffForRemoval(buff);
                return this;
            }

            buff.CurrentStacks -= stack;
            if (buff.CurrentStacks <= 0)
            {
                QueueBuffForRemoval(buff);
                return this;
            }

            buff.OnReduceStack?.Invoke(buff);
            InvokeBuffModules(buff, BuffCallBackType.OnReduceStack);
            return this;
        }

        #endregion

        #region ����Buff�Ƴ�

        public BuffManager RemoveBuff(Buff buff)
        {
            if (buff == null)
                return this;

            switch (buff.BuffData.BuffRemoveStrategy)
            {
                case BuffRemoveType.All:
                    QueueBuffForRemoval(buff);
                    break;
                case BuffRemoveType.OneStack:
                    DecreaseBuffStacks(buff);
                    break;
                case BuffRemoveType.Manual:
                    break;
            }

            return this;
        }

        private BuffManager QueueBuffForRemoval(Buff buff)
        {
            if (buff == null)
                return this;

            _buffsToRemove.Add(buff);
            ProcessBuffRemovals();

            return this;
        }

        #endregion

        #region Ŀ������Ƴ�����

        public BuffManager RemoveAllBuffs(object target)
        {
            if (_targetToBuffs.TryGetValue(target, out List<Buff> buffs))
            {
                foreach (var buff in buffs)
                {
                    _buffsToRemove.Add(buff);
                }
                ProcessBuffRemovals();
            }
            return this;
        }

        public BuffManager RemoveBuffByID(object target, string buffID)
        {
            if (_targetToBuffs.TryGetValue(target, out List<Buff> buffs))
            {
                Buff buff = buffs.FirstOrDefault(b => b.BuffData.ID == buffID);
                if (buff != null)
                {
                    _buffsToRemove.Add(buff);
                    ProcessBuffRemovals();
                }
            }
            return this;
        }

        public BuffManager RemoveBuffsByTag(object target, string tag)
        {
            if (_targetToBuffs.TryGetValue(target, out List<Buff> buffs))
            {
                foreach (var buff in buffs.Where(b => b.BuffData.HasTag(tag)))
                {
                    _buffsToRemove.Add(buff);
                }
                ProcessBuffRemovals();
            }
            return this;
        }

        public BuffManager RemoveBuffsByLayer(object target, string layer)
        {
            if (_targetToBuffs.TryGetValue(target, out List<Buff> buffs))
            {
                foreach (var buff in buffs.Where(b => b.BuffData.InLayer(layer)))
                {
                    _buffsToRemove.Add(buff);
                }
                ProcessBuffRemovals();
            }
            return this;
        }

        #endregion

        #region ȫ���Ƴ�����

        public BuffManager RemoveAllBuffsByID(string buffID)
        {
            if (_buffsByID.TryGetValue(buffID, out var buffs))
            {
                foreach (var buff in buffs)
                {
                    _buffsToRemove.Add(buff);
                }
                ProcessBuffRemovals();
            }
            return this;
        }

        public BuffManager RemoveAllBuffsByTag(string tag)
        {
            if (_buffsByTag.TryGetValue(tag, out var buffs))
            {
                foreach (var buff in buffs)
                {
                    _buffsToRemove.Add(buff);
                }
                ProcessBuffRemovals();
            }
            return this;
        }

        public BuffManager RemoveAllBuffsByLayer(string layer)
        {
            if (_buffsByLayer.TryGetValue(layer, out var buffs))
            {
                foreach (var buff in buffs)
                {
                    _buffsToRemove.Add(buff);
                }
                ProcessBuffRemovals();
            }
            return this;
        }

        public BuffManager FlushPendingRemovals()
        {
            ProcessBuffRemovals();
            return this;
        }

        #endregion

        #region �����Ƴ�����ʵ��

        /// <summary>
        /// �����Ƴ�Buff�ĺ���ʵ�֣�����ص�����������
        /// </summary>
        private void ProcessBuffRemovals()
        {
            if (_buffsToRemove.Count == 0)
                return;

            // ִ���Ƴ��ص�
            foreach (var buff in _buffsToRemove)
            {
                buff.OnRemove?.Invoke(buff);
                InvokeBuffModules(buff, BuffCallBackType.OnRemove);
            }

            // �����Ӹ����б��Ƴ�
            BatchRemoveFromList(_allBuffs, _buffPositions, _buffsToRemove);
            BatchRemoveFromList(_timedBuffs, _timedBuffPositions, _buffsToRemove);
            BatchRemoveFromList(_permanentBuffs, _permanentBuffPositions, _buffsToRemove);

            // ��Ŀ�������Ƴ�
            var targetGroups = _buffsToRemove.GroupBy(b => b.Target);
            foreach (var group in targetGroups)
            {
                if (_targetToBuffs.TryGetValue(group.Key, out List<Buff> targetBuffs))
                {
                    foreach (var buff in group)
                    {
                        SwapRemoveFromList(targetBuffs, buff);
                    }

                    if (targetBuffs.Count == 0)
                    {
                        _targetToBuffs.Remove(group.Key);
                    }
                }
            }

            // �ӿ��ٲ��������Ƴ�
            foreach (var buff in _buffsToRemove)
            {
                UnregisterBuffFromIndexes(buff);
            }

            _buffsToRemove.Clear();
        }

        private void UnregisterBuffFromIndexes(Buff buff)
        {
            // ��ID�������Ƴ�
            if (_buffsByID.TryGetValue(buff.BuffData.ID, out var idList))
            {
                SwapRemoveFromList(idList, buff);
                if (idList.Count == 0)
                {
                    _buffsByID.Remove(buff.BuffData.ID);
                }
            }

            // �ӱ�ǩ�������Ƴ�
            if (buff.BuffData.Tags != null)
            {
                foreach (var tag in buff.BuffData.Tags)
                {
                    if (_buffsByTag.TryGetValue(tag, out var tagList))
                    {
                        SwapRemoveFromList(tagList, buff);
                        if (tagList.Count == 0)
                        {
                            _buffsByTag.Remove(tag);
                        }
                    }
                }
            }

            // �Ӳ㼶�������Ƴ�
            if (buff.BuffData.Layers != null)
            {
                foreach (var layer in buff.BuffData.Layers)
                {
                    if (_buffsByLayer.TryGetValue(layer, out var layerList))
                    {
                        SwapRemoveFromList(layerList, buff);
                        if (layerList.Count == 0)
                        {
                            _buffsByLayer.Remove(layer);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// �����Ӵ�λ���������б����Ƴ�Ԫ�أ�ʹ��O(1)��swap-remove�Ż�
        /// </summary>
        private void BatchRemoveFromList(List<Buff> list, Dictionary<Buff, int> positions, HashSet<Buff> itemsToRemove)
        {
            if (list.Count == 0 || itemsToRemove.Count == 0)
                return;

            _removalIndices.Clear();

            // �ռ���Ҫ�Ƴ�������
            foreach (var item in itemsToRemove)
            {
                if (positions.TryGetValue(item, out int index))
                {
                    _removalIndices.Add(index);
                }
            }

            if (_removalIndices.Count == 0)
                return;

            // �Ӹߵ������������������Ƴ�ʱ�����仯
            _removalIndices.Sort((a, b) => b.CompareTo(a));

            // �����Ƴ�
            foreach (int index in _removalIndices)
            {
                if (index < list.Count)
                {
                    Buff removedBuff = list[index];
                    int lastIndex = list.Count - 1;

                    if (index != lastIndex)
                    {
                        // swap-remove�Ż��������Ԫ���滻��ǰԪ��
                        Buff lastBuff = list[lastIndex];
                        list[index] = lastBuff;
                        positions[lastBuff] = index;
                    }

                    list.RemoveAt(lastIndex);
                    positions.Remove(removedBuff);
                }
            }
        }

        /// <summary>
        /// ���ٴ���λ���������б����Ƴ�Ԫ��
        /// </summary>
        private void SwapRemoveFromList(List<Buff> list, Buff item)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == item)
                {
                    // swap-remove�Ż�
                    list[i] = list[list.Count - 1];
                    list.RemoveAt(list.Count - 1);
                    break;
                }
            }
        }

        #endregion

        #region Ŀ���ѯ����

        public bool ContainsBuff(object target, string buffID)
        {
            if (target == null || string.IsNullOrEmpty(buffID))
                return false;
            if (_targetToBuffs.TryGetValue(target, out List<Buff> buffs))
            {
                return buffs.Any(b => b.BuffData.ID == buffID);
            }
            return false;
        }

        public Buff GetBuff(object target, string buffID)
        {
            if (target == null || string.IsNullOrEmpty(buffID))
                return null;
            if (_targetToBuffs.TryGetValue(target, out List<Buff> buffs))
            {
                return buffs.FirstOrDefault(b => b.BuffData.ID == buffID);
            }
            return null;
        }

        public List<Buff> GetTargetBuffs(object target)
        {
            if (target == null)
                return new List<Buff>();

            if (_targetToBuffs.TryGetValue(target, out List<Buff> buffs))
            {
                return new List<Buff>(buffs);
            }
            return new List<Buff>();
        }

        public List<Buff> GetBuffsByTag(object target, string tag)
        {
            if (target == null || string.IsNullOrEmpty(tag))
                return new List<Buff>();

            if (_targetToBuffs.TryGetValue(target, out List<Buff> buffs))
            {
                return buffs.Where(b => b.BuffData.HasTag(tag)).ToList();
            }
            return new List<Buff>();
        }

        public List<Buff> GetBuffsByLayer(object target, string layer)
        {
            if (target == null || string.IsNullOrEmpty(layer))
                return new List<Buff>();

            if (_targetToBuffs.TryGetValue(target, out List<Buff> buffs))
            {
                return buffs.Where(b => b.BuffData.InLayer(layer)).ToList();
            }
            return new List<Buff>();
        }

        #endregion

        #region ȫ�ֲ�ѯ����

        public List<Buff> GetAllBuffsByID(string buffID)
        {
            if (_buffsByID.TryGetValue(buffID, out var buffs))
            {
                return new List<Buff>(buffs);
            }
            return new List<Buff>();
        }

        public List<Buff> GetAllBuffsByTag(string tag)
        {
            if (_buffsByTag.TryGetValue(tag, out var buffs))
            {
                return new List<Buff>(buffs);
            }
            return new List<Buff>();
        }

        public List<Buff> GetAllBuffsByLayer(string layer)
        {
            if (_buffsByLayer.TryGetValue(layer, out var buffs))
            {
                return new List<Buff>(buffs);
            }
            return new List<Buff>();
        }

        public bool ContainsBuffWithID(string buffID)
        {
            return _buffsByID.ContainsKey(buffID) && _buffsByID[buffID].Count > 0;
        }

        public bool ContainsBuffWithTag(string tag)
        {
            return _buffsByTag.ContainsKey(tag) && _buffsByTag[tag].Count > 0;
        }

        public bool ContainsBuffWithLayer(string layer)
        {
            return _buffsByLayer.ContainsKey(layer) && _buffsByLayer[layer].Count > 0;
        }

        #endregion

        #region ����ѭ��

        /// <summary>
        /// ������ѭ��������ʱ��Buff������Buff��ʱ������봥��
        /// </summary>
        public BuffManager Update(float deltaTime)
        {
            _removedBuffs.Clear();
            _triggeredBuffs.Clear();

            // ������ʱ�����Ƶ�Buff
            ProcessTimedBuffs(deltaTime);

            // ��������Buff�Ĵ���ʱ��
            ProcessPermanentBuffs(deltaTime);

            // ����ִ�д����͸��»ص�
            ExecuteTriggeredBuffs();
            ExecuteBuffUpdates();

            // �Ƴ����ڵ�Buff
            foreach (var buff in _removedBuffs)
            {
                QueueBuffForRemoval(buff);
            }

            return this;
        }

        private void ProcessTimedBuffs(float deltaTime)
        {
            for (int i = _timedBuffs.Count - 1; i >= 0; i--)
            {
                var buff = _timedBuffs[i];

                // ���³���ʱ��
                buff.DurationTimer -= deltaTime;
                if (buff.DurationTimer <= 0)
                {
                    _removedBuffs.Add(buff);
                    continue;
                }

                // ��鴥�����
                buff.TriggerTimer -= deltaTime;
                if (buff.TriggerTimer <= 0)
                {
                    buff.TriggerTimer = buff.BuffData.TriggerInterval;
                    _triggeredBuffs.Add(buff);
                }
            }
        }

        private void ProcessPermanentBuffs(float deltaTime)
        {
            for (int i = 0; i < _permanentBuffs.Count; i++)
            {
                var buff = _permanentBuffs[i];

                // ��鴥�����
                buff.TriggerTimer -= deltaTime;
                if (buff.TriggerTimer <= 0)
                {
                    buff.TriggerTimer = buff.BuffData.TriggerInterval;
                    _triggeredBuffs.Add(buff);
                }
            }
        }

        private void ExecuteTriggeredBuffs()
        {
            foreach (var buff in _triggeredBuffs)
            {
                buff.OnTrigger?.Invoke(buff);
                InvokeBuffModules(buff, BuffCallBackType.OnTick);
            }
        }

        private void ExecuteBuffUpdates()
        {
            // ������ʱ�����Ƶ�Buff
            foreach (var buff in _timedBuffs)
            {
                if (!_removedBuffs.Contains(buff))
                {
                    buff.OnUpdate?.Invoke(buff);
                    InvokeBuffModules(buff, BuffCallBackType.OnUpdate);
                }
            }

            // ��������Buff
            foreach (var buff in _permanentBuffs)
            {
                buff.OnUpdate?.Invoke(buff);
                InvokeBuffModules(buff, BuffCallBackType.OnUpdate);
            }
        }

        #endregion

        #region ģ��ִ��ϵͳ

        /// <summary>
        /// ִ��Buffģ�飬֧�����ȼ����������ɸѡ
        /// </summary>
        private void InvokeBuffModules(Buff buff, BuffCallBackType callBackType, string customCallbackName = "", params object[] parameters)
        {
            if (buff.BuffData.BuffModules == null || buff.BuffData.BuffModules.Count == 0)
                return;

            // ɸѡ��Ҫִ�е�ģ��
            _moduleCache.Clear();
            var modules = buff.BuffData.BuffModules;
            for (int i = 0; i < modules.Count; i++)
            {
                if (modules[i].ShouldExecute(callBackType, customCallbackName))
                {
                    _moduleCache.Add(modules[i]);
                }
            }

            // �����ȼ���������
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

            // ִ��ģ��
            for (int i = 0; i < _moduleCache.Count; i++)
            {
                _moduleCache[i].Execute(buff, callBackType, customCallbackName, parameters);
            }
        }

        #endregion
    }
}