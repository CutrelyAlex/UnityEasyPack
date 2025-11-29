using System.Collections.Generic;
using UnityEngine;

namespace EasyPack.EmeCardSystem
{
    /// <summary>
    ///     移动卡牌位置效果：将目标卡牌强制移动到指定位置。
    ///     
    ///     <para>
    ///         <strong>行为说明</strong>：
    ///         <list type="bullet">
    ///             <item>只移动一个卡牌（Take 自动设为 1）</item>
    ///             <item>强制覆盖：原位置的卡牌（若存在）会被转移到虚空位置（z=-1）</item>
    ///             <item>虚空位置特性：子卡牌天然位于虚空，不占据地图位置</item>
    ///         </list>
    ///     </para>
    ///     
    /// </summary>
    public class MoveCardToPositionEffect : IRuleEffect, ITargetSelection
    {
        /// <summary>
        ///     选择起点（默认 Container）。
        /// </summary>
        public SelectionRoot Root { get; set; } = SelectionRoot.MatchRoot;

        /// <summary>
        ///     选择范围（默认 Matched）。
        /// </summary>
        public TargetScope Scope { get; set; } = TargetScope.Matched;

        /// <summary>
        ///     过滤模式（默认 None）。
        /// </summary>
        public CardFilterMode Filter { get; set; } = CardFilterMode.None;

        /// <summary>
        ///     目标过滤值：当 <see cref="Filter" /> 为 ByTag/ById/ByCategory 时生效。
        /// </summary>
        public string FilterValue { get; set; }

        public int? Take { get; set; } = 1;

        /// <summary>
        ///     递归深度限制（仅对 Scope=Descendants 生效，null 表示不限制）。
        /// </summary>
        public int? MaxDepth { get; set; } = null;

        /// <summary>
        ///     目标位置。
        /// </summary>
        public Vector3 TargetPosition { get; set; } = Vector3.zero;

        /// <summary>
        ///     执行位置转移（强制移动一个卡牌到目标位置）。
        ///     
        ///     <para>
        ///         如果目标位置已有卡牌，该卡牌会被转移到虚空位置。
        ///     </para>
        /// </summary>
        /// <param name="ctx">规则上下文。</param>
        /// <param name="matched">匹配阶段结果（当 <see cref="Scope" />=Matched 时使用）。</param>
        public void Execute(CardRuleContext ctx, IReadOnlyList<Card> matched)
        {
            IReadOnlyList<Card> targets;

            if (Scope == TargetScope.Matched)
            {
                // 使用匹配结果
                if (matched == null || matched.Count == 0) return;

                targets = matched;

                // 应用过滤条件（FilterMode）
                if (Filter != CardFilterMode.None && !string.IsNullOrEmpty(FilterValue))
                    targets = TargetSelector.ApplyFilter(targets, Filter, FilterValue);
            }
            else
            {
                // 使用 TargetSelector 选择
                targets = TargetSelector.SelectForEffect(this, ctx);
            }

            if (targets == null || targets.Count == 0) return;

            // 只移动第一个卡牌
            Card cardToMove = targets[0];
            if (ctx.Engine == null) return;

            // 强制覆盖：如果目标位置有卡牌，将其转移到虚空
            Card cardAtTarget = ctx.Engine.GetCardByPosition(TargetPosition);
            if (cardAtTarget != null && cardAtTarget != cardToMove)
            {
                // 原位置物体转移到虚空（如果它不是 cardToMove）
                ctx.Engine.MoveCardToPosition(cardAtTarget, CardEngine.VOID_POSITION);
            }

            // 移动选中的卡牌到目标位置
            ctx.Engine.MoveCardToPosition(cardToMove, TargetPosition);
        }
    }
}
