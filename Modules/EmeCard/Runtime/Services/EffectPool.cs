using System;
using System.Collections.Generic;

namespace EasyPack.EmeCardSystem
{
    /// <summary>
    ///     效果池条目：表示一个待执行的效果及其上下文。
    /// </summary>
    public readonly struct EffectPoolEntry : IComparable<EffectPoolEntry>
    {
        /// <summary>要执行的效果</summary>
        public IRuleEffect Effect { get; }

        /// <summary>规则执行上下文</summary>
        public CardRuleContext Context { get; }

        /// <summary>匹配的卡牌列表</summary>
        public IReadOnlyList<Card> Matched { get; }

        /// <summary>规则优先级（用于排序）</summary>
        public int Priority { get; }

        /// <summary>注册顺序（用于同优先级排序）</summary>
        public int OrderIndex { get; }

        /// <summary>效果在规则中的索引（用于保持效果顺序）</summary>
        public int EffectIndex { get; }

        public EffectPoolEntry(
            IRuleEffect effect,
            CardRuleContext context,
            IReadOnlyList<Card> matched,
            int priority,
            int orderIndex,
            int effectIndex)
        {
            Effect = effect;
            Context = context;
            Matched = matched;
            Priority = priority;
            OrderIndex = orderIndex;
            EffectIndex = effectIndex;
        }

        /// <summary>
        ///     比较顺序：优先级 → 注册顺序 → 效果索引
        /// </summary>
        public int CompareTo(EffectPoolEntry other)
        {
            // 优先级越小越优先
            int cmp = Priority.CompareTo(other.Priority);
            if (cmp != 0) return cmp;

            // 注册顺序越小越优先
            cmp = OrderIndex.CompareTo(other.OrderIndex);
            if (cmp != 0) return cmp;

            // 效果索引越小越优先
            return EffectIndex.CompareTo(other.EffectIndex);
        }
    }

    /// <summary>
    ///     效果池：收集并按优先级执行所有命中规则的效果。
    ///     <para>
    ///         用于确保效果执行顺序的确定性：
    ///         1. 所有命中规则的效果先收集到池中
    ///         2. 按规则优先级排序
    ///         3. 统一执行
    ///     </para>
    /// </summary>
    public sealed class EffectPool
    {
        private readonly List<EffectPoolEntry> _entries = new();
        private bool _isSorted = false;

        /// <summary>当前池中的效果数量</summary>
        public int Count => _entries.Count;

        /// <summary>
        ///     添加规则的所有效果到池中。
        /// </summary>
        /// <param name="rule">规则</param>
        /// <param name="context">规则上下文</param>
        /// <param name="matched">匹配的卡牌</param>
        /// <param name="orderIndex">规则的注册顺序</param>
        public void AddRuleEffects(CardRule rule, CardRuleContext context, IReadOnlyList<Card> matched, int orderIndex)
        {
            if (rule?.Effects == null || rule.Effects.Count == 0) return;

            int priority = rule.Priority;
            for (int i = 0; i < rule.Effects.Count; i++)
            {
                var effect = rule.Effects[i];
                if (effect != null)
                {
                    _entries.Add(new EffectPoolEntry(effect, context, matched, priority, orderIndex, i));
                    _isSorted = false;
                }
            }
        }

        /// <summary>
        ///     添加单个效果到池中。
        /// </summary>
        public void AddEffect(IRuleEffect effect, CardRuleContext context, IReadOnlyList<Card> matched,
                              int priority = 0, int orderIndex = 0, int effectIndex = 0)
        {
            if (effect == null) return;

            _entries.Add(new EffectPoolEntry(effect, context, matched, priority, orderIndex, effectIndex));
            _isSorted = false;
        }

        /// <summary>
        ///     执行池中所有效果（按优先级排序后执行）。
        /// </summary>
        /// <returns>执行的效果数量</returns>
        public int ExecuteAll()
        {
            if (_entries.Count == 0) return 0;

            // 排序
            if (!_isSorted)
            {
                _entries.Sort();
                _isSorted = true;
            }

            // 执行
            int count = _entries.Count;
            foreach (var entry in _entries)
            {
                entry.Effect.Execute(entry.Context, entry.Matched);
            }

            return count;
        }

        /// <summary>
        ///     执行池中所有效果，支持中断。
        /// </summary>
        /// <param name="shouldStop">检查是否应该停止的委托（每个效果执行后调用）</param>
        /// <returns>执行的效果数量</returns>
        public int ExecuteWithInterrupt(Func<EffectPoolEntry, bool> shouldStop)
        {
            if (_entries.Count == 0) return 0;

            // 排序
            if (!_isSorted)
            {
                _entries.Sort();
                _isSorted = true;
            }

            // 执行
            int count = 0;
            foreach (var entry in _entries)
            {
                entry.Effect.Execute(entry.Context, entry.Matched);
                count++;

                if (shouldStop?.Invoke(entry) == true)
                    break;
            }

            return count;
        }

        /// <summary>
        ///     清空池。
        /// </summary>
        public void Clear()
        {
            _entries.Clear();
            _isSorted = false;
        }

        /// <summary>
        ///     获取排序后的效果列表（只读）。
        /// </summary>
        public IReadOnlyList<EffectPoolEntry> GetSortedEntries()
        {
            if (!_isSorted && _entries.Count > 1)
            {
                _entries.Sort();
                _isSorted = true;
            }

            return _entries;
        }
    }
}
