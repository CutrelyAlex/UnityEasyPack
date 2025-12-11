using System;

namespace EasyPack.EmeCardSystem
{
    /// <summary>
    ///     表示"无数据"的占位类型，用于无数据事件。
    ///     <para>
    ///     </para>
    /// </summary>
    /// <code>
    /// // 定义无数据事件
    /// public static readonly CardEventDefinition&lt;Unit&gt; GameStart 
    ///     = CardEventTypes.Define&lt;Unit&gt;("GameStart");
    /// 
    /// // 触发无数据事件
    /// card.RaiseEvent("GameStart");
    /// </code>
    public readonly struct Unit : IEquatable<Unit>
    {
        /// <summary>
        ///     唯一的 Unit 实例。
        /// </summary>
        public static readonly Unit Default = default;

        public override string ToString() => "()";

        public override int GetHashCode() => 0;

        public override bool Equals(object obj) => obj is Unit;

        public bool Equals(Unit other) => true;

        public static bool operator ==(Unit left, Unit right) => true;

        public static bool operator !=(Unit left, Unit right) => false;
    }

    /// <summary>
    ///     泛型卡牌事件：携带强类型数据的事件实现。
    ///     <para>
    ///         这是 ICardEvent&lt;TData&gt; 的主要实现类。
    ///         使用字符串 EventType 标识事件类型，支持任意自定义事件。
    ///     </para>
    /// </summary>
    /// <typeparam name="TData">事件数据类型。</typeparam>
    /// <example>
    ///     // 创建碰撞事件
    ///     var collisionData = new CollisionData { Target = enemy, Force = 10f };
    ///     var evt = new CardEvent&lt;CollisionData&gt;("Collision", collisionData);
    ///     // 创建带自定义 ID 的事件
    ///     var tickEvent = new CardEvent&lt;float&gt;("Tick", deltaTime, "MainLoop");
    /// </example>
    public readonly struct CardEvent<TData> : ICardEvent<TData>
    {
        /// <inheritdoc />
        public string EventType { get; }

        /// <inheritdoc />
        public string EventId { get; }

        /// <inheritdoc />
        public TData Data { get; }

        /// <inheritdoc />
        public object DataObject => Data;

        /// <inheritdoc />
        public EEventPumpType PumpType { get; }

        /// <summary>
        ///     创建强类型事件。
        /// </summary>
        /// <param name="eventType">事件类型标识符（如 "Collision"、"Damage"）。</param>
        /// <param name="data">事件数据。</param>
        /// <param name="eventId">事件实例 ID（可选，默认使用 eventType）。</param>
        /// <param name="pumpType">泵入何处（可选，默认使用 Normal）。</param>
        /// <exception cref="ArgumentNullException">当 eventType 为 null 时抛出。</exception>
        public CardEvent(string eventType, TData data, string eventId = null,EEventPumpType pumpType=EEventPumpType.Normal)
        {
            EventType = eventType ?? throw new ArgumentNullException(nameof(eventType));
            EventId = eventId ?? eventType;
            Data = data;
            PumpType = pumpType;
        }

        public override string ToString() =>
            $"CardEvent<{typeof(TData).Name}>[Type={EventType}, Id={EventId}, Data={Data}]";

        /// <summary>
        ///     隐式转换为基础接口。
        /// </summary>
        public static implicit operator CardEvent<object>(CardEvent<TData> evt) =>
            new(evt.EventType, evt.Data, evt.EventId,evt.PumpType);
    }

    /// <summary>
    ///     事件类型定义：用于创建预定义的强类型事件。
    ///     <para>
    ///         通过 CardEventTypes 注册表定义标准事件类型，
    ///         或创建自定义事件类型定义。
    ///     </para>
    /// </summary>
    /// <typeparam name="TData">事件携带的数据类型。</typeparam>
    /// <example>
    ///     // 定义碰撞事件类型
    ///     public static readonly CardEventDefinition&lt;CollisionData&gt; Collision
    ///     = new CardEventDefinition&lt;CollisionData&gt;("Collision");
    ///     // 创建事件
    ///     var evt = Collision.Create(new CollisionData { ... });
    /// </example>
    public class CardEventDefinition<TData>
    {
        /// <summary>
        ///     事件类型标识符。
        /// </summary>
        public string EventType { get; }

        /// <summary>
        ///     创建事件类型定义。
        /// </summary>
        /// <param name="eventType">事件类型标识符。</param>
        /// <exception cref="ArgumentNullException">当 eventType 为 null 或空时抛出。</exception>
        public CardEventDefinition(string eventType)
        {
            if (string.IsNullOrEmpty(eventType))
                throw new ArgumentNullException(nameof(eventType));
            EventType = eventType;
        }

        /// <summary>
        ///     创建携带指定数据的事件实例。
        /// </summary>
        /// <param name="data">事件数据。</param>
        /// <param name="eventId">事件实例 ID（可选）。</param>
        /// <returns>强类型事件实例。</returns>
        public CardEvent<TData> CreateEvent(TData data, string eventId = null) => new(EventType, data, eventId);

        /// <summary>
        ///     检查事件是否匹配此类型定义。
        /// </summary>
        /// <param name="evt">要检查的事件。</param>
        /// <returns>如果事件类型匹配返回 true。</returns>
        public bool Matches(ICardEvent evt) => evt != null && evt.EventType == EventType;

        /// <summary>
        ///     尝试从 ICardEvent 提取强类型数据。
        /// </summary>
        /// <param name="evt">源事件。</param>
        /// <param name="data">输出数据。</param>
        /// <returns>如果提取成功返回 true。</returns>
        public bool TryGetData(ICardEvent evt, out TData data)
        {
            if (Matches(evt) && evt is ICardEvent<TData> typedEvt)
            {
                data = typedEvt.Data;
                return true;
            }

            // Fallback: 尝试从 DataObject 转换
            if (Matches(evt) && evt.DataObject is TData obj)
            {
                data = obj;
                return true;
            }

            data = default;
            return false;
        }

        public override string ToString() => $"CardEventDefinition<{typeof(TData).Name}>({EventType})";
    }
}