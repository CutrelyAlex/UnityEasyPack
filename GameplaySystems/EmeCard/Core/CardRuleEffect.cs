using System;
using System.Collections.Generic;
using System.Linq;

namespace EasyPack
{
    // 决定效果作用对象
    public enum TargetKind
    {
        Matched,            // 匹配到的卡
        Source,             // 触发源
        Container,          // 匹配容器本体
        ContainerChildren,  // 容器内所有子卡
        ByTag,              // 按标签过滤容器内子卡
        ById                // 按ID过滤容器内子卡
    }

    // 规则效果接口
    public interface IRuleEffect
    {
        void Execute(CardRuleContext ctx, IReadOnlyList<Card> matched);
    }
    public static class TargetSelector
    {
        public static IReadOnlyList<Card> Select(TargetKind kind, CardRuleContext ctx, string value = null)
        {
            switch (kind)
            {
                case TargetKind.Source:
                    return ctx.Source != null ? new[] { ctx.Source } : Array.Empty<Card>();
                case TargetKind.Container:
                    return ctx.Container != null ? new[] { ctx.Container } : Array.Empty<Card>();
                case TargetKind.ContainerChildren:
                    return ctx.Container != null ? (IReadOnlyList<Card>)ctx.Container.Children.ToList() : Array.Empty<Card>();
                case TargetKind.ByTag:
                    return ctx.Container != null && !string.IsNullOrEmpty(value)
                        ? ctx.Container.Children.Where(c => c.HasTag(value)).ToList()
                        : Array.Empty<Card>();
                case TargetKind.ById:
                    return ctx.Container != null && !string.IsNullOrEmpty(value)
                        ? ctx.Container.Children.Where(c => string.Equals(c.Id, value, StringComparison.Ordinal)).ToList()
                        : Array.Empty<Card>();
                default:
                    return Array.Empty<Card>();
            }
        }
    }

    // 修改属性效果:不产出卡，仅改变目标卡的 GameProperty
    public sealed class ModifyPropertyEffect : IRuleEffect
    {
        public TargetKind TargetKind { get; set; } = TargetKind.Matched;
        public string TargetValueFilter { get; set; } // 当 ByTag/ById 时生效

        public enum Mode { AddModifier, RemoveModifier, AddToBase, SetBase }
        public IModifier Modifier { get; set; }
        public Mode ApplyMode { get; set; } = Mode.AddToBase;
        public float Value { get; set; } = 0f;

        public void Execute(CardRuleContext ctx, IReadOnlyList<Card> matched)
        {
            IReadOnlyList<Card> targets;
            if (TargetKind == TargetKind.Matched)
                targets = matched;
            else
                targets = TargetSelector.Select(TargetKind, ctx, TargetValueFilter);

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

    // 删除卡效果：不产出卡，仅移除目标卡（固有子卡仍会被保护）
    public sealed class RemoveCardsEffect : IRuleEffect
    {
        public TargetKind TargetKind { get; set; } = TargetKind.Matched;
        public string TargetValueFilter { get; set; }

        public void Execute(CardRuleContext ctx, IReadOnlyList<Card> matched)
        {
            IReadOnlyList<Card> targets;
            if (TargetKind == TargetKind.Matched)
                targets = matched;
            else
                targets = TargetSelector.Select(TargetKind, ctx, TargetValueFilter);

            foreach (var t in targets.ToArray())
            {
                if (t.Owner != null)
                {
                    t.Owner.RemoveChild(t, force: false); // 固有子卡不会被移除
                }
            }
        }
    }
}