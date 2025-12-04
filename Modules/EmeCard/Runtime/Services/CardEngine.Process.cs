using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace EasyPack.EmeCardSystem
{
    /// <summary>
    ///     卡牌引擎 - 事件处理、规则匹配与效果执行
    /// </summary>
    public sealed partial class CardEngine
    {
        #region Rule与效果缓存

        /// <summary>
        ///     下一个可用的 RuleUID。
        /// </summary>
        private int _nextRuleUID;

        /// <summary>
        ///     RuleUID -> Rule 的快速查找映射。
        /// </summary>
        private readonly Dictionary<int, CardRule> _rulesByUID = new();

        // 规则表（按事件类型字符串索引）
        private readonly Dictionary<string, List<CardRule>> _rules = new();

        // 卡牌事件队列（统一使用 IEventEntry）
        private readonly Queue<IEventEntry> _queue = new();

        /// <summary>
        ///     获取当前队列中待处理的事件数量
        /// </summary>
        public int PendingEventCount => _queue.Count;

        // 全局效果池，跨事件收集效果，确保全局优先级排序
        private readonly EffectPool _globalEffectPool = new();

        // 全局效果池的规则顺序计数器
        private int _globalOrderIndex;

        /// <summary>
        ///     根据 RuleUID 获取规则。
        /// </summary>
        /// <param name="ruleUID">规则的唯一标识符。</param>
        /// <returns>找到的规则，如果未找到返回 null。</returns>
        public CardRule GetRuleByUID(int ruleUID)
        {
            _rulesByUID.TryGetValue(ruleUID, out CardRule rule);
            return rule;
        }

        #endregion

        #region 对象池与缓存比较器

        [ThreadStatic] private static List<CardRule> t_rulesToProcess;
        [ThreadStatic] private static List<(CardRule, HashSet<Card>, CardRuleContext, int)> t_evals;
        [ThreadStatic] private static HashSet<Card> t_distinctSet;

        // 缓存排序比较器
        private static readonly Comparison<(CardRule rule, HashSet<Card> matched, CardRuleContext ctx, int orderIndex)>
            s_priorityComparison = (a, b) =>
            {
                int cmp = a.rule.Priority.CompareTo(b.rule.Priority);
                return cmp != 0 ? cmp : a.orderIndex.CompareTo(b.orderIndex);
            };

        private static readonly Comparison<(CardRule rule, HashSet<Card> matched, CardRuleContext ctx, int orderIndex)>
            s_orderComparison = (a, b) => a.orderIndex.CompareTo(b.orderIndex);

        /// <summary>
        ///     从线程局部池获取或创建 evals 列表
        /// </summary>
        private static List<(CardRule, HashSet<Card>, CardRuleContext, int)> RentEvalsList(int capacity = 8)
        {
            var list = t_evals;
            if (list == null)
            {
                t_evals = new List<(CardRule, HashSet<Card>, CardRuleContext, int)>(capacity);
                return t_evals;
            }
            list.Clear();
            if (list.Capacity < capacity)
                list.Capacity = capacity;
            return list;
        }

        /// <summary>
        ///     从线程局部池获取或创建 HashSet&lt;Card&gt; 用于去重
        /// </summary>
        private static HashSet<Card> RentDistinctSet()
        {
            var set = t_distinctSet;
            if (set == null)
            {
                t_distinctSet = new HashSet<Card>();
                return t_distinctSet;
            }
            set.Clear();
            return set;
        }

        /// <summary>
        ///     使用 HashSet 去重
        /// </summary>
        private static List<Card> DistinctInPlace(List<Card> list)
        {
            if (list is not { Count: > 1 }) return list;

            var set = RentDistinctSet();
            int writeIndex = 0;

            for (int i = 0; i < list.Count; i++)
            {
                if (set.Add(list[i]))
                {
                    list[writeIndex++] = list[i];
                }
            }

            // 移除重复的尾部元素
            if (writeIndex < list.Count)
            {
                list.RemoveRange(writeIndex, list.Count - writeIndex);
            }

            return list;
        }

        #endregion

        #region 规则注册

        /// <summary>
        ///     注册一条规则到引擎。
        ///     <para>自动分配唯一的 RuleUID 并建立索引。</para>
        /// </summary>
        /// <param name="rule">规则实例。</param>
        public void RegisterRule(CardRule rule)
        {
            if (rule == null) throw new ArgumentNullException(nameof(rule));
            if (string.IsNullOrEmpty(rule.EventType))
                throw new ArgumentException("Rule must have an EventType", nameof(rule));

            // 分配 RuleUID（如果尚未分配）
            if (rule.RuleUID < 0)
            {
                rule.RuleUID = _nextRuleUID++;
            }

            // 添加到 RuleUID 索引
            _rulesByUID[rule.RuleUID] = rule;

            // 确保事件类型的规则列表存在
            if (!_rules.TryGetValue(rule.EventType, out var ruleList))
            {
                ruleList = new();
                _rules[rule.EventType] = ruleList;
            }

            ruleList.Add(rule);
        }

        /// <summary>
        ///     使用 CardRuleBuilder 注册规则。
        /// </summary>
        /// <param name="configure">规则配置委托。</param>
        /// <returns>已注册的规则实例。</returns>
        public CardRule RegisterRule(Action<CardRuleBuilder> configure)
        {
            if (configure == null) throw new ArgumentNullException(nameof(configure));

            var builder = new CardRuleBuilder();
            configure(builder);
            CardRule rule = builder.Build();
            RegisterRule(rule);
            return rule;
        }

        /// <summary>
        ///     批量注册规则集。
        /// </summary>
        /// <param name="configures">规则配置委托集合。</param>
        public void RegisterRules(IReadOnlyList<Action<CardRuleBuilder>> configures)
        {
            if (configures == null) throw new ArgumentNullException(nameof(configures));

            foreach (Action<CardRuleBuilder> configure in configures)
            {
                if (configure != null)
                {
                    RegisterRule(configure);
                }
            }
        }

        /// <summary>
        ///     从引擎中注销一条规则。
        /// </summary>
        /// <param name="rule">要注销的规则实例。</param>
        /// <returns>如果成功注销返回true，否则返回false。</returns>
        public bool UnregisterRule(CardRule rule)
        {
            if (rule == null || string.IsNullOrEmpty(rule.EventType)) return false;

            bool removed = false;
            if (_rules.TryGetValue(rule.EventType, out var ruleList)) removed = ruleList.Remove(rule);

            // 从 RuleUID 索引移除
            if (removed && rule.RuleUID >= 0)
            {
                _rulesByUID.Remove(rule.RuleUID);
            }

            return removed;
        }

        #endregion

        #region 事件入队与策略

        /// <summary>
        ///     引擎全局策略
        /// </summary>
        public EnginePolicy Policy { get; } = new();

        // Pump 状态标志
        private bool _isPumping;
        private bool _isFlushingEffectPool; // 防止效果池嵌套刷新

        // 分帧处理相关
        private float _frameStartTime; // 当前帧开始处理的时间（毫秒）
        private int _frameProcessedCount; // 当前帧已处理事件数

        // 时间限制分帧机制
        private float _batchStartTime;
        private float _batchTimeLimit;

        public bool IsBatchProcessing { get; private set; }

        /// <summary>
        ///     卡牌事件回调，入队并驱动事件处理。
        /// </summary>
        private void OnCardEvent(Card source, ICardEvent evt)
        {
            // 创建 CardEventEntry 作为 IEventEntry
            var entry = new CardEventEntry(source, evt);
            _queue.Enqueue(entry);

            // 分帧模式下不自动Pump，等待下一帧主动调用PumpFrame
            // 非分帧模式保持原有即时处理行为
            if (!_isPumping && !Policy.EnableFrameDistribution) Pump();
        }

        #endregion

        #region Pump 事件驱动

        /// <summary>
        ///     同步一次性处理所有事件（无分帧）
        /// </summary>
        public void Pump()
        {
            if (_isPumping) return;
            _isPumping = true;

            try
            {
                // 阶段0: 处理 PumpStart 事件（在所有普通事件处理前）
                ProcessPumpLifecycleEvent(CardEventTypes.PUMP_START);

                // 循环处理：事件收集 → 效果执行 → 检查新事件 → 重复
                const int MaxIterations = 1000;
                int iteration = 0;

                while (iteration < MaxIterations)
                {
                    // 阶段1: 处理当前队列中的所有事件（收集效果到池中）
                    // 在此阶段，效果只收集不执行
                    while (_queue.Count > 0)
                    {
                        IEventEntry entry = _queue.Dequeue();
                        Process(entry.SourceCard, entry.Event);
                    }

                    // 阶段2: 执行效果池中的所有效果
                    if (Policy.EnableEffectPool && Policy.EffectPoolFlushMode == EffectPoolFlushMode.AfterPump)
                    {
                        FlushEffectPool();
                    }

                    // 检查是否有新事件（由效果执行触发）
                    // 如果没有新事件，Pump 结束
                    if (_queue.Count == 0)
                    {
                        break;
                    }

                    iteration++;
                }

                if (iteration >= MaxIterations)
                {
                    Debug.LogWarning($"[CardEngine] Pump 达到最大迭代次数 {MaxIterations}，可能存在无限循环");
                }

                // 阶段3: 处理 PumpEnd 事件（在所有普通事件处理后）
                // 适用于延迟删除、资源清理等需要在所有事件处理完成后执行的逻辑
                ProcessPumpLifecycleEvent(CardEventTypes.PUMP_END);
            }
            finally
            {
                _isPumping = false;
            }
        }

        /// <summary>
        ///     处理 Pump 生命周期事件（PumpStart/PumpEnd）。
        ///     <para>
        ///         此方法遍历所有已注册卡牌，为每张卡牌触发指定的生命周期事件。
        ///         处理流程与普通事件一致，包括规则匹配和效果执行。
        ///     </para>
        /// </summary>
        /// <param name="eventType">生命周期事件类型（PUMP_START 或 PUMP_END）。</param>
        private void ProcessPumpLifecycleEvent(string eventType)
        {
            // 检查是否有注册此事件类型的规则
            if (!_rules.TryGetValue(eventType, out var ruleList) || ruleList.Count == 0)
            {
                return;
            }

            // 创建生命周期事件
            var lifecycleEvent = new CardEvent<object>(eventType, null, eventType);

            // 遍历所有已注册卡牌，为每张卡牌处理生命周期事件
            var cardSnapshot = new List<Card>(_cardsByUID.Values);

            foreach (Card card in cardSnapshot)
            {
                // 跳过已被移除的卡牌（可能在处理过程中被删除）
                if (!_cardsByUID.ContainsKey(card.UID))
                {
                    continue;
                }

                ProcessCore(card, lifecycleEvent);
            }

            // 如果启用效果池，刷新收集的效果
            if (Policy.EnableEffectPool)
            {
                FlushEffectPool();
            }
        }

        /// <summary>
        ///     按帧时间预算处理事件
        /// </summary>
        public void PumpFrameWithBudget()
        {
            if (_isPumping) return;
            _isPumping = true;
            int processed = 0;

            try
            {
                _frameStartTime = Time.realtimeSinceStartup * 1000f;
                _frameProcessedCount = 0;

                float frameBudget = Policy.FrameBudgetMs;
                int maxEvents = Policy.MaxEventsPerFrame;
                int minEvents = Policy.MinEventsPerFrame;

                while (_queue.Count > 0 && processed < maxEvents)
                {
                    // 检查时间预算（但至少处理MinEventsPerFrame个事件）
                    if (_frameProcessedCount >= minEvents)
                    {
                        float elapsed = Time.realtimeSinceStartup * 1000f - _frameStartTime;
                        if (elapsed >= frameBudget)
                            // 超时，保留剩余到下一帧
                            break;
                    }

                    IEventEntry entry = _queue.Dequeue();
                    Process(entry.SourceCard, entry.Event);
                    processed++;
                    _frameProcessedCount++;
                }
            }
            finally
            {
                _isPumping = false;

                try
                {
                    // 如果启用全局效果池且模式为 AfterFrame，在本帧结束时刷新
                    if (Policy.EnableEffectPool && Policy.EffectPoolFlushMode == EffectPoolFlushMode.AfterFrame)
                    {
                        FlushEffectPool();
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[CardEngine] PumpFrameWithBudget 错误: {ex}");
                }
            }
        }

        /// <summary>
        ///     初始化时间限制批次处理
        ///     必须调用此方法后，才能在Update中调用PumpTimeLimitedBatch()
        /// </summary>
        /// <param name="timeLimitSeconds">自定义时间限制（秒），null则使用Policy.BatchTimeLimitSeconds</param>
        public void BeginTimeLimitedBatch(float? timeLimitSeconds = null)
        {
            if (IsBatchProcessing) return;

            IsBatchProcessing = true;
            _batchStartTime = Time.realtimeSinceStartup;
            _batchTimeLimit = timeLimitSeconds ?? Policy.BatchTimeLimitSeconds;
        }

        /// <summary>
        ///     驱动时间限制批次处理（在Update中调用）
        ///     必须先调用BeginTimeLimitedBatch()初始化
        ///     在指定时间内分帧处理，超时后同步完成所有
        /// </summary>
        public void PumpTimeLimitedBatch()
        {
            if (!IsBatchProcessing) return;

            float elapsed = Time.realtimeSinceStartup - _batchStartTime;
            float remaining = _batchTimeLimit - elapsed;
            bool batchCompleted = false;

            if (remaining > 0)
            {
                // 分帧阶段：使用帧预算处理事件
                float frameStart = Time.realtimeSinceStartup;
                float frameBudgetSec = Policy.FrameBudgetMs / 1000f;
                int maxEvents = Policy.MaxEventsPerFrame;

                int processedInFrame = 0;
                while (_queue.Count > 0 &&
                       Time.realtimeSinceStartup - frameStart < frameBudgetSec &&
                       processedInFrame < maxEvents)
                {
                    IEventEntry entry = _queue.Dequeue();
                    Process(entry.SourceCard, entry.Event);
                    processedInFrame++;
                }

                // 处理后检查队列是否已清空
                if (_queue.Count == 0)
                {
                    IsBatchProcessing = false;
                    batchCompleted = true;
                }
                else
                {
                    // 分帧模式下，每帧结束时刷新效果池（如果配置为 AfterFrame）
                    if (Policy.EnableEffectPool && Policy.EffectPoolFlushMode == EffectPoolFlushMode.AfterFrame)
                    {
                        FlushEffectPool();
                    }
                }
            }
            else
            {
                // 超时：同步完成所有剩余事件
                while (_queue.Count > 0)
                {
                    IEventEntry entry = _queue.Dequeue();
                    Process(entry.SourceCard, entry.Event);
                }

                IsBatchProcessing = false;
                batchCompleted = true;
            }

            // 批次完成时刷新效果池（如果配置为 AfterPump）
            if (batchCompleted && Policy.EnableEffectPool && Policy.EffectPoolFlushMode == EffectPoolFlushMode.AfterPump)
            {
                FlushEffectPool();
            }
        }

        #endregion

        #region 事件处理核心

        /// <summary>
        ///     处理单个事件，匹配规则并执行效果。
        /// </summary>
        private void Process(Card source, ICardEvent evt)
        {
            // Pump 生命周期事件（PumpStart/PumpEnd）不在普通事件处理中执行
            // 它们仅在 Pump 边界由 ProcessPumpLifecycleEvent 处理
            if (CardEventTypes.IsPumpLifecycle(evt))
            {
                return;
            }

            ProcessCore(source, evt);
        }

        /// <summary>
        ///     核心事件处理逻辑。
        /// </summary>
        private void ProcessCore(Card source, ICardEvent evt)
        {
            int typeRulesCount = 0;

            if (_rules.TryGetValue(evt.EventType, out var typeRules))
                typeRulesCount = typeRules.Count;

            if (typeRulesCount == 0) return;

            // 根据配置选择串行或并行评估
            var evals = Policy.EnableParallelMatching && typeRulesCount >= Policy.ParallelThreshold
                ? EvaluateRulesParallel(typeRules, source, evt)
                : EvaluateRulesSerial(typeRules, source, evt);

            if (evals.Count == 0) return;
            
            evals.Sort(Policy.RuleSelection == RuleSelectionMode.Priority ? s_priorityComparison : s_orderComparison);

            // 处理规则效果
            ProcessRulesEffects(evals);
        }

        #endregion

        #region 规则评估

        /// <summary>
        ///     串行评估规则（默认模式）。
        /// </summary>
        private List<(CardRule rule, HashSet<Card> matched, CardRuleContext ctx, int orderIndex)>
            EvaluateRulesSerial(List<CardRule> rules, Card source, ICardEvent evt)
        {
            // 使用对象池复用 evals 列表
            var evals = RentEvalsList(rules.Count);

            for (int i = 0; i < rules.Count; i++)
            {
                CardRule rule = rules[i];

                CardRuleContext ctx = BuildContext(rule, source, evt);
                if (ctx == null) continue;

                if (!EvaluateRequirements(ctx, rule.Requirements, out var matched, i)) continue;

                evals.Add((rule, matched, ctx, i));
            }

            return evals;
        }

        /// <summary>
        ///     并行评估规则
        ///     <para>
        ///         注意：Requirements 实现必须是线程安全的才能使用此模式。
        ///         建议仅在 Requirements 不修改共享状态时启用。
        ///     </para>
        /// </summary>
        private List<(CardRule rule, HashSet<Card> matched, CardRuleContext ctx, int orderIndex)>
            EvaluateRulesParallel(List<CardRule> rules, Card source, ICardEvent evt)
        {
            var results = new ConcurrentBag<(CardRule rule, HashSet<Card> matched, CardRuleContext ctx, int orderIndex)>();

            var options = new ParallelOptions();
            if (Policy.MaxDegreeOfParallelism > 0)
                options.MaxDegreeOfParallelism = Policy.MaxDegreeOfParallelism;

            Parallel.For(0, rules.Count, options, i =>
            {
                CardRule rule = rules[i];

                CardRuleContext ctx = BuildContext(rule, source, evt);
                if (ctx == null) return;

                if (EvaluateRequirements(ctx, rule.Requirements, out var matched, i))
                {
                    results.Add((rule, matched, ctx, i));
                }
            });

            return results.ToList();
        }

        /// <summary>
        ///     评估规则的所有条件要求。使用 HashSet 对象池避免频繁分配。
        /// </summary>
        private bool EvaluateRequirements(CardRuleContext ctx, List<IRuleRequirement> requirements,
                                          out HashSet<Card> matchedAll, int ruleId = -1)
        {
            // 使用对象池获取 HashSet，自动清空
            matchedAll = RentDistinctSet();
            if (requirements == null) return true;

            foreach (IRuleRequirement req in requirements)
            {
                if (req == null) return false;

                if (!req.TryMatch(ctx, out var picks)) return false;

                if (picks is not { Count: > 0 }) continue;

                foreach (Card t in picks)
                    matchedAll.Add(t);
            }

            return true;
        }

        /// <summary>
        ///     构建规则上下文。
        /// </summary>
        private CardRuleContext BuildContext(CardRule rule, Card source, ICardEvent evt)
        {
            // 解析匹配根和效果根
            Card matchRoot = CardRule.ResolveRootCard(source, rule.MatchRootHops);
            Card effectRoot = CardRule.ResolveRootCard(source, rule.EffectRootHops);

            if (matchRoot == null) return null;

            return new CardRuleContext(
                source,
                matchRoot,
                effectRoot,
                evt,
                this,       // 传递当前引擎实例
                rule.MaxDepth,
                rule        // 传递当前规则
            );
        }

        #endregion

        #region 效果执行

        /// <summary>
        ///     执行规则效果。
        ///     <para>
        ///         如果启用了全局效果池，效果会被收集到池中而非立即执行。
        ///         池中的效果将在 Pump 结束时或手动刷新时按全局优先级执行。
        ///     </para>
        /// </summary>
        private void ProcessRulesEffects(List<(CardRule rule, HashSet<Card> matched, CardRuleContext ctx, int orderIndex)> evals)
        {
            if (Policy.EnableEffectPool)
            {
                // 收集效果到全局池
                CollectEffectsToPool(evals);
            }
            else
            {
                // 直接执行效果（原有逻辑）
                ExecuteRulesDirect(evals);
            }
        }

        /// <summary>
        ///     直接执行规则效果（不使用效果池）。
        /// </summary>
        private void ExecuteRulesDirect(List<(CardRule rule, HashSet<Card> matched, CardRuleContext ctx, int orderIndex)> evals)
        {
            if (Policy.FirstMatchOnly)
            {
                var first = evals.First();
                ExecuteOne(first);
            }
            else
            {
                foreach (var e in evals)
                {
                    if (ExecuteOne(e))
                        break;
                }
            }
        }

        /// <summary>
        ///     收集效果到全局效果池。
        ///     <para>
        ///         同一事件的所有规则共享同一个 EventIndex，
        ///         以便 StopEventOnSuccess 能够正确跳过同事件的后续规则。
        ///         同时保留规则的注册顺序（RuleOrderIndex）用于同优先级排序。
        ///     </para>
        /// </summary>
        private void CollectEffectsToPool(List<(CardRule rule, HashSet<Card> matched, CardRuleContext ctx, int orderIndex)> evals)
        {
            if (evals.Count == 0) return;

            int currentEventIndex = _globalOrderIndex;

            foreach (var (rule, matched, ctx, orderIndex) in evals)
            {
                if (matched == null || rule.Effects == null || rule.Effects.Count == 0)
                    continue;

                // 同一事件的所有规则使用相同的 EventIndex，但保留各自的规则注册顺序
                _globalEffectPool.AddRuleEffects(rule, ctx, matched, currentEventIndex, orderIndex);
            }

            // 事件处理完毕，递增 EventIndex 用于下一个事件
            _globalOrderIndex++;
        }

        /// <summary>
        ///     刷新全局效果池，按优先级执行所有收集的效果。
        ///     <para>
        ///         支持 StopEventOnSuccess：当一个规则的所有效果执行完毕后，
        ///         如果该规则设置了 StopEventOnSuccess，则跳过同一 OrderIndex（同一事件）的后续规则。
        ///     </para>
        /// </summary>
        /// <returns>执行的效果数量</returns>
        public int FlushEffectPool()
        {
            // 防止嵌套刷新，如果正在刷新效果池，跳过
            if (_isFlushingEffectPool) return 0;
            if (_globalEffectPool.Count == 0) return 0;

            _isFlushingEffectPool = true;
            try
            {
                int count = _globalEffectPool.ExecuteWithStopEventOnSuccess();
                _globalEffectPool.Clear();
                _globalOrderIndex = 0; // 重置顺序计数器
                return count;
            }
            finally
            {
                _isFlushingEffectPool = false;
            }
        }

        /// <summary>
        ///     获取全局效果池中待执行的效果数量。
        /// </summary>
        public int PendingEffectCount => _globalEffectPool.Count;

        /// <summary>
        ///     执行单个规则的效果。
        /// </summary>
        private bool ExecuteOne((CardRule rule, HashSet<Card> matched, CardRuleContext ctx, int orderIndex) e)
        {
            if (e.matched == null || e.rule.Effects == null || e.rule.Effects.Count == 0) return false;

            foreach (IRuleEffect eff in e.rule.Effects)
            {
                eff.Execute(e.ctx, e.matched);
            }

            return e.rule.Policy?.StopEventOnSuccess == true;
        }

        #endregion
    }
}
