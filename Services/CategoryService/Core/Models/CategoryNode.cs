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
        public CategoryNode Parent { get; private set; }

        /// <summary>
        /// 子节点集合
        /// </summary>
        public Dictionary<string, CategoryNode> Children { get; }

        /// <summary>
        /// 实体 ID 集合（仅该节点直接包含的实体）
        /// </summary>
        public HashSet<string> EntityIds { get; }

        /// <summary>
        /// 子树实体 ID 集合（该节点及其所有后代节点的实体，预计算缓存）
        /// 用于加速 includeChildren=true 的查询
        /// </summary>
        public HashSet<string> SubtreeEntityIds { get; private set; }

        /// <summary>
        /// 读写锁
        /// </summary>
        private ReaderWriterLockSlim Lock { get; }

        public CategoryNode(string name, string fullPath)
        {
            Name = name;
            FullPath = fullPath;
            Children = new Dictionary<string, CategoryNode>();
            EntityIds = new HashSet<string>();
            SubtreeEntityIds = new HashSet<string>();
            Lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
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
        /// 添加实体到该节点，并更新所有祖先的子树索引
        /// </summary>
        /// <param name="entityId">实体 ID</param>
        public void AddEntityAndPropagate(string entityId)
        {
            Lock.EnterWriteLock();
            try
            {
                EntityIds.Add(entityId);
                SubtreeEntityIds.Add(entityId);
            }
            finally
            {
                Lock.ExitWriteLock();
            }

            // 向上传播到祖先节点
            Parent?.PropagateEntityToAncestors(entityId);
        }

        /// <summary>
        /// 向祖先节点传播实体 ID（仅在子树索引中记录）
        /// 内部方法，假定调用者已持有该节点的写锁
        /// </summary>
        private void PropagateEntityToAncestors(string entityId)
        {
            Lock.EnterWriteLock();
            try
            {
                SubtreeEntityIds.Add(entityId);
            }
            finally
            {
                Lock.ExitWriteLock();
            }

            // 继续向上传播
            Parent?.PropagateEntityToAncestors(entityId);
        }

        /// <summary>
        /// 移除实体，并更新所有祖先的子树索引
        /// </summary>
        /// <param name="entityId">实体 ID</param>
        public void RemoveEntityAndPropagate(string entityId)
        {
            Lock.EnterWriteLock();
            try
            {
                EntityIds.Remove(entityId);
                SubtreeEntityIds.Remove(entityId);
            }
            finally
            {
                Lock.ExitWriteLock();
            }

            // 向上传播到祖先节点
            Parent?.RemoveEntityFromAncestors(entityId);
        }

        /// <summary>
        /// 从祖先节点移除实体 ID
        /// 内部方法，假定调用者已持有该节点的写锁
        /// </summary>
        private void RemoveEntityFromAncestors(string entityId)
        {
            Lock.EnterWriteLock();
            try
            {
                SubtreeEntityIds.Remove(entityId);
            }
            finally
            {
                Lock.ExitWriteLock();
            }

            // 继续向上传播
            Parent?.RemoveEntityFromAncestors(entityId);
        }

        /// <summary>
        /// 重新计算子树索引（从该节点开始的所有后代）
        /// 用于数据修复或同步
        /// </summary>
        public void RebuildSubtreeIndex()
        {
            Lock.EnterWriteLock();
            try
            {
                SubtreeEntityIds.Clear();
                SubtreeEntityIds.UnionWith(EntityIds);

                // 添加所有直接子节点的子树实体
                foreach (var child in Children.Values)
                {
                    child.RebuildSubtreeIndex();
                    SubtreeEntityIds.UnionWith(child.SubtreeEntityIds);
                }
            }
            finally
            {
                Lock.ExitWriteLock();
            }

            // 向上同步祖先
            Parent?.RebuildSubtreeIndexFromParent(SubtreeEntityIds);
        }

        /// <summary>
        /// 从子节点更新本节点的子树索引
        /// 内部方法，用于自下而上的索引更新
        /// </summary>
        private void RebuildSubtreeIndexFromParent(HashSet<string> childSubtreeIds)
        {
            Lock.EnterWriteLock();
            try
            {
                SubtreeEntityIds.UnionWith(childSubtreeIds);
            }
            finally
            {
                Lock.ExitWriteLock();
            }

            // 继续向上
            Parent?.RebuildSubtreeIndexFromParent(SubtreeEntityIds);
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
