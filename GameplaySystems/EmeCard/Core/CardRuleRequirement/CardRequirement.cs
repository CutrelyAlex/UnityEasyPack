using System;
using System.Collections.Generic;
using System.Linq;

namespace EasyPack
{
    /// <summary>
    /// 要求项的根锚点：决定以谁为根进行选择。
    /// </summary>
    public enum RequirementRoot
    {
        /// <summary>以上下文容器（ctx.Container）为根。</summary>
        Container,
        /// <summary>以触发源（ctx.Source）为根。</summary>
        Source
    }

    /// <summary>
    /// 选择器式要求项：
    /// - 以 Root(容器/源) 为根，使用 TargetKind + Filter 选择目标；
    /// - 命中条件：被选择的数量 >= MinCount；
    /// - matched 返回至多 MinCount 个目标，供效果作为“Matched”输入。
    /// 说明：
    /// - 递归类 TargetKind（ContainerDescendants/ByTagRecursive/ByIdRecursive）会尊重 ctx.MaxDepth；
    /// - 非递归类 TargetKind 只在根的一层 Children 内选择，Container 则仅选择根本体。
    /// </summary>
    public sealed class CardRequirement : IRuleRequirement
    {
        /// <summary>选择起点（默认 Container）。</summary>
        public RequirementRoot Root = RequirementRoot.Container;

        /// <summary>选择类型（支持 Container/Children/Descendants/ByTag/ById 等）。</summary>
        public TargetKind TargetKind = TargetKind.ContainerChildren;

        /// <summary>过滤值（当 ByTag/ById 等需要时填写）。</summary>
        public string Filter;

        /// <summary>至少需要命中的数量（默认 1；<=0 视为无需命中）。</summary>
        public int MinCount = 1;

        public bool TryMatch(CardRuleContext ctx, out List<Card> matched)
        {
            matched = new List<Card>();
            if (ctx == null) return false;

            var root = Root == RequirementRoot.Container ? ctx.Container : ctx.Source;
            if (root == null) return false;

            // 构造一个“以 root 为容器”的上下文，复用选择逻辑
            var localCtx = new CardRuleContext
            {
                Source = ctx.Source,
                Container = root,
                Event = ctx.Event,
                Factory = ctx.Factory,
                RecursiveSearch = ctx.RecursiveSearch,
                MaxDepth = ctx.MaxDepth
            };

            // 选择集合（递归类 TargetKind 使用深度限制）
            IReadOnlyList<Card> picks = Select(localCtx, TargetKind, Filter);

            int count = picks?.Count ?? 0;
            if (count == 0) return MinCount <= 0;

            // 按约定仅返回至多 MinCount 个
            int take = MinCount > 0 ? MinCount : count;
            matched.AddRange(picks.Take(take));

            return count >= (MinCount > 0 ? MinCount : 0);
        }

        // 与 TargetSelector 保持一致语义；递归选择尊重 ctx.MaxDepth
        private static IReadOnlyList<Card> Select(CardRuleContext ctx, TargetKind kind, string value)
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
                    if (string.IsNullOrEmpty(value)) return Array.Empty<Card>();
                    return ctx.Container.Children.Where(c => c.HasTag(value)).ToList();

                case TargetKind.ByTagRecursive:
                    if (string.IsNullOrEmpty(value)) return Array.Empty<Card>();
                    {
                        int max = ctx.MaxDepth > 0 ? ctx.MaxDepth : int.MaxValue;
                        return TraversalUtil.EnumerateDescendants(ctx.Container, max)
                                            .Where(c => c.HasTag(value)).ToList();
                    }

                case TargetKind.ById:
                    if (string.IsNullOrEmpty(value)) return Array.Empty<Card>();
                    return ctx.Container.Children.Where(c => string.Equals(c.Id, value, StringComparison.Ordinal)).ToList();

                case TargetKind.ByIdRecursive:
                    if (string.IsNullOrEmpty(value)) return Array.Empty<Card>();
                    {
                        int max = ctx.MaxDepth > 0 ? ctx.MaxDepth : int.MaxValue;
                        return TraversalUtil.EnumerateDescendants(ctx.Container, max)
                                            .Where(c => string.Equals(c.Id, value, StringComparison.Ordinal)).ToList();
                    }

                // Matched 在 Requirement 里不适用，返回空
                case TargetKind.Matched:
                default:
                    return Array.Empty<Card>();
            }
        }
    }
}