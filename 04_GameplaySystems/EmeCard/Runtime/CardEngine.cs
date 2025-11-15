using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EasyPack.EmeCardSystem
{
    public sealed class CardEngine
    {
        public event Action<PumpMetricsSnapshot> PumpMetricsUpdated;
        public event Action<Card, CardEvent> EventProcessingStarted;
        public event Action<Card, CardEvent> EventProcessingEnded;
        public CardEngine(ICardFactory factory)
        {
            CardFactory = factory;
            factory.Owner = this;
            foreach (CardEventType t in Enum.GetValues(typeof(CardEventType)))
                _rules[t] = new List<CardRule>();
        }

        #region 基本属性
        public ICardFactory CardFactory { get; set; }
        /// <summary>
        /// 引擎全局策略
        /// </summary>
        public EnginePolicy Policy { get; } = new EnginePolicy();

        /// <summary>
        /// 分帧处理配置
        /// </summary>
        public FrameDistributionConfig FrameDistributionConfig { get; } = new FrameDistributionConfig();

        private bool _isPumping = false;
        // 延迟事件队列
        private readonly Queue<EventEntry> _deferredQueue = new();
        private int _processingDepth = 0; // 处理深度

        // 分帧处理相关
        private float _frameStartTime; // 当前帧开始处理的时间（毫秒）
        private int _frameProcessedCount; // 当前帧已处理事件数

        // 时间限制分帧机制
        private bool _isBatchProcessing = false;
        private float _batchStartTime;
        private float _batchTimeLimit;

        /// <summary>
        /// 获取当前队列中待处理的事件数量
        /// </summary>
        public int PendingEventCount => _queue.Count;

        /// <summary>
        /// 获取延迟队列中的事件数量
        /// </summary>
        public int DeferredEventCount => _deferredQueue.Count;

        // RESEARCH: 获取批次处理状态用于研究验证
        public bool IsBatchProcessing => _isBatchProcessing;
        #endregion

        #region 事件和缓存
        private struct EventEntry
        {
            public Card Source;
            public CardEvent Event;
            public EventEntry(Card s, CardEvent e) { Source = s; Event = e; }
        }
        // 规则表
        private readonly Dictionary<CardEventType, List<CardRule>> _rules = new();
        // 卡牌事件队列
        private readonly Queue<EventEntry> _queue = new();
        // 已注册的卡牌集合
        private readonly HashSet<Card> _registeredCards = new();
        // 卡牌Key->Card缓存
        private readonly Dictionary<CardKey, Card> _cardMap = new();
        // id->index集合缓存
        private readonly Dictionary<string, HashSet<int>> _idIndexes = new();

        #endregion

        #region 规则处理
        /// <summary>
        /// 注册一条规则到引擎。
        /// </summary>
        /// <param name="rule">规则实例。</param>
        public void RegisterRule(CardRule rule)
        {
            if (rule == null) throw new ArgumentNullException(nameof(rule));
            _rules[rule.Trigger].Add(rule);
        }

        /// <summary>
        /// 从引擎中注销一条规则。
        /// </summary>
        /// <param name="rule">要注销的规则实例。</param>
        /// <returns>如果成功注销返回true，否则返回false。</returns>
        public bool UnregisterRule(CardRule rule)
        {
            if (rule == null) return false;
            return _rules[rule.Trigger].Remove(rule);
        }

        /// <summary>
        /// 卡牌事件回调，入队并驱动事件处理。
        /// </summary>
        private void OnCardEvent(Card source, CardEvent evt)
        {
            // 如果正在处理事件，新事件进入延迟队列
            if (_processingDepth > 0)
            {
                _deferredQueue.Enqueue(new EventEntry(source, evt));
            }
            else
            {
                _queue.Enqueue(new EventEntry(source, evt));

                // 分帧模式下不自动Pump，等待下一帧主动调用PumpFrame
                // 非分帧模式保持原有即时处理行为
                if (!_isPumping && !Policy.EnableFrameDistribution)
                {
                    Pump();
                }
            }
        }

        #region 分帧处理API

        /// <summary>
        /// 同步一次性处理所有事件（无分帧）
        /// </summary>
        public void Pump()
        {
            if (_isPumping) return;
            _isPumping = true;
            int processed = 0;

            try
            {
                _frameStartTime = Time.realtimeSinceStartup * 1000f;

                while (_queue.Count > 0)
                {
                    var entry = _queue.Dequeue();
                    Process(entry.Source, entry.Event);
                    processed++;
                }
            }
            finally
            {
                _isPumping = false;

                try
                {
                    float elapsedMs = (Time.realtimeSinceStartup * 1000f) - _frameStartTime;
                    PumpMetricsUpdated?.Invoke(new PumpMetricsSnapshot
                    {
                        EventsProcessed = processed,
                        FrameTimeMs = elapsedMs,
                        PendingEvents = _queue.Count,
                        Timestamp = DateTime.UtcNow
                    });
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[CardEngine] 分帧快照记录出现异常: {ex}");
                }
            }
        }

        /// <summary>
        /// 按帧时间预算处理事件
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

                float frameBudget = FrameDistributionConfig.FrameBudgetMs;
                int maxEvents = FrameDistributionConfig.MaxEventsPerFrame;
                int minEvents = FrameDistributionConfig.MinEventsPerFrame;

                while (_queue.Count > 0 && processed < maxEvents)
                {
                    // 检查时间预算（但至少处理MinEventsPerFrame个事件）
                    if (_frameProcessedCount >= minEvents)
                    {
                        float elapsed = (Time.realtimeSinceStartup * 1000f) - _frameStartTime;
                        if (elapsed >= frameBudget)
                        {
                            // 超时，保留剩余到下一帧
                            break;
                        }
                    }

                    var entry = _queue.Dequeue();
                    Process(entry.Source, entry.Event);
                    processed++;
                    _frameProcessedCount++;
                }
            }
            finally
            {
                _isPumping = false;

                try
                {
                    float elapsedMs = (Time.realtimeSinceStartup * 1000f) - _frameStartTime;
                    PumpMetricsUpdated?.Invoke(new PumpMetricsSnapshot
                    {
                        EventsProcessed = processed,
                        FrameTimeMs = elapsedMs,
                        PendingEvents = _queue.Count,
                        Timestamp = DateTime.UtcNow
                    });
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[CardEngine] PumpFrameWithBudget metrics handler threw: {ex}");
                }
            }
        }

        /// <summary>
        /// 初始化时间限制批次处理
        /// 必须调用此方法后，才能在Update中调用PumpTimeLimitedBatch()
        /// </summary>
        /// <param name="timeLimitSeconds">自定义时间限制（秒），null则使用FrameDistributionConfig.BatchTimeLimitSeconds</param>
        public void BeginTimeLimitedBatch(float? timeLimitSeconds = null)
        {
            if (_isBatchProcessing)
            {
                Debug.LogWarning("[CardEngine] 批次处理已在进行中，忽略新请求");
                return;
            }

            _isBatchProcessing = true;
            _batchStartTime = Time.realtimeSinceStartup;
            _batchTimeLimit = timeLimitSeconds ?? FrameDistributionConfig.BatchTimeLimitSeconds;

            Debug.Log($"[CardEngine] 开始时间限制批次处理 - 时间限制: {_batchTimeLimit}s, 初始事件数: {_queue.Count}");
        }

        /// <summary>
        /// 驱动时间限制批次处理（在Update中调用）
        /// 必须先调用BeginTimeLimitedBatch()初始化
        /// 在指定时间内分帧处理，超时后同步完成所有
        /// </summary>
        public void PumpTimeLimitedBatch()
        {
            if (!_isBatchProcessing)
            {
                return;
            }

            if (_queue.Count == 0)
            {
                float totalTime = Time.realtimeSinceStartup - _batchStartTime;
                Debug.Log($"[CardEngine] 时间限制批次处理完成 - 总耗时: {totalTime:F3}s");
                _isBatchProcessing = false;
                return;
            }

            float elapsed = Time.realtimeSinceStartup - _batchStartTime;
            float remaining = _batchTimeLimit - elapsed;

            if (remaining > 0)
            {
                // 分帧阶段：使用帧预算处理事件
                float frameStart = Time.realtimeSinceStartup;
                float frameBudgetSec = FrameDistributionConfig.FrameBudgetMs / 1000f;
                int maxEvents = FrameDistributionConfig.MaxEventsPerFrame;

                int processedInFrame = 0;
                while (_queue.Count > 0 &&
                       (Time.realtimeSinceStartup - frameStart) < frameBudgetSec &&
                       processedInFrame < maxEvents)
                {
                    var entry = _queue.Dequeue();
                    Process(entry.Source, entry.Event);
                    processedInFrame++;
                }
            }
            else
            {
                // 超时：同步完成所有剩余事件
                while (_queue.Count > 0)
                {
                    var entry = _queue.Dequeue();
                    Process(entry.Source, entry.Event);
                }

                _isBatchProcessing = false;
                Debug.Log("[CardEngine] 批次处理超时，已同步完成所有剩余事件");
            }
        }

        #endregion

        /// <summary>
        /// 处理单个事件，匹配规则并执行效果。
        /// </summary>
        private void Process(Card source, CardEvent evt)
        {
            _processingDepth++; // 进入处理，期间触发的事件会进入延迟队列

            // notify profiling listeners
            try
            {
                EventProcessingStarted?.Invoke(source, evt);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CardEngine] EventProcessingStarted handler threw: {ex}");
            }
            try
            {
                var rules = _rules[evt.Type];
                if (rules == null || rules.Count == 0) return;

                // 预分配容量减少扩容
                var evals = new List<(CardRule rule, List<Card> matched, CardRuleContext ctx, int orderIndex)>(rules.Count);
                for (int i = 0; i < rules.Count; i++)
                {
                    var rule = rules[i];

                    if (evt.Type == CardEventType.Custom &&
                        !string.IsNullOrEmpty(rule.CustomId) &&
                        !string.Equals(rule.CustomId, evt.ID, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    var ctx = BuildContext(rule, source, evt);
                    if (ctx == null) continue;

                    if (EvaluateRequirements(ctx, rule.Requirements, out var matched))
                    {
                        if ((rule.Policy?.DistinctMatched ?? true) && matched != null && matched.Count > 1)
                            matched = matched.Distinct().ToList();

                        evals.Add((rule, matched, ctx, i));
                    }
                }

                if (evals.Count == 0) return;

                IEnumerable<(CardRule rule, List<Card> matched, CardRuleContext ctx, int orderIndex)> ordered =
                    Policy.RuleSelection == RuleSelectionMode.Priority
                        ? evals.OrderBy(e => e.rule.Priority).ThenBy(e => e.orderIndex)
                        : evals.OrderBy(e => e.orderIndex);

                if (Policy.FirstMatchOnly)
                {
                    var first = ordered.First();
                    ExecuteOne(first);
                }
                else
                {
                    foreach (var e in ordered)
                    {
                        if (ExecuteOne(e)) break;
                    }
                }

                // Process deferred events immediately
                while (_deferredQueue.Count > 0)
                {
                    var deferredEvent = _deferredQueue.Dequeue();
                    Process(deferredEvent.Source, deferredEvent.Event);
                }
            }
            finally
            {
                _processingDepth--;

                try
                {
                    EventProcessingEnded?.Invoke(source, evt);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[CardEngine] EventProcessingEnded handler threw: {ex}");
                }
            }
        }

        private bool ExecuteOne((CardRule rule, List<Card> matched, CardRuleContext ctx, int orderIndex) e)
        {
            if (e.rule.Effects == null || e.rule.Effects.Count == 0) return false;
            foreach (var eff in e.rule.Effects)
                eff.Execute(e.ctx, e.matched);
            return e.rule.Policy?.StopEventOnSuccess == true;
        }

        private CardRuleContext BuildContext(CardRule rule, Card source, CardEvent evt)
        {
            var container = SelectContainer(rule.OwnerHops, source);
            if (container == null) return null;
            return new CardRuleContext(
                source: source,
                container: container,
                evt: evt,
                factory: CardFactory,
                maxDepth: rule.MaxDepth
            );
        }
        #endregion

        #region 容器方法
        private static Card SelectContainer(int ownerHops, Card source)
        {
            if (source == null) return null;

            if (ownerHops == 0) return source;

            if (ownerHops < 0)
            {
                var curr = source;
                while (curr.Owner != null) curr = curr.Owner;
                return curr;
            }

            var node = source;
            int hops = ownerHops;
            while (hops > 0 && node.Owner != null)
            {
                node = node.Owner;
                hops--;
            }
            return node ?? source;
        }

        private static bool EvaluateRequirements(CardRuleContext ctx, List<IRuleRequirement> requirements, out List<Card> matchedAll)
        {
            matchedAll = new List<Card>();
            if (requirements == null || requirements.Count == 0) return true;

            foreach (var req in requirements)
            {
                if (req == null) return false;
                if (!req.TryMatch(ctx, out var picks)) return false;
                if (picks != null && picks.Count > 0) matchedAll.AddRange(picks);
            }
            return true;
        }
        #endregion

        #region 卡牌创建
        /// <summary>
        /// 按ID创建并注册卡牌实例。
        /// </summary>
        public T CreateCard<T>(string id) where T : Card
        {
            if (id == null) throw new ArgumentNullException(nameof(id));

            T card = null;
            if (CardFactory != null)
            {
                card = CardFactory.Create<T>(id);
            }

            if (card == null)
            {
                return null;
            }

            AddCard(card);

            return card;
        }
        /// <summary>
        /// 按ID创建并注册Card类型的卡牌。
        /// </summary>
        public Card CreateCard(string id)
        {
            return CreateCard<Card>(id);
        }
        #endregion

        #region 查询服务
        /// <summary>
        /// 按ID和Index精确查找卡牌。
        /// </summary>
        public Card GetCardByKey(string id, int index)
        {
            if (string.IsNullOrEmpty(id)) return null;
            var key = new CardKey(id, index);
            if (_cardMap.TryGetValue(key, out var c)) return c;

            return null;
        }
        /// <summary>
        /// 按ID返回所有已注册卡牌。
        /// </summary>
        public IEnumerable<Card> GetCardsById(string id)
        {
            if (string.IsNullOrEmpty(id)) yield break;
            foreach (var kv in _cardMap)
            {
                if (string.Equals(kv.Key.Id, id, StringComparison.Ordinal))
                    yield return kv.Value;
            }
        }
        /// <summary>
        /// 按ID返回第一个已注册卡牌。
        /// </summary>
        public Card GetCardById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;

            foreach (var kv in _cardMap)
            {
                if (string.Equals(kv.Key.Id, id, StringComparison.Ordinal))
                    return kv.Value;
            }

            return null;
        }
        /// <summary>
        /// 检查指定卡牌是否接入引擎
        /// </summary>
        /// <returns></returns>
        public bool HasCard(Card card)
        {
            return GetCardByKey(card.Id, card.Index) == card;
        }
        #endregion

        #region 卡牌缓存处理
        /// <summary>
        /// 添加卡牌到引擎，分配唯一Index并订阅事件。
        /// </summary>
        public CardEngine AddCard(Card c)
        {
            if (c == null) return this;
            if (_registeredCards.Add(c))
            {
                c.OnEvent += OnCardEvent;

                var id = c.Id;
                if (!_idIndexes.TryGetValue(id, out var indexes))
                {
                    indexes = new HashSet<int>();
                    _idIndexes[id] = indexes;
                }

                int next = c.Index;
                if (next < 0 || indexes.Contains(next))
                {
                    next = 0;
                    while (indexes.Contains(next)) next++;
                    c.Index = next;
                }

                indexes.Add(c.Index);
                var key = new CardKey(c.Id, c.Index);
                _cardMap[key] = c;
            }
            return this;
        }
        /// <summary>
        /// 移除卡牌，移除事件订阅与索引。
        /// </summary>
        public CardEngine RemoveCard(Card c)
        {
            if (c == null) return this;
            if (_registeredCards.Remove(c))
            {
                c.OnEvent -= OnCardEvent;
                var key = new CardKey(c.Id, c.Index);
                if (_cardMap.TryGetValue(key, out var existing) && ReferenceEquals(existing, c))
                    _cardMap.Remove(key);

                if (_idIndexes.TryGetValue(c.Id, out var indexes))
                {
                    indexes.Remove(c.Index);
                    if (indexes.Count == 0)
                        _idIndexes.Remove(c.Id);
                }
            }
            return this;
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

        public bool Equals(CardKey other)
        {
            return string.Equals(Id, other.Id, StringComparison.Ordinal) && Index == other.Index;
        }

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

/// <summary>
/// Pump metrics snapshot emitted by CardEngine after each Pump invocation
/// </summary>
public struct PumpMetricsSnapshot
{
    public int EventsProcessed { get; set; }
    public float FrameTimeMs { get; set; }
    public int PendingEvents { get; set; }
    public DateTime Timestamp { get; set; }
}
