namespace EasyPack.EmeCardSystem
{
    /// <summary>
    ///     标准事件类型注册表：定义常用的强类型事件。
    ///     <para>
    ///         使用 CardEventDefinition&lt;T&gt; 创建类型安全的事件。
    ///         自定义事件可以通过 Custom&lt;T&gt;() 方法或直接创建 CardEventDefinition。
    ///     </para>
    /// </summary>
    /// <example>
    ///     // 使用标准事件
    ///     var tickEvent = CardEventTypes.Tick.Create(deltaTime);
    ///     // 创建自定义事件类型
    ///     public static readonly CardEventDefinition&lt;CollisionData&gt; Collision
    ///     = CardEventTypes.Define&lt;CollisionData&gt;("Collision");
    /// </example>
    public static class CardEventTypes
    {
        #region 事件类型常量

        /// <summary>Tick 事件类型标识符</summary>
        public const string TICK = "Tick";

        /// <summary>添加到容器事件类型标识符</summary>
        public const string ADDED_TO_OWNER = "AddedToOwner";

        /// <summary>从容器移除事件类型标识符</summary>
        public const string REMOVED_FROM_OWNER = "RemovedFromOwner";

        /// <summary>使用卡牌事件类型标识符</summary>
        public const string USE = "Use";

        #endregion

        #region 系统事件

        /// <summary>
        ///     Tick 事件：携带时间增量（deltaTime）。
        /// </summary>
        public static readonly CardEventDefinition<float> Tick = new(TICK);

        #endregion

        #region 生命周期事件

        /// <summary>
        ///     添加到容器事件：携带目标容器卡牌。
        /// </summary>
        public static readonly CardEventDefinition<Card> AddedToOwner = new(ADDED_TO_OWNER);

        /// <summary>
        ///     从容器移除事件：携带原容器卡牌。
        /// </summary>
        public static readonly CardEventDefinition<Card> RemovedFromOwner = new(REMOVED_FROM_OWNER);

        #endregion

        #region 使用事件

        /// <summary>
        ///     使用卡牌事件：携带使用目标（可为 null）。
        /// </summary>
        public static readonly CardEventDefinition<Card> Use = new(USE);

        #endregion

        #region 事件类型工厂

        /// <summary>
        ///     定义自定义事件类型。
        /// </summary>
        /// <typeparam name="TData">事件数据类型。</typeparam>
        /// <param name="eventType">事件类型标识符。</param>
        /// <returns>事件类型定义。</returns>
        public static CardEventDefinition<TData> Define<TData>(string eventType) => new(eventType);

        /// <summary>
        ///     定义无数据的自定义事件类型。
        /// </summary>
        /// <param name="eventType">事件类型标识符。</param>
        /// <returns>事件类型定义（数据类型为 object）。</returns>
        public static CardEventDefinition<object> Define(string eventType) => new(eventType);

        /// <summary>
        ///     快速创建事件。
        /// </summary>
        /// <typeparam name="TData">事件数据类型。</typeparam>
        /// <param name="eventType">事件类型标识符。</param>
        /// <param name="data">事件数据。</param>
        /// <param name="eventId">事件实例 ID（可选）。</param>
        /// <returns>事件实例。</returns>
        public static CardEvent<TData> Create<TData>(string eventType, TData data, string eventId = null) =>
            new(eventType, data, eventId);

        #endregion

        #region 事件类型匹配

        /// <summary>
        ///     检查事件是否为 Tick 类型。
        /// </summary>
        public static bool IsTick(ICardEvent evt) => evt?.EventType == TICK;

        /// <summary>
        ///     检查事件是否为 AddedToOwner 类型。
        /// </summary>
        public static bool IsAddedToOwner(ICardEvent evt) => evt?.EventType == ADDED_TO_OWNER;

        /// <summary>
        ///     检查事件是否为 RemovedFromOwner 类型。
        /// </summary>
        public static bool IsRemovedFromOwner(ICardEvent evt) => evt?.EventType == REMOVED_FROM_OWNER;

        /// <summary>
        ///     检查事件是否为 Use 类型。
        /// </summary>
        public static bool IsUse(ICardEvent evt) => evt?.EventType == USE;

        #endregion
    }
}