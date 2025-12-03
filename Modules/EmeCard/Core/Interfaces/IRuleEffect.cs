using System.Collections.Generic;

namespace EasyPack.EmeCardSystem
{
    /// <summary>
    ///     规则效果接口：在规则匹配成功后执行的效果逻辑。
    ///     <para>
    ///         效果可以修改卡牌状态、触发新事件、创建/移除卡牌等。
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         <b>上下文访问示例：</b>
    ///     </para>
    ///     <code>
    ///     public void Execute(CardRuleContext ctx, IReadOnlyList&lt;Card&gt; matched)
    ///     {
    ///         // 访问效果作用根节点
    ///         var effectRoot = ctx.EffectRoot;
    ///         
    ///         // 访问事件源卡牌
    ///         var source = ctx.Source;
    ///         
    ///         // 访问强类型事件数据
    ///         if (ctx.TryGetEventData&lt;DamageData&gt;(out var damage))
    ///         {
    ///             // 应用伤害效果
    ///             foreach (var target in matched)
    ///             {
    ///                 // 处理每个匹配的目标
    ///             }
    ///         }
    ///         
    ///         // 通过引擎触发新事件
    ///         ctx.Engine?.EnqueueEvent(source, new CardEvent&lt;int&gt;("Heal", 10));
    ///         
    ///         // 使用引擎创建新卡牌
    ///         var newCard = ctx.Engine?.CreateCard("CardId");
    ///         if (newCard != null)
    ///         {
    ///             ctx.EffectRoot.AddChild(newCard);
    ///         }
    ///     }
    ///     </code>
    /// </remarks>
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