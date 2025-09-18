using CardModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EasyPack
{
    /// <summary>
    /// 匹配条件类型：用于规则在容器中筛选卡牌的方式。
    /// </summary>
    public enum MatchKind
    {
        /// <summary>按标签匹配。</summary>
        Tag,
        /// <summary>按卡牌 ID 精确匹配。</summary>
        Id,
        /// <summary>按类别匹配。</summary>
        Category
    }

    /// <summary>
    /// 单个卡牌匹配条件。
    /// </summary>
    public sealed class CardRequirement
    {
        /// <summary>
        /// 匹配方式（标签/ID/类别）。
        /// </summary>
        public MatchKind Kind;

        /// <summary>
        /// 当 <see cref="Kind"/> 为 <see cref="MatchKind.Tag"/> 或 <see cref="MatchKind.Id"/> 时的匹配值。
        /// </summary>
        public string Value;

        /// <summary>
        /// 当 <see cref="Kind"/> 为 <see cref="MatchKind.Category"/> 时指定的类别。
        /// </summary>
        public CardCategory? Category;

        /// <summary>
        /// 该条件需要匹配的卡牌最小数量。
        /// </summary>
        public int MinCount = 1;

        /// <summary>
        /// 是否允许将“触发源”一并纳入候选集合（取决于规则作用域）。
        /// </summary>
        public bool IncludeSelf = false;

        /// <summary>
        /// 判断指定卡牌是否满足该条件。
        /// </summary>
        /// <param name="c">待检查的卡牌。</param>
        /// <returns>满足则返回 true。</returns>
        public bool Matches(Card c)
        {
            switch (Kind)
            {
                case MatchKind.Tag: return !string.IsNullOrEmpty(Value) && c.HasTag(Value);
                case MatchKind.Id: return !string.IsNullOrEmpty(Value) && string.Equals(c.Id, Value, StringComparison.Ordinal);
                case MatchKind.Category: return Category.HasValue && c.Category == Category.Value;
                default: return false;
            }
        }
    }

    /// <summary>
    /// 规则作用域：决定在何处进行匹配与执行。
    /// </summary>
    public enum RuleScope
    {
        /// <summary>在触发源自身作为容器进行匹配（包含其 Children）。</summary>
        Self,
        /// <summary>在触发源的持有者作为容器进行匹配（常用于“制作/交互”等）。</summary>
        Owner
    }

    /// <summary>
    /// 数据驱动的卡牌规则：
    /// - 指定触发事件（<see cref="Trigger"/>），必要时用 <see cref="CustomId"/> 过滤自定义/条件事件；
    /// - 在 <see cref="Scope"/> 指定的容器中，用 <see cref="Requirements"/> 做模式匹配；
    /// - 命中后可选择消耗输入（<see cref="ConsumeInputs"/>）、产出卡（<see cref="OutputCardIds"/>）或执行效果管线（<see cref="Effects"/>）。
    /// </summary>
    public sealed class CardRule
    {
        /// <summary>
        /// 触发该规则的事件类型（Tick/Use/Custom/Condition 等）。
        /// </summary>
        public CardEventType Trigger;

        /// <summary>
        /// 当 <see cref="Trigger"/> 为 <see cref="CardEventType.Custom"/> 或 <see cref="CardEventType.Condition"/> 时，用于基于事件 ID 进行过滤。
        /// 为空表示不做 ID 过滤（同类事件均生效）。
        /// </summary>
        public string CustomId;

        /// <summary>
        /// 规则的匹配与执行作用域（Self/Owner）。
        /// </summary>
        public RuleScope Scope = RuleScope.Owner;

        /// <summary>
        /// 匹配条件集合。所有条件均需满足（与关系）。
        /// 每个条件可要求最小匹配数量（MinCount）。
        /// </summary>
        public List<CardRequirement> Requirements = new List<CardRequirement>();

        /// <summary>
        /// 命中后是否消耗匹配到的输入卡牌（固有子卡不会被移除）。
        /// </summary>
        public bool ConsumeInputs = false;

        /// <summary>
        /// 需要产出的卡牌 ID 列表。留空表示不产卡。
        /// 需在引擎上配置 <see cref="CardRuleEngine.CardFactory"/> 才能创建。
        /// </summary>
        public List<string> OutputCardIds = new List<string>();

        /// <summary>
        /// 命中后执行的效果管线（非产卡副作用，如修改属性、移除卡、日志等）。
        /// </summary>
        public List<IRuleEffect> Effects = new List<IRuleEffect>();
    }

    /// <summary>
    /// 规则执行上下文：为效果提供触发源、容器与原始事件等信息。
    /// </summary>
    public sealed class CardRuleContext
    {
        /// <summary>触发该规则的卡牌（事件源）。</summary>
        public Card Source;

        /// <summary>用于匹配与执行的容器（取决于 <see cref="CardRule.Scope"/>）。</summary>
        public Card Container;

        /// <summary>原始事件载体（包含类型、ID、数据等）。</summary>
        public CardEvent Event;

        /// <summary>
        /// 便捷访问 Tick 的 deltaTime（秒）。
        /// 非 Tick 事件时返回 0。
        /// </summary>
        public float DeltaTime
        {
            get
            {
                if (Event.Type == CardEventType.Tick && Event.Data is float f)
                    return f;
                return 0f;
            }
        }
    }

    /// <summary>
    /// 规则引擎：
    /// - 通过订阅卡牌事件（<see cref="Attach(Card)"/>）接入事件流；
    /// - 按事件类型分派规则，进行作用域选择与条件匹配；
    /// - 命中时可消耗输入、产出卡（通过 <see cref="CardFactory"/>）并执行效果管线。
    /// </summary>
    public sealed class CardRuleEngine
    {
        private readonly Dictionary<CardEventType, List<CardRule>> _rules = new Dictionary<CardEventType, List<CardRule>>();

        /// <summary>
        /// 可选的卡牌工厂，用于规则产出卡牌（<see cref="CardRule.OutputCardIds"/>）。
        /// 不设置则规则不会产卡，但仍可执行效果。
        /// </summary>
        public ICardFactory CardFactory { get; set; }

        /// <summary>
        /// 创建规则引擎实例。
        /// </summary>
        /// <param name="factory">可选的卡牌工厂，用于生成规则产物。</param>
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
        /// 接入一张卡牌的事件流（订阅 <see cref="Card.OnEvent"/>）。
        /// </summary>
        /// <param name="card">需要被引擎监听的卡牌。</param>
        public void Attach(Card card) => card.OnEvent += Handle;

        /// <summary>
        /// 断开一张卡牌的事件流（取消订阅 <see cref="Card.OnEvent"/>）。
        /// </summary>
        /// <param name="card">不再被引擎监听的卡牌。</param>
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
                        Event = evt
                    };

                    // 1) 消耗匹配输入（固有子卡不会被移除）
                    if (rule.ConsumeInputs && matched.Count > 0)
                    {
                        foreach (var c in matched)
                            container.RemoveChild(c, force: false);
                    }

                    // 2) 生成输出（如果配置了，且工厂可用）
                    if (rule.OutputCardIds != null && rule.OutputCardIds.Count > 0 && CardFactory != null)
                    {
                        foreach (var outId in rule.OutputCardIds)
                        {
                            var created = CardFactory.Create(outId);
                            if (created != null) container.AddChild(created);
                        }
                    }

                    // 3) 执行效果管线（支持“无产卡”的一切副作用）
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