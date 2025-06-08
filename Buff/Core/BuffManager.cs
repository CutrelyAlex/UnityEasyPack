using System;
using System.Collections.Generic;

namespace RPGPack
{
    /// <summary>
    /// 只负责Buff的生命周期管理和事件，不直接操作IProperty。
    /// Buff的应用与移除由BuffHandle负责。
    /// </summary>
    public class BuffManager
    {
        private readonly List<IRPGBuff> _buffs = new();
        private static readonly HashSet<IRPGBuff> _allBuffs = new();

        /// <summary>
        /// Buff添加事件
        /// </summary>
        public event Action<IRPGBuff> BuffAddedOrChanged;
        /// <summary>
        /// Buff移除事件
        /// </summary>
        public event Action<IRPGBuff> BuffRemoved;
        /// <summary>
        /// Buff过期事件
        /// </summary>
        public event Action<IRPGBuff> BuffExpired;
        /// <summary>
        /// Buff移除一层事件
        /// </summary>
        public event Action<IRPGBuff> BuffStackRemoved;

        /// <summary>
        /// 添加Buff到管理器
        /// </summary>
        /// <param name="buff">要添加的Buff实例</param>
        /// <returns>返回自身以便链式调用</returns>
        public bool AddBuff(IRPGBuff buff)
        {
            if (buff == null) return false;

            _allBuffs.Add(buff);

            // 克隆Buff对象
            var newBuff = (buff as Buff)?.Clone() ?? buff;

            IRPGBuff existing = null;
            // 优先根据Layer分组联合BuffID查找，否则直接用BuffID查找
            if (!string.IsNullOrEmpty(buff.Layer))
                existing = _buffs.Find(b => b.Layer == buff.Layer && b.BuffID == buff.BuffID);
            else
                existing = _buffs.Find(b => b.BuffID == buff.BuffID);

            if (existing != null)
            {
                // 只保留叠加型Buff逻辑，其余类型直接忽略
                if (existing.CanStack)
                {
                    int maxStack = existing.MaxStackCount > 0 ? existing.MaxStackCount : int.MaxValue;
                    int newStack = Math.Min(existing.StackCount + buff.StackCount, maxStack);
                    if (newStack > existing.StackCount)
                    {
                        existing.StackCount = newStack;
                    }
                    // 叠加时刷新持续时间
                    existing.Elapsed = 0;
                    BuffAddedOrChanged?.Invoke(existing);
                    return true;
                }
                // 非叠加型Buff直接忽略
                return false;
            }
            else
            {
                _buffs.Add(newBuff);
                BuffAddedOrChanged?.Invoke(buff);
                return true;
            }
        }

        /// <summary>
        /// 移除指定Buff（移除所有层）
        /// </summary>
        /// <param name="buffId">Buff的唯一ID</param>
        /// <param name="source">Buff来源对象，可选</param>
        /// <returns>返回自身以便链式调用</returns>
        public BuffManager RemoveBuff(string buffId, object source = null)
        {
            return RemoveBuffStack(buffId, int.MaxValue, source);
        }

        /// <summary>
        /// 移除指定Buff的指定层数
        /// </summary>
        /// <param name="buffId">Buff的唯一ID</param>
        /// <param name="layer">要移除的层数，默认为1</param>
        /// <param name="source">Buff来源对象，可选</param>
        /// <returns>返回自身以便链式调用</returns>
        public BuffManager RemoveBuffStack(string buffId, int layer = 1, object source = null)
        {
            if (string.IsNullOrEmpty(buffId)) return this;

            var buff = _buffs.Find(b => b.BuffID == buffId && (source == null || b.Source == source));
            if (buff != null)
            {
                // 叠加型Buff只减少层数，不足则移除整个Buff
                if (buff.CanStack && buff.StackCount > 1)
                {
                    if (layer >= buff.StackCount)
                    {
                        BuffRemoved?.Invoke(buff);
                        _buffs.Remove(buff);
                        _allBuffs.Remove(buff);
                    }
                    else
                    {
                        buff.StackCount -= layer;
                        BuffStackRemoved?.Invoke(buff);
                    }
                }
                else
                {
                    BuffRemoved?.Invoke(buff);
                    _buffs.Remove(buff);
                    _allBuffs.Remove(buff);
                }
            }
            return this;
        }

        /// <summary>
        /// 获取指定Buff，可按Layer查询
        /// </summary>
        /// <param name="buffId">Buff的唯一ID</param>
        /// <param name="layer">Buff分组，可选。为空则不按Layer过滤</param>
        /// <param name="source">Buff来源对象，可选</param>
        /// <returns>返回找到的Buff实例，未找到返回null</returns>
        public IRPGBuff GetBuff(string buffId, string layer = null, object source = null)
        {
            if (string.IsNullOrEmpty(buffId)) return null;
            if (string.IsNullOrEmpty(layer))
            {
                // 不按Layer过滤
                return _buffs.Find(b => b.BuffID == buffId && (source == null || b.Source == source));
            }
            else
            {
                // 按Layer过滤
                return _buffs.Find(b => b.BuffID == buffId && b.Layer == layer && (source == null || b.Source == source));
            }
        }


        /// <summary>
        /// 获取所有已创建的 Buff（全局静态集合）
        /// </summary>
        /// <returns>Buff枚举集合</returns>
        public static IEnumerable<IRPGBuff> GetAllBuffs() => _allBuffs;

        /// <summary>
        /// 更新Buff的生命周期（需每帧调用）
        /// </summary>
        /// <param name="deltaTime">距离上次更新的时间（秒）</param>
        /// <returns>返回自身以便链式调用</returns>
        public BuffManager Update(float deltaTime)
        {
            var expiredBuffs = new List<IRPGBuff>();
            // 倒序遍历，移除时不会影响索引
            for (int i = _buffs.Count - 1; i >= 0; i--)
            {
                var buff = _buffs[i];

                // 触发型Buff尝试触发
                if (buff is TriggerableBuff triggerableBuff)
                {
                    triggerableBuff.TryTrigger();
                }

                // 只处理有持续时间的Buff
                if (buff.Duration > 0)
                {
                    buff.Elapsed += deltaTime;
                    if (buff.IsExpired)
                    {
                        // 支持每次过期只移除一层
                        if (buff.RemoveOneStackEachDuration && buff.CanStack && buff.StackCount > 1)
                        {
                            RemoveBuffStack(buff.BuffID, 1, buff.Source);
                            buff.Elapsed = 0;
                            continue;
                        }
                        BuffRemoved?.Invoke(buff);
                        expiredBuffs.Add(buff);
                        _buffs.RemoveAt(i);
                        _allBuffs.Remove(buff);
                    }
                }
            }
            // 统一触发过期事件
            foreach (var buff in expiredBuffs)
            {
                BuffExpired?.Invoke(buff);
            }
            return this;
        }

        /// <summary>
        /// 清空所有Buff
        /// </summary>
        /// <returns>返回自身以便链式调用</returns>
        public BuffManager Clear()
        {
            foreach (var buff in _buffs)
            {
                BuffRemoved?.Invoke(buff);
            }
            _buffs.Clear();
            _allBuffs.Clear();
            return this;
        }
    }
}