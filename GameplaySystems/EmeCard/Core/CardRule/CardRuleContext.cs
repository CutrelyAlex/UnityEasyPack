namespace EasyPack
{
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

        /// <summary>产卡工厂。</summary>
        public ICardFactory Factory;

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
}