using System.Collections.Generic;

namespace EasyPack.EmeCardSystem
{
    public interface IRuleEffect
    {
        /// <summary>
        ///     执行效果逻辑。
        /// </summary>
        /// <param name="ctx">
        ///     规则上下文，提供完整的执行信息：
        ///     <list type="bullet">
        ///         <item>
        ///             <description><see cref="CardRuleContext.Source" /> - 触发该规则的卡牌（事件源）</description>
        ///         </item>
        ///         <item>
        ///             <description><see cref="CardRuleContext.EffectRoot" /> - 效果作用的根节点（由 EffectRootHops 决定）</description>
        ///         </item>
        ///         <item>
        ///             <description><see cref="CardRuleContext.MatchRoot" /> - 用于匹配的容器根节点</description>
        ///         </item>
        ///         <item>
        ///             <description><see cref="CardRuleContext.Event" /> - 原始事件载体</description>
        ///         </item>
        ///         <item>
        ///             <description><see cref="CardRuleContext.Engine" /> - 卡牌引擎引用（可用于入队新事件）</description>
        ///         </item>
        ///         <item>
        ///             <description><see cref="CardRuleContext.Factory" /> - 卡牌工厂（可用于创建新卡牌）</description>
        ///         </item>
        ///         <item>
        ///             <description><see cref="CardRuleContext.TryGetEventData{T}" /> - 获取强类型事件数据</description>
        ///         </item>
        ///     </list>
        /// </param>
        /// <param name="matched">规则匹配阶段选中的卡牌集合。</param>
        void Execute(CardRuleContext ctx, HashSet<Card> matched);
    }
}