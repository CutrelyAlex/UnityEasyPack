namespace EasyPack.ObjectPool
{
    /// <summary>
    /// 对象池统计信息提供者接口。
    /// 用于对象池向外暴露统计数据。
    /// </summary>
    public interface IPoolStatisticsProvider
    {
        /// <summary>
        /// 获取对象池的统计信息。
        /// </summary>
        PoolStatistics GetStatistics();
    }
}

