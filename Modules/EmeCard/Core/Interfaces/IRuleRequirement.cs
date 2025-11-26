using System.Collections.Generic;

namespace EasyPack.EmeCardSystem
{
    /// <summary>
    ///     规则匹配的"要求项"抽象：
    ///     <para>
    ///         - 返回是否匹配成功；
    ///     </para>
    ///     <para>
    ///         - 如需，可返回本要求项所匹配到的卡牌集合（用于效果管线的 Matched）。
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         <b>上下文访问示例：</b>
    ///     </para>
    ///     <code>
    ///     public bool TryMatch(CardRuleContext ctx, out List&lt;Card&gt; matched)
    ///     {
    ///         matched = new List&lt;Card&gt;();
    ///         
    ///         // 访问事件源卡牌
    ///         var source = ctx.Source;
    ///         
    ///         // 访问匹配范围根节点
    ///         var root = ctx.MatchRoot;
    ///         
    ///         // 访问强类型事件数据
    ///         if (ctx.TryGetEventData&lt;DamageData&gt;(out var damage))
    ///         {
    ///             // 使用伤害数据进行匹配
    ///         }
    ///         
    ///         // 访问卡牌引擎（如需要全局查询）
    ///         var engine = ctx.Engine;
    ///         
    ///         // 检查事件类型
    ///         if (ctx.IsEventType("Attack"))
    ///         {
    ///             // 处理攻击事件
    ///         }
    ///         
    ///         return true;
    ///     }
    ///     </code>
    /// </remarks>
    public interface IRuleRequirement
    {
        /// <summary>
        ///     在给定上下文下尝试匹配。
        /// </summary>
        /// <param name="ctx">
        ///     规则上下文，提供完整的执行信息：
        ///     <list type="bullet">
        ///         <item>
        ///             <description><see cref="CardRuleContext.Source" /> - 触发该规则的卡牌（事件源）</description>
        ///         </item>
        ///         <item>
        ///             <description><see cref="CardRuleContext.MatchRoot" /> - 用于匹配的容器根节点</description>
        ///         </item>
        ///         <item>
        ///             <description><see cref="CardRuleContext.EffectRoot" /> - 效果作用的根节点</description>
        ///         </item>
        ///         <item>
        ///             <description><see cref="CardRuleContext.Event" /> - 原始事件载体</description>
        ///         </item>
        ///         <item>
        ///             <description><see cref="CardRuleContext.Engine" /> - 卡牌引擎引用</description>
        ///         </item>
        ///         <item>
        ///             <description><see cref="CardRuleContext.EventEntry" /> - 事件条目（抽象事件源）</description>
        ///         </item>
        ///         <item>
        ///             <description><see cref="CardRuleContext.TryGetEventData{T}" /> - 获取强类型事件数据</description>
        ///         </item>
        ///     </list>
        /// </param>
        /// <param name="matched">本要求项匹配到的卡集合；可为空或空集。</param>
        /// <returns>是否匹配成功。</returns>
        bool TryMatch(CardRuleContext ctx, out List<Card> matched);
    }
}