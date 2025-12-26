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
        ///     初始化根节点或叶子节点
        /// </summary>
        /// <param name="termId">词汇 ID，应从 IntegerMapper 获取</param>
        /// <param name="parentNode">父节点，根节点为 null</param>
        public CategoryNode(int termId, CategoryNode parentNode = null)
        {
            if (termId < 0)
            {
                throw new ArgumentException($"TermId must be >= 0, got {termId}", nameof(termId));
            }

            TermId = termId;
            ParentNode = parentNode;
            _children = new();
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
            {
                throw new ArgumentException($"ChildTermId must be >= 0, got {childTermId}", nameof(childTermId));
            }

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
        ///     字符串表示调试
        /// </summary>
        public override string ToString() =>
            $"CategoryNode(TermId={TermId}, Children={ChildCount}, Path={GetFullIntPath()})";
    }
}