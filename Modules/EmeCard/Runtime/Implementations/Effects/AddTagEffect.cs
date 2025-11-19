using System.Collections.Generic;

namespace EasyPack.EmeCardSystem
{
    /// <summary>
    /// 添加标签效果：为目标卡添加指定的标签（若已存在则保持不变）。
    /// </summary>
    public class AddTagEffect : IRuleEffect, ITargetSelection
    {
        /// <summary>
        /// 选择起点（默认 Container）。
        /// </summary>
        public SelectionRoot Root { get; set; } = SelectionRoot.Container;

        /// <summary>
        /// 选择范围（默认 Matched）。
        /// </summary>
        public TargetScope Scope { get; set; } = TargetScope.Matched;

        /// <summary>
        /// 过滤模式（默认 None）。
        /// </summary>
        public CardFilterMode Filter { get; set; } = CardFilterMode.None;

        /// <summary>
        /// 目标过滤值：当 <see cref="Filter"/> 为 ByTag/ById/ByCategory 时生效。
        /// </summary>
        public string FilterValue { get; set; }

        /// <summary>
        /// 仅作用前 N 个目标（null 表示不限制）。
        /// </summary>
        public int? Take { get; set; } = null;

        /// <summary>
        /// 递归深度限制（仅对 Scope=Descendants 生效，null 表示不限制）。
        /// </summary>
        public int? MaxDepth { get; set; } = null;

        /// <summary>
        /// 要添加的标签文本（非空时才会尝试添加）。
        /// </summary>
        public string Tag { get; set; }

        /// <summary>
        /// 执行添加标签。
        /// </summary>
        /// <param name="ctx">规则上下文。</param>
        /// <param name="matched">匹配阶段结果（当 <see cref="Scope"/>=Matched 时使用）。</param>
        public void Execute(CardRuleContext ctx, IReadOnlyList<Card> matched)
        {
            IReadOnlyList<Card> targets;

            if (Scope == TargetScope.Matched)
            {
                // 使用匹配结果
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
                    var limited = new List<Card>(Take.Value);
                    for (int i = 0; i < Take.Value && i < targets.Count; i++)
                        limited.Add(targets[i]);
                    targets = limited;
                }
            }
            else
            {
                // 使用 TargetSelector 选择
                targets = TargetSelector.SelectForEffect(this, ctx);
            }

            if (targets == null) return;

            // 添加标签
            for (int i = 0; i < targets.Count; i++)
            {
                if (!string.IsNullOrEmpty(Tag))
                    targets[i].AddTag(Tag);
            }
        }
    }
}
