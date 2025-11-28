using System;
using EasyPack.Category;

namespace EasyPack.EmeCardSystem
{
    /// <summary>
    ///     规则执行上下文：为效果提供触发源、容器与原始事件等信息。
    ///     <para>
    ///         支持两种构造方式：
    ///         1. 传统方式：直接传入 Source、Event 等参数（向后兼容）
    ///         2. 新方式：传入 IEventEntry 接口（推荐，支持多种事件源类型）
    ///     </para>
    /// </summary>
    public sealed class CardRuleContext
    {
        #region 构造函数

        /// <summary>
        ///     从 IEventEntry 创建规则上下文。
        /// </summary>
        /// <param name="eventEntry">事件条目（抽象事件源）</param>
        /// <param name="matchRoot">匹配范围根节点</param>
        /// <param name="effectRoot">效果作用根节点（可选，默认为 eventEntry.EffectRoot 或 matchRoot）</param>
        /// <param name="engine">卡牌引擎引用</param>
        /// <param name="currentRule">当前执行的规则</param>
        /// <param name="factory">产卡工厂（可选）</param>
        /// <param name="maxDepth">递归搜索最大深度</param>
        public CardRuleContext(
            IEventEntry eventEntry,
            Card matchRoot,
            Card effectRoot = null,
            CardEngine engine = null,
            CardRule currentRule = null,
            ICardFactory factory = null,
            int maxDepth = 0)
        {
            EventEntry = eventEntry ?? throw new ArgumentNullException(nameof(eventEntry));
            MatchRoot = matchRoot;
            EffectRoot = effectRoot ?? eventEntry.EffectRoot ?? matchRoot;
            Engine = engine;
            CurrentRule = currentRule;
            Factory = factory ?? engine?.CardFactory;
            MaxDepth = maxDepth;
        }

        /// <summary>
        ///     从基本参数创建规则上下文。
        /// </summary>
        /// <param name="source">触发该规则的卡牌（事件源）</param>
        /// <param name="matchRoot">用于匹配与执行的容器</param>
        /// <param name="event">事件数据（ICardEvent）</param>
        /// <param name="factory">产卡工厂</param>
        /// <param name="maxDepth">递归搜索最大深度</param>
        public CardRuleContext(Card source, Card matchRoot, ICardEvent @event, ICardFactory factory, int maxDepth)
            : this(source, matchRoot, matchRoot, @event, factory, maxDepth)
        {
        }

        /// <summary>
        ///     从基本参数创建规则上下文，支持独立的匹配根和效果根。
        /// </summary>
        /// <param name="source">触发该规则的卡牌（事件源）</param>
        /// <param name="matchRoot">用于匹配的容器根节点（由规则的 MatchRootHops 选择）</param>
        /// <param name="effectRoot">效果作用的根节点（由规则的 EffectRootHops 选择）</param>
        /// <param name="event">事件数据（ICardEvent）</param>
        /// <param name="factory">产卡工厂</param>
        /// <param name="maxDepth">递归搜索最大深度</param>
        public CardRuleContext(Card source, Card matchRoot, Card effectRoot, ICardEvent @event, ICardFactory factory, int maxDepth)
            : this(source, matchRoot, effectRoot, @event, factory, maxDepth, null, null)
        {
        }

        /// <summary>
        ///     从基本参数创建规则上下文，支持完整的上下文信息。
        /// </summary>
        /// <param name="source">触发该规则的卡牌（事件源）</param>
        /// <param name="matchRoot">用于匹配的容器根节点（由规则的 MatchRootHops 选择）</param>
        /// <param name="effectRoot">效果作用的根节点（由规则的 EffectRootHops 选择）</param>
        /// <param name="event">事件数据（ICardEvent）</param>
        /// <param name="factory">产卡工厂</param>
        /// <param name="maxDepth">递归搜索最大深度</param>
        /// <param name="engine">卡牌引擎引用（可选，默认从 source.Engine 获取）</param>
        /// <param name="currentRule">当前执行的规则（可选）</param>
        public CardRuleContext(
            Card source, 
            Card matchRoot, 
            Card effectRoot, 
            ICardEvent @event, 
            ICardFactory factory, 
            int maxDepth,
            CardEngine engine,
            CardRule currentRule)
        {
            // 创建 CardEventEntry 作为 IEventEntry
            EventEntry = new CardEventEntry(source, @event, effectRoot);
            MatchRoot = matchRoot;
            EffectRoot = effectRoot ?? matchRoot;
            Factory = factory;
            MaxDepth = maxDepth;
            Engine = engine ?? source?.Engine;
            CurrentRule = currentRule;
        }

        #endregion

        #region IEventEntry 相关属性

        /// <summary>
        ///     事件条目：抽象事件的来源和载体。
        ///     <para>
        ///         通过此属性可访问事件的完整元数据，包括来源类型、时间戳、优先级等。
        ///         新代码推荐使用此属性而非直接访问 Source/Event。
        ///     </para>
        /// </summary>
        public IEventEntry EventEntry { get; }

        /// <summary>
        ///     事件源类型（Card、Rule、System、External）。
        /// </summary>
        public EventSourceType SourceType => EventEntry.SourceType;

        /// <summary>
        ///     事件时间戳（UTC 时间）。
        /// </summary>
        public DateTime EventTimestamp => EventEntry.EventTimestamp;

        /// <summary>
        ///     源规则 UID（如果事件由规则触发，否则为 null）。
        /// </summary>
        public int? SourceRuleUID => EventEntry.SourceRuleUID;

        /// <summary>
        ///     事件优先级。
        /// </summary>
        public int EventPriority => EventEntry.Priority;

        #endregion

        #region 规则执行上下文

        /// <summary>触发该规则的卡牌（事件源）。委托到 EventEntry.SourceCard。</summary>
        public Card Source => EventEntry.SourceCard;

        /// <summary>用于匹配的容器根节点（由规则的 MatchRootHops 选择）。</summary>
        public Card MatchRoot { get; }

        /// <summary>效果作用的根节点（由规则的 EffectRootHops 选择）。</summary>
        public Card EffectRoot { get; }

        /// <summary>原始事件载体（ICardEvent 接口）。委托到 EventEntry.Event。</summary>
        public ICardEvent Event => EventEntry.Event;

        /// <summary>卡牌引擎引用。</summary>
        public CardEngine Engine { get; }

        /// <summary>当前执行的规则。</summary>
        public CardRule CurrentRule { get; }
        
        /// <summary>分类管理器。</summary>
        public ICategoryManager<Card, int> CategoryManager => Engine?.CategoryManager;

        /// <summary>产卡工厂。</summary>
        public ICardFactory Factory { get; }

        /// <summary>
        ///     递归搜索最大深度（>0 生效，1 表示仅子级一层）。
        /// </summary>
        public int MaxDepth { get; }

        #endregion

        #region 事件数据访问

        /// <summary>
        ///     事件类型标识符。
        /// </summary>
        public string EventType => Event.EventType;

        /// <summary>
        ///     事件实例 ID。
        /// </summary>
        public string EventId => Event.EventId;

        /// <summary>
        ///     从Tick事件中获取时间增量（DeltaTime）。
        ///     仅当事件类型为 "Tick" 且数据为 float 时返回有效值，否则返回 0。
        /// </summary>
        public float DeltaTime
        {
            get
            {
                if (CardEventTypes.IsTick(Event) && Event is ICardEvent<float> tickEvent)
                    return tickEvent.Data;
                return 0f;
            }
        }

        /// <summary>
        ///     将触发源卡牌转换为指定类型。
        /// </summary>
        /// <typeparam name="T">目标卡牌类型。</typeparam>
        /// <returns>转换后的卡牌对象，失败返回 null。</returns>
        public T GetSource<T>() where T : Card => Source as T;

        /// <summary>
        ///     将容器卡牌转换为指定类型。
        /// </summary>
        /// <typeparam name="T">目标卡牌类型。</typeparam>
        /// <returns>转换后的卡牌对象，失败返回 null。</returns>
        public T GetContainer<T>() where T : Card => MatchRoot as T;

        /// <summary>
        ///     尝试获取强类型事件数据
        ///     <para>
        ///         用于访问 ICardEvent&lt;T&gt; 类型的事件数据。
        ///         如果事件实现了 ICardEvent&lt;T&gt; 则直接返回强类型数据。
        ///     </para>
        /// </summary>
        /// <typeparam name="T">目标事件数据类型。</typeparam>
        /// <param name="value">输出参数，获取成功时为转换后的值。</param>
        /// <returns>转换成功返回 true，否则返回 false。</returns>
        /// <code>
        /// // 获取碰撞数据
        /// if (ctx.TryGetEventData&lt;CollisionData&gt;(out var collision))
        /// {
        ///     var target = collision.Target;
        ///     var force = collision.Force;
        /// }
        /// </code>
        public bool TryGetEventData<T>(out T value)
        {
            if (Event is ICardEvent<T> typedEvent)
            {
                value = typedEvent.Data;
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        ///     检查事件是否为指定类型。
        /// </summary>
        /// <param name="eventType">事件类型标识符。</param>
        /// <returns>如果匹配返回 true。</returns>
        public bool IsEventType(string eventType) => Event.EventType == eventType;

        /// <summary>
        ///     检查事件是否匹配指定的事件定义。
        /// </summary>
        /// <typeparam name="T">事件数据类型。</typeparam>
        /// <param name="eventDef">事件类型定义。</param>
        /// <returns>如果匹配返回 true。</returns>
        public bool IsEventType<T>(CardEventDefinition<T> eventDef) => eventDef?.Matches(Event) ?? false;

        #endregion

        #region 调试与输出

        public override string ToString() =>
            "CardRuleContext:\n" +
            $"  SourceType: {SourceType}\n" +
            $"  Source: {Source}\n" +
            $"  MatchRoot: {MatchRoot}\n" +
            $"  EffectRoot: {EffectRoot}\n" +
            $"  Event: Type={Event.EventType}, Id={Event.EventId}, Data={Event.DataObject}\n" +
            $"  Engine: {(Engine != null ? "set" : "null")}\n" +
            $"  CurrentRule: {(CurrentRule != null ? CurrentRule.ToString() : "null")}\n" +
            $"  Factory: {Factory}\n" +
            $"  MaxDepth: {MaxDepth}\n" +
            $"  DeltaTime: {DeltaTime}\n" +
            $"  EventTimestamp: {EventTimestamp:O}";

        #endregion
    }
}