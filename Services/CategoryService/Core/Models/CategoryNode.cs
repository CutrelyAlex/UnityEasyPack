using System.Collections.Generic;
using System.Threading;

namespace EasyPack.Category
{
    /// <summary>
    /// 分类树节点
    /// 表示层级分类结构中的一个节点
    /// </summary>
    public class CategoryNode
    {
        /// <summary>
        /// 节点名称（不含路径）
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 完整路径
        /// </summary>
        public string FullPath { get; }

        /// <summary>
        /// 父节点
        /// </summary>
        public CategoryNode Parent { get; internal set; }

        /// <summary>
        /// 子节点集合
        /// </summary>
        public Dictionary<string, CategoryNode> Children { get; }

        /// <summary>
        /// 实体 ID 集合
        /// </summary>
        public HashSet<string> EntityIds { get; }

        /// <summary>
        /// 读写锁
        /// </summary>
        public ReaderWriterLockSlim Lock { get; }

        public CategoryNode(string name, string fullPath)
        {
            Name = name;
            FullPath = fullPath;
            Children = new Dictionary<string, CategoryNode>();
            EntityIds = new HashSet<string>();
            Lock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        }

        /// <summary>
        /// 添加子节点
        /// </summary>
        public void AddChild(CategoryNode child)
        {
            Lock.EnterWriteLock();
            try
            {
                child.Parent = this;
                Children[child.Name] = child;
            }
            finally
            {
                Lock.ExitWriteLock();
            }
        }

        /// <summary>
        /// 移除子节点
        /// </summary>
        public bool RemoveChild(string childName)
        {
            Lock.EnterWriteLock();
            try
            {
                if (Children.TryGetValue(childName, out var child))
                {
                    child.Parent = null;
                    return Children.Remove(childName);
                }
                return false;
            }
            finally
            {
                Lock.ExitWriteLock();
            }
        }

        /// <summary>
        /// 获取所有后代节点（包括自身）
        /// </summary>
        public List<CategoryNode> GetDescendants()
        {
            var result = new List<CategoryNode> { this };

            Lock.EnterReadLock();
            try
            {
                foreach (var child in Children.Values)
                {
                    result.AddRange(child.GetDescendants());
                }
            }
            finally
            {
                Lock.ExitReadLock();
            }

            return result;
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            Lock?.Dispose();
            foreach (var child in Children.Values)
            {
                child.Dispose();
            }
        }
    }
}
