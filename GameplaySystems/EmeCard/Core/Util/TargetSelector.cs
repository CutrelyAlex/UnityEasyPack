using System;
using System.Collections.Generic;
using System.Linq;

namespace EasyPack
{
    // 决定效果作用对象
    public enum TargetKind
    {
        Matched,            // 匹配到的卡
        Source,             // 触发源
        Container,          // 匹配容器本体
        ContainerChildren,  // 容器内所有子卡（仅一层）
        ContainerDescendants, // 容器内所有子卡（递归）
        ByTag,              // 按标签过滤容器内子卡（仅一层）
        ByTagRecursive,     // 按标签过滤容器内子卡（递归）
        ById,               // 按ID过滤容器内子卡（仅一层）
        ByIdRecursive       // 按ID过滤容器内子卡（递归）
    }

    /// <summary>
    /// 目标选择器：基于 <see cref="TargetKind"/> 从上下文（<see cref="CardRuleContext"/>）中挑选目标卡牌。
    /// 注意：
    /// - 选择范围以 <see cref="CardRuleContext.Container"/> 作为容器，默认仅扫描其直接子卡（不递归）。
    /// - <see cref="TargetKind.Matched"/> 通常由效果方直接使用（即传入匹配结果），本选择器不处理该分支。
    /// </summary>
    public static class TargetSelector
    {
        /// <summary>
        /// 根据 <paramref name="kind"/> 在上下文中选择目标列表。
        /// </summary>
        /// <param name="kind">目标类型。</param>
        /// <param name="ctx">规则执行上下文。</param>
        /// <param name="value">可选过滤值（用于 ByTag/ById）。</param>
        /// <returns>选中的卡牌列表（只读）。当上下文无效或无结果时返回空数组。</returns>
        public static IReadOnlyList<Card> Select(TargetKind kind, CardRuleContext ctx, string value = null)
        {
            switch (kind)
            {
                case TargetKind.Source:
                    return ctx.Source != null ? new[] { ctx.Source } : Array.Empty<Card>();
                case TargetKind.Container:
                    return ctx.Container != null ? new[] { ctx.Container } : Array.Empty<Card>();
                case TargetKind.ContainerChildren:
                    return ctx.Container != null ? ctx.Container.Children.ToList() : Array.Empty<Card>();
                case TargetKind.ContainerDescendants:
                    return ctx.Container != null ? TraversalUtil.EnumerateDescendants(ctx.Container, int.MaxValue).ToList() : Array.Empty<Card>();
                case TargetKind.ByTag:
                    return ctx.Container != null && !string.IsNullOrEmpty(value)
                        ? ctx.Container.Children.Where(c => c.HasTag(value)).ToList()
                        : Array.Empty<Card>();
                case TargetKind.ByTagRecursive:
                    return ctx.Container != null && !string.IsNullOrEmpty(value)
                        ? TraversalUtil.EnumerateDescendants(ctx.Container, int.MaxValue).Where(c => c.HasTag(value)).ToList()
                        : Array.Empty<Card>();
                case TargetKind.ById:
                    return ctx.Container != null && !string.IsNullOrEmpty(value)
                        ? ctx.Container.Children.Where(c => string.Equals(c.Id, value, StringComparison.Ordinal)).ToList()
                        : Array.Empty<Card>();
                case TargetKind.ByIdRecursive:
                    return ctx.Container != null && !string.IsNullOrEmpty(value)
                        ? TraversalUtil.EnumerateDescendants(ctx.Container, int.MaxValue).Where(c => string.Equals(c.Id, value, StringComparison.Ordinal)).ToList()
                        : Array.Empty<Card>();
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
    }
}