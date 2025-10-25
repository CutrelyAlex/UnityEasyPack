namespace EasyPack.EmeCardSystem
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

        /// <summary>
        /// 从 Tick 事件中获取时间增量（DeltaTime）。
        /// 仅当事件类型为 Tick 且数据为 float 时返回有效值，否则返回 0。
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

        /// <summary>获取事件的 ID。</summary>
        public string EventId => Event.ID;

        /// <summary>将事件数据作为 Card 类型返回（失败返回 null）。</summary>
        public Card DataCard => Event.Data as Card;

        /// <summary>
        /// 将事件数据作为指定 Card 子类型返回。
        /// </summary>
        /// <typeparam name="T">目标卡牌类型。</typeparam>
        /// <returns>转换后的卡牌对象，失败返回 null。</returns>
        public T DataCardAs<T>() where T : Card => Event.Data as T;

        /// <summary>
        /// 将触发源卡牌转换为指定类型。
        /// </summary>
        /// <typeparam name="T">目标卡牌类型。</typeparam>
        /// <returns>转换后的卡牌对象，失败返回 null。</returns>
        public T GetSource<T>() where T : Card => Source as T;

        /// <summary>
        /// 将容器卡牌转换为指定类型。
        /// </summary>
        /// <typeparam name="T">目标卡牌类型。</typeparam>
        /// <returns>转换后的卡牌对象，失败返回 null。</returns>
        public T GetContainer<T>() where T : Card => Container as T;

        /// <summary>
        /// 将事件数据作为指定引用类型返回。
        /// </summary>
        /// <typeparam name="T">目标引用类型。</typeparam>
        /// <returns>转换后的对象，失败返回 null。</returns>
        public T DataAs<T>() where T : class => Event.Data as T;

        /// <summary>
        /// 从事件数据数组中获取指定索引的元素（引用类型）。
        /// </summary>
        /// <typeparam name="T">目标引用类型。</typeparam>
        /// <param name="i">数组索引。</param>
        /// <returns>转换后的对象，失败返回 null。</returns>
        public T DataAs<T>(int i) where T : class => DataAs<object[]>()[i] as T;

        /// <summary>
        /// 将事件数据作为指定值类型返回。
        /// </summary>
        /// <typeparam name="T">目标值类型。</typeparam>
        /// <returns>转换后的值。</returns>
        public T DataIs<T>() where T : struct => (T)Event.Data;

        /// <summary>
        /// 从事件数据数组中获取指定索引的元素（值类型）。
        /// </summary>
        /// <typeparam name="T">目标值类型。</typeparam>
        /// <param name="i">数组索引。</param>
        /// <returns>转换后的值。</returns>
        public T DataIs<T>(int i) where T : struct => (T)DataAs<object[]>()[i];

        /// <summary>
        /// 尝试安全地获取事件数据为指定类型。
        /// </summary>
        /// <typeparam name="T">目标类型。</typeparam>
        /// <param name="value">输出参数，获取成功时为转换后的值，失败时为默认值。</param>
        /// <returns>转换成功返回 true，否则返回 false。</returns>
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

        /// <summary>
        /// 尝试从事件数据数组中获取指定索引的元素。
        /// </summary>
        /// <typeparam name="T">目标类型。</typeparam>
        /// <param name="i">数组索引。</param>
        /// <param name="value">输出参数，获取成功时为转换后的值，失败时为默认值。</param>
        /// <returns>转换成功且索引有效返回 true，否则返回 false。</returns>
        public bool TryGetData<T>(int i, out T value)
        {
            if (Event.Data is object[] array && i >= 0 && i < array.Length && array[i] is T v)
            {
                value = v;
                return true;
            }
            value = default;
            return false;
        }

        public override string ToString()
        {
            return "CardRuleContext:\n" +
                   $"  Source: {Source}\n" +
                   $"  Container: {Container}\n" +
                   $"  Event: Type={Event.Type}, ID={Event.ID}, Data={Event.Data}\n" +
                   $"  Factory: {Factory}\n" +
                   $"  MaxDepth: {MaxDepth}\n" +
                   $"  DeltaTime: {DeltaTime}";
        }
    }
}
