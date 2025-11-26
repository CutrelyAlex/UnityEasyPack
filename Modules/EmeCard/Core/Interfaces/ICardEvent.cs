namespace EasyPack.EmeCardSystem
{
    /// <summary>
    /// 卡牌事件接口（非泛型基础接口）。
    /// <para>
    /// 所有卡牌事件的基础接口。事件通过字符串 EventType 标识，
    /// 而非枚举，以支持任意自定义事件类型。
    /// </para>
    /// <example>
    /// // 创建碰撞事件
    /// var collisionEvent = new CardEvent&lt;CollisionData&gt;("Collision", collisionData);
    /// 
    /// // 在规则中获取数据
    /// if (ctx.TryGetEventData&lt;CollisionData&gt;(out var data))
    /// {
    ///     // 处理碰撞
    /// }
    /// </example>
    /// </summary>
    public interface ICardEvent
    {
        /// <summary>
        /// 事件类型标识符（字符串，如 "Tick"、"Collision"、"Damage"）。
        /// <para>
        /// 使用字符串而非枚举，以支持任意自定义事件类型。
        /// 标准事件类型定义在 <see cref="CardEventTypes"/> 中。
        /// </para>
        /// </summary>
        string EventType { get; }

        /// <summary>
        /// 事件实例标识符（可选，用于区分同类型的不同事件实例）。
        /// </summary>
        string EventId { get; }

        /// <summary>
        /// 事件数据（非类型安全访问）。
        /// <para>
        /// 推荐使用 <see cref="ICardEvent{TData}.Data"/> 进行类型安全访问。
        /// </para>
        /// </summary>
        object DataObject { get; }
    }

    /// <summary>
    /// 泛型卡牌事件接口：携带强类型数据的事件。
    /// <para>
    /// 推荐使用此接口而非直接访问 DataObject。
    /// </para>
    /// </summary>
    /// <typeparam name="TData">事件数据类型（协变）。</typeparam>
    /// <example>
    /// // 定义碰撞事件数据
    /// public class CollisionData
    /// {
    ///     public Card Target { get; set; }
    ///     public float Force { get; set; }
    /// }
    /// 
    /// // 创建碰撞事件
    /// ICardEvent&lt;CollisionData&gt; evt = new CardEvent&lt;CollisionData&gt;("Collision", data);
    /// 
    /// // 协变允许
    /// ICardEvent baseEvent = evt;
    /// </example>
    public interface ICardEvent<out TData> : ICardEvent
    {
        /// <summary>
        /// 强类型事件数据。
        /// </summary>
        TData Data { get; }
    }
}
