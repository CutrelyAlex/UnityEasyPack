using EasyPack.Architecture;
using System.Collections.Generic;

namespace EasyPack.ObjectPool
{
    /// <summary>
    /// 泛型 HashSet&lt;T&gt; 专用对象池。
    /// </summary>
    /// <typeparam name="T">集合中元素的类型。</typeparam>
    public static class HashSetPool<T>
    {
        private static ObjectPool<HashSet<T>> _pool;
        private static bool _isInitialized = false;
        private static readonly object _lockObj = new();

        /// <summary>
        /// 初始化哈希集合池。
        /// </summary>
        /// <param name="poolService">对象池服务实例。</param>
        /// <param name="maxCapacity">池的最大容量，默认为32。</param>
        public static void Initialize(IObjectPoolService poolService, int maxCapacity = 32)
        {
            if (_isInitialized)
            {
                UnityEngine.Debug.LogWarning("[HashSetPool] 已初始化，跳过重复初始化");
                return;
            }

            lock (_lockObj)
            {
                if (_isInitialized) return;

                _pool = poolService.CreatePool(
                    factory: () => new HashSet<T>(),
                    cleanup: hashSet => hashSet.Clear(),
                    maxCapacity: maxCapacity
                );
                _isInitialized = true;
            }
        }

        /// <summary>
        /// 自动初始化（如果尚未初始化）。
        /// </summary>
        private static void EnsureInitialized()
        {
            if (_isInitialized) return;

            lock (_lockObj)
            {
                if (_isInitialized) return;

                try
                {
                    var poolService = EasyPackArchitecture.GetObjectPoolServiceAsync().GetAwaiter().GetResult();
                    Initialize(poolService);
                }
                catch (System.Exception ex)
                {
                    throw new System.InvalidOperationException(
                        $"HashSetPool<{typeof(T).Name}> 自动初始化失败。请确保 EasyPackArchitecture 已正确初始化，或手动调用 HashSetPool<T>.Initialize()", ex);
                }
            }
        }

        /// <summary>
        /// 从池中租用一个哈希集合。
        /// </summary>
        /// <returns>清洁的哈希集合实例。</returns>
        public static HashSet<T> Rent()
        {
            EnsureInitialized();
            return _pool.Rent();
        }

        /// <summary>
        /// 将哈希集合归还到池中。集合将自动清空。
        /// </summary>
        /// <param name="hashSet">要归还的哈希集合。</param>
        public static void Return(HashSet<T> hashSet)
        {
            if (!_isInitialized)
            {
                return;
            }
            _pool.Return(hashSet);
        }

        /// <summary>
        /// 获取池的统计信息。
        /// </summary>
        public static PoolStatistics GetStatistics()
        {
            return _pool?.GetStatistics();
        }
    }
}
