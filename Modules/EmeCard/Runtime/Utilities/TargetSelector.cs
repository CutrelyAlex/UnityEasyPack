using System;
using System.Collections.Generic;

namespace EasyPack.EmeCardSystem
{
    /// <summary>
    ///     根据 TargetScope、FilterMode 等参数从上下文中选择卡牌
    ///     Tag/Category基于CardData筛选。
    /// </summary>
    public static class TargetSelector
    {
        /// <summary>
        ///     根据作用域和过滤条件选择目标卡牌。
        /// </summary>
        /// <param name="scope">选择范围（Matched/Children/Descendants）</param>
        /// <param name="filter">过滤模式（None/ByTag/ById/ByCategory）</param>
        /// <param name="ctx">规则上下文</param>
        /// <param name="filterValue">过滤值（标签名/ID/Category名）</param>
        /// <param name="maxDepth">递归最大深度（仅对 Descendants 生效）</param>
        /// <returns>符合条件的卡牌集合</returns>
        public static HashSet<Card> Select(
            TargetScope scope,
            CardFilterMode filter,
            CardRuleContext ctx,
            string filterValue = null,
            int? maxDepth = null) =>
            ctx?.MatchRoot == null
                ? new()
                : Select(scope, filter, ctx.MatchRoot, filterValue, maxDepth ?? ctx.MaxDepth);

        /// <summary>
        ///     根据作用域和过滤条件选择目标卡牌。
        /// </summary>
        public static HashSet<Card> Select(
            TargetScope scope,
            CardFilterMode filter,
            Card root,
            string filterValue = null,
            int maxDepth = int.MaxValue)
        {
            if (root == null)
            {
                return new();
            }

            // 特殊处理：Matched 不应该在这里处理，由调用方直接使用匹配结果
            if (scope == TargetScope.Matched) return new();

            HashSet<Card> candidates;

            // 第一步：根据 Scope 选择候选集
            switch (scope)
            {
                case TargetScope.Children:
                    // 创建副本以避免并发修改
                    candidates = new(root.Children);
                    break;

                case TargetScope.Descendants:
                    {
                        int depth = maxDepth;
                        if (depth <= 0) depth = int.MaxValue;
                        var descendantsList = TraversalUtil.EnumerateDescendantsAsList(root, depth);
                        candidates = new(descendantsList);
                        break;
                    }

                default:
                    return new();
            }

            // 第二步：根据 FilterMode 过滤
            return ApplyFilter(candidates, filter, filterValue);
        }

        /// <summary>
        ///     供效果使用的选择方法：根据 ITargetSelection 配置构建局部上下文并选择目标。
        /// </summary>
        /// <param name="selection">目标选择配置</param>
        /// <param name="ctx">当前规则上下文</param>
        /// <returns>符合条件的卡牌集合</returns>
        public static HashSet<Card> SelectForEffect(ITargetSelection selection, CardRuleContext ctx)
        {
            if (selection == null || ctx == null)
            {
                return new();
            }

            // Matched 由调用方处理
            if (selection.Scope == TargetScope.Matched)
            {
                return new();
            }

            // 确定根容器：效果使用 EffectRoot
            Card root = selection.Root == SelectionRoot.Source ? ctx.Source : ctx.EffectRoot;
            if (root == null)
            {
                return new();
            }

            // 选择目标
            var targets = Select(
                selection.Scope,
                selection.Filter,
                root,
                selection.FilterValue,
                selection.MaxDepth ?? ctx.MaxDepth
            );

            // 应用 Take 限制
            if (selection.Take is > 0 && targets.Count > selection.Take.Value)
            {
                int takeCount = selection.Take.Value;
                var limited = new HashSet<Card>();
                int count = 0;
                foreach (Card card in targets)
                {
                    if (count >= takeCount) break;
                    limited.Add(card);
                    count++;
                }

                return limited;
            }

            return targets;
        }


        /// <summary>
        ///     对已有的卡牌集合应用过滤条件（线程安全）。针对 HashSet&lt;Card&gt;
        /// </summary>
        /// <param name="cards">要过滤的卡牌集合</param>
        /// <param name="filter">过滤模式</param>
        /// <param name="filterValue">过滤值</param>
        /// <param name="categoryManager">可选的 CategoryManager（用于标签和分类查询）</param>
        /// <returns>过滤后的卡牌集合</returns>
        public static HashSet<Card> ApplyFilter(
            HashSet<Card> cards,
            CardFilterMode filter,
            string filterValue)
        {
            if (cards == null || cards.Count == 0)
            {
                return new();
            }

            switch (filter)
            {
                case CardFilterMode.None:
                    return new(cards);

                case CardFilterMode.ByTag:
                    return FilterByTag(cards, filterValue);

                case CardFilterMode.ById:
                    return FilterById(cards, filterValue);

                case CardFilterMode.ByCategory:
                    return FilterByCategory(cards, filterValue);

                default:
                    return new();
            }
        }

        /// <summary>
        ///     按标签过滤（线程安全）。针对 HashSet&lt;Card&gt;
        /// </summary>
        private static HashSet<Card> FilterByTag(
            HashSet<Card> cards,
            string tag)
        {
            if (string.IsNullOrEmpty(tag))
            {
                return new();
            }

            var results = new HashSet<Card>();
            foreach (Card card in cards)
            {
                if (card.HasTag(tag))
                {
                    results.Add(card);
                }
            }

            return results;
        }

        /// <summary>
        ///     按 ID 过滤。针对 HashSet&lt;Card&gt;
        /// </summary>
        private static HashSet<Card> FilterById(HashSet<Card> cards, string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return new();
            }

            var results = new HashSet<Card>();
            foreach (Card card in cards)
            {
                if (string.Equals(card.Id, id, StringComparison.Ordinal))
                {
                    results.Add(card);
                }
            }

            return results;
        }

        /// <summary>
        ///     按分类过滤。使用 CategoryManager 检查卡牌是否属于指定分类。针对 HashSet&lt;Card&gt;
        /// </summary>
        private static HashSet<Card> FilterByCategory(
            HashSet<Card> cards,
            string categoryStr)
        {
            if (string.IsNullOrEmpty(categoryStr))
            {
                return new();
            }

            var results = new HashSet<Card>();

            // 精确匹配或前缀层级匹配（如 Equipment 匹配 Equipment.Weapon）
            foreach (Card card in cards)
            {
                string cardCategory = card.Data?.Category;
                if (!string.IsNullOrEmpty(cardCategory) &&
                    (string.Equals(cardCategory, categoryStr, StringComparison.OrdinalIgnoreCase)
                     || cardCategory.StartsWith(categoryStr + ".", StringComparison.OrdinalIgnoreCase)))
                {
                    results.Add(card);
                }
            }

            return results;
        }
    }
}