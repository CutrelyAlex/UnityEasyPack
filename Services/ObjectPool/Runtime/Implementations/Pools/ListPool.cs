using System;
using System.Collections.Generic;

namespace EasyPack.ObjectPool
{
    /// <summary>
    ///     泛型 List&lt;T&gt; 专用对象池。
    /// </summary>
    /// <typeparam name="T">列表中元素的类型。</typeparam>
    public static class ListPool<T>
    {
        private static readonly Stack<List<T>> _stack = new(8);
        private const int DefaultCapacity = 8;
        private const int MaxPoolSize = 128;

        /// <summary>
        ///     获取当前池中的对象数量。
        /// </summary>
        public static int Count => _stack.Count;

        /// <summary>
        ///     从池中获取一个列表。如果池为空，则创建新列表。
        /// </summary>
        /// <returns>清洁的列表实例。</returns>
        public static List<T> Get()
        {
            return _stack.Count > 0 ? _stack.Pop() : new List<T>(DefaultCapacity);
        }

        /// <summary>
        ///     从池中获取一个列表，并指定初始容量。
        /// </summary>
        /// <param name="capacity">列表的初始容量。</param>
        /// <returns>清洁的列表实例。</returns>
        public static List<T> Get(int capacity)
        {
            var list = Get();
            if (list.Capacity < capacity)
            {
                list.Capacity = capacity;
            }
            return list;
        }

        /// <summary>
        ///     将列表归还到池中。列表将自动清空。
        /// </summary>
        /// <param name="list">要归还的列表。</param>
        public static void Release(List<T> list)
        {
            if (list == null) return;

            list.Clear();

            if (_stack.Count < MaxPoolSize)
            {
                _stack.Push(list);
            }
        }

        /// <summary>
        ///     清空池中的所有列表。
        /// </summary>
        /// <param name="onClearItem">可选的清理回调，用于处理每个列表。</param>
        public static void Clear(Action<List<T>> onClearItem = null)
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
    ///     List&lt;T&gt; 的扩展方法，提供便捷的归还方式。
    /// </summary>
    public static class ListPoolExtensions
    {
        /// <summary>
        ///     将列表归还到池中。
        /// </summary>
        public static void Release2Pool<T>(this List<T> list)
        {
            ListPool<T>.Release(list);
        }
    }
}