using System.Collections.Generic;

namespace EasyPack
{
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
}