using System.Collections.Generic;

namespace EasyPack.EmeCardSystem
{
    internal static class TraversalUtil
    {
        /// <summary>
        /// 枚举 root 的子树（不包含 root），最大深度限制：1=仅直接子级，int.MaxValue=无限
        /// </summary>
        /// <param name="root"></param>
        /// <param name="maxDepth"></param>
        /// <returns></returns>
        public static IEnumerable<Card> EnumerateDescendants(Card root, int maxDepth)
        {
            if (root == null || maxDepth <= 0) yield break;

            var stack = new Stack<(Card node, int depth)>();
            // 从子级开始
            for (int i = root.Children.Count - 1; i >= 0; i--)
            {
                var child = root.Children[i];
                yield return child;
                stack.Push((child, 1));
            }

            while (stack.Count > 0)
            {
                var (node, depth) = stack.Pop();
                if (depth >= maxDepth) continue;

                for (int i = node.Children.Count - 1; i >= 0; i--)
                {
                    var child = node.Children[i];
                    yield return child;
                    stack.Push((child, depth + 1));
                }
            }
        }
    }
}
