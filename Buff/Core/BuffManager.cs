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

        // �����г���ʱ���Buff������Buff
        private readonly List<Buff> _timedBuffs = new List<Buff>();
        private readonly List<Buff> _permanentBuffs = new List<Buff>();

        // ������Ҫ������Buff
        private readonly List<Buff> _triggeredBuffs = new List<Buff>();

        // ģ��ִ�л���
        private readonly List<BuffModule> _moduleCache = new List<BuffModule>();

        // ���ٲ�������
        private readonly Dictionary<string, List<Buff>> _buffsByID = new Dictionary<string, List<Buff>>();
        private readonly Dictionary<string, List<Buff>> _buffsByTag = new Dictionary<string, List<Buff>>();
        private readonly Dictionary<string, List<Buff>> _buffsByLayer = new Dictionary<string, List<Buff>>();

        // Buffλ������
        private readonly Dictionary<Buff, int> _buffPositions = new Dictionary<Buff, int>();
        private readonly Dictionary<Buff, int> _timedBuffPositions = new Dictionary<Buff, int>();
        private readonly Dictionary<Buff, int> _permanentBuffPositions = new Dictionary<Buff, int>();
        // �����Ƴ�����
        private readonly HashSet<Buff> _buffsToRemove = new HashSet<Buff>();
        private readonly List<int> _removalIndices = new List<int>();


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

                _buffPositions[buff] = _allBuffs.Count;
                _allBuffs.Add(buff);

                // ���ݳ���ʱ�����
                if (buff.DurationTimer > 0)
                {
                    _timedBuffs.Add(buff);
                }
                else
                {
                    _permanentBuffs.Add(buff);
                }

                // ��ӵ�����
                AddBuffToIndexes(buff);

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
        /// ��Buff��ӵ�����������
        /// </summary>
        private void AddBuffToIndexes(Buff buff)
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

        /// <summary>
        /// �Ƴ�ָ��Ŀ�������Buff
        /// </summary>
        public BuffManager RemoveAllBuffs(object target)
        {
            if (_targetToBuffs.TryGetValue(target, out List<Buff> buffs))
            {
                // �����Ƴ��Ż�
                foreach (var buff in buffs)
                {
                    _buffsToRemove.Add(buff);
                }

                // ִ�������Ƴ�
                BatchRemoveBuffs();
            }
            return this;
        }

        /// <summary>
        /// �Ӹ����������Ƴ�Buff
        /// </summary>
        private void RemoveBuffFromIndexes(Buff buff)
        {
            // ��ID�������Ƴ�
            if (_buffsByID.TryGetValue(buff.BuffData.ID, out var idList))
            {
                FastRemoveFromList(idList, buff);
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
                        FastRemoveFromList(tagList, buff);
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
        /// �����Ƴ�Buff
        /// </summary>
        private void BatchRemoveBuffs()
        {
            if (_buffsToRemove.Count == 0)
                return;

            // ִ���Ƴ��ص�
            foreach (var buff in _buffsToRemove)
            {
                buff.OnRemove?.Invoke(buff);
                ExecuteBuffModules(buff, BuffCallBackType.OnRemove);
            }

            // ���������б��Ƴ�
            BatchRemoveFromList(_allBuffs, _buffPositions, _buffsToRemove);

            // ������ʱ���б��Ƴ�
            BatchRemoveFromList(_timedBuffs, _timedBuffPositions, _buffsToRemove);

            // �����������б��Ƴ�
            BatchRemoveFromList(_permanentBuffs, _permanentBuffPositions, _buffsToRemove);

            // ��Ŀ�������Ƴ�
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

            // �ӿ��������Ƴ�
            foreach (var buff in _buffsToRemove)
            {
                RemoveBuffFromIndexes(buff);
            }

            _buffsToRemove.Clear();
        }


        /// <summary>
        /// �������б����Ƴ�Ԫ��
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

            // �����������Ӹߵ����Ƴ�
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
                        // �����һ��Ԫ���Ƶ���ǰλ��
                        Buff lastBuff = list[lastIndex];
                        list[index] = lastBuff;
                        positions[lastBuff] = index;
                    }

                    // �Ƴ����һ��Ԫ��
                    list.RemoveAt(lastIndex);
                    positions.Remove(removedBuff);
                }
            }
        }

        /// <summary>
        /// ���ٴ��б����Ƴ�Ԫ��
        /// </summary>
        private void FastRemoveFromList(List<Buff> list, Buff item)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == item)
                {
                    // �����һ��Ԫ���Ƶ���ǰλ�ã�Ȼ���Ƴ����һ��Ԫ��
                    list[i] = list[list.Count - 1];
                    list.RemoveAt(list.Count - 1);
                    break;
                }
            }
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

            _buffsToRemove.Add(buff);
            BatchRemoveBuffs();

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
                    _buffsToRemove.Add(buff);
                    BatchRemoveBuffs();
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
                foreach (var buff in buffs.Where(b => b.BuffData.HasTag(tag)))
                {
                    _buffsToRemove.Add(buff);
                }
                BatchRemoveBuffs();
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
                foreach (var buff in buffs.Where(b => b.BuffData.InLayer(layer)))
                {
                    _buffsToRemove.Add(buff);
                }
                BatchRemoveBuffs();
            }
            return this;
        }


        /// <summary>
        /// �Ƴ�����ָ��ID��Buff������Ŀ�꣩
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
        /// �Ƴ����д����ض���ǩ��Buff������Ŀ�꣩
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
        /// �Ƴ������ض��㼶��Buff������Ŀ�꣩
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
        /// ����ִ�����д��Ƴ���Buff
        /// </summary>
        public BuffManager FlushPendingRemovals()
        {
            BatchRemoveBuffs();
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

        /// <summary>
        /// ��ȡָ��Ŀ���ϴ����ض���ǩ������Buff
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
        /// ��ȡָ��Ŀ�����ض��㼶������Buff
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
        /// ��ȡ����ָ��ID��Buff������Ŀ�꣩
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
        /// ��ȡ���д����ض���ǩ��Buff������Ŀ�꣩
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
        /// ��ȡ�����ض��㼶��Buff������Ŀ�꣩
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
        /// ����Ƿ����ָ��ID��Buff���κ�Ŀ�꣩
        /// </summary>
        public bool HasBuffByID(string buffID)
        {
            return _buffsByID.ContainsKey(buffID) && _buffsByID[buffID].Count > 0;
        }

        /// <summary>
        /// ����Ƿ���ڴ����ض���ǩ��Buff���κ�Ŀ�꣩
        /// </summary>
        public bool HasBuffByTag(string tag)
        {
            return _buffsByTag.ContainsKey(tag) && _buffsByTag[tag].Count > 0;
        }

        /// <summary>
        /// ����Ƿ�����ض��㼶��Buff���κ�Ŀ�꣩
        /// </summary>
        public bool HasBuffByLayer(string layer)
        {
            return _buffsByLayer.ContainsKey(layer) && _buffsByLayer[layer].Count > 0;
        }


        #endregion

        #region ����
        // <summary>
        /// ��������Buff
        /// </summary>
        public BuffManager Update(float deltaTime)
        {
            _removedBuffs.Clear();
            _triggeredBuffs.Clear();

            // ֻ������ʱ�����Ƶ�Buff
            UpdateTimedBuffs(deltaTime);

            // �ֱ�������Buff�Ĵ���
            UpdatePermanentBuffs(deltaTime);

            // ��������Buff
            BatchTriggerBuffs();

            // ����ִ�и��»ص�
            BatchUpdateBuffs();

            // �Ƴ����ڵ�Buff
            foreach (var buff in _removedBuffs)
            {
                InternalRemoveBuff(buff);
            }

            return this;
        }

        /// <summary>
        /// ������ʱ�����Ƶ�Buff
        /// </summary>
        private void UpdateTimedBuffs(float deltaTime)
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

        /// <summary>
        /// ��������Buff
        /// </summary>
        private void UpdatePermanentBuffs(float deltaTime)
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

        /// <summary>
        /// ��������Buff
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
        /// ����ִ�и��»ص�
        /// </summary>
        private void BatchUpdateBuffs()
        {
            // ������ʱ�����Ƶ�Buff
            foreach (var buff in _timedBuffs)
            {
                if (!_removedBuffs.Contains(buff))
                {
                    buff.OnUpdate?.Invoke(buff);
                    ExecuteBuffModules(buff, BuffCallBackType.OnUpdate);
                }
            }

            // ��������Buff
            foreach (var buff in _permanentBuffs)
            {
                buff.OnUpdate?.Invoke(buff);
                ExecuteBuffModules(buff, BuffCallBackType.OnUpdate);
            }
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
            if (buff.BuffData.BuffModules == null || buff.BuffData.BuffModules.Count == 0)
                return;

            // ʹ�û����б�
            _moduleCache.Clear();

            var modules = buff.BuffData.BuffModules;
            for (int i = 0; i < modules.Count; i++)
            {
                if (modules[i].ShouldExecute(callBackType, customCallbackName))
                {
                    _moduleCache.Add(modules[i]);
                }
            }

            // ����
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