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
        // 延迟事件队列
        private readonly Queue<EventEntry> _deferredQueue = new();
        private int _processingDepth = 0; // 处理深度
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
                if (!_isPumping) Pump();
            }
        }
        /// <summary>
        /// 事件主循环，依次处理队列中的所有事件。
        /// </summary>
        /// <param name="maxEvents">最大处理事件数。</param>
        public void Pump(int maxEvents = 2048)
        {
            if (_isPumping) return;
            _isPumping = true;
            int processed = 0;
            try
            {
                while (_queue.Count > 0 && processed < maxEvents)
                {
                    var entry = _queue.Dequeue();
                    Process(entry.Source, entry.Event);
                    processed++;
                }
            }
            finally
            {
                if (processed >= maxEvents)
                {
                    Debug.Log($"一个动作就调用了{maxEvents}次处理，这绝对死循环了吧");
                }
                _isPumping = false;
            }
        }

        /// <summary>
        /// 处理单个事件，匹配规则并执行效果。
        /// </summary>
        private void Process(Card source, CardEvent evt)
        {
            _processingDepth++; // 进入处理，期间触发的事件会进入延迟队列
            try
            {
                var rules = _rules[evt.Type];
                if (rules == null || rules.Count == 0) return;

                var evals = new List<(CardRule rule, List<Card> matched, CardRuleContext ctx, int orderIndex)>();
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
            }
            finally
            {
                _processingDepth--;
                
                // 如果处理完成（深度回到0），将延迟队列的事件批量移入主队列
                if (_processingDepth == 0 && _deferredQueue.Count > 0)
                {
                    while (_deferredQueue.Count > 0)
                    {
                        _queue.Enqueue(_deferredQueue.Dequeue());
                    }
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
