using System.Collections.Generic;
using System.Linq;
using EasyPack.GamePropertySystem;

namespace EasyPack.EmeCardSystem
{
    /// <summary>
    /// 修改属性效果：对目标卡的 <see cref="GameProperty"/> 进行数值或修饰符层面的调整。
    /// 说明：
    /// <para></para>
    /// <see cref="ApplyMode"/> 应用模式 <para></para>
    /// <see cref="Modifier"/> 要应用在属性上的修饰器 <para></para>
    ///  <see cref="PropertyName"/> 要应用的属性名称 <para></para>
    /// </summary>
    public class ModifyPropertyEffect : IRuleEffect, ITargetSelection
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
        /// 目标过滤值
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
        /// 要修改的属性名（留空代表全部属性）。
        /// </summary>
        public string PropertyName { get; set; } = "";

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
        /// <param name="matched">匹配阶段的结果（当 <see cref="Scope"/>=Matched 时使用）。</param>
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
                    targets = targets.Take(Take.Value).ToList();
                }
            }
            else
            {
                // 使用 TargetSelector 选择
                targets = TargetSelector.SelectForEffect(this, ctx);
            }

            if (targets == null) return;

            foreach (var t in targets)
            {
                var properties = t.Properties;
                if (properties == null || properties.Count == 0) continue;

                IEnumerable<GameProperty> propsToModify
                    = string.IsNullOrEmpty(PropertyName)
                    ? properties
                    : properties.Where(p => p.ID == PropertyName);

                foreach (var gp in propsToModify)
                {
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
}
