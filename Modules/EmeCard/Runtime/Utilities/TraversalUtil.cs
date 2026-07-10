using System;
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
                for (int i = root.Children.Count - 1; i >= 0; i--)
                {
                    stack.Push((root.Children[i], 1));
                }

                while (stack.Count > 0)
                {
                    (Card node, int depth) = stack.Pop();
                    yield return node;
                    if (depth >= maxDepth) continue;

                    for (int i = node.Children.Count - 1; i >= 0; i--)
                    {
                        stack.Push((node.Children[i], depth + 1));
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
            {
                return Array.Empty<Card>();
            }

            int estimatedCapacity = Math.Min(root.Children.Count * 2, 64);
            var result = new List<Card>(Math.Max(estimatedCapacity, 4));

            foreach (Card card in EnumerateDescendants(root, maxDepth))
            {
                result.Add(card);
            }

            return result;
        }
    }
}
