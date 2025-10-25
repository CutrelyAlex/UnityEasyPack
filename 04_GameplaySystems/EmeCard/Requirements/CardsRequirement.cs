using System;
using System.Collections.Generic;
using System.Linq;

namespace EasyPack.EmeCardSystem
{
    /// <summary>
    /// 选择器式要求项：
    /// - 以 Root(容器/源) 为根，使用 Scope + FilterMode + FilterValue 选择目标；
    /// - 命中条件：被选择的数量 >= MinCount；
    /// - matched 返回的卡牌数量由 MaxMatched 控制。
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
        public CardFilterMode FilterMode = CardFilterMode.None;

        /// <summary>过滤值（当 FilterMode 为 ByTag/ById/ByCategory 时填写）。</summary>
        public string FilterValue;

        /// <summary>至少需要命中的数量（默认 1，<=0 视为无需命中）。</summary>
        public int MinCount = 1;

        /// <summary>
        /// 返回给效果的最大卡牌数量（默认 -1，表示使用 MinCount 作为上限；0 表示返回所有选中卡牌）。
        /// 示例：MinCount=3, MaxMatched=1 表示"至少3张才触发，但只返回1张给效果"。
        /// </summary>
        public int MaxMatched = -1;

        /// <summary>递归深度限制（仅对 Scope=Descendants 生效，null 或 <=0 表示不限制）。</summary>
        public int? MaxDepth = null;

        public bool TryMatch(CardRuleContext ctx, out List<Card> matched)
        {
            matched = new List<Card>();
            if (ctx == null) return false;

            var root = Root == SelectionRoot.Container ? ctx.Container : ctx.Source;
            if (root == null) return false;

            // 以 root 为容器重建局部上下文，统一走 TargetSelector
            var localCtx = new CardRuleContext(
                source: ctx.Source,
                container: root,
                evt: ctx.Event,
                factory: ctx.Factory,
                maxDepth: MaxDepth ?? ctx.MaxDepth
            );

            var picks = TargetSelector.Select(Scope, FilterMode, localCtx, FilterValue);
            int count = picks?.Count ?? 0;

            // 检查匹配条件：至少 MinCount 个
            bool isMatch = MinCount > 0 ? count >= MinCount : true;

            if (isMatch && count > 0)
            {
                // 确定返回数量
                int maxReturn;
                if (MaxMatched > 0)
                {
                    // 显式指定返回数量
                    maxReturn = MaxMatched;
                }
                else if (MaxMatched == 0)
                {
                    // 0 表示返回所有选中卡牌
                    maxReturn = count;
                }
                else
                {
                    // -1（默认）使用原逻辑：MinCount > 0 时取 MinCount，否则取 count
                    maxReturn = MinCount > 0 ? MinCount : count;
                }

                matched.AddRange(picks.Take(Math.Min(maxReturn, count)));
            }
            return isMatch;
        }
    }
}

