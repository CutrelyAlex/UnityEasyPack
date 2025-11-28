using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EasyPack.Architecture;
using UnityEngine;

namespace EasyPack.ObjectPool
{
    /// <summary>
    ///     泛型 HashSet&lt;T&gt; 专用对象池。
    /// </summary>
    /// <typeparam name="T">集合中元素的类型。</typeparam>
    public static class HashSetPool<T>
    {
        private static ObjectPool<HashSet<T>> _pool;
        private static bool _isInitialized;
        private static readonly object _lockObj = new();

        /// <summary>
        ///     初始化哈希集合池。
        /// </summary>
        /// <param name="poolService">对象池服务实例。</param>
        /// <param name="maxCapacity">池的最大容量，默认为32。</param>
        public static void Initialize(IObjectPoolService poolService, int maxCapacity = 32)
        {
            if (_isInitialized)
            {
                Debug.LogWarning("[HashSetPool] 已初始化，跳过重复初始化");
                return;
            }

            lock (_lockObj)
            {
                if (_isInitialized) return;

                _pool = poolService.CreatePool(
                    () => new HashSet<T>(),
                    hashSet => hashSet.Clear(),
                    maxCapacity
                );
                _isInitialized = true;
            }
        }

        /// <summary>
        ///     异步初始化哈希集合池。在 EasyPackArchitecture 启动时集中调用。
        /// </summary>
        /// <param name="maxCapacity">池的最大容量，默认为32。</param>
        public static async Task InitializeAsync(int maxCapacity = 32)
        {
            if (_isInitialized) return;

            IObjectPoolService poolService = await EasyPackArchitecture.GetObjectPoolServiceAsync();
            Initialize(poolService, maxCapacity);
        }

        /// <summary>
        ///     确保池已初始化，否则抛出异常。
        /// </summary>
        private static void EnsureInitialized()
        {
            if (_isInitialized) return;

            throw new InvalidOperationException(
                $"HashSetPool<{typeof(T).Name}> 尚未初始化。请在 EasyPackArchitecture.OnInit() 或启动阶段调用 HashSetPool<T>.InitializeAsync()");
        }

        /// <summary>
        ///     从池中租用一个哈希集合。
        /// </summary>
        /// <returns>清洁的哈希集合实例。</returns>
        public static HashSet<T> Rent()
        {
            EnsureInitialized();
            return _pool.Rent();
        }

        /// <summary>
        ///     将哈希集合归还到池中。集合将自动清空。
        /// </summary>
        /// <param name="hashSet">要归还的哈希集合。</param>
        public static void Return(HashSet<T> hashSet)
        {
            if (!_isInitialized) return;

            _pool.Return(hashSet);
        }
    }
}