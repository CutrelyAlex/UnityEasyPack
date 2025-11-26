using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EasyPack.Category;
using UnityEngine;

namespace EasyPack.EmeCardSystem
{
    public sealed class CardEngine
    {
        #region 初始化

        public CardEngine(ICardFactory factory)
        {
            CardFactory = factory;
            factory.Owner = this;

            // 使用 Card.Id + Card.Index 作为唯一标识
            CategoryManager = new CategoryManager<Card>(card => $"{card.Id}#{card.Index}");

            PreCacheAllCardTemplates();
            InitializeTargetSelectorCache();

            // 预注册标准事件类型
            _rules[CardEventTypes.TICK] = new();
            _rules[CardEventTypes.ADDED_TO_OWNER] = new();
            _rules[CardEventTypes.REMOVED_FROM_OWNER] = new();
            _rules[CardEventTypes.USE] = new();
        }

        public void Init()
        {
            PreCacheAllCardTemplates();
            InitializeTargetSelectorCache();
        }

        /// <summary>
        ///     初始化TargetSelector的Tag缓存。应在所有卡牌注册完成后调用。
        /// </summary>
        private void InitializeTargetSelectorCache()
        {
            // 同时传递 CategoryManager 以支持线程安全的标签查询
            TargetSelector.InitializeTagCache(_registeredCardsTemplates, CategoryManager);
        }

        /// <summary>
        ///     清除TargetSelector的Tag缓存
        /// </summary>
        public void ClearTargetSelectorCache()
        {
            TargetSelector.ClearTagCache();
        }

        /// <summary>
        ///     从工厂创建所有卡牌的副本并缓存。
        ///     应在系统初始化时调用一次。
        /// </summary>
        private void PreCacheAllCardTemplates()
        {
            _registeredCardsTemplates.Clear();

            if (CardFactory == null) return;

            // 获取工厂中所有注册的卡牌ID
            var cardIds = CardFactory.GetAllCardIds();
            if (cardIds == null || cardIds.Count == 0) return;

            foreach (string id in cardIds)
            {
                // 为每个ID创建一个副本
                Card templateCard = CardFactory.Create(id);
                if (templateCard != null) _registeredCardsTemplates.Add(templateCard);
            }
        }

        #endregion

        #region 基本属性

        public ICardFactory CardFactory { get; set; }

        /// <summary>
        ///     分类管理系统，用于统一管理卡牌的分类和标签。
        ///     提供基于标签的 O(1) 查询和基于层级分类的 O(log n) 查询。
        /// </summary>
        public ICategoryManager<Card> CategoryManager { get; }

        /// <summary>
        ///     引擎全局策略
        /// </summary>
        public EnginePolicy Policy { get; } = new();

        private bool _isPumping = false;

        // 分帧处理相关
        private float _frameStartTime; // 当前帧开始处理的时间（毫秒）
        private int _frameProcessedCount; // 当前帧已处理事件数

        // 时间限制分帧机制
        private bool _isBatchProcessing = false;
        private float _batchStartTime;
        private float _batchTimeLimit;

        /// <summary>
        ///     获取当前队列中待处理的事件数量
        /// </summary>
        public int PendingEventCount => _queue.Count;

        public bool IsBatchProcessing => _isBatchProcessing;

        #endregion

        #region 事件和缓存

        // 规则表（按事件类型字符串索引）
        private readonly Dictionary<string, List<CardRule>> _rules = new();

        // 卡牌事件队列（统一使用 IEventEntry）
        private readonly Queue<IEventEntry> _queue = new();

        // 已注册的卡牌集合
        private readonly HashSet<Card> _registeredCardsTemplates = new();

        // 卡牌Key->Card缓存
        private readonly Dictionary<CardKey, Card> _cardMap = new();

        // id->index集合缓存
        private readonly Dictionary<string, HashSet<int>> _idIndexes = new();

        // id->Card列表缓存，用于快速查找
        private readonly Dictionary<string, List<Card>> _cardsById = new();

        // Custom规则按ID分组缓存
        private readonly Dictionary<string, List<CardRule>> _customRulesById = new();

        // UID->Card缓存，支持 O(1) UID 查询
        private readonly Dictionary<int, Card> _cardsByUID = new();

        // 全局效果池，跨事件收集效果，确保全局优先级排序
        private readonly EffectPool _globalEffectPool = new();

        // 全局效果池的规则顺序计数器
        private int _globalOrderIndex = 0;

        #endregion

        #region 规则处理

        /// <summary>
        ///     注册一条规则到引擎。
        /// </summary>
        /// <param name="rule">规则实例。</param>
        public void RegisterRule(CardRule rule)
        {
            if (rule == null) throw new ArgumentNullException(nameof(rule));
            if (string.IsNullOrEmpty(rule.EventType))
                throw new ArgumentException("Rule must have an EventType", nameof(rule));

            // 确保事件类型的规则列表存在
            if (!_rules.TryGetValue(rule.EventType, out var ruleList))
            {
                ruleList = new();
                _rules[rule.EventType] = ruleList;
            }

            ruleList.Add(rule);

            // 如果有 CustomId，也添加到 CustomId 索引
            if (!string.IsNullOrEmpty(rule.CustomId))
            {
                if (!_customRulesById.TryGetValue(rule.CustomId, out var customRuleList))
                {
                    customRuleList = new();
                    _customRulesById[rule.CustomId] = customRuleList;
                }

                customRuleList.Add(rule);
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

            // 如果有 CustomId，也从 CustomId 索引移除
            if (removed && !string.IsNullOrEmpty(rule.CustomId))
                if (_customRulesById.TryGetValue(rule.CustomId, out var customRuleList))
                {
                    customRuleList.Remove(rule);
                    if (customRuleList.Count == 0) _customRulesById.Remove(rule.CustomId);
                }

            return removed;
        }

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

        #region 分帧处理API

        /// <summary>
        ///     同步一次性处理所有事件（无分帧）
        /// </summary>
        public void Pump()
        {
            if (_isPumping) return;
            _isPumping = true;

            try
            {
                while (_queue.Count > 0)
                {
                    IEventEntry entry = _queue.Dequeue();
                    Process(entry.SourceCard, entry.Event);
                }
            }
            finally
            {
                _isPumping = false;
                
                // 如果启用全局效果池且模式为 AfterPump，刷新效果池
                if (Policy.EnableEffectPool && Policy.EffectPoolFlushMode == EffectPoolFlushMode.AfterPump)
                {
                    FlushEffectPool();
                }
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
                    float elapsedMs = Time.realtimeSinceStartup * 1000f - _frameStartTime;
                    
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
            if (_isBatchProcessing) return;

            _isBatchProcessing = true;
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
            if (!_isBatchProcessing) return;

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
                    _isBatchProcessing = false;
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

                _isBatchProcessing = false;
                batchCompleted = true;
            }

            // 批次完成时刷新效果池（如果配置为 AfterPump）
            if (batchCompleted && Policy.EnableEffectPool && Policy.EffectPoolFlushMode == EffectPoolFlushMode.AfterPump)
            {
                FlushEffectPool();
            }
        }

        #endregion

        /// <summary>
        ///     处理单个事件，匹配规则并执行效果。
        /// </summary>
        private void Process(Card source, ICardEvent evt)
        {
            ProcessCore(source, evt);
        }

        /// <summary>
        ///     核心事件处理逻辑。
        /// </summary>
        private void ProcessCore(Card source, ICardEvent evt)
        {
            var rulesToProcess = new List<CardRule>();

            // 获取匹配事件类型的规则
            if (_rules.TryGetValue(evt.EventType, out var typeRules) && typeRules.Count > 0)
                rulesToProcess.AddRange(typeRules);

            // 对于自定义事件，也检查 EventId 匹配
            if (!string.IsNullOrEmpty(evt.EventId) && evt.EventId != evt.EventType)
                if (_customRulesById.TryGetValue(evt.EventId, out var idRules))
                    rulesToProcess.AddRange(idRules);

            if (rulesToProcess.Count == 0) return;

            // 根据配置选择串行或并行评估
            List<(CardRule rule, List<Card> matched, CardRuleContext ctx, int orderIndex)> evals;
            
            if (Policy.EnableParallelMatching && rulesToProcess.Count >= Policy.ParallelThreshold)
            {
                evals = EvaluateRulesParallel(rulesToProcess, source, evt);
            }
            else
            {
                evals = EvaluateRulesSerial(rulesToProcess, source, evt);
            }

            if (evals.Count == 0) return;

            // 排序规则
            if (Policy.RuleSelection == RuleSelectionMode.Priority)
                evals.Sort((a, b) =>
                {
                    int cmp = a.rule.Priority.CompareTo(b.rule.Priority);
                    return cmp != 0 ? cmp : a.orderIndex.CompareTo(b.orderIndex);
                });
            else
                evals.Sort((a, b) => a.orderIndex.CompareTo(b.orderIndex));

            // 执行规则效果
            ExecuteRules(evals);
        }

        /// <summary>
        ///     串行评估规则（默认模式）。
        /// </summary>
        private List<(CardRule rule, List<Card> matched, CardRuleContext ctx, int orderIndex)> 
            EvaluateRulesSerial(List<CardRule> rules, Card source, ICardEvent evt)
        {
            var evals = new List<(CardRule rule, List<Card> matched, CardRuleContext ctx, int orderIndex)>(rules.Count);
            
            for (int i = 0; i < rules.Count; i++)
            {
                CardRule rule = rules[i];

                CardRuleContext ctx = BuildContext(rule, source, evt);
                if (ctx == null) continue;

                if (EvaluateRequirements(ctx, rule.Requirements, out var matched, i))
                {
                    if ((rule.Policy?.DistinctMatched ?? true) && matched is { Count: > 1 })
                        matched = matched.Distinct().ToList();

                    evals.Add((rule, matched, ctx, i));
                }
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
        private List<(CardRule rule, List<Card> matched, CardRuleContext ctx, int orderIndex)> 
            EvaluateRulesParallel(List<CardRule> rules, Card source, ICardEvent evt)
        {
            var results = new ConcurrentBag<(CardRule rule, List<Card> matched, CardRuleContext ctx, int orderIndex)>();
            
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
                    if ((rule.Policy?.DistinctMatched ?? true) && matched is { Count: > 1 })
                        matched = matched.Distinct().ToList();

                    results.Add((rule, matched, ctx, i));
                }
            });

            return results.ToList();
        }

        /// <summary>
        ///     执行规则效果。
        ///     <para>
        ///         如果启用了全局效果池，效果会被收集到池中而非立即执行。
        ///         池中的效果将在 Pump 结束时或手动刷新时按全局优先级执行。
        ///     </para>
        /// </summary>
        private void ExecuteRules(List<(CardRule rule, List<Card> matched, CardRuleContext ctx, int orderIndex)> evals)
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
        private void ExecuteRulesDirect(List<(CardRule rule, List<Card> matched, CardRuleContext ctx, int orderIndex)> evals)
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
        /// </summary>
        private void CollectEffectsToPool(List<(CardRule rule, List<Card> matched, CardRuleContext ctx, int orderIndex)> evals)
        {
            foreach (var e in evals)
            {
                if (e.matched == null || e.rule.Effects == null || e.rule.Effects.Count == 0)
                    continue;

                // 使用全局顺序索引确保跨事件的顺序一致性
                _globalEffectPool.AddRuleEffects(e.rule, e.ctx, e.matched, _globalOrderIndex++);
            }
        }

        /// <summary>
        ///     刷新全局效果池，按优先级执行所有收集的效果。
        /// </summary>
        /// <returns>执行的效果数量</returns>
        public int FlushEffectPool()
        {
            if (_globalEffectPool.Count == 0) return 0;

            int count = _globalEffectPool.ExecuteAll();
            _globalEffectPool.Clear();
            _globalOrderIndex = 0; // 重置顺序计数器

            return count;
        }

        /// <summary>
        ///     获取全局效果池中待执行的效果数量。
        /// </summary>
        public int PendingEffectCount => _globalEffectPool.Count;

        private bool ExecuteOne((CardRule rule, List<Card> matched, CardRuleContext ctx, int orderIndex) e)
        {
            if (e.matched == null || e.rule.Effects == null || e.rule.Effects.Count == 0) return false;

            foreach (IRuleEffect eff in e.rule.Effects)
            {
                eff.Execute(e.ctx, e.matched);
            }

            return e.rule.Policy?.StopEventOnSuccess == true;
        }

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
                CardFactory,
                rule.MaxDepth,
                this,       // 传递当前引擎实例
                rule        // 传递当前规则
            );
        }

        #endregion

        #region 容器方法

        private bool EvaluateRequirements(CardRuleContext ctx, List<IRuleRequirement> requirements,
                                          out List<Card> matchedAll, int ruleId = -1)
        {
            matchedAll = new();
            if (requirements == null) return true;

            foreach (IRuleRequirement req in requirements)
            {
                if (req == null) return false;

                if (!req.TryMatch(ctx, out var picks)) return false;

                if (picks?.Count > 0) matchedAll.AddRange(picks);
            }

            return true;
        }

        #endregion


        #region 卡牌创建

        /// <summary>
        ///     按ID创建并注册卡牌实例。
        /// </summary>
        public T CreateCard<T>(string id) where T : Card
        {
            if (id == null) throw new ArgumentNullException(nameof(id));

            T card = null;
            if (CardFactory != null) card = CardFactory.Create<T>(id);

            if (card == null) return null;

            // AddCard 会设置 Engine 引用并注册到 CategoryManager
            AddCard(card);

            return card;
        }

        /// <summary>
        ///     按ID创建并注册Card类型的卡牌。
        /// </summary>
        public Card CreateCard(string id) => CreateCard<Card>(id);

        #endregion

        #region 查询服务

        /// <summary>
        ///     按ID和Index精确查找卡牌。
        /// </summary>
        public Card GetCardByKey(string id, int index)
        {
            if (string.IsNullOrEmpty(id)) return null;
            var key = new CardKey(id, index);
            return _cardMap.GetValueOrDefault(key);
        }

        /// <summary>
        ///     按ID返回所有已注册卡牌。
        /// </summary>
        public IEnumerable<Card> GetCardsById(string id)
        {
            if (string.IsNullOrEmpty(id)) yield break;
            if (_cardsById.TryGetValue(id, out var cards))
                foreach (Card card in cards)
                {
                    yield return card;
                }
        }

        /// <summary>
        ///     按ID返回第一个已注册卡牌。
        /// </summary>
        public Card GetCardById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (_cardsById.TryGetValue(id, out var cards) && cards.Count > 0) return cards[0];

            return null;
        }

        /// <summary>
        ///     检查指定卡牌是否接入引擎
        /// </summary>
        /// <returns></returns>
        public bool HasCard(Card card) => GetCardByKey(card.Id, card.Index) == card;

        /// <summary>
        ///     根据 UID 获取卡牌，O(1) 时间复杂度。
        /// </summary>
        /// <param name="uid">卡牌的唯一标识符。</param>
        /// <returns>找到的卡牌，或 null 如果未找到。</returns>
        public Card GetCardByUID(int uid)
        {
            _cardsByUID.TryGetValue(uid, out Card card);
            return card;
        }

        /// <summary>
        ///     按标签查询卡牌 。
        /// </summary>
        /// <param name="tag">要查询的标签。</param>
        /// <returns>包含该标签的所有卡牌列表。</returns>
        public IReadOnlyList<Card> GetCardsByTag(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return Array.Empty<Card>();
            return CategoryManager.GetByTag(tag);
        }

        /// <summary>
        ///     按分类查询卡牌，支持通配符匹配和子分类包含。
        /// </summary>
        /// <param name="pattern">分类名称或通配符模式（如 "Object"、"Creature.*"）。</param>
        /// <param name="includeChildren">是否包含子分类中的卡牌。</param>
        /// <returns>匹配分类的所有卡牌列表。</returns>
        public IReadOnlyList<Card> GetCardsByCategory(string pattern, bool includeChildren = false)
        {
            if (string.IsNullOrEmpty(pattern)) return Array.Empty<Card>();
            return CategoryManager.GetByCategory(pattern, includeChildren);
        }

        /// <summary>
        ///     按分类和标签的交集查询卡牌。
        /// </summary>
        /// <param name="category">分类名称。</param>
        /// <param name="tag">标签名称。</param>
        /// <param name="includeChildren">是否包含子分类。</param>
        /// <returns>同时匹配分类和标签的卡牌列表。</returns>
        public IReadOnlyList<Card> GetCardsByCategoryAndTag(string category, string tag, bool includeChildren = true)
        {
            if (string.IsNullOrEmpty(category) || string.IsNullOrEmpty(tag)) return Array.Empty<Card>();
            return CategoryManager.GetByCategoryAndTag(category, tag, includeChildren);
        }

        #endregion

        #region 卡牌缓存处理

        /// <summary>
        ///     添加卡牌到引擎，分配唯一Index、UID并订阅事件。
        /// </summary>
        public CardEngine AddCard(Card c)
        {
            if (c == null) return this;

            // 设置卡牌的Engine引用
            c.Engine = this;

            string id = c.Id;

            // 第一步: 处理 UID
            if (c.UID < 0)
            {
                // 未分配 UID，分配新的
                EmeCardSystem.CardFactory.AssignUID(c);
            }
            else if (_cardsByUID.ContainsKey(c.UID))
            {
                c.UID = -1; // 重置为未分配状态
                EmeCardSystem.CardFactory.AssignUID(c);
            }

            // 第二步: 分配索引（如果还未分配）
            if (!_idIndexes.TryGetValue(id, out var indexes))
            {
                indexes = new();
                _idIndexes[id] = indexes;
            }

            int assignedIndex = c.Index;
            // 只有当Index不已分配时才分配（Index < 0表示未分配），或者索引已被占用时，需要重新分配
            if (assignedIndex < 0 || indexes.Contains(assignedIndex))
            {
                assignedIndex = 0;
                while (indexes.Contains(assignedIndex))
                {
                    assignedIndex++;
                }

                c.Index = assignedIndex;
            }

            // 第三步: 检查是否已在实际存储中（这时Index经已正常分配）
            var key = new CardKey(c.Id, c.Index);
            if (_cardMap.ContainsKey(key))
                return this; // 已存在，不重复添加

            c.OnEvent += OnCardEvent;

            // 第四步: 添加到索引
            indexes.Add(c.Index);
            var actualKey = new CardKey(c.Id, c.Index);
            _cardMap[actualKey] = c;

            // 添加到 UID 索引
            _cardsByUID[c.UID] = c;

            // 更新_cardsById缓存
            if (!_cardsById.TryGetValue(id, out var cardList))
            {
                cardList = new();
                _cardsById[id] = cardList;
            }

            cardList.Add(c);

            // 第五步: 注册到 CategoryManager（同时处理标签）
            RegisterToCategoryManager(c);

            // 第六步: 将卡牌的所有标签加入TargetSelector缓存
            foreach (string tag in c.Tags)
            {
                TargetSelector.OnCardTagAdded(c, tag);
            }

            return this;
        }

        /// <summary>
        ///     移除卡牌，移除事件订阅、UID 映射与索引。
        /// </summary>
        public CardEngine RemoveCard(Card c)
        {
            if (c == null) return this;

            var key = new CardKey(c.Id, c.Index);
            if (_cardMap.TryGetValue(key, out Card existing) && ReferenceEquals(existing, c))
            {
                _cardMap.Remove(key);
                c.OnEvent -= OnCardEvent;

                // 从 UID 索引中移除
                if (c.UID >= 0) _cardsByUID.Remove(c.UID);

                if (_idIndexes.TryGetValue(c.Id, out var indexes))
                {
                    indexes.Remove(c.Index);
                    if (indexes.Count == 0) _idIndexes.Remove(c.Id);
                }

                // 更新_cardsById缓存
                if (_cardsById.TryGetValue(c.Id, out var cardList))
                {
                    cardList.Remove(c);
                    if (cardList.Count == 0) _cardsById.Remove(c.Id);
                }

                // 从TargetSelector缓存中移除卡牌的标签（在注销前获取标签）
                var tagsToRemove = c.Tags.ToList();
                
                // 从 CategoryManager 注销
                UnregisterFromCategoryManager(c);
                
                // 移除标签缓存
                foreach (string tag in tagsToRemove)
                {
                    TargetSelector.OnCardTagRemoved(c, tag);
                }
            }

            return this;
        }

        public void ClearAllCards()
        {
            // 从 CategoryManager 批量注销所有卡牌
            foreach (var card in _cardMap.Values)
            {
                UnregisterFromCategoryManager(card);
            }

            _cardMap.Clear();
            _idIndexes.Clear();
            _cardsById.Clear();
            _cardsByUID.Clear();
        }

        #endregion

        #region CategoryManager 集成

        /// <summary>
        ///     将卡牌注册到 CategoryManager，提取分类并应用所有标签（默认标签+额外标签）。
        /// </summary>
        /// <param name="card">要注册的卡牌。</param>
        private void RegisterToCategoryManager(Card card)
        {
            if (card == null) return;
            
            string category = ExtractCategoryPath(card);

            // 注册实体
            var registration = CategoryManager.RegisterEntity(card, category);

            // 应用运行时的DefaultTags（来自CardData）
            if (card.Data?.DefaultTags != null)
            {
                foreach (string tag in card.Data.DefaultTags)
                {
                    if (!string.IsNullOrWhiteSpace(tag))
                    {
                        registration = registration.WithTags(tag);
                    }
                }
            }

            // 待应用
            if (card.PendingExtraTags != null && card.PendingExtraTags.Count > 0)
            {
                foreach (string tag in card.PendingExtraTags)
                {
                    if (!string.IsNullOrWhiteSpace(tag))
                    {
                        registration = registration.WithTags(tag);
                    }
                }
                // 清空临时标签列表
                card.PendingExtraTags = null;
            }

            // 完成注册
            var result = registration.Complete();
            if (!result.IsSuccess)
            {
                Debug.LogWarning($"[CardEngine] CategoryManager 注册失败: Card={card.Id}#{card.Index}, Error={result.ErrorMessage}");
            }
        }

        /// <summary>
        ///     从 CategoryManager 注销卡牌。
        /// </summary>
        /// <param name="card">要注销的卡牌。</param>
        private void UnregisterFromCategoryManager(Card card)
        {
            if (card == null) return;

            string entityId = $"{card.Id}#{card.Index}";
            var result = CategoryManager.DeleteEntity(entityId);
            if (!result.IsSuccess && result.ErrorCode != ErrorCode.NotFound)
            {
                Debug.LogWarning($"[CardEngine] CategoryManager 注销失败: Card={entityId}, Error={result.ErrorMessage}");
            }
        }

        /// <summary>
        ///     从卡牌数据中提取分类路径字符串。
        ///     将 CardCategory 枚举转换为层级路径格式（如 "Object"、"Action" 等）。
        /// </summary>
        /// <param name="card">卡牌实例。</param>
        /// <returns>分类路径字符串。</returns>
        private static string ExtractCategoryPath(Card card)
        {
            if (card?.Data == null)
            {
                return CardData.DEFAULT_CATEGORY;
            }

            // 优先使用新的 DefaultCategory，如果为空则回退到 DEFAULT_CATEGORY
            return string.IsNullOrEmpty(card.Data.Category) 
                ? CardData.DEFAULT_CATEGORY 
                : card.Data.Category;
        }

        #endregion
    }

    public readonly struct CardKey : IEquatable<CardKey>
    {
        public readonly string Id;
        public readonly int Index;

        public CardKey(string id, int index)
        {
            Id = id ?? string.Empty;
            Index = index;
        }

        public bool Equals(CardKey other) =>
            string.Equals(Id, other.Id, StringComparison.Ordinal) && Index == other.Index;

        public override bool Equals(object obj) => obj is CardKey k && Equals(k);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + StringComparer.Ordinal.GetHashCode(Id ?? string.Empty);
                hash = hash * 31 + Index;
                return hash;
            }
        }

        public override string ToString() => $"{Id}#{Index}";
    }
}