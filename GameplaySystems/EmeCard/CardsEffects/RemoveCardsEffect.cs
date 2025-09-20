using System.Collections.Generic;
using System.Linq;

namespace EasyPack
{
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
                targets = TargetSelector.SelectOnContext(TargetKind, ctx, TargetValueFilter);

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