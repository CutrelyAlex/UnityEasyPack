using System;

namespace EasyPack.EmeCardSystem
{
    /// <summary>
    /// 事件条目接口
    /// <para>
    /// 支持多种事件源类型（卡牌事件、规则触发事件、系统事件、外部事件）。
    /// CardRuleContext 持有此接口以访问事件相关数据。
    /// </para>
    /// </summary>
    public interface IEventEntry
    {
        /// <summary>
        /// 事件数据（强类型事件接口）。
        /// </summary>
        ICardEvent Event { get; }

        /// <summary>
        /// 事件时间戳（UTC 时间）。
        /// </summary>
        DateTime EventTimestamp { get; }

        /// <summary>
        /// 事件优先级（数值越大优先级越高）。
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// 事件源类型（Card、Rule、System、External）。
        /// </summary>
        EventSourceType SourceType { get; }

        /// <summary>
        /// 源卡牌（如果事件由卡牌触发，否则为 null）。
        /// </summary>
        Card SourceCard { get; }

        /// <summary>
        /// 源规则 UID（如果事件由规则触发，否则为 null）。
        /// </summary>
        int? SourceRuleUID { get; }

        /// <summary>
        /// 效果根节点（可选覆盖，用于指定效果作用范围）。
        /// </summary>
        Card EffectRoot { get; }
    }
}
