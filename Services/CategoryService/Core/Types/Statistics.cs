namespace EasyPack.Category
{
    /// <summary>
    /// CategoryService 统计信息
    /// 提供实体数量、分类数量、标签数量、缓存命中率、内存使用等运行时统计
    /// </summary>
    public class Statistics
    {
        /// <summary>
        /// 实体总数
        /// </summary>
        public int TotalEntities { get; set; }

        /// <summary>
        /// 分类总数
        /// </summary>
        public int TotalCategories { get; set; }

        /// <summary>
        /// 标签总数
        /// </summary>
        public int TotalTags { get; set; }

        /// <summary>
        /// 缓存命中率 (0.0 - 1.0)
        /// </summary>
        public float CacheHitRate { get; set; }

        /// <summary>
        /// 内存使用量（字节）
        /// </summary>
        public long MemoryUsageBytes { get; set; }

        /// <summary>
        /// 最大分类深度
        /// </summary>
        public int MaxCategoryDepth { get; set; }

        /// <summary>
        /// 缓存查询总次数
        /// </summary>
        public long TotalCacheQueries { get; set; }

        /// <summary>
        /// 缓存命中次数
        /// </summary>
        public long CacheHits { get; set; }

        /// <summary>
        /// 缓存未命中次数
        /// </summary>
        public long CacheMisses { get; set; }

        /// <summary>
        /// 平均每个分类的实体数
        /// </summary>
        public float AverageEntitiesPerCategory
        {
            get
            {
                if (TotalCategories == 0) return 0;
                return (float)TotalEntities / TotalCategories;
            }
        }

        /// <summary>
        /// 平均每个实体的标签数
        /// </summary>
        public float AverageTagsPerEntity
        {
            get
            {
                if (TotalEntities == 0) return 0;
                return (float)TotalTags / TotalEntities;
            }
        }

        /// <summary>
        /// 内存使用量（KB）
        /// </summary>
        public float MemoryUsageKB => MemoryUsageBytes / 1024f;

        /// <summary>
        /// 内存使用量（MB）
        /// </summary>
        public float MemoryUsageMB => MemoryUsageBytes / (1024f * 1024f);

        /// <summary>
        /// 每个实体的平均内存占用（字节）
        /// </summary>
        public float AverageMemoryPerEntity
        {
            get
            {
                if (TotalEntities == 0) return 0;
                return (float)MemoryUsageBytes / TotalEntities;
            }
        }

        /// <summary>
        /// 返回统计信息的格式化字符串
        /// </summary>
        public override string ToString()
        {
            return $"Statistics:\n" +
                   $"  Entities: {TotalEntities}\n" +
                   $"  Categories: {TotalCategories}\n" +
                   $"  Tags: {TotalTags}\n" +
                   $"  Cache Hit Rate: {CacheHitRate:P2}\n" +
                   $"  Memory Usage: {MemoryUsageMB:F2} MB\n" +
                   $"  Max Category Depth: {MaxCategoryDepth}\n" +
                   $"  Avg Entities/Category: {AverageEntitiesPerCategory:F2}\n" +
                   $"  Avg Tags/Entity: {AverageTagsPerEntity:F2}\n" +
                   $"  Avg Memory/Entity: {AverageMemoryPerEntity:F2} bytes";
        }
    }
}