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
    /// 单个卡牌匹配条件（基于容器子卡的筛选）：
    /// - 支持标签/ID/类别；
    /// - 支持 MinCount 与 IncludeSelf；
    /// - 命中会返回被选中的卡牌列表（最多 MinCount 个）。
    /// </summary>
    public sealed class CardRequirement : IRuleRequirement
    {
        /// <summary>
        /// 匹配方式（标签/ID/类别）。
        /// </summary>
        public MatchKind Kind;

        /// <summary>
        /// 当 <see cref="Kind"/> 为 <see cref="MatchKind.Tag"/> 或 <see cref="MatchKind.Id"/> 时的匹配值。
        /// </summary>
        public string Value;

        /// <summary>
        /// 当 <see cref="Kind"/> 为 <see cref="MatchKind.Category"/> 时指定的类别。
        /// </summary>
        public CardCategory? Category;

        /// <summary>
        /// 该条件需要匹配的卡牌最小数量。
        /// </summary>
        public int MinCount = 1;

        /// <summary>
        /// 是否允许将“触发源”一并纳入候选集合（取决于规则作用域）。
        /// </summary>
        public bool IncludeSelf = false;

        /// <summary>
        /// 判断指定卡牌是否满足该条件。
        /// </summary>
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

        /// <summary>
        /// 在容器上下文中尝试匹配，命中返回至多 MinCount 张卡。
        /// </summary>
        public bool TryMatch(CardRuleContext ctx, out List<Card> matched)
        {
            matched = new List<Card>();
            if (ctx == null || ctx.Container == null) return false;

            IEnumerable<Card> pool = ctx.RecursiveSearch
                ? TraversalUtil.EnumerateDescendants(ctx.Container, ctx.MaxDepth)
                : (IEnumerable<Card>)ctx.Container.Children; // 冰:这里改成RecursiveSearch==false只返回容器，==true之后再往下遍历感觉会更清晰

            if (IncludeSelf)
            {
                if (ReferenceEquals(ctx.Container, ctx.Source))
                    pool = pool.Concat(new[] { ctx.Container });
                else if (ReferenceEquals(ctx.Container, ctx.Source?.Owner))
                    pool = pool.Concat(new[] { ctx.Source });
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