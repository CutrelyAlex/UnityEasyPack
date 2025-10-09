namespace EasyPack
{
    /// <summary>
    /// 规则执行上下文：为效果提供触发源、容器与原始事件等信息。
    /// </summary>
    public sealed class CardRuleContext
    {
        /// <summary>
        /// 构造函数：创建规则上下文实例。
        /// </summary>
        /// <param name="source">触发该规则的卡牌（事件源）</param>
        /// <param name="container">用于匹配与执行的容器</param>
        /// <param name="evt">原始事件载体</param>
        /// <param name="factory">产卡工厂</param>
        /// <param name="maxDepth">递归搜索最大深度</param>
        public CardRuleContext(Card source, Card container, CardEvent evt, ICardFactory factory, int maxDepth)
        {
            Source = source;
            Container = container;
            Event = evt;
            Factory = factory;
            MaxDepth = maxDepth;
        }

        /// <summary>触发该规则的卡牌（事件源）。</summary>
        public Card Source { get; }

        /// <summary>用于匹配与执行的容器（由规则的 OwnerHops 选择）。</summary>
        public Card Container { get; }

        /// <summary>原始事件载体（包含类型、ID、数据等）。</summary>
        public CardEvent Event { get; }

        /// <summary>产卡工厂。</summary>
        public ICardFactory Factory { get; }

        /// <summary>
        /// 递归搜索最大深度（>0 生效，1 表示仅子级一层）。
        /// </summary>
        public int MaxDepth { get; }

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