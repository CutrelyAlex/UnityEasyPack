using System;
using System.Collections.Generic;
using System.Linq;

namespace EasyPack
{
    /// <summary>
    /// 选择器式要求项：
    /// - 以 Root(容器/源) 为根，使用 Scope + FilterMode + FilterValue 选择目标；
    /// - 命中条件：被选择的数量 >= MinCount；
    /// - matched 返回至多 MinCount 个目标，供效果作为"Matched"输入。
    /// 说明：
    /// - Scope=Descendants 时会尊重 MaxDepth；
    /// - Scope=Children 只在根的一层 Children 内选择。
    /// </summary>
    public sealed class CardsRequirement : IRuleRequirement
    {
        /// <summary>选择起点（默认 Container）。</summary>
        public SelectionRoot Root = SelectionRoot.Container;

        /// <summary>选择范围（默认 Children）。</summary>
        public TargetScope Scope = TargetScope.Children;

        /// <summary>过滤模式（默认 None）。</summary>
        public FilterMode FilterMode = FilterMode.None;

        /// <summary>过滤值（当 FilterMode 为 ByTag/ById/ByCategory 时填写）。</summary>
        public string FilterValue;

        /// <summary>至少需要命中的数量（默认 1，&lt;=0 视为无需命中）。</summary>
        public int MinCount = 1;

        /// <summary>递归深度限制（仅对 Scope=Descendants 生效，null 或 &lt;=0 表示不限制）。</summary>
        public int? MaxDepth = null;

        public bool TryMatch(CardRuleContext ctx, out List<Card> matched)
        {
            matched = new List<Card>();
            if (ctx == null) return false;

            var root = Root == SelectionRoot.Container ? ctx.Container : ctx.Source;
            if (root == null) return false;

            // 以 root 为容器重建局部上下文，统一走 TargetSelector
            var localCtx = new CardRuleContext
            {
                Source = ctx.Source,
                Container = root,
                Event = ctx.Event,
                Factory = ctx.Factory,
                MaxDepth = MaxDepth ?? ctx.MaxDepth
            };

            var picks = TargetSelector.Select(Scope, FilterMode, localCtx, FilterValue);
            int count = picks?.Count ?? 0;

            int take = MinCount > 0 ? MinCount : count;
            if (count > 0)
            {
                matched.AddRange(picks.Take(take));
            }

            return count >= (MinCount > 0 ? MinCount : 0);
        }
    }
}
