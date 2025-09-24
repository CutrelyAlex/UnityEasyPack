using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace EasyPack
{
    public sealed class CardEngine
    {
        private readonly Dictionary<CardEventType, List<CardRule>> _rules = new Dictionary<CardEventType, List<CardRule>>();
        public ICardFactory CardFactory { get; set; }

        /// <summary>引擎策略</summary>
        public EnginePolicy Policy { get; } = new EnginePolicy();

        private bool _isPumping = false;

        // 事件封装
        private struct EventEntry
        {
            public Card Source;
            public CardEvent Event;
            public EventEntry(Card s, CardEvent e) { Source = s; Event = e; }
        }
        private readonly Queue<EventEntry> _queue = new Queue<EventEntry>();

        public CardEngine(ICardFactory factory = null)
        {
            CardFactory = factory;
            foreach (CardEventType t in Enum.GetValues(typeof(CardEventType)))
                _rules[t] = new List<CardRule>();
        }

        public void RegisterRule(CardRule rule)
        {
            if (rule == null) throw new ArgumentNullException(nameof(rule));
            _rules[rule.Trigger].Add(rule);
        }

        public CardEngine Attach(Card card) { card.OnEvent += OnCardEvent; return this; }
        public CardEngine Detach(Card card) { card.OnEvent -= OnCardEvent; return this; }

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

            // 评估阶段：记录所有命中与上下文快照（按注册顺序）
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

                if (TryMatch(ctx, rule.Requirements, out var matched))
                {
                    if ((rule.Policy?.DistinctMatched ?? true) && matched != null && matched.Count > 1)
                        matched = matched.Distinct().ToList();

                    evals.Add((rule, matched, ctx, i));
                }
            }

            if (evals.Count == 0) return;

            // 排序：按注册顺序或优先级
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
                    if (ExecuteOne(e)) break; // 支持规则级 StopEventOnSuccess
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

        // 基于 OwnerHops 选择容器：0=Self，1=Owner，N>1 上溯，-1=Root
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

        private static bool TryMatch(CardRuleContext ctx, List<IRuleRequirement> requirements, out List<Card> matchedAll)
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
    }
}