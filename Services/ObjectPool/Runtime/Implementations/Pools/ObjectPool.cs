using System;

namespace EasyPack.ObjectPool
{
    /// <summary>
    ///     对象池包装器。
    /// </summary>
    /// <typeparam name="T">对象类型，必须是引用类型。</typeparam>
    public class ObjectPool<T> : IClearable where T : class
    {
        private readonly UnityEngine.Pool.ObjectPool<T> _pool;
        private readonly bool _isPoolable;

        /// <summary>
        ///     获取当前池中的对象数量。
        /// </summary>
        public int CountInactive => _pool.CountInactive;

        /// <summary>
        ///     获取池的最大容量。
        /// </summary>
        public int MaxCapacity { get; }

        /// <summary>
        ///     创建对象池实例。
        /// </summary>
        /// <param name="factory">对象工厂方法，用于创建新对象。</param>
        /// <param name="cleanup">对象清理方法，在归还时调用（可选）。</param>
        /// <param name="maxCapacity">池的最大容量，默认为64。超出时将丢弃归还的对象。</param>
        public ObjectPool(Func<T> factory, Action<T> cleanup = null, int maxCapacity = 64)
        {
            if (maxCapacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxCapacity), "最大容量必须大于0");
            }

            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            MaxCapacity = maxCapacity;
            _isPoolable = typeof(IPoolable).IsAssignableFrom(typeof(T));

            // 使用 Unity 官方 ObjectPool，传入生命周期回调
            _pool = new(
                factory,
                OnGet,
                obj => OnRelease(obj, cleanup),
                obj => OnRelease(obj, cleanup),
                false,
                Math.Min(maxCapacity, 16),
                maxCapacity
            );
        }

        /// <summary>
        ///     对象从池中获取时的回调。
        /// </summary>
        private void OnGet(T obj)
        {
            if (_isPoolable && obj is IPoolable poolable)
            {
                poolable.IsRecycled = false;
                poolable.OnAllocate();
            }
        }

        /// <summary>
        ///     对象归还到池中时的回调。
        /// </summary>
        private void OnRelease(T obj, Action<T> cleanup)
        {
            if (_isPoolable && obj is IPoolable poolable)
            {
                poolable.OnRecycle();
                poolable.IsRecycled = true;
            }

            cleanup?.Invoke(obj);
        }

        /// <summary>
        ///     从池中租用一个对象。如果池为空，则创建新对象。
        /// </summary>
        /// <returns>可用的对象实例。</returns>
        public T Rent() => _pool.Get();

        /// <summary>
        ///     将对象归还到池中。如果池已满，对象将被丢弃。
        ///     对于实现了 IPoolable 的对象，会检查是否已被回收。
        /// </summary>
        /// <param name="obj">要归还的对象。</param>
        public void Return(T obj)
        {
            if (obj == null)
            {
                return;
            }

            // 防止重复回收
            if (_isPoolable && obj is IPoolable poolable && poolable.IsRecycled)
            {
                return;
            }

            _pool.Release(obj);
        }

        /// <summary>
        ///     清空池中的所有对象。
        /// </summary>
        public void Clear()
        {
            _pool.Clear();
        }
    }
}