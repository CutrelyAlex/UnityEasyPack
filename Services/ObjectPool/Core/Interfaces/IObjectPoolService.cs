using System;
using System.Collections.Generic;
using EasyPack.ENekoFramework;

namespace EasyPack.ObjectPool
{
    /// <summary>
    /// 对象池服务接口，提供全局对象池管理功能。
    /// </summary>
    public interface IObjectPoolService : IService
    {
        /// <summary>
        /// 创建指定类型的对象池。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        /// <param name="factory">对象工厂方法，用于创建新对象。</param>
        /// <param name="cleanup">对象清理方法，在归还时调用（可选）。</param>
        /// <param name="maxCapacity">池的最大容量，超出时丢弃对象。</param>
        /// <returns>创建的对象池实例。</returns>
        ObjectPool<T> CreatePool<T>(Func<T> factory, Action<T> cleanup = null, int maxCapacity = 64) where T : class;

        /// <summary>
        /// 获取指定类型的对象池。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        /// <returns>对象池实例，如果不存在则返回null。</returns>
        ObjectPool<T> GetPool<T>() where T : class;

        /// <summary>
        /// 获取或创建指定类型的对象池。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        /// <param name="factory">对象工厂方法，仅在池不存在时使用。</param>
        /// <param name="cleanup">对象清理方法，仅在池不存在时使用。</param>
        /// <param name="maxCapacity">池的最大容量，仅在池不存在时使用。</param>
        /// <returns>对象池实例。</returns>
        ObjectPool<T> GetOrCreatePool<T>(Func<T> factory, Action<T> cleanup = null, int maxCapacity = 64) where T : class;

        /// <summary>
        /// 销毁指定类型的对象池。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        /// <returns>如果成功销毁返回true，否则返回false。</returns>
        bool DestroyPool<T>() where T : class;

        /// <summary>
        /// 从池中租用一个对象。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        /// <returns>租用的对象实例。</returns>
        T Rent<T>() where T : class;

        /// <summary>
        /// 将对象归还到池中。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        /// <param name="obj">要归还的对象。</param>
        void Return<T>(T obj) where T : class;

        /// <summary>
        /// 获取所有活跃池的统计信息。
        /// </summary>
        /// <returns>统计信息列表。</returns>
        IEnumerable<PoolStatistics> GetAllStatistics();

        /// <summary>
        /// 重置所有池的统计信息。
        /// </summary>
        void ResetAllStatistics();
    }
}
