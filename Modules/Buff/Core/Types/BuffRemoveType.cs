namespace EasyPack.BuffSystem
{
    /// <summary>
    /// Buff 移除策略
    /// </summary>
    public enum BuffRemoveType
    {
        /// <summary>
        /// 移除所有堆叠层
        /// </summary>
        All,

        /// <summary>
        /// 仅移除一层堆叠
        /// </summary>
        OneStack,

        /// <summary>
        /// 手动移除（不自动移除）
        /// </summary>
        Manual,
    }
}
