using System;
using System.Collections.Generic;
using System.Text;

namespace EasyPack.Category
{
    /// <summary>
    ///     分类节点，表示分类树中的一个节点
    /// </summary>
    public class CategoryNode
    {
        /// <summary>
        ///     词汇 ID（分类名称的整数映射）
        /// </summary>
        public int TermId { get; }

        /// <summary>
        ///     父节点的引用
        ///     为 null 表示根节点
        /// </summary>
        public CategoryNode ParentNode { get; }

        /// <summary>
        ///     子节点字典，键为子节点的 TermId，值为 CategoryNode 实例
        /// </summary>
        private readonly Dictionary<int, CategoryNode> _children;

        /// <summary>
        ///     该节点下关联的实体 ID 列表
        /// </summary>
        private readonly List<string> _entityIds;

        /// <summary>
        ///     子树实体 ID 缓存
        ///     当实体添加/删除或子节点变化时失效
        /// </summary>
        private List<string> _subtreeEntityIdsCache;

        private bool _subtreeEntityIdsCacheValid;

        /// <summary>
        ///     初始化根节点或叶子节点
        /// </summary>
        /// <param name="termId">词汇 ID，应从 IntegerMapper 获取</param>
        /// <param name="parentNode">父节点，根节点为 null</param>
        public CategoryNode(int termId, CategoryNode parentNode = null)
        {
            if (termId < 0)
                throw new ArgumentException($"TermId must be >= 0, got {termId}", nameof(termId));

            TermId = termId;
            ParentNode = parentNode;
            _children = new();
            _entityIds = new();
            _subtreeEntityIdsCache = new();
            _subtreeEntityIdsCacheValid = false;
        }

        /// <summary>
        ///     获取或创建子节点
        ///     用于构建分类树
        /// </summary>
        /// <param name="childTermId">子节点词汇 ID</param>
        /// <returns>子节点实例</returns>
        public CategoryNode GetOrCreateChild(int childTermId)
        {
            if (childTermId < 0)
                throw new ArgumentException($"ChildTermId must be >= 0, got {childTermId}", nameof(childTermId));

            if (!_children.TryGetValue(childTermId, out CategoryNode child))
            {
                child = new(childTermId, this);
                _children[childTermId] = child;
            }

            return child;
        }

        /// <summary>
        ///     尝试获取子节点（不创建）
        /// </summary>
        /// <param name="childTermId">子节点词汇 ID</param>
        /// <param name="child">输出的子节点</param>
        /// <returns>是否存在</returns>
        public bool TryGetChild(int childTermId, out CategoryNode child) =>
            _children.TryGetValue(childTermId, out child);

        /// <summary>
        ///     检查是否存在指定的子节点
        /// </summary>
        public bool HasChild(int childTermId) => _children.ContainsKey(childTermId);

        /// <summary>
        ///     获取所有子节点
        /// </summary>
        public IReadOnlyCollection<CategoryNode> Children => _children.Values;

        /// <summary>
        ///     删除指定的子节点
        /// </summary>
        /// <param name="childTermId">子节点词汇 ID</param>
        /// <returns>是否成功删除</returns>
        public bool RemoveChild(int childTermId)
        {
            if (_children.Remove(childTermId, out _))
            {
                InvalidateSubtreeCache();
                return true;
            }

            return false;
        }

        /// <summary>
        ///     添加实体 ID 到该分类节点
        /// </summary>
        /// <param name="entityId">实体 ID</param>
        public void AddEntity(string entityId)
        {
            if (!_entityIds.Contains(entityId))
            {
                _entityIds.Add(entityId);
                InvalidateSubtreeCache();
            }
        }

        /// <summary>
        ///     移除实体 ID
        /// </summary>
        /// <param name="entityId">实体 ID</param>
        /// <returns>是否成功移除</returns>
        public bool RemoveEntity(string entityId)
        {
            if (_entityIds.Remove(entityId))
            {
                InvalidateSubtreeCache();
                return true;
            }

            return false;
        }

        /// <summary>
        ///     检查实体是否属于该分类
        /// </summary>
        /// <param name="entityId">实体 ID</param>
        /// <returns>是否存在</returns>
        public bool ContainsEntity(string entityId) => _entityIds.Contains(entityId);

        /// <summary>
        ///     获取该节点直接关联的实体 ID 列表
        /// </summary>
        public IReadOnlyList<string> EntityIds => _entityIds.AsReadOnly();

        /// <summary>
        ///     获取该节点及其所有子孙节点关联的所有实体 ID
        /// </summary>
        /// <returns>所有实体 ID 的列表</returns>
        public IReadOnlyList<string> GetSubtreeEntityIds()
        {
            // 如果缓存有效，直接返回
            if (_subtreeEntityIdsCacheValid) return _subtreeEntityIdsCache.AsReadOnly();

            var result = new List<string>(_entityIds);
            var queue = new Queue<CategoryNode>();

            // 初始化队列：将所有直接子节点加入
            foreach (CategoryNode child in _children.Values)
            {
                queue.Enqueue(child);
            }

            // 广度优先遍历所有子孙节点
            while (queue.Count > 0)
            {
                CategoryNode node = queue.Dequeue();

                // 添加当前节点的实体
                result.AddRange(node._entityIds);

                // 将其子节点加入队列
                foreach (CategoryNode child in node._children.Values)
                {
                    queue.Enqueue(child);
                }
            }

            // 更新缓存
            _subtreeEntityIdsCache = result;
            _subtreeEntityIdsCacheValid = true;

            return result.AsReadOnly();
        }

        /// <summary>
        ///     失效子树实体 ID 缓存
        ///     当该节点或子节点的实体关联发生变化时调用
        ///     级联失效所有父节点的缓存
        /// </summary>
        private void InvalidateSubtreeCache()
        {
            _subtreeEntityIdsCacheValid = false;

            // 级联失效父节点的缓存
            CategoryNode parent = ParentNode;
            while (parent != null)
            {
                parent._subtreeEntityIdsCacheValid = false;
                parent = parent.ParentNode;
            }
        }

        /// <summary>
        ///     <para>获取从根节点到当前节点的完整路径（使用词汇 ID） </para>
        ///     <example>示例：root -> category1 -> category2 -> leaf = "0.1.2"</example>
        ///     <para>方法用于调试，生产环境建议：</para>
        ///     <para>1. 使用 GetPathAsIds() 获取高效的 int[]</para>
        ///     <para>2. 在 CategoryManager 中调用 GetReadablePathFromIds()</para>
        /// </summary>
        /// <returns>路径字符串，用 CategoryConstants.CATEGORY_SEPARATOR 分隔</returns>
        public string GetFullIntPath()
        {
            int[] pathIds = GetPathAsIds();

            // 使用 StringBuilder 高效构造字符串
            var sb = new StringBuilder();
            for (int i = 0; i < pathIds.Length; i++)
            {
                if (i > 0) sb.Append(CategoryConstants.CATEGORY_SEPARATOR);
                sb.Append(pathIds[i]);
            }

            return sb.ToString();
        }

        /// <summary>
        ///     获取从根节点到当前节点的所有词汇 ID 数组
        /// </summary>
        /// <returns>路径 ID 数组</returns>
        public int[] GetPathAsIds()
        {
            var path = new List<int>();
            CategoryNode current = this;

            while (current != null)
            {
                path.Add(current.TermId);
                current = current.ParentNode;
            }

            path.Reverse();

            return path.ToArray();
        }

        /// <summary>
        ///     获取节点的深度（0 = 根节点）
        /// </summary>
        public int GetDepth()
        {
            int depth = 0;
            CategoryNode current = ParentNode;

            while (current != null)
            {
                depth++;
                current = current.ParentNode;
            }

            return depth;
        }

        /// <summary>
        ///     获取子节点数量
        /// </summary>
        public int ChildCount => _children.Count;

        /// <summary>
        ///     获取该节点直接关联的实体数量
        /// </summary>
        public int EntityCount => _entityIds.Count;

        /// <summary>
        ///     清除所有实体关联（不删除子节点）
        /// </summary>
        public void ClearEntities()
        {
            _entityIds.Clear();
            InvalidateSubtreeCache();
        }

        /// <summary>
        ///     清除所有子节点和实体
        /// </summary>
        public void Clear()
        {
            _children.Clear();
            _entityIds.Clear();
            InvalidateSubtreeCache();
        }

        /// <summary>
        ///     字符串表示调试
        /// </summary>
        public override string ToString() =>
            $"CategoryNode(TermId={TermId}, Children={ChildCount}, Entities={EntityCount}, Path={GetFullIntPath()})";
    }
}