using System;
using System.Collections.Generic;
using System.Net.Http.Headers;

namespace RPGPack
{
    /// <summary>
    /// 管理Buff的添加、移除、更新等操作。
    /// 支持Buff叠加、过期检测、批量管理等功能。
    /// </summary>
    public class BuffManager
    {
        // 每个GameProperty维护自己的Buff列表
        private readonly Dictionary<GameProperty, List<IRPGBuff>> _gamePropertyBuffs = new();
        private readonly CombineGamePropertyManager _combineManager;

        /// <summary>
        /// 当Buff添加到GameProperty时触发
        /// </summary>
        public event Action<GameProperty, IRPGBuff> BuffAdded;
        /// <summary>
        /// 当Buff被完全移除时触发
        /// </summary>
        public event Action<GameProperty, IRPGBuff> BuffRemoved;
        /// <summary>
        /// 当Buff过期时触发
        /// </summary>
        public event Action<GameProperty, IRPGBuff> BuffExpired;
        /// <summary>
        /// 当Buff移除一层时触发（仅对可叠加Buff，且未完全移除时）
        /// </summary>
        public event Action<GameProperty, IRPGBuff> BuffStackRemoved;

        public BuffManager(CombineGamePropertyManager combineManager)
        {
            _combineManager = combineManager;
        }

        /// <summary>
        /// 给指定GameProperty添加Buff
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

            // 优先按Group查找同组Buff（Group为null时退回按ID+Source查找）
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
                                // 叠加层数
                                for (int i = 0; i < (newStack - existing.StackCount); i++)
                                    property.AddModifier(buff.Modifier);
                                existing.StackCount = newStack;
                            }
                            // 刷新持续时间
                            existing.Elapsed = 0;
                        }
                        break;
                    case BuffStackType.Override:
                        if (buff.Priority >= existing.Priority)
                        {
                            // 覆盖：移除旧Modifier，添加新Modifier
                            for (int i = 0; i < existing.StackCount; i++)
                                property.RemoveModifier(existing.Modifier);
                            buffList.Remove(existing);

                            property.AddModifier(buff.Modifier);
                            buffList.Add(buff);
                        }
                        // 否则忽略
                        break;
                    case BuffStackType.IgnoreIfExists:
                        // 已存在则忽略
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
        /// 从指定GameProperty完全移除指定Buff（同步移除Modifier）
        /// </summary>
        public BuffManager RemoveBuff(GameProperty property, string buffId, object source = null)
        {
            return RemoveBuffStack(property, buffId, int.MaxValue, source);
        }

        /// <summary>
        /// 叠加Buff层数减少，或完全移除
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
        /// 获取指定GameProperty上的Buff
        /// </summary>
        public IRPGBuff GetBuff(GameProperty property, string buffId, object source = null)
        {
            if (property == null || string.IsNullOrEmpty(buffId)) return null;
            if (_gamePropertyBuffs.TryGetValue(property, out var buffList))
                return buffList.Find(b => b.BuffID == buffId && (source == null || b.Source == source));
            return null;
        }

        /// <summary>
        /// 获取所有属性上的所有Buff
        /// </summary>
        public IEnumerable<IRPGBuff> GetAllBuffs()
        {
            foreach (var kv in _gamePropertyBuffs)
                foreach (var buff in kv.Value)
                    yield return buff;
        }

        /// <summary>
        /// 更新所有Buff的持续时间，并移除已过期的Buff（同步移除Modifier）
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

                    // 触发型Buff处理
                    if (buff is TriggerableBuff triggerableBuff)
                    {
                        triggerableBuff.TryTrigger();
                    }

                    // 更新持续时间
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
        /// 清空所有Buff（同步移除所有Modifier）
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