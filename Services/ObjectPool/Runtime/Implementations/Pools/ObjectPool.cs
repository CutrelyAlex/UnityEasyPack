using System;
using UnityEngine.Pool;

namespace EasyPack.ObjectPool
{
    /// <summary>
    /// 对象池包装器，基于 UnityEngine.Pool.ObjectPool 实现。
    /// </summary>
    /// <typeparam name="T">对象类型，必须是引用类型。</typeparam>
    public class ObjectPool<T> where T : class
    {
        private readonly UnityEngine.Pool.ObjectPool<T> _pool;
        private readonly int _maxCapacity;

        /// <summary>
        /// 获取当前池中的对象数量。
        /// </summary>
        public int CountInactive => _pool.CountInactive;

        /// <summary>
        /// 获取池的最大容量。
        /// </summary>
        public int MaxCapacity => _maxCapacity;

        /// <summary>
        /// 该池的标记。用于区分同类型不同配置的池。
        /// 默认值为 <see cref="PoolTag.Default"/>。
        /// </summary>
        public PoolTag PoolTag { get; internal set; } = PoolTag.Default;

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

            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            _maxCapacity = maxCapacity;

            // 使用 Unity 官方 ObjectPool，传入清理方法
            _pool = new UnityEngine.Pool.ObjectPool<T>(
                createFunc: factory,
                actionOnGet: null,
                actionOnRelease: cleanup,
                actionOnDestroy: cleanup,
                collectionCheck: false,
                defaultCapacity: Math.Min(maxCapacity, 16),
                maxSize: maxCapacity
            );
        }

        /// <summary>
        /// 从池中租用一个对象。如果池为空，则创建新对象。
        /// </summary>
        /// <returns>可用的对象实例。</returns>
        public T Rent()
        {
            return _pool.Get();
        }

        /// <summary>
        /// 将对象归还到池中。如果池已满，对象将被丢弃。
        /// </summary>
        /// <param name="obj">要归还的对象。</param>
        public void Return(T obj)
        {
            if (obj == null)
                return;

            _pool.Release(obj);
        }

        /// <summary>
        /// 清空池中的所有对象。
        /// </summary>
        public void Clear()
        {
            _pool.Clear();
        }
    }
}
