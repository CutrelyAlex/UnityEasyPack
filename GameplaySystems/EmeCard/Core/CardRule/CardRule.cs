using System.Collections.Generic;

namespace EasyPack
{
    /// <summary>
    /// 数据驱动的卡牌规则：
    /// - 指定触发事件（<see cref="Trigger"/>），必要时用 <see cref="CustomId"/> 过滤自定义事件；
    /// - 容器锚点由 <see cref="OwnerHops"/> 决定：0=Self，1=Owner，N>1=向上N级，-1=Root；
    /// - 在容器中，用 <see cref="Requirements"/> 做模式匹配；
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
        /// 容器锚点选择：0=Self，1=Owner（默认），N>1=沿 Owner 链上溯 N 层，-1=最顶层 Root。
        /// </summary>
        public int OwnerHops = 1;

        /// <summary>
        /// 递归搜索的最大深度（>0 生效，1 表示仅子级一层；int.MaxValue 表示不限深）。
        /// </summary>
        public int MaxDepth = int.MaxValue;

        /// <summary>
        /// 匹配条件集合（与关系）。项类型为 <see cref="IRuleRequirement"/>，可使用
        /// <see cref="CardRequirement"/>，也可自定义扩展。
        /// </summary>
        public List<IRuleRequirement> Requirements = new List<IRuleRequirement> ();

        /// <summary>
        /// 命中后执行的效果管线
        /// </summary>
        public List<IRuleEffect> Effects = new List<IRuleEffect>();
    }
}