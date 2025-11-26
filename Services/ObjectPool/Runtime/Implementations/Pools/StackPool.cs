using EasyPack.Architecture;
using System.Collections.Generic;

namespace EasyPack.ObjectPool
{
    /// <summary>
    ///     泛型 Stack&lt;T&gt; 专用对象池。
    /// </summary>
    /// <typeparam name="T">栈中元素的类型。</typeparam>
    public static class StackPool<T>
    {
        private static ObjectPool<Stack<T>> _pool;
        private static bool _isInitialized = false;
        private static readonly object _lockObj = new();

        /// <summary>
        ///     初始化栈池。
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
                    () => new Stack<T>(),
                    stack => stack.Clear(),
                    maxCapacity
                );
                _isInitialized = true;
            }
        }

        /// <summary>
        ///     异步初始化栈池。在 EasyPackArchitecture 启动时集中调用。
        /// </summary>
        /// <param name="maxCapacity">池的最大容量，默认为32。</param>
        public static async System.Threading.Tasks.Task InitializeAsync(int maxCapacity = 32)
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

            throw new System.InvalidOperationException(
                $"StackPool<{typeof(T).Name}> 尚未初始化。请在 EasyPackArchitecture.OnInit() 或启动阶段调用 StackPool<T>.InitializeAsync()");
        }

        /// <summary>
        ///     从池中租用一个栈。
        /// </summary>
        /// <returns>清洁的栈实例。</returns>
        public static Stack<T> Rent()
        {
            EnsureInitialized();
            return _pool.Rent();
        }

        /// <summary>
        ///     将栈归还到池中。栈将自动清空。
        /// </summary>
        /// <param name="stack">要归还的栈。</param>
        public static void Return(Stack<T> stack)
        {
            if (!_isInitialized) return;

            _pool.Return(stack);
        }
    }
}