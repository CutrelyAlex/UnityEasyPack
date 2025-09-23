using System;
using System.Collections.Generic;
using System.Linq;

namespace EasyPack
{
    /// <summary>
    /// 规则引擎：
    /// - 通过订阅卡牌事件接入事件流（入队）；
    /// - 使用内部队列按 FIFO 顺序依次处理事件，避免重入（效果触发的新事件会排在后面）；
    /// - 按事件类型分派规则，进行容器选择与要求项匹配；
    /// - 命中时执行效果管线（效果可进行消耗/产出等操作）。
    /// </summary>
    public sealed class CardRuleEngine
    {
        private readonly Dictionary<CardEventType, List<CardRule>> _rules = new Dictionary<CardEventType, List<CardRule>>();

        /// <summary>可选的卡牌工厂，供产卡类效果使用。</summary>
        public ICardFactory CardFactory { get; set; }

        /// <summary>
        /// 引擎策略
        /// </summary>
        public EnginePolicy Policy { get; } = new EnginePolicy();

        
        private readonly Queue<EventEntry> _pending = new Queue<EventEntry>();
        private bool _isPumping = false;
        // 事件队列与泵
        private struct EventEntry
        {
            public Card Source;
            public CardEvent Event;
            public EventEntry(Card s, CardEvent e) { Source = s; Event = e; }
        }

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

        private void Process(Card source, CardEvent evt)
        {
            var rules = _rules[evt.Type];
            if (rules == null || rules.Count == 0) return;

            // 记录所有命中与上下文快照（按注册顺序）
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

                var container = SelectContainer(rule.OwnerHops, source);
                if (container == null) continue;

                var ctx = new CardRuleContext
                {
                    Source = source,
                    Container = container,
                    Event = evt,
                    Factory = CardFactory,
                    MaxDepth = rule.MaxDepth
                };

                if (TryMatch(ctx, rule.Requirements, out var matched))
                {
                    // 规则去重
                    if ((rule.Policy?.DistinctMatched ?? true) && matched != null && matched.Count > 1)
                        matched = matched.Distinct().ToList();

                    evals.Add((rule, matched, ctx, i));
                }
            }

            if (evals.Count == 0) return;

            if (Policy.FirstMatchOnly)
            {
                // 只执行具体度最高的一条
                var winner = SelectMostSpecific(evals);
                if (winner != null && winner.Value.rule.Effects != null)
                {
                    foreach (var eff in winner.Value.rule.Effects)
                        eff.Execute(winner.Value.ctx, winner.Value.matched);
                }
            }
            else
            {
                // 执行全部命中（按注册顺序）
                foreach (var e in evals.OrderBy(e => e.orderIndex))
                {
                    if (e.rule.Effects == null) continue;
                    foreach (var eff in e.rule.Effects)
                        eff.Execute(e.ctx, e.matched);
                }
            }
        }

        private static (CardRule rule, List<Card> matched, CardRuleContext ctx, int orderIndex)? SelectMostSpecific(
           List<(CardRule rule, List<Card> matched, CardRuleContext ctx, int orderIndex)> evals)
        {
            if (evals == null || evals.Count == 0) return null;

            return evals
                .OrderByDescending(e => ComputeSpecificity(e.rule))
                .ThenBy(e => e.orderIndex)
                .First();
        }

        // “具体度”评分：材料越多/需求越明确，得分越高。可被 RulePolicy.SpecificityOverride 覆盖
        private static int ComputeSpecificity(CardRule rule)
        {
            var ov = rule.Policy?.SpecificityOverride;
            if (ov.HasValue) return ov.Value;

            int score = 0;
            if (rule.Requirements == null) return score;

            foreach (var req in rule.Requirements)
            {
                if (req is CardRequirement cr)
                {
                    switch (cr.TargetKind)
                    {
                        case TargetKind.ById:
                        case TargetKind.ByTag:
                        case TargetKind.ByCategory:
                        case TargetKind.ByIdRecursive:
                        case TargetKind.ByTagRecursive:
                        case TargetKind.ByCategoryRecursive:
                        case TargetKind.ContainerChildren:
                        case TargetKind.ContainerDescendants:
                            score += Math.Max(cr.MinCount, 1);
                            if (cr.Root == RequirementRoot.Container) score += 1;
                            break;
                        default:
                            break;
                    }
                }
            }
            return score;
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