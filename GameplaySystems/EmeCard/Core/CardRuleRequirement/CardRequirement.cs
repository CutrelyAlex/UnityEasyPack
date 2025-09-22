using System;
using System.Collections.Generic;
using System.Linq;

namespace EasyPack
{
    /// <summary>
    /// 匹配条件类型：用于规则在容器中筛选卡牌的方式。
    /// </summary>
    public enum MatchKind
    {
        /// <summary>按标签匹配。</summary>
        Tag,
        /// <summary>按卡牌 ID 精确匹配。</summary>
        Id,
        /// <summary>按类别匹配。</summary>
        Category
    }

    /// <summary>
    /// 要求项的根锚点：决定以谁为根进行范围搜索。
    /// </summary>
    public enum RequirementAnchor
    {
        /// <summary>以上下文容器（ctx.Container）为根。</summary>
        Container,
        /// <summary>以触发源（ctx.Source）为根。</summary>
        Source
    }

    /// <summary>
    /// 要求项的搜索范围：决定在根的哪一部分进行匹配。
    /// </summary>
    public enum RequirementSearchScope
    {
        /// <summary>仅根本体（不包含其子级）。</summary>
        ContainerOnly,
        /// <summary>仅根的直接子级（默认）。</summary>
        Children,
        /// <summary>根的子树（递归，含所有后代）。</summary>
        Descendants
    }

    /// <summary>
    /// 单个卡牌匹配条件（基于根锚点与范围的筛选）：
    /// - 支持标签/ID/类别；
    /// - 支持 MinCount；
    /// - 支持“锚点（Container/Source）+ 搜索范围（本体/一层/递归）”；
    /// - 命中会返回被选中的卡牌列表（最多 MinCount 个）。
    /// </summary>
    public sealed class CardRequirement : IRuleRequirement
    {
        /// <summary>匹配方式（标签/ID/类别）。</summary>
        public MatchKind Kind;

        /// <summary>当 Kind 为 Tag/Id 时的匹配值。</summary>
        public string Value;

        /// <summary>当 Kind 为 Category 时指定的类别。</summary>
        public CardCategory? Category;

        /// <summary>该条件需要匹配的卡牌最小数量。</summary>
        public int MinCount = 1;

        /// <summary>
        /// 根锚点（默认 Container）。
        /// - Container：以 ctx.Container 为根；
        /// - Source：以 ctx.Source 为根。
        /// </summary>
        public RequirementAnchor Anchor = RequirementAnchor.Container;

        /// <summary>
        /// 搜索范围（默认 Children）。
        /// 当为 Children 且规则层启用了 Recursive 时，会自动升级为递归（与旧行为兼容）。
        /// </summary>
        public RequirementSearchScope Search = RequirementSearchScope.Children;

        /// <summary>
        /// 递归深度（仅当 Search=Descendants 时生效；<=0 表示沿用 ctx.MaxDepth 或无限）。
        /// </summary>
        public int Depth = 0;

        /// <summary>判断指定卡牌是否满足该条件。</summary>
        public bool Matches(Card c)
        {
            switch (Kind)
            {
                case MatchKind.Tag: return !string.IsNullOrEmpty(Value) && c.HasTag(Value);
                case MatchKind.Id: return !string.IsNullOrEmpty(Value) && string.Equals(c.Id, Value, StringComparison.Ordinal);
                case MatchKind.Category: return Category.HasValue && c.Category == Category.Value;
                default: return false;
            }
        }

        /// <summary>在上下文中尝试匹配，命中返回至多 MinCount 张卡。</summary>
        public bool TryMatch(CardRuleContext ctx, out List<Card> matched)
        {
            matched = new List<Card>();
            if (ctx == null) return false;

            var root = (Anchor == RequirementAnchor.Container) ? ctx.Container : ctx.Source;
            if (root == null) return false;

            // 决定候选池：本地 Search 优先；当 Search=Children 且 ctx.RecursiveSearch=true 时升级为递归
            IEnumerable<Card> pool;
            bool useRecursive = (Search == RequirementSearchScope.Descendants) ||
                                (Search == RequirementSearchScope.Children && ctx.RecursiveSearch);

            if (Search == RequirementSearchScope.ContainerOnly)
            {
                pool = new[] { root };
            }
            else if (useRecursive)
            {
                int max = Depth > 0 ? Depth : (ctx.MaxDepth > 0 ? ctx.MaxDepth : int.MaxValue);
                pool = TraversalUtil.EnumerateDescendants(root, max);
            }
            else // Children
            {
                pool = root.Children;
            }

            foreach (var c in pool)
            {
                if (Matches(c))
                {
                    matched.Add(c);
                    if (matched.Count >= MinCount) break;
                }
            }

            return matched.Count >= MinCount;
        }
    }
}