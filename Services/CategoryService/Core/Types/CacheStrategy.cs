namespace EasyPack.Category
{
    /// <summary>
    /// 缓存策略枚举
    /// </summary>
    public enum CacheStrategy
    {
        /// <summary>
        /// 松散缓存：缓存所有查询，无淘汰策略
        /// </summary>
        Loose,

        /// <summary>
        /// 平衡缓存：LRU 淘汰，3 次访问阈值
        /// </summary>
        Balanced,

        /// <summary>
        /// 高效缓存：仅缓存 ID，动态获取实体
        /// </summary>
        Efficient,

        /// <summary>
        /// 激进缓存：预加载常见模式，无失效策略
        /// </summary>
        Aggressive
    }
}