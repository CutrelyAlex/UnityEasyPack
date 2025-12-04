using System;

namespace EasyPack.EmeCardSystem
{
    /// <summary>
    ///     卡牌事件条目：由卡牌直接触发的事件。
    ///     <para>
    ///         用于表示卡牌使用、状态变化等由卡牌发起的事件。
    ///         SourceCard 必须非空
    ///     </para>
    /// </summary>
    public readonly struct CardEventEntry : IEventEntry
    {
        /// <inheritdoc />
        public ICardEvent Event { get; }

        /// <inheritdoc />
        public int Priority { get; }

        /// <inheritdoc />
        public EventSourceType SourceType => EventSourceType.Card;

        /// <inheritdoc />
        public Card SourceCard { get; }

        /// <inheritdoc />
        public int? SourceRuleUID => null;

        /// <inheritdoc />
        public Card EffectRoot { get; }

        /// <summary>
        ///     创建卡牌事件条目。
        /// </summary>
        /// <param name="source">触发事件的源卡牌（必须非空）。</param>
        /// <param name="event">事件数据（ICardEvent）。</param>
        /// <param name="effectRoot">效果根节点（可选，默认为 source）。</param>
        /// <param name="priority">事件优先级（默认为 0）。</param>
        /// <exception cref="ArgumentNullException">当 source 或 event 为 null 时抛出。</exception>
        public CardEventEntry(Card source, ICardEvent @event, Card effectRoot = null, int priority = 0)
        {
            SourceCard = source ?? throw new ArgumentNullException(nameof(source));
            Event = @event ?? throw new ArgumentNullException(nameof(@event));
            Priority = priority;
            EffectRoot = effectRoot ?? source;
        }
        public override string ToString() =>
            $"CardEventEntry[Source={SourceCard?.Id ?? "null"}, Event={Event.EventType}:{Event.EventId}, Priority={Priority}]";
    }

    /// <summary>
    ///     规则事件条目：由规则执行过程中触发的后续事件。
    ///     <para>
    ///         用于表示规则执行后产生的连锁事件。
    ///         SourceRuleUID 必须有效，SourceCard 可选（表示规则关联的卡牌）。
    ///     </para>
    /// </summary>
    public readonly struct RuleEventEntry : IEventEntry
    {
        /// <inheritdoc />
        public ICardEvent Event { get; }

        /// <inheritdoc />
        public int Priority { get; }

        /// <inheritdoc />
        public EventSourceType SourceType => EventSourceType.Rule;

        /// <inheritdoc />
        public Card SourceCard { get; }

        /// <inheritdoc />
        public int? SourceRuleUID { get; }

        /// <inheritdoc />
        public Card EffectRoot { get; }

        /// <summary>
        ///     创建规则事件条目。
        /// </summary>
        /// <param name="ruleUID">触发事件的规则 UID。</param>
        /// <param name="event">事件数据（ICardEvent）。</param>
        /// <param name="sourceCard">关联的源卡牌（可选）。</param>
        /// <param name="effectRoot">效果根节点（可选，默认为 sourceCard）。</param>
        /// <param name="priority">事件优先级（默认为 0）。</param>
        /// <exception cref="ArgumentNullException">当 event 为 null 时抛出。</exception>
        public RuleEventEntry(int ruleUID, ICardEvent @event, Card sourceCard = null, Card effectRoot = null,
                              int priority = 0)
        {
            SourceRuleUID = ruleUID;
            Event = @event ?? throw new ArgumentNullException(nameof(@event));
            SourceCard = sourceCard;
            Priority = priority;
            EffectRoot = effectRoot ?? sourceCard;
        }

        public override string ToString() =>
            $"RuleEventEntry[RuleUID={SourceRuleUID}, Event={Event.EventType}:{Event.EventId}, Source={SourceCard?.Id ?? "null"}, Priority={Priority}]";
    }

    /// <summary>
    ///     系统事件条目：由系统触发的事件
    ///     <para>
    ///         用于表示系统级别的事件，如帧更新、计时器触发等
    ///     </para>
    /// </summary>
    public readonly struct SystemEventEntry : IEventEntry
    {
        /// <inheritdoc />
        public ICardEvent Event { get; }

        /// <inheritdoc />
        public int Priority { get; }

        /// <inheritdoc />
        public EventSourceType SourceType => EventSourceType.System;

        /// <inheritdoc />
        public Card SourceCard => null;

        /// <inheritdoc />
        public int? SourceRuleUID => null;

        /// <inheritdoc />
        public Card EffectRoot => null;

        /// <summary>
        ///     创建系统事件条目。
        /// </summary>
        /// <param name="event">事件数据（ICardEvent）。</param>
        /// <param name="priority">事件优先级（默认为 0）。</param>
        /// <exception cref="ArgumentNullException">当 event 为 null 时抛出。</exception>
        public SystemEventEntry(ICardEvent @event, int priority = 0)
        {
            Event = @event ?? throw new ArgumentNullException(nameof(@event));
            Priority = priority;
        }

        public override string ToString() =>
            $"SystemEventEntry[Event={Event.EventType}:{Event.EventId}, Priority={Priority}]";
    }

    /// <summary>
    ///     外部事件条目：由外部代码或 API 调用触发的事件。
    ///     <para>
    ///         用于表示来自游戏逻辑外部的事件，如网络消息、用户输入等
    ///     </para>
    /// </summary>
    public readonly struct ExternalEventEntry : IEventEntry
    {
        /// <inheritdoc />
        public ICardEvent Event { get; }

        /// <inheritdoc />
        public int Priority { get; }

        /// <inheritdoc />
        public EventSourceType SourceType => EventSourceType.External;

        /// <inheritdoc />
        public Card SourceCard { get; }

        /// <inheritdoc />
        public int? SourceRuleUID => null;

        /// <inheritdoc />
        public Card EffectRoot { get; }

        /// <summary>
        ///     创建外部事件条目。
        /// </summary>
        /// <param name="event">事件数据（ICardEvent）。</param>
        /// <param name="sourceCard">关联的源卡牌（可选）。</param>
        /// <param name="effectRoot">效果根节点（可选，默认为 sourceCard）。</param>
        /// <param name="priority">事件优先级（默认为 0）。</param>
        /// <exception cref="ArgumentNullException">当 event 为 null 时抛出。</exception>
        public ExternalEventEntry(ICardEvent @event, Card sourceCard = null, Card effectRoot = null, int priority = 0)
        {
            Event = @event ?? throw new ArgumentNullException(nameof(@event));
            SourceCard = sourceCard;
            Priority = priority;
            EffectRoot = effectRoot ?? sourceCard;
        }

        public override string ToString() =>
            $"ExternalEventEntry[Event={Event.EventType}:{Event.EventId}, Source={SourceCard?.Id ?? "null"}, Priority={Priority}]";
    }
}