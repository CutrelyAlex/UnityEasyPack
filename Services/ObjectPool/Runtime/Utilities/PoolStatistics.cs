namespace EasyPack
{
    /// <summary>
    /// 对象池统计数据。
    /// </summary>
    public class PoolStatistics
    {
        /// <summary>
        /// 获取或设置对象类型名称。
        /// </summary>
        public string TypeName { get; set; }

        /// <summary>
        /// 获取或设置总租用次数。
        /// </summary>
        public int RentCount { get; set; }

        /// <summary>
        /// 获取或设置对象创建次数。
        /// </summary>
        public int CreateCount { get; set; }

        /// <summary>
        /// 获取或设置池命中次数。
        /// </summary>
        public int HitCount { get; set; }

        /// <summary>
        /// 获取或设置命中率（0-1之间）。
        /// </summary>
        public float HitRate { get; set; }

        /// <summary>
        /// 获取或设置池的峰值大小。
        /// </summary>
        public int PeakPoolSize { get; set; }

        /// <summary>
        /// 获取或设置当前池大小。
        /// </summary>
        public int CurrentPoolSize { get; set; }

        /// <summary>
        /// 获取或设置池的最大容量。
        /// </summary>
        public int MaxCapacity { get; set; }

        /// <summary>
        /// 格式化输出统计信息。
        /// </summary>
        /// <returns>格式化的统计字符串。</returns>
        public override string ToString()
        {
            return $"[{TypeName}] 租用:{RentCount} 创建:{CreateCount} 命中:{HitCount} " +
                   $"命中率:{HitRate:P2} 当前:{CurrentPoolSize}/{MaxCapacity} 峰值:{PeakPoolSize}";
        }
    }
}
