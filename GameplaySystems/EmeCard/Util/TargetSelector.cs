using System;
using System.Collections.Generic;
using System.Linq;

namespace EasyPack
{

    /// <summary>
    /// 决定效果作用对象/选择域。
    /// 注意：
    /// - 对“效果”而言，根由 ITargetSelection.Root 指定（Container 或 Source）。
    /// - 对“要求项”（如 CardsRequirement）而言，根由 RequirementRoot 指定（Container 或 Source）。
    /// - 递归枚举受最大深度限制（如 MaxDepth）。
    /// </summary>
    public enum TargetKind
    {
        /// <summary>
        /// Matched：来自“所有要求项”返回的匹配卡集合的聚合（并非仅最后一项）。
        /// 是否去重取决于规则/引擎策略（如 DistinctMatched）。
        /// </summary>
        Matched,
        /// <summary>
        /// 选定根的一层子卡（不递归）。
        /// </summary>
        Children,
        /// <summary>
        /// 选定根的所有后代（递归）。
        /// </summary>
        Descendants,
        /// <summary>
        /// 按标签过滤选定根的一层子卡（不递归）。
        /// </summary>
        ByTag,
        /// <summary>
        /// 按标签过滤选定根的后代（递归）。
        /// </summary>
        ByTagRecursive,
        /// <summary>
        /// 按ID过滤选定根的一层子卡（不递归）。
        /// </summary>
        ById,
        /// <summary>
        /// 按ID过滤选定根的后代（递归）。
        /// </summary>
        ByIdRecursive,
        /// <summary>
        /// 按类别过滤选定根的一层子卡（不递归）。
        /// </summary>
        ByCategory,
        /// <summary>
        /// 按类别过滤选定根的后代（递归）。
        /// </summary>
        ByCategoryRecursive
    }

    /// <summary>
    /// 目标选择器：基于 <see cref="TargetKind"/> 从上下文（<see cref="CardRuleContext"/>）中挑选目标卡牌。
    /// </summary>
    public static class TargetSelector
    {
        /// <summary>
        /// 供要求项使用：以 ctx.Container 为根进行选择。
        /// </summary>
        public static IReadOnlyList<Card> Select(TargetKind kind, CardRuleContext ctx, string value = null)
        {
            if (ctx == null || ctx.Container == null) return Array.Empty<Card>();

            switch (kind)
            {
                case TargetKind.Children:
                    return ctx.Container.Children.ToList();
                case TargetKind.Descendants:
                {
                    int max = ctx.MaxDepth > 0 ? ctx.MaxDepth : int.MaxValue;
                    return TraversalUtil.EnumerateDescendants(ctx.Container, max).ToList();
                }
                case TargetKind.ByTag:
                    return !string.IsNullOrEmpty(value)
                        ? ctx.Container.Children.Where(c => c.HasTag(value)).ToList()
                        : Array.Empty<Card>();
                case TargetKind.ByTagRecursive:
                {
                    if (string.IsNullOrEmpty(value)) return Array.Empty<Card>();
                    int max = ctx.MaxDepth > 0 ? ctx.MaxDepth : int.MaxValue;
                    return TraversalUtil.EnumerateDescendants(ctx.Container, max)
                                        .Where(c => c.HasTag(value)).ToList();
                }
                case TargetKind.ById:
                    return !string.IsNullOrEmpty(value)
                        ? ctx.Container.Children.Where(c => string.Equals(c.Id, value, StringComparison.Ordinal)).ToList()
                        : Array.Empty<Card>();
                case TargetKind.ByIdRecursive:
                {
                    if (string.IsNullOrEmpty(value)) return Array.Empty<Card>();
                    int max = ctx.MaxDepth > 0 ? ctx.MaxDepth : int.MaxValue;
                    return TraversalUtil.EnumerateDescendants(ctx.Container, max)
                                        .Where(c => string.Equals(c.Id, value, StringComparison.Ordinal)).ToList();
                }
                case TargetKind.ByCategory:
                {
                    if (!TryParseCategory(value, out var cat)) return Array.Empty<Card>();
                    return ctx.Container.Children.Where(c => c.Category == cat).ToList();
                }
                case TargetKind.ByCategoryRecursive:
                {
                    if (!TryParseCategory(value, out var cat)) return Array.Empty<Card>();
                    int max = ctx.MaxDepth > 0 ? ctx.MaxDepth : int.MaxValue;
                    return TraversalUtil.EnumerateDescendants(ctx.Container, max)
                                        .Where(c => c.Category == cat).ToList();
                }
                default:
                    return Array.Empty<Card>();
            }
        }

        /// <summary>
        /// 在既有选择基础上限制数量（take>0 生效）。
        /// </summary>
        public static IReadOnlyList<Card> Select(TargetKind kind, CardRuleContext ctx, string value, int take)
        {
            var list = Select(kind, ctx, value);
            if (take > 0 && list.Count > take)
                return list.Take(take).ToList();
            return list;
        }

        /// <summary>
        /// 供效果使用：根据 ITargetSelection.Root 先构建以 Root 为根的局部上下文，再进行选择。
        /// </summary>
        public static IReadOnlyList<Card> SelectForEffect(ITargetSelection selection, CardRuleContext ctx)
        {
            if (selection == null || ctx == null) return Array.Empty<Card>();

            Card root = selection.Root == EffectRoot.Source ? ctx.Source : ctx.Container;
            if (root == null) return Array.Empty<Card>();

            var local = new CardRuleContext
            {
                Source = ctx.Source,
                Container = root,
                Event = ctx.Event,
                Factory = ctx.Factory,
                MaxDepth = ctx.MaxDepth
            };

            return Select(selection.TargetKind, local, selection.TargetValueFilter, selection.Take);
        }

        private static bool TryParseCategory(string value, out CardCategory cat)
        {
            cat = default(CardCategory);
            if (string.IsNullOrEmpty(value)) return false;
            return Enum.TryParse<CardCategory>(value, true, out cat);
        }
    }
}