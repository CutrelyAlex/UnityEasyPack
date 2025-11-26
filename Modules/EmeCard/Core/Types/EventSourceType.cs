namespace EasyPack.EmeCardSystem
{
    /// <summary>
    ///     事件源类型枚举：标识事件的触发来源
    /// </summary>
    public enum EventSourceType
    {
        /// <summary>
        ///     卡牌触发：事件由卡牌直接触发（如使用卡牌、卡牌状态变化）。
        /// </summary>
        Card,

        /// <summary>
        ///     规则触发：事件由规则执行过程中触发的后续事件。
        /// </summary>
        Rule,

        /// <summary>
        ///     系统触发：事件由系统触发（如 Tick 计时器、帧更新）。
        /// </summary>
        System,

        /// <summary>
        ///     外部触发：事件由外部代码或 API 调用触发。
        /// </summary>
        External,
    }
}