using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using EasyPack.ENekoFramework;
using UnityEngine;

namespace EasyPack.ObjectPool
{
    /// <summary>
    ///     对象池服务，提供全局对象池管理功能。
    ///     基于 UnityEngine.Pool.ObjectPool 实现。
    /// </summary>
    public class ObjectPoolService : BaseService, IObjectPoolService
    {
        private readonly ConcurrentDictionary<Type, object> _pools = new();

        /// <summary>
        ///     服务初始化时调用。
        /// </summary>
        protected override Task OnInitializeAsync()
        {
            Debug.Log("[ObjectPoolService] 对象池服务初始化完成");
            return Task.CompletedTask;
        }

        /// <summary>
        ///     服务释放时调用，清空所有对象池。
        /// </summary>
        protected override Task OnDisposeAsync()
        {
            Debug.Log("[ObjectPoolService] 开始释放对象池服务");

            foreach (var kvp in _pools)
            {
                if (kvp.Value is IClearable clearable)
                {
                    clearable.Clear();
                }
                else if (kvp.Value is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }

            _pools.Clear();
            Debug.Log("[ObjectPoolService] 对象池服务释放完成");
            return Task.CompletedTask;
        }

        /// <summary>
        ///     创建指定类型的对象池。
        /// </summary>
        public ObjectPool<T> CreatePool<T>(Func<T> factory, Action<T> cleanup = null, int maxCapacity = 64)
            where T : class
        {
            Type type = typeof(T);
            if (_pools.ContainsKey(type))
            {
                Debug.LogWarning($"[ObjectPoolService] 类型 {type.Name} 的池已存在，将返回现有池");
                return GetPool<T>();
            }

            var pool = new ObjectPool<T>(factory, cleanup, maxCapacity);
            _pools[type] = pool;
            return pool;
        }

        /// <summary>
        ///     获取指定类型的对象池。
        /// </summary>
        public ObjectPool<T> GetPool<T>() where T : class
        {
            Type type = typeof(T);
            if (_pools.TryGetValue(type, out object pool)) return pool as ObjectPool<T>;

            return null;
        }

        /// <summary>
        ///     获取或创建指定类型的对象池。
        /// </summary>
        public ObjectPool<T> GetOrCreatePool<T>(Func<T> factory, Action<T> cleanup = null, int maxCapacity = 64)
            where T : class
        {
            var pool = GetPool<T>();
            return pool ?? CreatePool(factory, cleanup, maxCapacity);
        }

        /// <summary>
        ///     销毁指定类型的对象池。
        /// </summary>
        public bool DestroyPool<T>() where T : class
        {
            Type type = typeof(T);
            if (_pools.TryRemove(type, out object pool))
            {
                var poolTyped = pool as ObjectPool<T>;
                poolTyped?.Clear();
                Debug.Log($"[ObjectPoolService] 已销毁类型 {type.Name} 的对象池");
                return true;
            }

            return false;
        }

        /// <summary>
        ///     从池中租用一个对象。
        /// </summary>
        public T Rent<T>() where T : class
        {
            var pool = GetPool<T>();
            return pool == null
                ? throw new InvalidOperationException($"类型 {typeof(T).Name} 的对象池不存在，请先调用 CreatePool 或 GetOrCreatePool")
                : pool.Rent();
        }

        /// <summary>
        ///     将对象归还到池中。
        /// </summary>
        public void Return<T>(T obj) where T : class
        {
            var pool = GetPool<T>();
            if (pool == null)
            {
                Debug.LogWarning($"[ObjectPoolService] 类型 {typeof(T).Name} 的对象池不存在，无法归还对象");
                return;
            }

            pool.Return(obj);
        }
    }
}