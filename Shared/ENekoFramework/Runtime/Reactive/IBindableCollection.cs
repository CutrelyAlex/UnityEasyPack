using System;
using System.Collections.Generic;

namespace EasyPack.ENekoFramework
{
    /// <summary>
    /// 可绑定集合接口，提供集合变更事件通知。
    /// 支持监听集合项的添加、移除和清空操作。
    /// </summary>
    /// <typeparam name="T">集合元素类型</typeparam>
    public interface IBindableCollection<T> : IEnumerable<T>
    {
        /// <summary>
        /// 当集合中添加新项时触发。
        /// </summary>
        event Action<T> OnItemAdded;

        /// <summary>
        /// 当集合中移除项时触发。
        /// </summary>
        event Action<T> OnItemRemoved;

        /// <summary>
        /// 当集合被清空时触发。
        /// </summary>
        event Action OnCollectionCleared;

        /// <summary>
        /// 集合中的元素数量。
        /// </summary>
        int Count { get; }

        /// <summary>
        /// 通过索引访问集合元素。
        /// </summary>
        /// <param name="index">元素索引</param>
        /// <returns>指定索引的元素</returns>
        T this[int index] { get; set; }

        /// <summary>
        /// 向集合中添加元素。
        /// </summary>
        /// <param name="item">要添加的元素</param>
        void Add(T item);

        /// <summary>
        /// 从集合中移除元素。
        /// </summary>
        /// <param name="item">要移除的元素</param>
        /// <returns>如果成功移除返回 true，否则返回 false</returns>
        bool Remove(T item);

        /// <summary>
        /// 清空集合中的所有元素。
        /// </summary>
        void Clear();

        /// <summary>
        /// 检查集合中是否包含指定元素。
        /// </summary>
        /// <param name="item">要检查的元素</param>
        /// <returns>如果包含返回 true，否则返回 false</returns>
        bool Contains(T item);
    }
}
