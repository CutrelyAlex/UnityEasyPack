namespace EasyPack.Category
{
    /// <summary>
    /// 缓存策略枚举
    /// 三级缓存架构：基础、平衡、高级
    /// </summary>
    public enum CacheStrategy
    {
        /// <summary>
        /// 追踪热点数据，基于访问频率阈值缓存
        /// </summary>
        HotspotTracking,

        /// <summary>
        /// LRU频率混合，结合LRU时间衰减和访问频率评分
        /// 本缓存策略可能出错
        /// </summary>
        LRUFrequencyHybrid,

        /// <summary>
        /// 分片无驱逐
        /// </summary>
        ShardedNoEviction
    }
}
