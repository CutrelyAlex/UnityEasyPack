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
    ///             <item>只能移动根卡牌（无 Owner 的卡牌）</item>
    ///             <item>强制覆盖：原位置的卡牌（若存在）会被移除出位置索引但保留在引擎中</item>
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
        public Vector3Int TargetPosition { get; set; } = Vector3Int.zero;

        /// <summary>
        ///     是否强制覆盖目标位置的卡牌。
        ///     如果为 true，目标位置的卡牌会被设置为无位置状态。
        ///     如果为 false，当目标位置有卡牌时不执行移动。
        /// </summary>
        public bool ForceOverwrite { get; set; } = true;

        /// <summary>
        ///     执行位置转移（强制移动一个根卡牌到目标位置）。
        /// </summary>
        /// <param name="ctx">规则上下文。</param>
        /// <param name="matched">匹配阶段结果（当 <see cref="Scope" />=Matched 时使用）。</param>
        public void Execute(CardRuleContext ctx, HashSet<Card> matched)
        {
            List<Card> targets;

            if (Scope == TargetScope.Matched)
            {
                // 使用匹配结果
                if (matched == null || matched.Count == 0) return;

                // 先转换为List下来
                var targetList = new List<Card>(matched);

                // 应用过滤条件（FilterMode）
                if (Filter != CardFilterMode.None && !string.IsNullOrEmpty(FilterValue))
                {
                    var filtered = TargetSelector.ApplyFilter(matched, Filter, FilterValue);
                    targets = new List<Card>(filtered);
                }
                else
                {
                    targets = targetList;
                }
            }
            else
            {
                // 使用 TargetSelector 选择
                var selected = TargetSelector.SelectForEffect(this, ctx);
                targets = new List<Card>(selected);
            }

            if (targets == null || targets.Count == 0) return;

            // 只移动第一个卡牌（必须是根卡牌）
            Card cardToMove = null;
            for (int i = 0; i < targets.Count; i++)
            {
                if (targets[i]?.Owner == null)
                {
                    cardToMove = targets[i];
                    break;
                }
            }

            if (cardToMove == null || ctx.Engine == null) return;

            // 检查目标位置是否有卡牌
            Card cardAtTarget = ctx.Engine.GetCardByPosition(TargetPosition);
            if (cardAtTarget != null && cardAtTarget != cardToMove)
            {
                if (!ForceOverwrite)
                {
                    // 不强制覆盖，目标位置有卡牌时不执行
                    return;
                }

                // 强制覆盖：将原位置卡牌的位置设为 null（移除出位置索引但保留在引擎中）
                ctx.Engine.ClearCardPosition(cardAtTarget);
            }

            // 移动选中的卡牌到目标位置
            ctx.Engine.TryMoveRootCardToPosition(cardToMove, TargetPosition);
        }
    }
}
