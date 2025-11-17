using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EasyPack.EmeCardSystem
{
    public sealed class CardEngine
    {
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

        private bool _isPumping = false;

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

        public bool IsBatchProcessing => _isBatchProcessing;

        #endregion

        #region 事件和缓存
        private struct EventEntry
        {
            public Card Source;
            public CardEvent Event;
            public bool IsProcessed;
            public EventEntry(Card s, CardEvent e)
            {
                Source = s;
                Event = e;
                IsProcessed = false;
            }
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
        // id->Card列表缓存，用于快速查找
        private readonly Dictionary<string, List<Card>> _cardsById = new();
        // Custom规则按ID分组缓存
        private readonly Dictionary<string, List<CardRule>> _customRulesById = new();

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

            // 如果是Custom规则且有CustomId，添加到分组缓存
            if (rule.Trigger == CardEventType.Custom && !string.IsNullOrEmpty(rule.CustomId))
            {
                if (!_customRulesById.TryGetValue(rule.CustomId, out var ruleList))
                {
                    ruleList = new List<CardRule>();
                    _customRulesById[rule.CustomId] = ruleList;
                }
                ruleList.Add(rule);
            }
        }

        /// <summary>
        /// 从引擎中注销一条规则。
        /// </summary>
        /// <param name="rule">要注销的规则实例。</param>
        /// <returns>如果成功注销返回true，否则返回false。</returns>
        public bool UnregisterRule(CardRule rule)
        {
            if (rule == null) return false;

            bool removed = _rules[rule.Trigger].Remove(rule);

            // 如果是Custom规则且有CustomId，从分组缓存移除
            if (removed && rule.Trigger == CardEventType.Custom && !string.IsNullOrEmpty(rule.CustomId))
            {
                if (_customRulesById.TryGetValue(rule.CustomId, out var ruleList))
                {
                    ruleList.Remove(rule);
                    if (ruleList.Count == 0)
                    {
                        _customRulesById.Remove(rule.CustomId);
                    }
                }
            }

            return removed;
        }

        /// <summary>
        /// 卡牌事件回调，入队并驱动事件处理。
        /// </summary>
        private void OnCardEvent(Card source, CardEvent evt)
        {
            _queue.Enqueue(new EventEntry(source, evt));

            // 分帧模式下不自动Pump，等待下一帧主动调用PumpFrame
            // 非分帧模式保持原有即时处理行为
            if (!_isPumping && !Policy.EnableFrameDistribution)
            {
                Pump();
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

                float frameBudget = Policy.FrameBudgetMs;
                int maxEvents = Policy.MaxEventsPerFrame;
                int minEvents = Policy.MinEventsPerFrame;

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
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[CardEngine] PumpFrameWithBudget 错误: {ex}");
                }
            }
        }

        /// <summary>
        /// 初始化时间限制批次处理
        /// 必须调用此方法后，才能在Update中调用PumpTimeLimitedBatch()
        /// </summary>
        /// <param name="timeLimitSeconds">自定义时间限制（秒），null则使用Policy.BatchTimeLimitSeconds</param>
        public void BeginTimeLimitedBatch(float? timeLimitSeconds = null)
        {
            if (_isBatchProcessing)
            {
                return;
            }

            _isBatchProcessing = true;
            _batchStartTime = Time.realtimeSinceStartup;
            _batchTimeLimit = timeLimitSeconds ?? Policy.BatchTimeLimitSeconds;
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

            float elapsed = Time.realtimeSinceStartup - _batchStartTime;
            float remaining = _batchTimeLimit - elapsed;

            if (remaining > 0)
            {
                // 分帧阶段：使用帧预算处理事件
                float frameStart = Time.realtimeSinceStartup;
                float frameBudgetSec = Policy.FrameBudgetMs / 1000f;
                int maxEvents = Policy.MaxEventsPerFrame;

                int processedInFrame = 0;
                while (_queue.Count > 0 &&
                       (Time.realtimeSinceStartup - frameStart) < frameBudgetSec &&
                       processedInFrame < maxEvents)
                {
                    var entry = _queue.Dequeue();
                    Process(entry.Source, entry.Event);
                    processedInFrame++;
                }

                // 处理后检查队列是否已清空
                if (_queue.Count == 0)
                {
                    _isBatchProcessing = false;
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
            }
        }

        #endregion

        /// <summary>
        /// 处理单个事件，匹配规则并执行效果。
        /// </summary>
        private void Process(Card source, CardEvent evt)
        {
            ProcessCore(source, evt);
        }

        /// <summary>
        /// 核心事件处理逻辑。
        /// </summary>
        private void ProcessCore(Card source, CardEvent evt)
        {
            List<CardRule> rulesToProcess;

            if (evt.Type == CardEventType.Custom)
            {
                rulesToProcess = new List<CardRule>();

                // 添加匹配CustomId的规则
                if (_customRulesById.TryGetValue(evt.ID, out var matchedRules))
                {
                    rulesToProcess.AddRange(matchedRules);
                }
            }
            else
            {
                rulesToProcess = _rules[evt.Type];
            }

            if (rulesToProcess == null || rulesToProcess.Count == 0) return;

            var evals = new List<(CardRule rule, List<Card> matched, CardRuleContext ctx, int orderIndex)>(rulesToProcess.Count);
            for (int i = 0; i < rulesToProcess.Count; i++)
            {
                var rule = rulesToProcess[i];

                var ctx = BuildContext(rule, source, evt);
                if (ctx == null) continue;

                if (EvaluateRequirements(ctx, rule.Requirements, out var matched, i))
                {
                    if ((rule.Policy?.DistinctMatched ?? true) && matched != null && matched.Count > 1)
                    {
                        matched = matched.Distinct().ToList();
                    }

                    evals.Add((rule, matched, ctx, i));
                }
            }

            if (evals.Count == 0) return;

            // 排序规则
            if (Policy.RuleSelection == RuleSelectionMode.Priority)
            {
                evals.Sort((a, b) =>
                {
                    int cmp = a.rule.Priority.CompareTo(b.rule.Priority);
                    return cmp != 0 ? cmp : a.orderIndex.CompareTo(b.orderIndex);
                });
            }
            else
            {
                evals.Sort((a, b) => a.orderIndex.CompareTo(b.orderIndex));
            }

            var ordered = evals;

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

        private bool EvaluateRequirements(CardRuleContext ctx, List<IRuleRequirement> requirements, out List<Card> matchedAll, int ruleId = -1)
        {
            matchedAll = new List<Card>();
            if (requirements == null || requirements.Count == 0)
            {
                return true;
            }

            foreach (var req in requirements)
            {
                if (req == null)
                {
                    return false;
                }
                if (!req.TryMatch(ctx, out var picks))
                {
                    return false;
                }
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

            // 设置卡牌的Engine引用以支持缓存清除
            card.Engine = this;

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
            if (_cardsById.TryGetValue(id, out var cards))
            {
                foreach (var card in cards)
                {
                    yield return card;
                }
            }
        }
        /// <summary>
        /// 按ID返回第一个已注册卡牌。
        /// </summary>
        public Card GetCardById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (_cardsById.TryGetValue(id, out var cards) && cards.Count > 0)
            {
                return cards[0];
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

                // 更新_cardsById缓存
                if (!_cardsById.TryGetValue(id, out var cardList))
                {
                    cardList = new List<Card>();
                    _cardsById[id] = cardList;
                }
                cardList.Add(c);
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
                {
                    _cardMap.Remove(key);
                }

                if (_idIndexes.TryGetValue(c.Id, out var indexes))
                {
                    indexes.Remove(c.Index);
                    if (indexes.Count == 0)
                    {
                        _idIndexes.Remove(c.Id);
                    }
                }

                // 更新_cardsById缓存
                if (_cardsById.TryGetValue(c.Id, out var cardList))
                {
                    cardList.Remove(c);
                    if (cardList.Count == 0)
                    {
                        _cardsById.Remove(c.Id);
                    }
                }
            }
            return this;
        }

        public void ClearAllCards()
        {
            _cardMap.Clear();
            _idIndexes.Clear();
            _cardsById.Clear();
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
