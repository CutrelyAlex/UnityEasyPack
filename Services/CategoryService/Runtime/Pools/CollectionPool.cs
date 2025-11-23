using System.Collections.Generic;

namespace EasyPack.Category
{
    /// <summary>
    /// 集合对象池管理器
    /// 为 CategoryService 提供 List 和 HashSet 的对象池支持
    /// 使用 EasyPack ObjectPool 系统作为底层实现
    /// </summary>
    public static class CollectionPool
    {
        #region List<T> 池化

        /// <summary>
        /// 从 ListPool 中获取 List&lt;T&gt; 实例
        /// </summary>
        /// <typeparam name="T">列表元素类型</typeparam>
        /// <returns>清洁的 List&lt;T&gt; 实例</returns>
        public static List<T> GetList<T>()
        {
            return ObjectPool.ListPool<T>.Rent();
        }

        /// <summary>
        /// 将 List&lt;T&gt; 实例归还到 ListPool
        /// </summary>
        /// <typeparam name="T">列表元素类型</typeparam>
        /// <param name="list">要归还的列表</param>
        public static void ReturnList<T>(List<T> list)
        {
            if (list == null) return;
            ObjectPool.ListPool<T>.Return(list);
        }

        #endregion

        #region HashSet<T> 池化

        /// <summary>
        /// 从 HashSetPool 中获取 HashSet&lt;T&gt; 实例
        /// </summary>
        /// <typeparam name="T">集合元素类型</typeparam>
        /// <returns>清洁的 HashSet&lt;T&gt; 实例</returns>
        public static HashSet<T> GetHashSet<T>()
        {
            return ObjectPool.HashSetPool<T>.Rent();
        }

        /// <summary>
        /// 将 HashSet&lt;T&gt; 实例归还到 HashSetPool
        /// </summary>
        /// <typeparam name="T">集合元素类型</typeparam>
        /// <param name="hashSet">要归还的集合</param>
        public static void ReturnHashSet<T>(HashSet<T> hashSet)
        {
            if (hashSet == null) return;
            ObjectPool.HashSetPool<T>.Return(hashSet);
        }

        /// <summary>
        /// 从 HashSetPool 中获取 HashSet&lt;string&gt; 实例
        /// </summary>
        /// <returns>清洁的 HashSet&lt;string&gt; 实例</returns>
        public static HashSet<string> GetHashSet()
        {
            return ObjectPool.HashSetPool<string>.Rent();
        }

        /// <summary>
        /// 将 HashSet&lt;string&gt; 实例归还到 HashSetPool
        /// </summary>
        /// <param name="hashSet">要归还的集合</param>
        public static void ReturnHashSet(HashSet<string> hashSet)
        {
            if (hashSet == null) return;
            ObjectPool.HashSetPool<string>.Return(hashSet);
        }

        #endregion
    }
}