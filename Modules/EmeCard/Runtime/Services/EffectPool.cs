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
        public HashSet<Card> Matched { get; }

        /// <summary>规则优先级（用于排序）</summary>
        public int Priority { get; }

        /// <summary>事件顺序索引（用于 StopEventOnSuccess 判断同一事件）</summary>
        public int EventIndex { get; }

        /// <summary>规则注册顺序（用于同优先级排序）</summary>
        public int RuleOrderIndex { get; }

        /// <summary>效果在规则中的索引（用于保持效果顺序）</summary>
        public int EffectIndex { get; }

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
        ///     比较顺序：优先级 → 规则注册顺序 → 效果索引
        ///     <para>
        ///         全局按优先级排序，确保高优先级效果先执行。
        ///         StopEventOnSuccess 通过维护已停止事件集合来实现，不依赖排序顺序。
        ///     </para>
        /// </summary>
        public int CompareTo(EffectPoolEntry other)
        {
            // 优先级越小越优先
            int cmp = Priority.CompareTo(other.Priority);
            if (cmp != 0) return cmp;

            // 规则注册顺序越小越优先
            cmp = RuleOrderIndex.CompareTo(other.RuleOrderIndex);
            return cmp != 0 ? cmp : EffectIndex.CompareTo(other.EffectIndex); // 效果索引越小越优先
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
        private readonly List<EffectPoolEntry> _entries;
        private bool _isSorted;

        // 复用的内部集合
        private HashSet<int> _stoppedEventsCache;
        private Dictionary<(int eventIndex, int ruleOrderIndex), int> _ruleEffectCountsCache;

        /// <summary>
        ///     创建 EffectPool 实例
        /// </summary>
        /// <param name="initialCapacity">预分配容量（可选，默认使用 List 默认行为）</param>
        public EffectPool(int initialCapacity)
        {
            _entries = new List<EffectPoolEntry>(initialCapacity);
        }

        /// <summary>
        ///     默认构造函数
        /// </summary>
        public EffectPool()
        {
            _entries = new List<EffectPoolEntry>();
        }

        /// <summary>当前池中的效果数量</summary>
        public int Count => _entries.Count;

        /// <summary>
        ///     添加规则的所有效果到池中。
        /// </summary>
        /// <param name="rule">规则</param>
        /// <param name="context">规则上下文</param>
        /// <param name="matched">匹配的卡牌</param>
        /// <param name="eventIndex">事件顺序索引（用于 StopEventOnSuccess）</param>
        /// <param name="ruleOrderIndex">规则的注册顺序</param>
        public void AddRuleEffects(CardRule rule, CardRuleContext context, HashSet<Card> matched, int eventIndex, int ruleOrderIndex)
        {
            if (rule?.Effects == null || rule.Effects.Count == 0) return;

            int priority = rule.Priority;
            int effectCount = rule.Effects.Count;

            for (int i = 0; i < effectCount; i++)
            {
                IRuleEffect effect = rule.Effects[i];
                if (effect != null)
                {
                    _entries.Add(new EffectPoolEntry(effect, context, matched, priority, eventIndex, ruleOrderIndex, i));
                    _isSorted = false;
                }
            }
        }

        /// <summary>
        ///     添加单个效果到池中。
        /// </summary>
        public void AddEffect(IRuleEffect effect, CardRuleContext context, HashSet<Card> matched,
                              int priority = 0, int eventIndex = 0, int ruleOrderIndex = 0, int effectIndex = 0)
        {
            if (effect == null) return;

            _entries.Add(new EffectPoolEntry(effect, context, matched, priority, eventIndex, ruleOrderIndex, effectIndex));
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
            foreach (EffectPoolEntry entry in _entries)
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
            foreach (EffectPoolEntry entry in _entries)
            {
                entry.Effect.Execute(entry.Context, entry.Matched);
                count++;

                if (shouldStop?.Invoke(entry) == true)
                    break;
            }

            return count;
        }

        /// <summary>
        ///     执行池中所有效果，支持 StopEventOnSuccess。
        ///     <para>
        ///         全局按优先级排序后执行。当一个规则的所有效果执行完毕后，
        ///         如果该规则设置了 StopEventOnSuccess，则跳过同一 EventIndex（同一事件）的后续规则的所有效果。
        ///     </para>
        ///     <para>
        ///     </para>
        /// </summary>
        /// <returns>执行的效果数量</returns>
        public int ExecuteWithStopEventOnSuccess()
        {
            if (_entries.Count == 0) return 0;

            // 排序
            if (!_isSorted)
            {
                _entries.Sort();
                _isSorted = true;
            }

            // 已停止事件集合
            _stoppedEventsCache ??= new HashSet<int>();
            _stoppedEventsCache.Clear();

            // 规则执行状态字典
            _ruleEffectCountsCache ??= new Dictionary<(int eventIndex, int ruleOrderIndex), int>();
            _ruleEffectCountsCache.Clear();

            // 执行
            int count = 0;

            for (int i = 0; i < _entries.Count; i++)
            {
                EffectPoolEntry entry = _entries[i];

                // 检查是否需要跳过当前效果（属于被中断的事件）
                if (_stoppedEventsCache.Count > 0 && _stoppedEventsCache.Contains(entry.EventIndex))
                {
                    continue;
                }

                // 获取当前规则
                CardRule currentRule = entry.Context?.CurrentRule;

                // 执行效果
                entry.Effect.Execute(entry.Context, entry.Matched);
                count++;

                // 更新规则的已执行效果计数
                var ruleKey = (entry.EventIndex, entry.RuleOrderIndex);
                if (!_ruleEffectCountsCache.TryGetValue(ruleKey, out int executedCount))
                {
                    executedCount = 0;
                }
                _ruleEffectCountsCache[ruleKey] = executedCount + 1;

                // 检查规则的所有效果是否已执行完毕
                int totalEffects = currentRule?.Effects?.Count ?? 0;
                if (_ruleEffectCountsCache[ruleKey] >= totalEffects && totalEffects > 0)
                {
                    // 规则执行完毕，检查 StopEventOnSuccess
                    if (currentRule?.Policy?.StopEventOnSuccess == true)
                    {
                        // 将此事件标记为已停止
                        _stoppedEventsCache.Add(entry.EventIndex);
                    }
                }
            }

            return count;
        }

        /// <summary>
        ///     清空池（保留容量）。
        /// </summary>
        public void Clear()
        {
            _entries.Clear();
            _isSorted = false;
            // 注意：不清理 _stoppedEventsCache 和 _ruleEffectCountsCache，它们会在下次使用时清空
        }

        /// <summary>
        ///     完全重置池（释放多余容量）。
        /// </summary>
        /// <param name="maxCapacity">保留的最大容量，超过此值会缩减</param>
        public void Reset(int maxCapacity = 256)
        {
            Clear();
            // 如果容量过大，释放一部分
            if (_entries.Capacity > maxCapacity)
            {
                _entries.Capacity = maxCapacity;
            }
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
