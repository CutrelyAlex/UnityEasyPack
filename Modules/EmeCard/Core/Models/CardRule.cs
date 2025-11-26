using System.Collections.Generic;

namespace EasyPack.EmeCardSystem
{
    /// <summary>
    ///     数据驱动的卡牌规则。
    /// </summary>
    public class CardRule
    {
        /// <summary>
        ///     事件触发类型（字符串标识符，如 "Tick"、"Use"、"Collision"）。
        ///     <para>
        ///         使用字符串而非枚举，支持任意自定义事件类型。
        ///         标准事件类型定义在 <see cref="CardEventTypes" /> 中。
        ///     </para>
        /// </summary>
        public string EventType = CardEventTypes.ADDED_TO_OWNER;

        /// <summary>
        ///     自定义事件 ID（可选，用于更精确的事件匹配）。
        /// </summary>
        public string CustomId;

        /// <summary>容器锚点选择：0=Self，1=Owner（默认），N>1 上溯，-1=Root。</summary>
        public int OwnerHops = 1;

        /// <summary>递归选择的最大深度（仅对递归类 TargetKind 生效）。</summary>
        public int MaxDepth = int.MaxValue;

        /// <summary>
        ///     规则优先级（数值越小优先级越高）。当引擎Policy选择模式为 Priority 时生效。
        /// </summary>
        public int Priority = 0;

        /// <summary>匹配条件集合（与关系）。</summary>
        public List<IRuleRequirement> Requirements = new();

        /// <summary>命中后执行的效果管线。</summary>
        public List<IRuleEffect> Effects = new();

        /// <summary>规则执行策略。</summary>
        public RulePolicy Policy { get; set; } = new();

        /// <summary>
        ///     创建空规则。
        /// </summary>
        public CardRule() { }

        /// <summary>
        ///     创建指定事件类型的规则。
        /// </summary>
        /// <param name="eventType">事件类型标识符。</param>
        public CardRule(string eventType) => EventType = eventType;
    }
}