using System;
using System.Collections.Generic;
using System.Linq;

namespace EasyPack
{
    /// <summary>
    /// 规则引擎：
    /// - 通过订阅卡牌事件（<see cref="Attach(Card)"/>）接入事件流；
    /// - 按事件类型分派规则，进行作用域选择与条件匹配；
    /// - 命中时执行效果管线（效果可自行进行消耗/产出等操作，如 RemoveCardsEffect/CreateCardsEffect）。
    /// </summary>
    public sealed class CardRuleEngine
    {
        private readonly Dictionary<CardEventType, List<CardRule>> _rules = new Dictionary<CardEventType, List<CardRule>>();

        /// <summary>
        /// 可选的卡牌工厂，供产卡类效果使用（如 <c>CreateCardsEffect</c>）。
        /// 不设置则不产卡，但其他效果仍可执行。
        /// </summary>
        public ICardFactory CardFactory { get; set; }

        /// <summary>
        /// 创建规则引擎实例。
        /// </summary>
        /// <param name="factory">可选的卡牌工厂，用于生成效果产物。</param>
        public CardRuleEngine(ICardFactory factory = null)
        {
            CardFactory = factory;
            foreach (CardEventType t in Enum.GetValues(typeof(CardEventType)))
                _rules[t] = new List<CardRule>();
        }

        /// <summary>
        /// 注册一条规则。
        /// </summary>
        /// <param name="rule">要注册的规则实例。</param>
        public void RegisterRule(CardRule rule)
        {
            if (rule == null) throw new ArgumentNullException(nameof(rule));
            _rules[rule.Trigger].Add(rule);
        }

        /// <summary>
        /// 接入/断开卡牌事件。
        /// </summary>
        public void Attach(Card card) => card.OnEvent += Handle;
        public void Detach(Card card) => card.OnEvent -= Handle;

        // 处理卡牌事件 -> 规则分派/匹配/执行
        private void Handle(Card source, CardEvent evt)
        {
            var rules = _rules[evt.Type];
            if (rules == null || rules.Count == 0) return;

            foreach (var rule in rules)
            {
                if ((evt.Type == CardEventType.Custom || evt.Type == CardEventType.Condition) &&
                    !string.IsNullOrEmpty(rule.CustomId) &&
                    !string.Equals(rule.CustomId, evt.ID, StringComparison.Ordinal))
                {
                    continue;
                }

                var container = SelectContainer(rule.Scope, source);
                if (container == null) continue;

                if (TryMatch(container, source, rule, out var matched))
                {
                    var ctx = new CardRuleContext
                    {
                        Source = source,
                        Container = container,
                        Event = evt,
                        Factory = CardFactory
                    };

                    // 执行效果管线
                    if (rule.Effects != null && rule.Effects.Count > 0)
                    {
                        foreach (var eff in rule.Effects)
                            eff.Execute(ctx, matched);
                    }
                }
            }
        }

        private static Card SelectContainer(RuleScope scope, Card source)
        {
            switch (scope)
            {
                case RuleScope.Self: return source;
                case RuleScope.Owner: return source.Owner ?? source;
                default: return source;
            }
        }

        private static bool TryMatch(Card container, Card source, CardRule rule, out List<Card> matched)
        {
            matched = new List<Card>();
            IEnumerable<Card> poolBase = container.Children;

            foreach (var req in rule.Requirements)
            {
                IEnumerable<Card> pool = poolBase;

                if (req.IncludeSelf)
                {
                    if (ReferenceEquals(container, source)) pool = pool.Concat(new[] { container });
                    else if (ReferenceEquals(container, source.Owner)) pool = pool.Concat(new[] { source });
                }

                var picks = new List<Card>();
                foreach (var c in pool)
                {
                    if (req.Matches(c))
                    {
                        picks.Add(c);
                        if (picks.Count >= req.MinCount) break;
                    }
                }

                if (picks.Count < req.MinCount)
                {
                    matched.Clear();
                    return false;
                }

                matched.AddRange(picks);
            }

            return true;
        }
    }
}