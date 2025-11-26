using System;

namespace EasyPack.EmeCardSystem
{
    /// <summary>
    /// 事件工具类：提供事件创建和转换的便捷方法。
    /// </summary>
    public static class CardEventHelper
    {
        /// <summary>
        /// 创建强类型事件。
        /// </summary>
        /// <typeparam name="T">事件数据类型。</typeparam>
        /// <param name="eventType">事件类型标识符。</param>
        /// <param name="data">事件数据。</param>
        /// <param name="eventId">事件实例 ID（可选）。</param>
        /// <returns>事件实例。</returns>
        public static CardEvent<T> CreateEvent<T>(string eventType, T data, string eventId = null)
        {
            return new CardEvent<T>(eventType, data, eventId);
        }

        /// <summary>
        /// 尝试从 ICardEvent 提取强类型数据。
        /// </summary>
        /// <typeparam name="T">目标数据类型。</typeparam>
        /// <param name="cardEvent">源事件。</param>
        /// <param name="value">输出数据。</param>
        /// <returns>如果提取成功返回 true。</returns>
        public static bool TryGetData<T>(this ICardEvent cardEvent, out T value)
        {
            if (cardEvent is ICardEvent<T> typed)
            {
                value = typed.Data;
                return true;
            }
            value = default;
            return false;
        }

        /// <summary>
        /// 检查事件是否为指定类型。
        /// </summary>
        /// <param name="cardEvent">要检查的事件。</param>
        /// <param name="eventType">事件类型标识符。</param>
        /// <returns>如果类型匹配返回 true。</returns>
        public static bool IsType(this ICardEvent cardEvent, string eventType)
        {
            return cardEvent?.EventType == eventType;
        }

        /// <summary>
        /// 检查事件是否匹配事件定义。
        /// </summary>
        /// <typeparam name="T">事件数据类型。</typeparam>
        /// <param name="cardEvent">要检查的事件。</param>
        /// <param name="eventDef">事件类型定义。</param>
        /// <returns>如果匹配返回 true。</returns>
        public static bool Matches<T>(this ICardEvent cardEvent, CardEventDefinition<T> eventDef)
        {
            return eventDef?.Matches(cardEvent) ?? false;
        }
    }

    /// <summary>
    /// 事件条目创建工具类。
    /// </summary>
    public static class EventEntryFactory
    {
        /// <summary>
        /// 创建卡牌事件条目。
        /// </summary>
        /// <typeparam name="T">事件数据类型。</typeparam>
        /// <param name="source">源卡牌。</param>
        /// <param name="eventType">事件类型。</param>
        /// <param name="data">事件数据。</param>
        /// <param name="effectRoot">效果根节点（可选）。</param>
        /// <param name="priority">优先级。</param>
        /// <returns>事件条目。</returns>
        public static CardEventEntry FromCard<T>(
            Card source,
            string eventType,
            T data,
            Card effectRoot = null,
            int priority = 0)
        {
            var evt = new CardEvent<T>(eventType, data);
            return new CardEventEntry(source, evt, effectRoot, priority);
        }

        /// <summary>
        /// 创建卡牌事件条目（使用事件定义）。
        /// </summary>
        /// <typeparam name="T">事件数据类型。</typeparam>
        /// <param name="source">源卡牌。</param>
        /// <param name="eventDef">事件类型定义。</param>
        /// <param name="data">事件数据。</param>
        /// <param name="effectRoot">效果根节点（可选）。</param>
        /// <param name="priority">优先级。</param>
        /// <returns>事件条目。</returns>
        public static CardEventEntry FromCard<T>(
            Card source,
            CardEventDefinition<T> eventDef,
            T data,
            Card effectRoot = null,
            int priority = 0)
        {
            var evt = eventDef.Create(data);
            return new CardEventEntry(source, evt, effectRoot, priority);
        }

        /// <summary>
        /// 创建规则事件条目。
        /// </summary>
        /// <typeparam name="T">事件数据类型。</typeparam>
        /// <param name="ruleUID">规则 UID。</param>
        /// <param name="eventType">事件类型。</param>
        /// <param name="data">事件数据。</param>
        /// <param name="sourceCard">源卡牌（可选）。</param>
        /// <param name="effectRoot">效果根节点（可选）。</param>
        /// <param name="priority">优先级。</param>
        /// <returns>事件条目。</returns>
        public static RuleEventEntry FromRule<T>(
            int ruleUID,
            string eventType,
            T data,
            Card sourceCard = null,
            Card effectRoot = null,
            int priority = 0)
        {
            var evt = new CardEvent<T>(eventType, data);
            return new RuleEventEntry(ruleUID, evt, sourceCard, effectRoot, priority);
        }

        /// <summary>
        /// 创建系统事件条目。
        /// </summary>
        /// <typeparam name="T">事件数据类型。</typeparam>
        /// <param name="eventType">事件类型。</param>
        /// <param name="data">事件数据。</param>
        /// <param name="priority">优先级。</param>
        /// <returns>事件条目。</returns>
        public static SystemEventEntry FromSystem<T>(
            string eventType,
            T data,
            int priority = 0)
        {
            var evt = new CardEvent<T>(eventType, data);
            return new SystemEventEntry(evt, priority);
        }

        /// <summary>
        /// 创建 Tick 事件条目。
        /// </summary>
        /// <param name="deltaTime">时间增量。</param>
        /// <param name="priority">优先级。</param>
        /// <returns>事件条目。</returns>
        public static SystemEventEntry Tick(float deltaTime, int priority = 0)
        {
            var evt = CardEventTypes.Tick.Create(deltaTime);
            return new SystemEventEntry(evt, priority);
        }
    }
}
