using System;
using System.Collections.Generic;

namespace EasyPack
{
    /// <summary>
    /// 泛型 Stack&lt;T&gt; 专用对象池。
    /// </summary>
    /// <typeparam name="T">栈中元素的类型。</typeparam>
    public static class StackPool<T>
    {
        private static ObjectPool<Stack<T>> _pool;
        private static bool _isInitialized = false;
        private static readonly object _lockObj = new object();

        /// <summary>
        /// 初始化栈池。
        /// </summary>
        /// <param name="poolService">对象池服务实例。</param>
        /// <param name="maxCapacity">池的最大容量，默认为32。</param>
        public static void Initialize(IObjectPoolService poolService, int maxCapacity = 32)
        {
            if (_isInitialized)
            {
                UnityEngine.Debug.LogWarning("[StackPool] 已初始化，跳过重复初始化");
                return;
            }

            lock (_lockObj)
            {
                if (_isInitialized) return;

                _pool = poolService.CreatePool(
                    factory: () => new Stack<T>(),
                    cleanup: stack => stack.Clear(),
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
                        $"StackPool<{typeof(T).Name}> 自动初始化失败。请确保 EasyPackArchitecture 已正确初始化，或手动调用 StackPool<T>.Initialize()", ex);
                }
            }
        }

        /// <summary>
        /// 从池中租用一个栈。
        /// </summary>
        /// <returns>清洁的栈实例。</returns>
        public static Stack<T> Rent()
        {
            EnsureInitialized();
            return _pool.Rent();
        }

        /// <summary>
        /// 将栈归还到池中。栈将自动清空。
        /// </summary>
        /// <param name="stack">要归还的栈。</param>
        public static void Return(Stack<T> stack)
        {
            if (!_isInitialized)
            {
                return;
            }
            _pool.Return(stack);
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
