using System.Collections.Generic;
using System.Linq;

namespace EasyPack
{
    // 删除卡效果：不产出卡，仅移除目标卡（固有子卡仍会被保护）
    public class RemoveCardsEffect : IRuleEffect, ITargetSelection
    {
        public TargetKind TargetKind { get; set; } = TargetKind.Matched;
        public string TargetValueFilter { get; set; }
        public int Take { get; set; } = 0;
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

            foreach (var t in targets.ToArray())
            {
                if (t?.Owner != null)
                {
                    t.Owner.RemoveChild(t, force: false); // 固有子卡不会被移除
                }
            }
        }
    }
}