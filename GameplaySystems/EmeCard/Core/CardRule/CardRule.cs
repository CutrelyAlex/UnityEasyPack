using System.Collections.Generic;

namespace EasyPack
{
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
    /// - 指定触发事件（<see cref="Trigger"/>），必要时用 <see cref="CustomId"/> 过滤自定义事件；
    /// - 在 <see cref="Scope"/> 指定的容器中，用 <see cref="Requirements"/> 做模式匹配；
    /// - 命中后执行效果管线（<see cref="Effects"/>）。需要产卡/移除/修改属性等请通过具体效果实现（例如 <c>CreateCardsEffect</c>）。
    /// </summary>
    public sealed class CardRule
    {
        /// <summary>
        /// 触发该规则的事件类型（Tick/Use/Custom 等）。
        /// </summary>
        public CardEventType Trigger;

        /// <summary>
        /// 当 <see cref="Trigger"/> 为 <see cref="CardEventType.Custom"/> 时，用于基于事件 ID 进行过滤。
        /// 为空表示不做 ID 过滤（同类事件均生效）。
        /// </summary>
        public string CustomId;

        /// <summary>
        /// 规则的匹配与执行作用域（Self/Owner）。
        /// </summary>
        public RuleScope Scope = RuleScope.Owner;

        /// <summary>
        /// 当 Scope=Owner 时：向上取第 N 级持有者。
        /// 取值说明：1=直接Owner（默认）、0=等价于 Self、-1=一直取到最顶层Root、N>1=沿Owner链上溯N层（不足则停在最顶层）。
        /// </summary>
        public int OwnerHops = 1;

        /// <summary>
        /// 是否在容器内递归搜索子级（匹配阶段）。默认 false 表示仅一层 Children。
        /// </summary>
        public bool Recursive = false;

        /// <summary>
        /// 递归搜索的最大深度（>0 生效，1 表示仅子级一层；int.MaxValue 表示不限深）。
        /// </summary>
        public int MaxDepth = int.MaxValue;


        /// <summary>
        /// 匹配条件集合（与关系）。项类型为 <see cref="IRuleRequirement"/>，可使用
        /// <see cref="CardRequirement"/>，也可自定义扩展。
        /// </summary>
        public List<IRuleRequirement> Requirements = new List<IRuleRequirement>();

        /// <summary>
        /// 命中后执行的效果管线（非产卡副作用，如修改属性、移除卡、日志等），
        /// 产卡请使用对应的效果实现（例如 <c>CreateCardsEffect</c>）。
        /// </summary>
        public List<IRuleEffect> Effects = new List<IRuleEffect>();
    }
}
