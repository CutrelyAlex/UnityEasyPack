using System;
using System.Collections.Generic;
using System.Net.Http.Headers;

namespace RPGPack
{
    /// <summary>
    /// ����Buff����ӡ��Ƴ������µȲ�����
    /// ֧��Buff���ӡ����ڼ�⡢��������ȹ��ܡ�
    /// </summary>
    public class BuffManager
    {
        // ÿ��GamePropertyά���Լ���Buff�б�
        private readonly Dictionary<GameProperty, List<IRPGBuff>> _gamePropertyBuffs = new();
        private readonly CombineGamePropertyManager _combineManager;

        /// <summary>
        /// ��Buff��ӵ�GamePropertyʱ����
        /// </summary>
        public event Action<GameProperty, IRPGBuff> BuffAdded;
        /// <summary>
        /// ��Buff����ȫ�Ƴ�ʱ����
        /// </summary>
        public event Action<GameProperty, IRPGBuff> BuffRemoved;
        /// <summary>
        /// ��Buff����ʱ����
        /// </summary>
        public event Action<GameProperty, IRPGBuff> BuffExpired;
        /// <summary>
        /// ��Buff�Ƴ�һ��ʱ���������Կɵ���Buff����δ��ȫ�Ƴ�ʱ��
        /// </summary>
        public event Action<GameProperty, IRPGBuff> BuffStackRemoved;

        public BuffManager(CombineGamePropertyManager combineManager)
        {
            _combineManager = combineManager;
        }

        /// <summary>
        /// ��ָ��GameProperty���Buff
        /// </summary>
        public BuffManager AddBuff(GameProperty property, IRPGBuff buff)
        {
            if (property == null || buff == null) return this;

            if (!_gamePropertyBuffs.TryGetValue(property, out var buffList))
            {
                buffList = new List<IRPGBuff>();
                _gamePropertyBuffs[property] = buffList;
            }

            var newBuff = (buff as Buff)?.Clone() ?? buff;

            // ���Ȱ�Group����ͬ��Buff��GroupΪnullʱ�˻ذ�ID+Source���ң�
            IRPGBuff existing = null;
            if (!string.IsNullOrEmpty(buff.Group))
                existing = buffList.Find(b => b.Group == buff.Group);
            else
                existing = buffList.Find(b => b.BuffID == buff.BuffID && b.Source == buff.Source);

            if (existing != null)
            {
                switch (buff.StackType)
                {
                    case BuffStackType.Stackable:
                        if (existing.CanStack)
                        {
                            int maxStack = existing.MaxStackCount > 0 ? existing.MaxStackCount : int.MaxValue;
                            int newStack = Math.Min(existing.StackCount + buff.StackCount, maxStack);
                            if (newStack > existing.StackCount)
                            {
                                // ���Ӳ���
                                for (int i = 0; i < (newStack - existing.StackCount); i++)
                                    property.AddModifier(buff.Modifier);
                                existing.StackCount = newStack;
                            }
                            // ˢ�³���ʱ��
                            existing.Elapsed = 0;
                        }
                        break;
                    case BuffStackType.Override:
                        if (buff.Priority >= existing.Priority)
                        {
                            // ���ǣ��Ƴ���Modifier�������Modifier
                            for (int i = 0; i < existing.StackCount; i++)
                                property.RemoveModifier(existing.Modifier);
                            buffList.Remove(existing);

                            property.AddModifier(buff.Modifier);
                            buffList.Add(buff);
                        }
                        // �������
                        break;
                    case BuffStackType.IgnoreIfExists:
                        // �Ѵ��������
                        break;
                }
            }
            else
            {
                property.AddModifier(newBuff.Modifier);
                buffList.Add(newBuff);
            }
            BuffAdded?.Invoke(property, buff);
            return this;
        }

        /// <summary>
        /// ��ָ��GameProperty��ȫ�Ƴ�ָ��Buff��ͬ���Ƴ�Modifier��
        /// </summary>
        public BuffManager RemoveBuff(GameProperty property, string buffId, object source = null)
        {
            return RemoveBuffStack(property, buffId, int.MaxValue, source);
        }

        /// <summary>
        /// ����Buff�������٣�����ȫ�Ƴ�
        /// </summary>
        public BuffManager RemoveBuffStack(GameProperty property, string buffId, int layer = 1, object source = null)
        {
            if (property == null || string.IsNullOrEmpty(buffId)) return this;

            if (_gamePropertyBuffs.TryGetValue(property, out var buffList))
            {
                var buff = buffList.Find(b => b.BuffID == buffId && (source == null || b.Source == source));
                if (buff != null)
                {
                    if (buff.CanStack && buff.StackCount > 1)
                    {
                        if (layer >= buff.StackCount)
                        {
                            for (int i = 0; i < buff.StackCount; i++)
                                property.RemoveModifier(buff.Modifier);
                            buffList.Remove(buff);
                            BuffRemoved?.Invoke(property, buff);
                        }
                        else
                        {
                            buff.StackCount -= layer;
                            for (int i = 0; i < layer; i++)
                                property.RemoveModifier(buff.Modifier);
                            BuffStackRemoved?.Invoke(property, buff);
                        }
                    }
                    else
                    {
                        property.RemoveModifier(buff.Modifier);
                        buffList.Remove(buff);
                        BuffRemoved?.Invoke(property, buff);
                    }
                }
            }
            return this;
        }

        /// <summary>
        /// ��ȡָ��GameProperty�ϵ�Buff
        /// </summary>
        public IRPGBuff GetBuff(GameProperty property, string buffId, object source = null)
        {
            if (property == null || string.IsNullOrEmpty(buffId)) return null;
            if (_gamePropertyBuffs.TryGetValue(property, out var buffList))
                return buffList.Find(b => b.BuffID == buffId && (source == null || b.Source == source));
            return null;
        }

        /// <summary>
        /// ��ȡ���������ϵ�����Buff
        /// </summary>
        public IEnumerable<IRPGBuff> GetAllBuffs()
        {
            foreach (var kv in _gamePropertyBuffs)
                foreach (var buff in kv.Value)
                    yield return buff;
        }

        /// <summary>
        /// ��������Buff�ĳ���ʱ�䣬���Ƴ��ѹ��ڵ�Buff��ͬ���Ƴ�Modifier��
        /// </summary>
        public BuffManager Update(float deltaTime)
        {
            var expiredPairs = new List<(GameProperty, IRPGBuff)>();
            foreach (var kv in _gamePropertyBuffs)
            {
                var property = kv.Key;
                var buffList = kv.Value;
                for (int i = buffList.Count - 1; i >= 0; i--)
                {
                    var buff = buffList[i];

                    // ������Buff����
                    if (buff is TriggerableBuff triggerableBuff)
                    {
                        triggerableBuff.TryTrigger();
                    }

                    // ���³���ʱ��
                    if (buff.Duration > 0)
                    {
                        buff.Elapsed += deltaTime;
                        if (buff.IsExpired)
                        {
                            // ÿ��ֻ�Ƴ�һ��
                            if (buff.RemoveOneStackEachDuration && buff.CanStack && buff.StackCount > 1)
                            {
                                RemoveBuffStack(property, buff.BuffID, 1, buff.Source);
                                buff.Elapsed = 0;
                                // ���Ƴ�����Buff����������
                                continue;
                            }
                            // ��ȫ�Ƴ�
                            property.RemoveModifier(buff.Modifier);
                            expiredPairs.Add((property, buff));
                            buffList.RemoveAt(i);
                            BuffRemoved?.Invoke(property, buff);
                        }
                    }
                }
            }
            foreach (var (property, buff) in expiredPairs)
            {
                BuffExpired?.Invoke(property, buff);
            }
            return this;
        }

        /// <summary>
        /// �������Buff��ͬ���Ƴ�����Modifier��
        /// </summary>
        public BuffManager Clear()
        {
            foreach (var kv in _gamePropertyBuffs)
            {
                var property = kv.Key;
                var buffList = kv.Value;
                foreach (var buff in buffList)
                {
                    property.RemoveModifier(buff.Modifier);
                    BuffRemoved?.Invoke(property, buff);
                }
                buffList.Clear();
            }
            _gamePropertyBuffs.Clear();
            return this;
        }
    }
}