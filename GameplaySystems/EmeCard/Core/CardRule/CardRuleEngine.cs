using System;
using System.Collections.Generic;
using System.Linq;

namespace EasyPack
{
    /// <summary>
    /// 规则引擎：
    /// - 通过订阅卡牌事件接入事件流（入队）；
    /// - 使用内部队列按 FIFO 顺序依次处理事件，避免重入（效果触发的新事件会排在后面）；
    /// - 按事件类型分派规则，进行作用域选择与要求项匹配；
    /// - 命中时执行效果管线（效果可进行消耗/产出等操作）。
    /// </summary>
    public sealed class CardRuleEngine
    {
        private readonly Dictionary<CardEventType, List<CardRule>> _rules = new Dictionary<CardEventType, List<CardRule>>();

        /// <summary>可选的卡牌工厂，供产卡类效果使用。</summary>
        public ICardFactory CardFactory { get; set; }

        // 事件队列与泵
        private struct EventEntry
        {
            public Card Source;
            public CardEvent Event;
            public EventEntry(Card s, CardEvent e) { Source = s; Event = e; }
        }
        private readonly Queue<EventEntry> _pending = new Queue<EventEntry>();
        private bool _isPumping = false;

        public CardRuleEngine(ICardFactory factory = null)
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

        public void Attach(Card card) => card.OnEvent += OnCardEvent;
        public void Detach(Card card) => card.OnEvent -= OnCardEvent;

        // 接收卡牌事件：统一入队，必要时自动泵
        private void OnCardEvent(Card source, CardEvent evt)
        {
            _pending.Enqueue(new EventEntry(source, evt));
            if (!_isPumping) Pump();
        }

        /// <summary>
        /// 主动驱动队列（通常无需手动调用）。可用于限制每帧处理事件数量。
        /// </summary>
        /// <param name="maxEvents">本次最多处理的事件数，默认尽可能多。</param>
        public void Pump(int maxEvents = int.MaxValue)
        {
            if (_isPumping) return;
            _isPumping = true;
            int processed = 0;
            try
            {
                while (_pending.Count > 0 && processed < maxEvents)
                {
                    var entry = _pending.Dequeue();
                    Process(entry.Source, entry.Event);
                    processed++;
                }
            }
            finally
            {
                _isPumping = false;
            }
        }

        // 处理单个事件 -> 规则分派/匹配/执行
        private void Process(Card source, CardEvent evt)
        {
            var rules = _rules[evt.Type];
            if (rules == null || rules.Count == 0) return;

            foreach (var rule in rules)
            {
                if (evt.Type == CardEventType.Custom &&
                    !string.IsNullOrEmpty(rule.CustomId) &&
                    !string.Equals(rule.CustomId, evt.ID, StringComparison.Ordinal))
                {
                    continue;
                }

                var container = SelectContainer(rule.Scope, source, rule.OwnerHops);
                if (container == null) continue;

                var ctx = new CardRuleContext
                {
                    Source = source,
                    Container = container,
                    Event = evt,
                    Factory = CardFactory,
                    RecursiveSearch = rule.Recursive,
                    MaxDepth = rule.MaxDepth
                };

                if (TryMatch(ctx, rule.Requirements, out var matched))
                {
                    if (rule.Effects != null && rule.Effects.Count > 0)
                    {
                        foreach (var eff in rule.Effects)
                            eff.Execute(ctx, matched);
                    }
                }
            }
        }

        private static Card SelectContainer(RuleScope scope, Card source, int ownerHops)
        {
            if (source == null) return null;

            if (scope == RuleScope.Self || ownerHops == 0)
                return source;

            if (scope == RuleScope.Owner)
            {
                if (ownerHops < 0)
                {
                    // 到最顶层 Root
                    var curr = source;
                    while (curr.Owner != null) curr = curr.Owner;
                    return curr;
                }

                int hops = Math.Max(1, ownerHops);
                var node = source;
                while (hops > 0 && node.Owner != null)
                {
                    node = node.Owner;
                    hops--;
                }
                // 若没有 Owner，退回自身
                return node ?? source;
            }

            return source;
        }

        private static bool TryMatch(CardRuleContext ctx, List<IRuleRequirement> requirements, out List<Card> matchedAll)
        {
            matchedAll = new List<Card>();
            if (requirements == null || requirements.Count == 0) return true; // 无要求项视为命中

            foreach (var req in requirements)
            {
                if (req == null) return false;

                if (!req.TryMatch(ctx, out var picks))
                    return false;

                if (picks != null && picks.Count > 0)
                    matchedAll.AddRange(picks);
            }

            return true;
        }
    }
}