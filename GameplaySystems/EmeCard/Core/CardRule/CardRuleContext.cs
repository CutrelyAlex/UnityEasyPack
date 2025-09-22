namespace EasyPack
{
    /// <summary>
    /// 规则执行上下文：为效果提供触发源、容器与原始事件等信息。
    /// </summary>
    public sealed class CardRuleContext
    {
        /// <summary>触发该规则的卡牌（事件源）。</summary>
        public Card Source;

        /// <summary>用于匹配与执行的容器（由规则的 OwnerHops 选择）。</summary>
        public Card Container;

        /// <summary>原始事件载体（包含类型、ID、数据等）。</summary>
        public CardEvent Event;

        /// <summary>产卡工厂。</summary>
        public ICardFactory Factory;

        /// <summary>
        /// 匹配时是否递归搜索容器子树（默认 false）。
        /// </summary>
        public bool RecursiveSearch;

        /// <summary>
        /// 递归搜索最大深度（>0 生效，1 表示仅子级一层）。
        /// </summary>
        public int MaxDepth;

        public float DeltaTime
        {
            get
            {
                if (Event.Type == CardEventType.Tick && Event.Data is float f)
                    return f;
                return 0f;
            }
        }

        public string EventId => Event.ID;
        public Card DataCard => Event.Data as Card;
        public T DataAs<T>() where T : class => Event.Data as T;

        public bool TryGetData<T>(out T value)
        {
            if (Event.Data is T v)
            {
                value = v;
                return true;
            }
            value = default;
            return false;
        }
    }
}