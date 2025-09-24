using System.Collections.Generic;
using System.Linq;

namespace EasyPack
{
    /// <summary>
    /// 修改属性效果：对目标卡的 <see cref="GameProperty"/> 进行数值或修饰符层面的调整。
    /// 说明：
    /// <para></para>
    /// - <see cref="ApplyMode"/> 为 AddModifier/RemoveModifier 时使用 <see cref="Modifier"/>；
    ///  <para></para>
    ///   为 AddToBase/SetBase 时使用 <see cref="Value"/>；
    /// </summary>
    public class ModifyPropertyEffect : IRuleEffect, ITargetSelection
    {
        /// <summary>
        /// 目标类型
        /// </summary>
        public TargetKind TargetKind { get; set; } = TargetKind.Matched;

        /// <summary>
        /// 目标过滤值
        /// </summary>
        public string TargetValueFilter { get; set; }

        /// <summary>
        /// 限量（<=0 不限）只对前 N 个目标生效。
        /// </summary>
        public int Take { get; set; } = 0;

        /// <summary>
        /// 应用模式：
        /// - AddModifier/RemoveModifier：添加/移除修饰符；
        /// - AddToBase：对基础值加上 <see cref="Value"/>；
        /// - SetBase：将基础值设为 <see cref="Value"/>。
        /// </summary>
        public enum Mode { AddModifier, RemoveModifier, AddToBase, SetBase }

        /// <summary>
        /// 当使用 AddModifier/RemoveModifier 模式时要应用/移除的修饰符。
        /// </summary>
        public IModifier Modifier { get; set; }

        /// <summary>
        /// 应用模式（默认 AddToBase）。
        /// </summary>
        public Mode ApplyMode { get; set; } = Mode.AddToBase;

        /// <summary>
        /// 数值参数：用于 AddToBase/SetBase 模式。
        /// </summary>
        public float Value { get; set; } = 0f;

        /// <summary>
        /// 执行属性修改。
        /// </summary>
        /// <param name="ctx">规则上下文。</param>
        /// <param name="matched">匹配阶段的结果（当 <see cref="TargetKind"/>=Matched 时使用）。</param>
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
                var gp = t.Property;
                if (gp == null) continue;

                switch (ApplyMode)
                {
                    case Mode.AddModifier:
                        if (Modifier != null)
                        {
                            gp.AddModifier(Modifier);
                        }
                        break;
                    case Mode.RemoveModifier:
                        if (Modifier != null)
                        {
                            gp.RemoveModifier(Modifier);
                        }
                        break;
                    case Mode.AddToBase:
                        gp.SetBaseValue(gp.GetBaseValue() + Value);
                        break;
                    case Mode.SetBase:
                        gp.SetBaseValue(Value);
                        break;
                }
            }
        }
    }
}