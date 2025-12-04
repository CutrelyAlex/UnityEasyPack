using EasyPack.Category;

namespace EasyPack.EmeCardSystem
{
    /// <summary>
    ///     规则执行上下文：为效果提供触发源、容器与原始事件等信息。
    /// </summary>
    public sealed class CardRuleContext
    {
        #region 核心属性

        /// <summary>触发该规则的卡牌（事件源）。</summary>
        public Card Source { get; set; }

        /// <summary>原始事件载体。</summary>
        public ICardEvent Event { get; set; }

        /// <summary>用于匹配的容器根节点。</summary>
        public Card MatchRoot { get; set; }

        /// <summary>效果作用的根节点。</summary>
        public Card EffectRoot { get; set; }

        /// <summary>递归搜索最大深度。</summary>
        public int MaxDepth { get; set; }

        /// <summary>卡牌引擎引用。</summary>
        public CardEngine Engine { get; set; }

        /// <summary>当前执行的规则。</summary>
        public CardRule CurrentRule { get; set; }

        #endregion

        #region 便捷属性

        /// <summary>事件类型标识符。</summary>
        public string EventType => Event?.EventType;

        /// <summary>事件实例 ID。</summary>
        public string EventId => Event?.EventId;

        /// <summary>事件源类型。</summary>
        public EventSourceType SourceType => Source != null ? EventSourceType.Card : EventSourceType.System;

        /// <summary>分类管理器。</summary>
        public ICategoryManager<Card, long> CategoryManager => Engine?.CategoryManager;

        /// <summary>卡牌工厂。</summary>
        public ICardFactoryRegistry Factory => Engine?.CardFactory;

        /// <summary>
        ///     从 Tick 事件获取时间增量。
        ///     非 Tick 事件返回 0。
        /// </summary>
        public float DeltaTime
        {
            get
            {
                if (Event != null && CardEventTypes.IsTick(Event) && Event is ICardEvent<float> tickEvent)
                    return tickEvent.Data;
                return 0f;
            }
        }

        #endregion

        #region 构造函数

        /// <summary>
        ///     创建规则上下文。
        /// </summary>
        public CardRuleContext(
            Card source,
            Card matchRoot,
            ICardEvent @event,
            CardEngine engine,
            int maxDepth)
        {
            Source = source;
            Event = @event;
            MatchRoot = matchRoot;
            EffectRoot = matchRoot;
            MaxDepth = maxDepth;
            Engine = engine ?? source?.Engine;
            CurrentRule = null;
        }

        /// <summary>
        ///     创建规则上下文（完整参数）。
        /// </summary>
        public CardRuleContext(
            Card source,
            Card matchRoot,
            Card effectRoot,
            ICardEvent @event,
            CardEngine engine,
            int maxDepth,
            CardRule currentRule)
        {
            Source = source;
            Event = @event;
            MatchRoot = matchRoot;
            EffectRoot = effectRoot ?? matchRoot;
            MaxDepth = maxDepth;
            Engine = engine ?? source?.Engine;
            CurrentRule = currentRule;
        }

        #endregion

        #region 类型转换方法

        /// <summary>将 Source 转换为指定类型。</summary>
        public T GetSource<T>() where T : Card => Source as T;

        /// <summary>将 MatchRoot 转换为指定类型。</summary>
        public T GetContainer<T>() where T : Card => MatchRoot as T;

        /// <summary>
        ///     尝试获取强类型事件数据。
        /// </summary>
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

        /// <summary>检查事件类型是否匹配。</summary>
        public bool IsEventType(string eventType) => Event?.EventType == eventType;

        /// <summary>检查事件是否匹配指定定义。</summary>
        public bool IsEventType<T>(CardEventDefinition<T> eventDef) => eventDef?.Matches(Event) ?? false;

        #endregion

        #region 调试

        public override string ToString() =>
            $"CardRuleContext[Source={Source?.Id ?? "null"}, Event={Event?.EventType}, Rule={CurrentRule?.EventType ?? "null"}]";

        #endregion
    }
}
