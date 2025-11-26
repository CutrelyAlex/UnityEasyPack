using System.Collections.Generic;
using EasyPack.Architecture;
using EasyPack.ObjectPool;

namespace EasyPack.EmeCardSystem
{
    /// <summary>
    ///     Stack&lt;(Card node, int depth)&gt; 专用对象池，用于 EmeCard 系统中的遍历操作。
    /// </summary>
    public static class TraversalStackPool
    {
        private static ObjectPool<Stack<(Card node, int depth)>> _pool;
        private static bool _isInitialized = false;

        /// <summary>
        ///     初始化遍历栈池。
        /// </summary>
        /// <param name="poolService">对象池服务实例。</param>
        /// <param name="maxCapacity">池的最大容量，默认为32。</param>
        private static void Initialize(IObjectPoolService poolService, int maxCapacity = 32)
        {
            if (_isInitialized)
            {
                UnityEngine.Debug.LogWarning("[TraversalStackPool] 已初始化，跳过重复初始化");
                return;
            }

            _pool = poolService.CreatePool(
                () => new Stack<(Card node, int depth)>(),
                stack => stack.Clear(),
                maxCapacity
            );
            _isInitialized = true;
        }

        /// <summary>
        ///     自动初始化（如果尚未初始化）。
        /// </summary>
        private static void EnsureInitialized()
        {
            if (_isInitialized) return;

            // 尝试自动初始化
            try
            {
                IObjectPoolService poolService =
                    EasyPackArchitecture.GetObjectPoolServiceAsync().GetAwaiter().GetResult();
                Initialize(poolService);
            }
            catch (System.Exception ex)
            {
                throw new System.InvalidOperationException(
                    "TraversalStackPool 自动初始化失败。请确保 EasyPackArchitecture 已正确初始化，或手动调用 TraversalStackPool.Initialize()",
                    ex);
            }
        }

        /// <summary>
        ///     从池中租用一个遍历栈。
        /// </summary>
        /// <returns>清洁的遍历栈实例。</returns>
        public static Stack<(Card node, int depth)> Rent()
        {
            EnsureInitialized();
            return _pool.Rent();
        }

        /// <summary>
        ///     将遍历栈归还到池中。栈将自动清空。
        /// </summary>
        /// <param name="stack">要归还的遍历栈。</param>
        public static void Return(Stack<(Card node, int depth)> stack)
        {
            if (!_isInitialized) return;

            _pool.Return(stack);
        }
    }
}