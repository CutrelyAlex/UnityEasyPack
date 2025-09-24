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
    /// - 递归类 TargetKind（ContainerDescendants/ByTagRecursive/ByIdRecursive/ByCategoryRecursive）会尊重 ctx.MaxDepth；
    /// - 非递归类 TargetKind 只在根的一层 Children 内选择，Container 则仅选择根本体。
    /// </summary>
    public sealed class CardsRequirement : IRuleRequirement
    {
        /// <summary>选择起点（默认 Container）。</summary>
        public RequirementRoot Root = RequirementRoot.Container;

        /// <summary>选择类型（支持 Container/Children/Descendants/ByTag/ById/ByCategory 等）。</summary>
        public TargetKind TargetKind = TargetKind.ContainerChildren;

        /// <summary>过滤值（当 ByTag/ById/ByCategory 等需要时填写）。</summary>
        public string Filter;

        /// <summary>至少需要命中的数量（默认 1&lt;=0 视为无需命中）。</summary>
        public int MinCount = 1;

        public bool TryMatch(CardRuleContext ctx, out List<Card> matched)
        {
            matched = new List<Card>();
            if (ctx == null) return false;

            var root = Root == RequirementRoot.Container ? ctx.Container : ctx.Source;
            if (root == null) return false;

            // 以 root 为容器重建局部上下文，统一走 TargetSelector
            var localCtx = new CardRuleContext
            {
                Source = ctx.Source,
                Container = root,
                Event = ctx.Event,
                Factory = ctx.Factory,
                MaxDepth = ctx.MaxDepth
            };

            var picks = TargetSelector.Select(TargetKind, localCtx, Filter);
            int count = picks?.Count ?? 0;
            if (count == 0) return MinCount <= 0;

            int take = MinCount > 0 ? MinCount : count;
            matched.AddRange(picks.Take(take));

            return count >= (MinCount > 0 ? MinCount : 0);
        }
    }
}