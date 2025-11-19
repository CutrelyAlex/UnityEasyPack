using System.Collections.Generic;
using System.Linq;

namespace EasyPack.EmeCardSystem
{
    /// <summary>
    /// 移除卡牌效果：将符合条件的目标卡牌从容器中移除。
    /// <para>
    /// - 当目标为"固有子卡"（intrinsic）时不会被移除
    /// </para>
    /// <para>
    /// - 若 <see cref="Scope"/> 为 <see cref="TargetScope.Matched"/> 且匹配集中出现重复项，则会按重复次数尝试处理
    /// </para>
    /// <para>
    /// - 建议配合规则或设置 <see cref="Take"/> 进行限量，以避免重复副作用
    /// </para>
    /// </summary>
    public class RemoveCardsEffect : IRuleEffect, ITargetSelection
    {
        /// <summary>
        /// 根容器：选择从哪个容器开始。
        /// <para>Container = 触发容器（规则所在的容器）</para>
        /// <para>Source = 触发源卡牌（触发规则的卡牌本身）</para>
        /// </summary>
        public SelectionRoot Root { get; set; } = SelectionRoot.Container;

        /// <summary>
        /// 选择范围：决定在哪个层级查找目标卡牌。
        /// </summary>
        public TargetScope Scope { get; set; } = TargetScope.Matched;

        /// <summary>
        /// 过滤模式：如何筛选目标卡牌。
        /// </summary>
        public CardFilterMode Filter { get; set; } = CardFilterMode.None;

        /// <summary>
        /// 过滤值：根据 <see cref="Filter"/> 模式提供对应的值。
        /// </summary>
        public string FilterValue { get; set; }

        /// <summary>
        /// 数量限制：最多移除多少张卡牌。
        /// </summary>
        public int? Take { get; set; } = null;

        /// <summary>
        /// 递归深度限制：仅对 <see cref="Scope"/> 为 <see cref="TargetScope.Descendants"/> 时生效。
        /// </summary>
        public int? MaxDepth { get; set; } = null;

        /// <summary>
        /// 执行移除卡牌效果。
        /// </summary>
        /// <param name="ctx">规则执行上下文</param>
        /// <param name="matched">规则匹配阶段的结果（当 <see cref="Scope"/> 为 <see cref="TargetScope.Matched"/> 时使用）</param>
        public void Execute(CardRuleContext ctx, IReadOnlyList<Card> matched)
        {
            IReadOnlyList<Card> targets;

            // 如果 Scope == Matched，使用已匹配的卡牌，但仍需要应用过滤
            if (Scope == TargetScope.Matched)
            {
                if (matched == null || matched.Count == 0)
                {
                    return;
                }

                targets = matched;

                // 应用过滤条件（FilterMode）
                if (Filter != CardFilterMode.None && !string.IsNullOrEmpty(FilterValue))
                {
                    targets = TargetSelector.ApplyFilter(targets, Filter, FilterValue);
                }

                // 应用 Take 限制
                if (Take.HasValue && Take.Value > 0 && targets.Count > Take.Value)
                {
                    targets = targets.Take(Take.Value).ToList();
                }
            }
            else
            {
                targets = TargetSelector.SelectForEffect(this, ctx);
            }

            if (targets == null || targets.Count == 0)
                return;

            foreach (var t in targets.ToArray())
            {
                if (t?.Owner != null)
                {
                    t.Owner.RemoveChild(t, force: false);
                }
            }
        }
    }
}
