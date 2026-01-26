using System;
using System.Collections.Generic;

namespace EasyPack.ObjectPool
{
    /// <summary>
    ///     泛型 HashSet&lt;T&gt; 专用对象池。
    /// </summary>
    /// <typeparam name="T">集合中元素的类型。</typeparam>
    public static class HashSetPool<T>
    {
        private static readonly Stack<HashSet<T>> _stack = new(8);
        private const int DefaultCapacity = 8;
        private const int MaxPoolSize = 128;

        /// <summary>
        ///     获取当前池中的对象数量。
        /// </summary>
        public static int Count => _stack.Count;

        /// <summary>
        ///     从池中获取一个哈希集合。如果池为空，则创建新集合。
        /// </summary>
        /// <returns>清洁的哈希集合实例。</returns>
        public static HashSet<T> Get()
        {
            return _stack.Count > 0 ? _stack.Pop() : new HashSet<T>(DefaultCapacity);
        }

        /// <summary>
        ///     从池中获取一个哈希集合，并指定初始容量。。
        /// </summary>
        /// <param name="capacity">集合的初始容量。</param>
        /// <returns>清洁的哈希集合实例。</returns>
        public static HashSet<T> Get(int capacity)
        {
            if (_stack.Count > 0)
            {
                var hashSet = _stack.Pop();
#if NET5_0_OR_GREATER || UNITY_2021_2_OR_NEWER
                hashSet.EnsureCapacity(capacity);
#endif
                return hashSet;
            }
            return new HashSet<T>(capacity);
        }

        /// <summary>
        ///     将哈希集合归还到池中。集合将自动清空。
        /// </summary>
        /// <param name="hashSet">要归还的哈希集合。</param>
        public static void Release(HashSet<T> hashSet)
        {
            if (hashSet == null) return;

            hashSet.Clear();

            if (_stack.Count < MaxPoolSize)
            {
                _stack.Push(hashSet);
            }
        }

        /// <summary>
        ///     清空池中的所有哈希集合。
        /// </summary>
        /// <param name="onClearItem">可选的清理回调，用于处理每个集合。</param>
        public static void Clear(Action<HashSet<T>> onClearItem = null)
        {
            if (onClearItem != null)
            {
                while (_stack.Count > 0)
                {
                    onClearItem(_stack.Pop());
                }
            }
            else
            {
                _stack.Clear();
            }
        }
    }

    /// <summary>
    ///     HashSet&lt;T&gt; 的扩展方法，提供便捷的归还方式。
    /// </summary>
    public static class HashSetPoolExtensions
    {
        /// <summary>
        ///     将哈希集合归还到池中。
        /// </summary>
        public static void Release2Pool<T>(this HashSet<T> hashSet)
        {
            HashSetPool<T>.Release(hashSet);
        }
    }
}