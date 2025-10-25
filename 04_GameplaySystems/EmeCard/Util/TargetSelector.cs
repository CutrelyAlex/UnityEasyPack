using System;
using System.Collections.Generic;
using System.Linq;

namespace EasyPack.EmeCardSystem
{
    /// <summary>
    /// 目标选择器：根据 TargetScope、FilterMode 等参数从上下文中选择卡牌。
    /// </summary>
    public static class TargetSelector
    {
        /// <summary>
        /// 根据作用域和过滤条件选择目标卡牌。
        /// </summary>
        /// <param name="scope">选择范围（Matched/Children/Descendants）</param>
        /// <param name="filter">过滤模式（None/ByTag/ById/ByCategory）</param>
        /// <param name="ctx">规则上下文</param>
        /// <param name="filterValue">过滤值（标签名/ID/Category名）</param>
        /// <param name="maxDepth">递归最大深度（仅对 Descendants 生效）</param>
        /// <returns>符合条件的卡牌列表</returns>
        public static IReadOnlyList<Card> Select(
            TargetScope scope,
            CardFilterMode filter,
            CardRuleContext ctx,
            string filterValue = null,
            int? maxDepth = null)
        {
            if (ctx == null || ctx.Container == null)
                return Array.Empty<Card>();

            // 特殊处理：Matched 不应该在这里处理，由调用方直接使用匹配结果
            if (scope == TargetScope.Matched)
            {
                return Array.Empty<Card>();
            }

            IEnumerable<Card> candidates;

            // 第一步：根据 Scope 选择候选集
            switch (scope)
            {
                case TargetScope.Children:
                    candidates = ctx.Container.Children;
                    break;

                case TargetScope.Descendants:
                    {
                        int depth = maxDepth ?? ctx.MaxDepth;
                        if (depth <= 0) depth = int.MaxValue;
                        candidates = TraversalUtil.EnumerateDescendants(ctx.Container, depth);
                    }
                    break;

                default:
                    return Array.Empty<Card>();
            }

            // 第二步：根据 FilterMode 过滤
            switch (filter)
            {
                case CardFilterMode.ByTag:
                    if (!string.IsNullOrEmpty(filterValue))
                        candidates = candidates.Where(c => c.HasTag(filterValue));
                    else
                        return Array.Empty<Card>();
                    break;

                case CardFilterMode.ById:
                    if (!string.IsNullOrEmpty(filterValue))
                        candidates = candidates.Where(c =>
                            string.Equals(c.Id, filterValue, StringComparison.Ordinal));
                    else
                        return Array.Empty<Card>();
                    break;

                case CardFilterMode.ByCategory:
                    if (TryParseCategory(filterValue, out var cat))
                        candidates = candidates.Where(c => c.Category == cat);
                    else
                        return Array.Empty<Card>();
                    break;

                case CardFilterMode.None:
                    break;

                default:
                    return Array.Empty<Card>();
            }

            return candidates.ToList();
        }

        /// <summary>
        /// 供效果使用的选择方法：根据 ITargetSelection 配置构建局部上下文并选择目标。
        /// </summary>
        /// <param name="selection">目标选择配置</param>
        /// <param name="ctx">当前规则上下文</param>
        /// <returns>符合条件的卡牌列表</returns>
        public static IReadOnlyList<Card> SelectForEffect(ITargetSelection selection, CardRuleContext ctx)
        {
            if (selection == null || ctx == null)
                return Array.Empty<Card>();

            // Matched 由调用方处理
            if (selection.Scope == TargetScope.Matched)
                return Array.Empty<Card>();

            // 确定根容器
            Card root = selection.Root == SelectionRoot.Source ? ctx.Source : ctx.Container;
            if (root == null)
                return Array.Empty<Card>();

            // 构建局部上下文
            var localCtx = new CardRuleContext(
                source: ctx.Source,
                container: root,
                evt: ctx.Event,
                factory: ctx.Factory,
                maxDepth: selection.MaxDepth ?? ctx.MaxDepth
            );

            // 选择目标
            var targets = Select(
                selection.Scope,
                selection.Filter,
                localCtx,
                selection.FilterValue,
                selection.MaxDepth
            );

            // 应用 Take 限制
            if (selection.Take.HasValue && selection.Take.Value > 0 && targets.Count > selection.Take.Value)
            {
                return targets.Take(selection.Take.Value).ToList();
            }

            return targets;
        }

        private static bool TryParseCategory(string value, out CardCategory cat)
        {
            cat = default(CardCategory);
            if (string.IsNullOrEmpty(value)) return false;
            return Enum.TryParse<CardCategory>(value, true, out cat);
        }

        /// <summary>
        /// 对已有的卡牌列表应用过滤条件。
        /// </summary>
        /// <param name="cards">要过滤的卡牌列表</param>
        /// <param name="filter">过滤模式</param>
        /// <param name="filterValue">过滤值</param>
        /// <returns>过滤后的卡牌列表</returns>
        public static IReadOnlyList<Card> ApplyFilter(IReadOnlyList<Card> cards, CardFilterMode filter, string filterValue)
        {
            if (cards == null || cards.Count == 0)
                return Array.Empty<Card>();

            IEnumerable<Card> filtered = cards;

            switch (filter)
            {
                case CardFilterMode.ByTag:
                    if (!string.IsNullOrEmpty(filterValue))
                        filtered = filtered.Where(c => c.HasTag(filterValue));
                    else
                        return Array.Empty<Card>();
                    break;

                case CardFilterMode.ById:
                    if (!string.IsNullOrEmpty(filterValue))
                        filtered = filtered.Where(c =>
                            string.Equals(c.Id, filterValue, StringComparison.Ordinal));
                    else
                        return Array.Empty<Card>();
                    break;

                case CardFilterMode.ByCategory:
                    if (TryParseCategory(filterValue, out var cat))
                        filtered = filtered.Where(c => c.Category == cat);
                    else
                        return Array.Empty<Card>();
                    break;

                case CardFilterMode.None:
                    return cards;

                default:
                    return Array.Empty<Card>();
            }

            return filtered.ToList();
        }
    }
}
