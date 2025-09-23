using System.Collections.Generic;
using System.Linq;

namespace EasyPack
{
    /// <summary>
    /// 添加标签效果：为目标卡添加指定的标签（若已存在则保持不变）。
    /// </summary>
    public class AddTagEffect : IRuleEffect, ITargetSelection
    {
        /// <summary>
        /// 目标类型（默认 Matched）。
        /// </summary>
        public TargetKind TargetKind { get; set; } = TargetKind.Matched;

        /// <summary>
        /// 目标过滤值：当 <see cref="TargetKind"/> 为 ByTag/ById/ByCategory 等时生效。
        /// </summary>
        public string TargetValueFilter { get; set; }

        /// <summary>
        /// 仅作用前 N 个目标（&lt;=0 表示不限制）。
        /// </summary>
        public int Take { get; set; } = 0;

        /// <summary>
        /// 要添加的标签文本（非空时才会尝试添加）。
        /// </summary>
        public string Tag { get; set; }

        /// <summary>
        /// 执行添加标签。
        /// </summary>
        /// <param name="ctx">规则上下文。</param>
        /// <param name="matched">匹配阶段结果（当 <see cref="TargetKind"/>=Matched 时使用）。</param>
        public void Execute(CardRuleContext ctx, IReadOnlyList<Card> matched)
        {
            IReadOnlyList<Card> targets =
                TargetKind == TargetKind.Matched
                    ? (matched == null || matched.Count == 0
                        ? matched
                        : (Take > 0 ? matched.Take(Take).ToList() : matched))
                    : TargetSelector.Select(TargetKind, ctx, TargetValueFilter, Take);

            if (targets == null) return;

            foreach (var t in targets)
            {
                if (!string.IsNullOrEmpty(Tag))
                    t.AddTag(Tag);
            }
        }
    }

}