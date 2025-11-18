using System;
using System.Collections;
using System.Collections.Generic;

namespace EasyPack.ENekoFramework
{
    /// <summary>
    /// 响应式集合，支持集合变更事件通知。
    /// 当集合发生添加、移除、清空操作时，自动触发相应事件。
    /// </summary>
    /// <typeparam name="T">集合元素类型</typeparam>
    public class BindableCollection<T> : IBindableCollection<T>
    {
        private readonly List<T> _items = new List<T>();

        /// <summary>
        /// 当集合中添加新项时触发。
        /// </summary>
        public event Action<T> OnItemAdded;

        /// <summary>
        /// 当集合中移除项时触发。
        /// </summary>
        public event Action<T> OnItemRemoved;

        /// <summary>
        /// 当集合被清空时触发。
        /// </summary>
        public event Action OnCollectionCleared;

        /// <summary>
        /// 集合中的元素数量。
        /// </summary>
        public int Count => _items.Count;

        /// <summary>
        /// 通过索引访问集合元素。
        /// 设置索引时会触发OnItemRemoved和OnItemAdded事件。
        /// </summary>
        /// <param name="index">元素索引</param>
        /// <returns>指定索引的元素</returns>
        public T this[int index]
        {
            get => _items[index];
            set
            {
                var oldItem = _items[index];
                _items[index] = value;
                
                OnItemRemoved?.Invoke(oldItem);
                OnItemAdded?.Invoke(value);
            }
        }

        /// <summary>
        /// 向集合末尾添加元素。
        /// </summary>
        /// <param name="item">要添加的元素</param>
        public void Add(T item)
        {
            _items.Add(item);
            OnItemAdded?.Invoke(item);
        }

        /// <summary>
        /// 从集合中移除第一个匹配的元素。
        /// </summary>
        /// <param name="item">要移除的元素</param>
        /// <returns>如果成功移除返回 true，否则返回 false</returns>
        public bool Remove(T item)
        {
            bool removed = _items.Remove(item);
            if (removed)
            {
                OnItemRemoved?.Invoke(item);
            }
            return removed;
        }

        /// <summary>
        /// 清空集合中的所有元素。
        /// </summary>
        public void Clear()
        {
            _items.Clear();
            OnCollectionCleared?.Invoke();
        }

        /// <summary>
        /// 检查集合中是否包含指定元素。
        /// </summary>
        /// <param name="item">要检查的元素</param>
        /// <returns>如果包含返回 true，否则返回 false</returns>
        public bool Contains(T item)
        {
            return _items.Contains(item);
        }

        /// <summary>
        /// 返回集合的枚举器。
        /// </summary>
        /// <returns>枚举器</returns>
        public IEnumerator<T> GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        /// <summary>
        /// 返回集合的非泛型枚举器。
        /// </summary>
        /// <returns>非泛型枚举器</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
