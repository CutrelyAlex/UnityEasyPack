using System;
using System.Collections.Generic;

namespace EasyPack.EmeCardSystem
{
    /// <summary>
    ///     选择器式要求项：基于上下文选择卡牌并验证数量条件。
    ///     <para>
    ///         以 Root（MatchRoot 或 Source）为根，使用 Scope + FilterMode + FilterValue 选择目标；
    ///         命中条件：被选择的数量 ≥ MinCount；
    ///         matched 返回的卡牌数量由 MaxMatched 控制。
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         <b>上下文使用：</b>
    ///     </para>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>使用 <see cref="CardRuleContext.MatchRoot" /> 作为容器根节点（Root=Container）</description>
    ///         </item>
    ///         <item>
    ///             <description>使用 <see cref="CardRuleContext.Source" /> 作为事件源根节点（Root=Source）</description>
    ///         </item>
    ///         <item>
    ///             <description>使用 <see cref="CardRuleContext.MaxDepth" /> 作为默认递归深度</description>
    ///         </item>
    ///     </list>
    ///     <para>
    ///         <b>说明：</b>
    ///     </para>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>Scope=Descendants 时会尊重 MaxDepth</description>
    ///         </item>
    ///         <item>
    ///             <description>Scope=Children 只在根的一层 Children 内选择</description>
    ///         </item>
    ///     </list>
    /// </remarks>
    public sealed class CardsRequirement : IRuleRequirement
    {
        /// <summary>选择起点：Container 使用 ctx.MatchRoot，Source 使用 ctx.Source。</summary>
        public SelectionRoot Root = SelectionRoot.Container;

        /// <summary>选择范围（默认 Children）。</summary>
        public TargetScope Scope = TargetScope.Children;

        /// <summary>过滤模式（默认 None）。</summary>
        public CardFilterMode FilterMode = CardFilterMode.None;

        /// <summary>过滤值（当 FilterMode 为 ByTag/ById/ByCategory 时填写）。</summary>
        public string FilterValue;

        /// <summary>至少需要命中的数量（默认 1，小于 0 视为无需命中）。</summary>
        public int MinCount = 1;

        /// <summary>
        ///     返回给效果的最大卡牌数量（默认 -1，表示使用 MinCount 作为上限；0 表示返回所有选中卡牌）。
        ///     <para>示例：MinCount=3, MaxMatched=1 表示"至少3张才触发，但只返回1张给效果"。</para>
        /// </summary>
        public int MaxMatched = -1;

        /// <summary>递归深度限制（仅对 Scope=Descendants 生效，null 或小于 0 表示使用 ctx.MaxDepth）。</summary>
        public int? MaxDepth = null;

        /// <inheritdoc />
        public bool TryMatch(CardRuleContext ctx, out List<Card> matched)
        {
            matched = new();
            if (ctx == null) return false;

            Card root = Root == SelectionRoot.Container ? ctx.MatchRoot : ctx.Source;
            if (root == null) return false;

            var picks = TargetSelector.Select(
                Scope, 
                FilterMode, 
                root, 
                FilterValue, 
                MaxDepth ?? ctx.MaxDepth,
                ctx.Engine?.CategoryManager);
            int count = picks?.Count ?? 0;

            // 检查匹配条件：至少 MinCount 个
            bool isMatch = MinCount <= 0 || count >= MinCount;

            if (isMatch && count > 0)
            {
                // 确定返回数量
                int maxReturn = MaxMatched switch
                {
                    > 0 => MaxMatched,
                    0 => count,
                    _ => MinCount > 0 ? MinCount : count,
                };

                int takeCount = Math.Min(maxReturn, count);
                if (takeCount == count && picks != null)
                    matched.AddRange(picks);
                else if (picks != null)
                {
                    for (int i = 0; i < takeCount; i++)
                    {
                        matched.Add(picks[i]);
                    }
                }
            }

            return isMatch;
        }
    }
}