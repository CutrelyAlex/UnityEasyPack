using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using EasyPack.ENekoFramework;
using UnityEngine;

namespace EasyPack.ObjectPool
{
    /// <summary>
    /// 对象池服务，提供全局对象池管理功能。
    /// </summary>
    public class ObjectPoolService : BaseService, IObjectPoolService
    {
        private readonly ConcurrentDictionary<Type, object> _pools = new();
        private bool _statisticsEnabled = false;

        /// <summary>
        /// 获取或设置是否启用统计收集。默认为 false。
        /// 仅在编辑器监控窗口打开时启用。
        /// </summary>
        public bool StatisticsEnabled
        {
            get => _statisticsEnabled;
            set
            {
                _statisticsEnabled = value;
                // 同步到所有现有的池
                foreach (var kvp in _pools)
                {
                    var poolType = kvp.Value.GetType();
                    var property = poolType.GetProperty("CollectStatistics");
                    property?.SetValue(kvp.Value, value);
                }
            }
        }

        /// <summary>
        /// 服务初始化时调用。
        /// </summary>
        protected override Task OnInitializeAsync()
        {
            Debug.Log("[ObjectPoolService] 对象池服务初始化完成");
            return Task.CompletedTask;
        }

        /// <summary>
        /// 服务暂停时调用。
        /// </summary>
        protected override void OnPause()
        {
            Debug.Log("[ObjectPoolService] 对象池服务已暂停");
        }

        /// <summary>
        /// 服务恢复时调用。
        /// </summary>
        protected override void OnResume()
        {
            Debug.Log("[ObjectPoolService] 对象池服务已恢复");
        }

        /// <summary>
        /// 服务释放时调用，清空所有对象池。
        /// </summary>
        protected override Task OnDisposeAsync()
        {
            Debug.Log("[ObjectPoolService] 开始释放对象池服务");

            foreach (var kvp in _pools)
            {
                var poolType = kvp.Value.GetType();
                var clearMethod = poolType.GetMethod("Clear");
                clearMethod?.Invoke(kvp.Value, null);

                var stats = poolType.GetMethod("GetStatistics")?.Invoke(kvp.Value, null);
                if (stats != null)
                {
                    Debug.Log($"[ObjectPoolService] 池统计: {stats}");
                }
            }

            _pools.Clear();
            Debug.Log("[ObjectPoolService] 对象池服务释放完成");
            return Task.CompletedTask;
        }

        /// <summary>
        /// 创建指定类型的对象池。
        /// </summary>
        public ObjectPool<T> CreatePool<T>(Func<T> factory, Action<T> cleanup = null, int maxCapacity = 64) where T : class
        {
            var type = typeof(T);
            if (_pools.ContainsKey(type))
            {
                Debug.LogWarning($"[ObjectPoolService] 类型 {type.Name} 的池已存在，将返回现有池");
                return GetPool<T>();
            }

            var pool = new ObjectPool<T>(factory, cleanup, maxCapacity);
            // 新池继承当前的统计设置
            pool.CollectStatistics = _statisticsEnabled;
            _pools[type] = pool;
            return pool;
        }

        /// <summary>
        /// 获取指定类型的对象池。
        /// </summary>
        public ObjectPool<T> GetPool<T>() where T : class
        {
            var type = typeof(T);
            if (_pools.TryGetValue(type, out var pool))
            {
                return pool as ObjectPool<T>;
            }
            return null;
        }

        /// <summary>
        /// 获取或创建指定类型的对象池。
        /// </summary>
        public ObjectPool<T> GetOrCreatePool<T>(Func<T> factory, Action<T> cleanup = null, int maxCapacity = 64) where T : class
        {
            var pool = GetPool<T>();
            if (pool != null)
            {
                return pool;
            }

            return CreatePool(factory, cleanup, maxCapacity);
        }

        /// <summary>
        /// 获取所有活跃的池的统计信息。
        /// </summary>
        public IEnumerable<PoolStatistics> GetAllStatistics()
        {
            var stats = new List<PoolStatistics>();
            foreach (var kvp in _pools)
            {
                var poolType = kvp.Value.GetType();
                var method = poolType.GetMethod("GetStatistics");
                if (method != null)
                {
                    var stat = method.Invoke(kvp.Value, null) as PoolStatistics;
                    if (stat != null)
                    {
                        stats.Add(stat);
                    }
                }
            }
            return stats;
        }

        /// <summary>
        /// 重置所有池的统计信息。
        /// </summary>
        public void ResetAllStatistics()
        {
            foreach (var kvp in _pools)
            {
                var poolType = kvp.Value.GetType();
                var method = poolType.GetMethod("ResetStatistics");
                method?.Invoke(kvp.Value, null);
            }
        }

        /// <summary>
        /// 销毁指定类型的对象池。
        /// </summary>
        public bool DestroyPool<T>() where T : class
        {
            var type = typeof(T);
            if (_pools.TryRemove(type, out var pool))
            {
                var poolTyped = pool as ObjectPool<T>;
                poolTyped?.Clear();
                Debug.Log($"[ObjectPoolService] 已销毁类型 {type.Name} 的对象池");
                return true;
            }
            return false;
        }

        /// <summary>
        /// 从池中租用一个对象。
        /// </summary>
        public T Rent<T>() where T : class
        {
            var pool = GetPool<T>();
            if (pool == null)
            {
                throw new InvalidOperationException($"类型 {typeof(T).Name} 的对象池不存在，请先调用 CreatePool 或 GetOrCreatePool");
            }
            return pool.Rent();
        }

        /// <summary>
        /// 将对象归还到池中。
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
