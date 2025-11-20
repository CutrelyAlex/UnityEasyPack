using System;
using System.Collections.Generic;

namespace EasyPack.ObjectPool
{
    /// <summary>
    /// 泛型对象池，提供对象复用功能以减少 GC 压力。
    /// </summary>
    /// <typeparam name="T">对象类型，必须是引用类型。</typeparam>
    public class ObjectPool<T> where T : class
    {
        private readonly Stack<T> _pool;
        private readonly Func<T> _factory;
        private readonly Action<T> _cleanup;

        // 统计数据（仅在启用统计时收集）

        /// <summary>
        /// 获取或设置是否启用统计收集。默认为 false。
        /// </summary>
        public bool CollectStatistics { get; set; }

        /// <summary>
        /// 获取总租用次数。
        /// </summary>
        public int RentCount { get; private set; }

        /// <summary>
        /// 获取对象创建次数。
        /// </summary>
        public int CreateCount { get; private set; }

        /// <summary>
        /// 获取池命中次数（从池中成功获取对象的次数）。
        /// </summary>
        public int HitCount { get; private set; }

        /// <summary>
        /// 获取池的峰值大小。
        /// </summary>
        public int PeakPoolSize { get; private set; }

        /// <summary>
        /// 获取当前池中的对象数量。
        /// </summary>
        public int CurrentPoolSize => _pool.Count;

        /// <summary>
        /// 获取池的最大容量。
        /// </summary>
        public int MaxCapacity { get; }

        /// <summary>
        /// 创建对象池实例。
        /// </summary>
        /// <param name="factory">对象工厂方法，用于创建新对象。</param>
        /// <param name="cleanup">对象清理方法，在归还时调用（可选）。</param>
        /// <param name="maxCapacity">池的最大容量，默认为64。超出时将丢弃归还的对象。</param>
        public ObjectPool(Func<T> factory, Action<T> cleanup = null, int maxCapacity = 64)
        {
            if (maxCapacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxCapacity), "最大容量必须大于0");

            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _cleanup = cleanup;
            MaxCapacity = maxCapacity;
            _pool = new Stack<T>(Math.Min(maxCapacity, 16)); // 初始容量避免过早扩容
            CollectStatistics = false; // 默认不收集统计，性能优先
        }

        /// <summary>
        /// 从池中租用一个对象。如果池为空，则创建新对象。
        /// </summary>
        /// <returns>可用的对象实例。</returns>
        public T Rent()
        {
            if (CollectStatistics)
            {
                RentCount++;
            }

            if (_pool.Count > 0)
            {
                if (CollectStatistics)
                {
                    HitCount++;
                }
                return _pool.Pop();
            }

            if (CollectStatistics)
            {
                CreateCount++;
            }
            return _factory();
        }

        /// <summary>
        /// 将对象归还到池中。如果池已满，对象将被丢弃。
        /// </summary>
        /// <param name="obj">要归还的对象。</param>
        public void Return(T obj)
        {
            if (obj == null)
                return;

            // 执行清理逻辑
            _cleanup?.Invoke(obj);

            // 检查容量限制
            if (_pool.Count >= MaxCapacity)
                return; // 丢弃对象，防止内存无限增长

            _pool.Push(obj);

            // 更新峰值
            if (CollectStatistics && _pool.Count > PeakPoolSize)
            {
                PeakPoolSize = _pool.Count;
            }
        }

        /// <summary>
        /// 清空池中的所有对象。
        /// </summary>
        public void Clear()
        {
            while (_pool.Count > 0)
            {
                var obj = _pool.Pop();
                _cleanup?.Invoke(obj);
            }

            // 重置统计信息
            if (CollectStatistics)
            {
                RentCount = 0;
                CreateCount = 0;
                HitCount = 0;
                PeakPoolSize = 0;
            }
        }

        /// <summary>
        /// 重置统计信息。仅在启用统计时有效。
        /// </summary>
        public void ResetStatistics()
        {
            if (!CollectStatistics)
                return;

            RentCount = 0;
            CreateCount = 0;
            HitCount = 0;
            PeakPoolSize = 0;
        }

        /// <summary>
        /// 获取池的统计信息。
        /// </summary>
        /// <returns>包含统计数据的对象。</returns>
        public PoolStatistics GetStatistics()
        {
            return new PoolStatistics
            {
                TypeName = typeof(T).Name,
                RentCount = RentCount,
                CreateCount = CreateCount,
                HitCount = HitCount,
                HitRate = RentCount > 0 ? (float)HitCount / RentCount : 0f,
                PeakPoolSize = PeakPoolSize,
                CurrentPoolSize = _pool.Count,
                MaxCapacity = MaxCapacity
            };
        }
    }
}
