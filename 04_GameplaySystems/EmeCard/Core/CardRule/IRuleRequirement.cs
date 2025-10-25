using System.Collections.Generic;

namespace EasyPack.EmeCardSystem
{
    /// <summary>
    /// 规则匹配的“要求项”抽象：
    /// - 返回是否匹配成功；
    /// - 如需，可返回本要求项所匹配到的卡牌集合（用于效果管线的 Matched）。
    /// </summary>
    public interface IRuleRequirement
    {
        /// <summary>
        /// 在给定上下文下尝试匹配。
        /// </summary>
        /// <param name="ctx">规则上下文（含 Source/Container/Event/Factory）。</param>
        /// <param name="matched">本要求项匹配到的卡集合；可为空或空集。</param>
        /// <returns>是否匹配成功。</returns>
        bool TryMatch(CardRuleContext ctx, out List<Card> matched);
    }
}
