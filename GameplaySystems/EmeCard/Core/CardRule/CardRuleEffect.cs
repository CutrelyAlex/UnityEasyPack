using System.Collections.Generic;

namespace EasyPack
{
    // 规则效果接口
    public interface IRuleEffect
    {
        void Execute(CardRuleContext ctx, IReadOnlyList<Card> matched);
    }
}