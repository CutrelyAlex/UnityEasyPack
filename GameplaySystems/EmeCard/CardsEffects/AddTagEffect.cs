using System.Collections.Generic;
using System.Linq;

namespace EasyPack
{
    public class AddTagEffect : IRuleEffect, ITargetSelection
    {
        public TargetKind TargetKind { get; set; } = TargetKind.Matched;
        public string TargetValueFilter { get; set; }
        public int Take { get; set; } = 0;
        public string Tag { get; set; }

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