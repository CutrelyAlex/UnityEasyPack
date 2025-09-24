using System.Collections.Generic;
using System.Linq;

namespace EasyPack
{
    /// <summary>
    /// 移除卡牌效果：不产出新卡，仅从容器中移除选中的目标卡。
    /// <para>
    /// - 当目标为“固有子卡”（intrinsic）时不会被移除
    /// </para>
    /// <para>
    /// - 若 <see cref="TargetKind"/> 为 <see cref="TargetKind.Matched"/> 且匹配集中出现重复项，则会按重复次数尝试处理
    /// </para>
    /// <para>
    /// - 建议配合规则或设置 <see cref="Take"/> 进行限量，以避免重复副作用
    /// </para>
    /// </summary>
    public class RemoveCardsEffect : IRuleEffect, ITargetSelection
    {
        /// <summary>
        /// 目标类型：
        /// - Matched：直接使用匹配阶段返回的 matched 列表
        /// <para></para>
        /// - ByTag/ById/ByCategory/Container...：在本效果执行时基于 <see cref="CardRuleContext.Container"/> 重新选择。
        /// </summary>
        public TargetKind TargetKind { get; set; } = TargetKind.Matched;

        /// <summary>
        /// 目标过滤值：当 <see cref="TargetKind"/> 为 ByTag/ById/ByCategory 等需要参数的选择器时生效，其它类型忽略。
        /// </summary>
        public string TargetValueFilter { get; set; }

        /// <summary>
        /// 仅作用前 N 个目标（&lt;=0 表示不限制）。
        /// <para>- 对 Matched：在 matched 上截断</para>
        /// <para>
        /// - 对其它 TargetKind：在 <see cref="TargetSelector.Select(TargetKind, CardRuleContext, string, int)"/> 中生效；
        /// </para>
        /// </summary>
        public int Take { get; set; } = 0;

        /// <summary>
        /// 执行移除逻辑。
        /// </summary>
        /// <param name="ctx">规则上下文（包含容器与工厂等）。</param>
        /// <param name="matched">匹配阶段的命中集合（当 <see cref="TargetKind"/>=Matched 时使用）。</param>
        public void Execute(CardRuleContext ctx, IReadOnlyList<Card> matched)
        {
            IReadOnlyList<Card> targets;

            if (TargetKind == TargetKind.Matched)
            {
                if (matched == null || matched.Count == 0)
                {
                    targets = matched;
                }
                else
                {
                    targets = (Take > 0) ? matched.Take(Take).ToList() : matched;
                }
            }
            else
            {
                targets = TargetSelector.Select(TargetKind, ctx, TargetValueFilter, Take);
            }

            if (targets == null) return;

            // ToArray 快照以避免遍历中集合被修改
            foreach (var t in targets.ToArray())
            {
                if (t?.Owner != null)
                {
                    // 固有子卡不会被移除（force=false）
                    t.Owner.RemoveChild(t, force: false);
                }
            }
        }
    }
}