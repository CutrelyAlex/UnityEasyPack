using System.Collections.Generic;

namespace EasyPack.EmeCardSystem
{
    internal static class TraversalUtil
    {
        /// <summary>
        ///     枚举 root 的子树（不包含 root），最大深度限制：1=仅直接子级，int.MaxValue=无限
        /// </summary>
        /// <param name="root"></param>
        /// <param name="maxDepth"></param>
        /// <returns></returns>
        public static IEnumerable<Card> EnumerateDescendants(Card root, int maxDepth)
        {
            if (root == null || maxDepth <= 0) yield break;

            var stack = TraversalStackPool.Rent();
            try
            {
                // 从子级开始
                for (int i = root.Children.Count - 1; i >= 0; i--)
                {
                    Card child = root.Children[i];
                    yield return child;
                    stack.Push((child, 1));
                }

                while (stack.Count > 0)
                {
                    (Card node, int depth) = stack.Pop();
                    if (depth >= maxDepth) continue;

                    for (int i = node.Children.Count - 1; i >= 0; i--)
                    {
                        Card child = node.Children[i];
                        yield return child;
                        stack.Push((child, depth + 1));
                    }
                }
            }
            finally
            {
                TraversalStackPool.Return(stack);
            }
        }

        /// <summary>
        ///     枚举 root 的子树并返回 List（不包含 root）
        ///     最大深度限制：1=仅直接子级，int.MaxValue=无限
        /// </summary>
        /// <param name="root">根节点</param>
        /// <param name="maxDepth">最大深度</param>
        /// <returns>后代卡牌列表</returns>
        public static IReadOnlyList<Card> EnumerateDescendantsAsList(Card root, int maxDepth)
        {
            if (root == null || maxDepth <= 0) 
                return System.Array.Empty<Card>();

            // 估算初始容量：直接子级数量 * 2（假设平均每个子级有1个后代）
            int estimatedCapacity = System.Math.Min(root.Children.Count * 2, 64);
            var result = new List<Card>(System.Math.Max(estimatedCapacity, 4));
            
            var stack = TraversalStackPool.Rent();
            try
            {
                // 从子级开始（正序添加以保持顺序一致性）
                for (int i = 0; i < root.Children.Count; i++)
                {
                    Card child = root.Children[i];
                    result.Add(child);
                    if (maxDepth > 1)
                    {
                        stack.Push((child, 1));
                    }
                }

                while (stack.Count > 0)
                {
                    (Card node, int depth) = stack.Pop();
                    if (depth >= maxDepth) continue;

                    for (int i = 0; i < node.Children.Count; i++)
                    {
                        Card child = node.Children[i];
                        result.Add(child);
                        stack.Push((child, depth + 1));
                    }
                }
            }
            finally
            {
                TraversalStackPool.Return(stack);
            }

            return result;
        }
    }
}