using System.Collections.Generic;

namespace EasyPack.EmeCardSystem
{
    /// <summary>
    /// 数据驱动的卡牌规则。
    /// </summary>
    public class CardRule
    {
        /// <summary>
        /// 事件触发类型。
        /// </summary>
        public CardEventType Trigger;
        public string CustomId;

        /// <summary>容器锚点选择：0=Self，1=Owner（默认），N>1 上溯，-1=Root。</summary>
        public int OwnerHops = 1;

        /// <summary>递归选择的最大深度（仅对递归类 TargetKind 生效）。</summary>
        public int MaxDepth = int.MaxValue;

        /// <summary>
        /// 规则优先级（数值越小优先级越高）。当引擎Policy选择模式为 Priority 时生效。
        /// </summary>
        public int Priority = 0;

        /// <summary>匹配条件集合（与关系）。</summary>
        public List<IRuleRequirement> Requirements = new();

        /// <summary>命中后执行的效果管线。</summary>
        public List<IRuleEffect> Effects = new();

        /// <summary>规则执行策略。</summary>
        public RulePolicy Policy { get; set; } = new RulePolicy();
    }
}
