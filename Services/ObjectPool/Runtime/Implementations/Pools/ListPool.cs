using EasyPack.Architecture;
using System.Collections.Generic;

namespace EasyPack.ObjectPool
{
    /// <summary>
    /// 泛型 List&lt;T&gt; 专用对象池。
    /// </summary>
    /// <typeparam name="T">列表中元素的类型。</typeparam>
    public static class ListPool<T>
    {
        private static ObjectPool<List<T>> _pool;
        private static bool _isInitialized = false;
        private static readonly object _lockObj = new();

        /// <summary>
        /// 初始化列表池。
        /// </summary>
        /// <param name="poolService">对象池服务实例。</param>
        /// <param name="maxCapacity">池的最大容量，默认为32。</param>
        public static void Initialize(IObjectPoolService poolService, int maxCapacity = 32)
        {
            if (_isInitialized)
            {
                UnityEngine.Debug.LogWarning("[ListPool] 已初始化，跳过重复初始化");
                return;
            }

            lock (_lockObj)
            {
                if (_isInitialized) return;

                _pool = poolService.CreatePool(
                    factory: () => new List<T>(),
                    cleanup: list => list.Clear(),
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
                        $"ListPool<{typeof(T).Name}> 自动初始化失败。请确保 EasyPackArchitecture 已正确初始化，或手动调用 ListPool<T>.Initialize()", ex);
                }
            }
        }

        /// <summary>
        /// 从池中租用一个列表。
        /// </summary>
        /// <returns>清洁的列表实例。</returns>
        public static List<T> Rent()
        {
            EnsureInitialized();
            return _pool.Rent();
        }

        /// <summary>
        /// 将列表归还到池中。列表将自动清空。
        /// </summary>
        /// <param name="list">要归还的列表。</param>
        public static void Return(List<T> list)
        {
            if (!_isInitialized)
            {
                return;
            }
            _pool.Return(list);
        }

    }
}
