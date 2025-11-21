namespace EasyPack.Category
{
    /// <summary>
    /// 缓存策略枚举
    /// 三级缓存架构：基础、平衡、高级
    /// </summary>
    public enum CacheStrategy
    {
        /// <summary>
        /// 基础缓存（Loose）：缓存所有查询，无淘汰策略
        /// 适用：查询频率低，数据集小，内存充足的场景
        /// </summary>
        Loose,

        /// <summary>
        /// 平衡缓存（Balanced）：自动缓存 + LRU 淘汰
        /// 适用：通用场景，平衡内存和性能
        /// </summary>
        Balanced,

        /// <summary>
        /// 高级缓存（Premium）：多层次缓存 + 预热 + 智能淘汰
        /// 适用：高频查询，数据集大，需要最佳性能的场景
        /// </summary>
        Premium
    }
}
