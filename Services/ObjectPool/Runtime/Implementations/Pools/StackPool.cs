using System;
using System.Collections.Generic;

namespace EasyPack.ObjectPool
{
    /// <summary>
    ///     泛型 Stack&lt;T&gt; 专用对象池。
    /// </summary>
    /// <typeparam name="T">栈中元素的类型。</typeparam>
    public static class StackPool<T>
    {
        private static readonly Stack<Stack<T>> _stack = new(8);
        private const int DefaultCapacity = 8;


        /// <summary>
        ///     获取当前池中的对象数量。
        /// </summary>
        public static int Count => _stack.Count;

        /// <summary>
        ///     从池中获取一个栈。如果池为空，则创建新栈。
        /// </summary>
        /// <returns>清洁的栈实例。</returns>
        public static Stack<T> Get()
        {
            return _stack.Count > 0 ? _stack.Pop() : new Stack<T>(DefaultCapacity);
        }

        /// <summary>
        ///     从池中获取一个栈，并指定初始容量。
        ///     注意：Stack 无法动态扩容，此方法仅返回池化对象。
        /// </summary>
        /// <param name="capacity">栈的初始容量（仅作为参考，实际不生效）。</param>
        /// <returns>清洁的栈实例。</returns>
        public static Stack<T> Get(int capacity)
        {
            // Stack<T> 没有 EnsureCapacity 方法，也无法动态设置容量
            // 这里仅提供 API 一致性，实际返回标准池对象
            return Get();
        }

        /// <summary>
        ///     将栈归还到池中。栈将自动清空。
        /// </summary>
        /// <param name="stack">要归还的栈。</param>
        public static void Release(Stack<T> stack)
        {
            if (stack == null) return;

            stack.Clear();
            _stack.Push(stack);
        }

        /// <summary>
        ///     清空池中的所有栈。
        /// </summary>
        /// <param name="onClearItem">可选的清理回调，用于处理每个栈。</param>
        public static void Clear(Action<Stack<T>> onClearItem = null)
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
    ///     Stack&lt;T&gt; 的扩展方法，提供便捷的归还方式。
    /// </summary>
    public static class StackPoolExtensions
    {
        /// <summary>
        ///     将栈归还到池中。
        /// </summary>
        public static void Release2Pool<T>(this Stack<T> stack)
        {
            StackPool<T>.Release(stack);
        }
    }
}