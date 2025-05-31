using System;
using System.Collections.Generic;

namespace RPGPack
{
    /// <summary>
    /// ����Buff����ӡ��Ƴ������µȲ�����
    /// ֧��Buff���ӡ����ڼ�⡢��������ȹ��ܡ�
    /// </summary>
    public class BuffManager
    {
        // ÿ��GamePropertyά���Լ���Buff�б�
        private readonly Dictionary<GameProperty, List<Buff>> _gamePropertyBuffs = new();
        private readonly CombineGamePropertyManager _combineManager;

        /// <summary>
        /// ��Buff��ӵ�GamePropertyʱ����
        /// </summary>
        public event Action<GameProperty, Buff> BuffAdded;
        /// <summary>
        /// ��Buff���Ƴ�ʱ����
        /// </summary>
        public event Action<GameProperty, Buff> BuffRemoved;
        /// <summary>
        /// ��Buff����ʱ����
        /// </summary>
        public event Action<GameProperty, Buff> BuffExpired;
        /// <summary>
        /// ��Buff�Ƴ�һ��ʱ���������Կɵ���Buff����δ��ȫ�Ƴ�ʱ��
        /// </summary>
        public event Action<GameProperty, Buff> BuffStackRemoved;

        public BuffManager(CombineGamePropertyManager combineManager)
        {
            _combineManager = combineManager;
        }

        /// <summary>
        /// ��ָ��GameProperty���Buff
        /// </summary>
        public void AddBuff(GameProperty property, Buff buff)
        {
            if (property == null || buff == null) return;

            if (!_gamePropertyBuffs.TryGetValue(property, out var buffList))
            {
                buffList = new List<Buff>();
                _gamePropertyBuffs[property] = buffList;
            }

            var existing = buffList.Find(b => b.BuffID == buff.BuffID && b.Source == buff.Source);
            if (existing != null)
            {
                if (existing.CanStack)
                {
                    // ���Ӳ�����������������
                    int newStack = Math.Min(existing.StackCount + 1, existing.MaxStackCount > 0 ? existing.MaxStackCount : int.MaxValue);
                    if (newStack > existing.StackCount)
                    {
                        existing.StackCount = newStack;
                        property.AddModifier(buff.Modifier); // ����ʱ���Modifier
                    }
                    // ���ó���ʱ��
                    existing.Elapsed = 0;
                }
                else
                {
                    // ���ǣ����Ƴ���Modifier�������Modifier
                    property.RemoveModifier(existing.Modifier);
                    property.AddModifier(buff.Modifier);
                    buffList.Remove(existing);
                    buffList.Add(buff);
                }
            }
            else
            {
                property.AddModifier(buff.Modifier);
                buffList.Add(buff);
            }
            BuffAdded?.Invoke(property, buff);
        }

        /// <summary>
        /// ��ָ��GameProperty��ȫ�Ƴ�ָ��Buff��ͬ���Ƴ�Modifier��
        /// </summary>
        public void RemoveBuff(GameProperty property, string buffId, object source = null)
        {
            RemoveBuffStack(property, buffId, int.MaxValue, source);
        }

        /// <summary>
        /// ����Buff�������٣�����ȫ�Ƴ�
        /// </summary>
        public void RemoveBuffStack(GameProperty property, string buffId, int layer = 1, object source = null)
        {
            if (property == null || string.IsNullOrEmpty(buffId)) return;

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
        }

        /// <summary>
        /// ��ȡָ��GameProperty�ϵ�Buff
        /// </summary>
        public Buff GetBuff(GameProperty property, string buffId, object source = null)
        {
            if (property == null || string.IsNullOrEmpty(buffId)) return null;
            if (_gamePropertyBuffs.TryGetValue(property, out var buffList))
                return buffList.Find(b => b.BuffID == buffId && (source == null || b.Source == source));
            return null;
        }

        /// <summary>
        /// ��ȡ���������ϵ�����Buff
        /// </summary>
        public IEnumerable<Buff> GetAllBuffs()
        {
            foreach (var kv in _gamePropertyBuffs)
                foreach (var buff in kv.Value)
                    yield return buff;
        }

        /// <summary>
        /// ��������Buff�ĳ���ʱ�䣬���Ƴ��ѹ��ڵ�Buff��ͬ���Ƴ�Modifier��
        /// </summary>
        public void Update(float deltaTime)
        {
            var expiredPairs = new List<(GameProperty, Buff)>();
            foreach (var kv in _gamePropertyBuffs)
            {
                var property = kv.Key;
                var buffList = kv.Value;
                for (int i = buffList.Count - 1; i >= 0; i--)
                {
                    var buff = buffList[i];
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
                        }
                    }
                }
            }
            foreach (var (property, buff) in expiredPairs)
            {
                BuffExpired?.Invoke(property, buff);
            }
        }

        /// <summary>
        /// �������Buff��ͬ���Ƴ�����Modifier��
        /// </summary>
        public void Clear()
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
        }
    }
}