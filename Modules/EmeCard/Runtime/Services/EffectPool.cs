using System;
using System.Collections.Generic;

namespace EasyPack.EmeCardSystem
{
    /// <summary>
    ///     效果池条目：表示一个待执行的效果及其上下文。
    /// </summary>
    public readonly struct EffectPoolEntry : IComparable<EffectPoolEntry>
    {
        /// <summary>要执行的效果。</summary>
        public readonly IRuleEffect Effect;

        /// <summary>规则执行上下文。</summary>
        public readonly CardRuleContext Context;

        /// <summary>匹配的卡牌列表。</summary>
        public readonly HashSet<Card> Matched;

        /// <summary>规则优先级（用于排序）。</summary>
        public readonly int Priority;

        /// <summary>事件顺序索引（用于 StopEventOnSuccess 判断同一事件）。</summary>
        public readonly int EventIndex;

        /// <summary>规则注册顺序（用于同优先级排序）。</summary>
        public readonly int RuleOrderIndex;

        /// <summary>效果在规则中的索引（用于保持效果顺序）。</summary>
        public readonly int EffectIndex;

        public EffectPoolEntry(
            IRuleEffect effect,
            CardRuleContext context,
            HashSet<Card> matched,
            int priority,
            int eventIndex,
            int ruleOrderIndex,
            int effectIndex)
        {
            Effect = effect;
            Context = context;
            Matched = matched;
            Priority = priority;
            EventIndex = eventIndex;
            RuleOrderIndex = ruleOrderIndex;
            EffectIndex = effectIndex;
        }

        /// <summary>
        ///     比较顺序：优先级 → 事件顺序 → 规则注册顺序 → 效果索引
        /// </summary>
        public int CompareTo(EffectPoolEntry other)
        {
            int cmp = Priority.CompareTo(other.Priority);
            if (cmp != 0) return cmp;

            // 相同优先级下，保持事件入队顺序。
            cmp = EventIndex.CompareTo(other.EventIndex);
            if (cmp != 0) return cmp;

            cmp = RuleOrderIndex.CompareTo(other.RuleOrderIndex);
            return cmp != 0 ? cmp : EffectIndex.CompareTo(other.EffectIndex);
        }
    }

    /// <summary>
    ///     效果池：收集并按优先级执行所有命中规则的效果。
    /// </summary>
    public sealed class EffectPool
    {
        private readonly List<EffectPoolEntry> _entries = new();
        private bool _isSorted;

        /// <summary>当前池中的效果数量。</summary>
        public int Count => _entries.Count;

        /// <summary>
        ///     添加规则的所有效果到池中。
        /// </summary>
        public void AddRuleEffects(
            CardRule rule,
            CardRuleContext context,
            HashSet<Card> matched,
            int eventIndex,
            int ruleOrderIndex)
        {
            if (rule?.Effects == null || rule.Effects.Count == 0)
                return;

            int priority = rule.Priority;

            for (int i = 0; i < rule.Effects.Count; i++)
            {
                IRuleEffect effect = rule.Effects[i];
                if (effect != null)
                {
                    _entries.Add(new EffectPoolEntry(
                        effect, context, matched,
                        priority, eventIndex, ruleOrderIndex, i));
                }
            }

            _isSorted = false;
        }

        /// <summary>
        ///     添加单个效果到池中。
        /// </summary>
        public void AddEffect(
            IRuleEffect effect,
            CardRuleContext context,
            HashSet<Card> matched,
            int priority = 0,
            int eventIndex = 0,
            int ruleOrderIndex = 0,
            int effectIndex = 0)
        {
            if (effect == null) return;

            _entries.Add(new EffectPoolEntry(
                effect, context, matched,
                priority, eventIndex, ruleOrderIndex, effectIndex));

            _isSorted = false;
        }

        /// <summary>
        ///     执行池中所有效果（按优先级排序后执行）。
        /// </summary>
        public int ExecuteAll()
        {
            if (_entries.Count == 0) return 0;

            EnsureSorted();

            foreach (var entry in _entries)
            {
                entry.Effect.Execute(entry.Context, entry.Matched);
            }

            return _entries.Count;
        }

        /// <summary>
        ///     执行池中所有效果，支持 StopEventOnSuccess。
        /// </summary>
        public int ExecuteWithStopEventOnSuccess()
        {
            if (_entries.Count == 0) return 0;

            EnsureSorted();

            // 快速路径：检查是否有任何规则使用了 StopEventOnSuccess
            bool hasStopEventOnSuccess = false;
            foreach (var entry in _entries)
            {
                if (entry.Context.CurrentRule?.Policy?.StopEventOnSuccess == true)
                {
                    hasStopEventOnSuccess = true;
                    break;
                }
            }

            // 无 StopEventOnSuccess 规则，直接执行全部
            if (!hasStopEventOnSuccess)
            {
                foreach (var entry in _entries)
                {
                    entry.Effect.Execute(entry.Context, entry.Matched);
                }
                return _entries.Count;
            }

            // 慢速路径：处理 StopEventOnSuccess
            return ExecuteWithStopEventOnSuccessSlow();
        }

        /// <summary>
        ///     慢速路径：处理 StopEventOnSuccess 的完整逻辑。
        /// </summary>
        private int ExecuteWithStopEventOnSuccessSlow()
        {
            var stoppedEvents = new HashSet<int>();
            var ruleEffectCounts = new Dictionary<long, int>();

            int executed = 0;

            foreach (var entry in _entries)
            {
                // 跳过已停止的事件
                if (stoppedEvents.Contains(entry.EventIndex))
                    continue;

                // 执行效果
                entry.Effect.Execute(entry.Context, entry.Matched);
                executed++;

                // 检查 StopEventOnSuccess
                var rule = entry.Context.CurrentRule;
                if (rule?.Policy?.StopEventOnSuccess == true)
                {
                    // 使用组合键追踪规则效果执行计数
                    long ruleKey = ((long)entry.EventIndex << 32) | (uint)entry.RuleOrderIndex;

                    ruleEffectCounts.TryGetValue(ruleKey, out int count);
                    count++;
                    ruleEffectCounts[ruleKey] = count;

                    // 当规则的所有效果都执行完毕后，停止该事件
                    int effectCount = rule.Effects?.Count ?? 0;
                    if (count >= effectCount && effectCount > 0)
                    {
                        stoppedEvents.Add(entry.EventIndex);
                    }
                }
            }

            return executed;
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
        ///     获取条目的只读列表（用于调试）。
        /// </summary>
        public IReadOnlyList<EffectPoolEntry> GetEntries()
        {
            EnsureSorted();
            return _entries;
        }

        /// <summary>
        ///     确保列表已排序。
        /// </summary>
        private void EnsureSorted()
        {
            if (!_isSorted && _entries.Count > 1)
            {
                _entries.Sort();
                _isSorted = true;
            }
        }
    }
}
