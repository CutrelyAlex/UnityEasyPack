using System.Collections.Generic;
using System.Linq;

namespace EasyPack
{
    // 修改属性效果:不产出卡，仅改变目标卡的 GameProperty
    public class ModifyPropertyEffect : IRuleEffect, ITargetSelection
    {
        public TargetKind TargetKind { get; set; } = TargetKind.Matched;
        public string TargetValueFilter { get; set; } // 当 ByTag/ById 时生效

        // 统一选择限量（<=0 表示不限制）
        public int Take { get; set; } = 0;

        public enum Mode { AddModifier, RemoveModifier, AddToBase, SetBase }
        public IModifier Modifier { get; set; }
        public Mode ApplyMode { get; set; } = Mode.AddToBase;
        public float Value { get; set; } = 0f;

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