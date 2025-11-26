using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace EasyPack.ObjectPool
{
    /// <summary>
    ///     对象池模板管理器。各模块可以继承此基类来管理自己的对象池实例。
    /// </summary>
    public abstract class PoolManagerBase
    {
        /// <summary>
        ///     双层字典：按类型分类，在同一类型下使用 PoolTag 区分不同配置的池。
        /// </summary>
        private readonly ConcurrentDictionary<Type, ConcurrentDictionary<ulong, object>> _pools
            = new();

        /// <summary>
        ///     获取或创建对象池。
        /// </summary>
        protected ObjectPool<T> GetOrCreatePool<T>(
            string configKey,
            Func<T> factory,
            Action<T> cleanup = null,
            int maxCapacity = 64) where T : class
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));

            Type typeKey = typeof(T);
            var tag = PoolTag.Create<T>(configKey);
            var poolsByType = _pools.GetOrAdd(typeKey, _ => new());

            return (ObjectPool<T>)poolsByType.GetOrAdd(tag.Value, _ =>
            {
                var pool = new ObjectPool<T>(factory, cleanup, maxCapacity) { PoolTag = tag };
                return pool;
            });
        }

        /// <summary>
        ///     获取指定类型和配置的对象池。
        /// </summary>
        protected ObjectPool<T> GetPool<T>(string configKey) where T : class
        {
            Type typeKey = typeof(T);
            if (!_pools.TryGetValue(typeKey, out var poolsByType)) return null;

            var tag = PoolTag.Create<T>(configKey);
            return poolsByType.TryGetValue(tag.Value, out object pool)
                ? pool as ObjectPool<T>
                : null;
        }

        /// <summary>
        ///     销毁指定类型和配置的对象池。
        /// </summary>
        protected bool DestroyPool<T>(string configKey) where T : class
        {
            Type typeKey = typeof(T);
            if (!_pools.TryGetValue(typeKey, out var poolsByType)) return false;

            var tag = PoolTag.Create<T>(configKey);
            if (!poolsByType.TryRemove(tag.Value, out object pool)) return false;

            (pool as ObjectPool<T>)?.Clear();
            if (poolsByType.IsEmpty) _pools.TryRemove(typeKey, out _);

            return true;
        }

        /// <summary>
        ///     清空所有池。
        /// </summary>
        public void Clear()
        {
            foreach (var byType in _pools.Values)
            foreach (object pool in byType.Values)
            {
                MethodInfo clearMethod = pool.GetType().GetMethod("Clear");
                clearMethod?.Invoke(pool, null);
            }

            _pools.Clear();
        }
    }
}