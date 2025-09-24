using System;
using System.Collections.Generic;
using System.Linq;

namespace EasyPack
{
    
    /// <summary>
    /// 决定效果作用对象
    /// </summary>
    public enum TargetKind
    {
        /// <summary>
        /// 匹配到的卡
        /// </summary>
        Matched,
        /// <summary>
        /// 触发源
        /// </summary>
        Source,
        /// <summary>
        /// 匹配容器本体
        /// </summary>
        Container,
        /// <summary>
        /// 容器内所有子卡（仅一层）
        /// </summary>
        ContainerChildren,
        /// <summary>
        /// 容器内所有子卡（递归）
        /// </summary>
        ContainerDescendants,
        /// <summary>
        /// 按标签过滤容器内子卡（仅一层）
        /// </summary>
        ByTag,
        /// <summary>
        /// 按标签过滤容器内子卡（递归）
        /// </summary>
        ByTagRecursive,
        /// <summary>
        /// 按ID过滤容器内子卡（仅一层）
        /// </summary>
        ById,
        /// <summary>
        /// 按ID过滤容器内子卡（递归）
        /// </summary>
        ByIdRecursive,
        /// <summary>
        /// 按类别过滤容器内子卡（仅一层）
        /// </summary>
        ByCategory,
        /// <summary>
        /// 按类别过滤容器内子卡（递归）
        /// </summary>
        ByCategoryRecursive
    }

    /// <summary>
    /// 目标选择器：基于 <see cref="TargetKind"/> 从上下文（<see cref="CardRuleContext"/>）中挑选目标卡牌。
    /// </summary>
    public static class TargetSelector
    {
        public static IReadOnlyList<Card> Select(TargetKind kind, CardRuleContext ctx, string value = null)
        {
            if (ctx == null || ctx.Container == null) return Array.Empty<Card>();

            switch (kind)
            {
                case TargetKind.Source:
                    return ctx.Source != null ? new[] { ctx.Source } : Array.Empty<Card>();
                case TargetKind.Container:
                    return new[] { ctx.Container };
                case TargetKind.ContainerChildren:
                    return ctx.Container.Children.ToList();
                case TargetKind.ContainerDescendants:
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

        private static bool TryParseCategory(string value, out CardCategory cat)
        {
            cat = default(CardCategory);
            if (string.IsNullOrEmpty(value)) return false;
            return Enum.TryParse<CardCategory>(value, true, out cat);
        }
    }
}