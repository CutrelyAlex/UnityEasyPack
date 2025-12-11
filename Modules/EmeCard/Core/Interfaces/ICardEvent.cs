namespace EasyPack.EmeCardSystem
{
    /// <summary>
    ///     卡牌事件接口。
    ///     <para>
    ///         所有卡牌事件的基础接口。事件通过字符串 <c>EventType</c> 标识，
    ///     </para>
    ///     <code>
    /// // 创建碰撞事件
    /// var collisionEvent = new CardEvent&lt;CollisionData&gt;("Collision", collisionData);
    /// // 在规则中获取数据
    /// if (ctx.TryGetEventData&lt;CollisionData&gt;(out var data)) {}
    /// </code>
    /// </summary>
    public interface ICardEvent
    {
        /// <summary>
        ///     事件类型标识符（字符串，如 "Tick"、"Collision"、"Damage"）。
        ///     <para>
        ///         标准事件类型定义在 <see cref="CardEventTypes" /> 中。
        ///     </para>
        /// </summary>
        string EventType { get; }

        /// <summary>
        ///     事件实例标识符
        /// </summary>
        string EventId { get; }

        /// <summary>
        ///     事件数据
        ///     <para>
        ///         推荐使用 <see cref="ICardEvent&lt;TData&gt;.Data" /> 进行类型安全访问。
        ///     </para>
        /// </summary>
        object DataObject { get; }
        
        /// <summary>
        ///     泵类型
        /// </summary>
        EEventPumpType PumpType { get;  }
    }

    /// <summary>
    ///     泛型卡牌事件接口
    ///     <para>
    ///         推荐使用此接口而非直接访问 DataObject。
    ///     </para>
    /// </summary>
    /// <typeparam name="TData">事件数据类型（协变）</typeparam>
    /// <code>
    /// // 定义碰撞事件数据
    /// public class CollisionData
    /// {
    ///     public Card Target { get; set; }
    ///     public float Force { get; set; }
    /// }
    /// // 创建碰撞事件
    /// ICardEvent&lt;CollisionData&gt; evt = new CardEvent&lt;CollisionData&gt;("Collision", data);
    /// // 允许协变
    /// ICardEvent baseEvent = evt;
    /// </code>
    public interface ICardEvent<out TData> : ICardEvent
    {
        /// <summary>
        ///     强类型事件数据。
        /// </summary>
        TData Data { get; }
    }

    public enum EEventPumpType
    {
        Start,Normal,End
    }
}