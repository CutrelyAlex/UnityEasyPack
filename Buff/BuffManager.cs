using System;
using System.Collections.Generic;

namespace RPGPack
{
    /// <summary>
    /// 管理Buff的添加、移除、更新等操作。
    /// 支持Buff叠加、过期检测、批量管理等功能。
    /// </summary>
    public class BuffManager
    {
        // 每个GameProperty维护自己的Buff列表
        private readonly Dictionary<GameProperty, List<Buff>> _gamePropertyBuffs = new();
        private readonly CombineGamePropertyManager _combineManager;

        /// <summary>
        /// 当Buff添加到GameProperty时触发
        /// </summary>
        public event Action<GameProperty, Buff> BuffAdded;
        /// <summary>
        /// 当Buff被移除时触发
        /// </summary>
        public event Action<GameProperty, Buff> BuffRemoved;
        /// <summary>
        /// 当Buff过期时触发
        /// </summary>
        public event Action<GameProperty, Buff> BuffExpired;
        /// <summary>
        /// 当Buff移除一层时触发（仅对可叠加Buff，且未完全移除时）
        /// </summary>
        public event Action<GameProperty, Buff> BuffStackRemoved;

        public BuffManager(CombineGamePropertyManager combineManager)
        {
            _combineManager = combineManager;
        }

        /// <summary>
        /// 给指定GameProperty添加Buff
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
                    // 叠加层数，受最大层数限制
                    int newStack = Math.Min(existing.StackCount + 1, existing.MaxStackCount > 0 ? existing.MaxStackCount : int.MaxValue);
                    if (newStack > existing.StackCount)
                    {
                        existing.StackCount = newStack;
                        property.AddModifier(buff.Modifier); // 叠加时添加Modifier
                    }
                    // 重置持续时间
                    existing.Elapsed = 0;
                }
                else
                {
                    // 覆盖：先移除旧Modifier再添加新Modifier
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
        /// 从指定GameProperty完全移除指定Buff（同步移除Modifier）
        /// </summary>
        public void RemoveBuff(GameProperty property, string buffId, object source = null)
        {
            RemoveBuffStack(property, buffId, int.MaxValue, source);
        }

        /// <summary>
        /// 叠加Buff层数减少，或完全移除
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
        /// 获取指定GameProperty上的Buff
        /// </summary>
        public Buff GetBuff(GameProperty property, string buffId, object source = null)
        {
            if (property == null || string.IsNullOrEmpty(buffId)) return null;
            if (_gamePropertyBuffs.TryGetValue(property, out var buffList))
                return buffList.Find(b => b.BuffID == buffId && (source == null || b.Source == source));
            return null;
        }

        /// <summary>
        /// 获取所有属性上的所有Buff
        /// </summary>
        public IEnumerable<Buff> GetAllBuffs()
        {
            foreach (var kv in _gamePropertyBuffs)
                foreach (var buff in kv.Value)
                    yield return buff;
        }

        /// <summary>
        /// 更新所有Buff的持续时间，并移除已过期的Buff（同步移除Modifier）
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
                            // 每次只移除一层
                            if (buff.RemoveOneStackEachDuration && buff.CanStack && buff.StackCount > 1)
                            {
                                RemoveBuffStack(property, buff.BuffID, 1, buff.Source);
                                buff.Elapsed = 0;
                                // 不移除整个Buff，继续保留
                                continue;
                            }
                            // 完全移除
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
        /// 清空所有Buff（同步移除所有Modifier）
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