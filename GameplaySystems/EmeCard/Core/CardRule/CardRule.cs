using System.Collections.Generic;

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
        /// 匹配条件集合（与关系）。项类型为 <see cref="IRuleRequirement"/>，可使用
        /// <see cref="CardRequirement"/> 或 <see cref="ConditionRequirement"/>，也可自定义扩展。
        /// </summary>
        public List<IRuleRequirement> Requirements = new List<IRuleRequirement>();

        /// <summary>
        /// 命中后执行的效果管线（非产卡副作用，如修改属性、移除卡、日志等）。
        /// </summary>
        public List<IRuleEffect> Effects = new List<IRuleEffect>();
    }
}