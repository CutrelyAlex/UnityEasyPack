using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace EasyPack
{
    public sealed class CardEngine
    {
        public CardEngine(ICardFactory factory)
        {
            CardFactory = factory;
            foreach (CardEventType t in Enum.GetValues(typeof(CardEventType)))
                _rules[t] = new List<CardRule>();
        }

        #region 基本属性
        public ICardFactory CardFactory { get; set; }
        public EnginePolicy Policy { get; } = new EnginePolicy();

        private bool _isPumping = false;
        #endregion

        #region 事件和缓存
        private struct EventEntry
        {
            public Card Source;
            public CardEvent Event;
            public EventEntry(Card s, CardEvent e) { Source = s; Event = e; }
        }
        private readonly Dictionary<CardEventType, List<CardRule>> _rules = new();
        private readonly Queue<EventEntry> _queue = new();
        private readonly HashSet<Card> _attachedCards = new();
        private readonly HashSet<Card> _registeredCards = new();
        private readonly Dictionary<CardKey, Card> _cardMap = new();
        #endregion

        #region 规则处理

        public void RegisterRule(CardRule rule)
        {
            if (rule == null) throw new ArgumentNullException(nameof(rule));
            _rules[rule.Trigger].Add(rule);
        }

        public CardEngine Attach(Card card)
        {
            if (card == null) throw new ArgumentNullException(nameof(card));
            _attachedCards.Add(card);
            return this;
        }
        public CardEngine Detach(Card card)
        {
            if (card == null) throw new ArgumentNullException(nameof(card));
            _attachedCards.Remove(card);
            return this;
        }

        private void OnCardEvent(Card source, CardEvent evt)
        {
            _queue.Enqueue(new EventEntry(source, evt));
            if (!_isPumping) Pump();
        }

        public void Pump(int maxEvents = int.MaxValue)
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
                _isPumping = false;
            }
        }

        private void Process(Card source, CardEvent evt)
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
            return new CardRuleContext
            {
                Source = source,
                Container = container,
                Event = evt,
                Factory = CardFactory,
                MaxDepth = rule.MaxDepth
            };
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
        public T CreateCard<T>(string id)where T:Card
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

            RegisterCard(card);

            return card;
        }

        public Card CreateCard(string id)
        {
            return CreateCard<Card>(id);
        }
        #endregion

        #region 查询服务
        public Card GetCardByKey(string id, int index)
        {
            if (string.IsNullOrEmpty(id)) return null;
            var key = new CardKey(id, index);
            if (_cardMap.TryGetValue(key, out var c)) return c;

            return null;
        }

        public IEnumerable<Card> GetCardsById(string id)
        {
            if (string.IsNullOrEmpty(id)) yield break;
            foreach (var kv in _cardMap)
            {
                if (string.Equals(kv.Key.Id, id, StringComparison.Ordinal))
                    yield return kv.Value;
            }
        }

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
        private void RegisterCard(Card c)
        {
            if (c == null) return;
            if (_registeredCards.Add(c))
            {
                c.OnEvent += OnCardEvent;

                var key = new CardKey(c.Id, c.Index);
                if (_cardMap.TryGetValue(key, out var existing))
                {
                    if (!ReferenceEquals(existing, c))
                    {
                        int next = 0;
                        while (_cardMap.ContainsKey(new CardKey(c.Id, next))) next++;
                        c.Index = next;
                        key = new CardKey(c.Id, c.Index);
                    }
                    else
                    {
                        _cardMap[key] = c;
                        return;
                    }
                }
                _cardMap[key] = c;
            }
        }

        private void UnregisterCard(Card c)
        {
            if (c == null) return;
            if (_registeredCards.Remove(c))
            {
                c.OnEvent -= OnCardEvent;
                var key = new CardKey(c.Id, c.Index);
                if (_cardMap.TryGetValue(key, out var existing) && ReferenceEquals(existing, c))
                    _cardMap.Remove(key);
            }
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