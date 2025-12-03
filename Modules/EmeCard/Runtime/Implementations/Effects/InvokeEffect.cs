using System;
using System.Collections.Generic;

namespace EasyPack.EmeCardSystem
{
    /// <summary>
    ///     委托调用效果：在效果阶段执行一段自定义委托逻辑。
    ///     <para>
    ///         这是最灵活的效果类型，允许在规则匹配成功后执行任意代码。
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         <b>上下文使用示例：</b>
    ///     </para>
    ///     <code>
    ///     new InvokeEffect((ctx, matched) =>
    ///     {
    ///         // 访问效果作用根节点
    ///         var effectRoot = ctx.EffectRoot;
    ///         
    ///         // 访问事件源卡牌
    ///         var source = ctx.Source;
    ///         
    ///         // 获取强类型事件数据
    ///         if (ctx.TryGetEventData&lt;DamageData&gt;(out var damage))
    ///         {
    ///             // 对每个匹配的目标应用伤害
    ///             foreach (var target in matched)
    ///             {
    ///                 // 处理伤害逻辑
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
    ///     })
    ///     </code>
    /// </remarks>
    public class InvokeEffect : IRuleEffect
    {
        /// <summary>
        ///     要执行的委托，签名为 (ctx, matched)。
        /// </summary>
        private readonly Action<CardRuleContext, HashSet<Card>> _action;

        /// <summary>
        ///     构造一个委托调用效果。
        /// </summary>
        /// <param name="action">要执行的委托（允许为 null，执行时将被忽略）。</param>
        public InvokeEffect(Action<CardRuleContext, HashSet<Card>> action) => _action = action;

        /// <inheritdoc />
        public void Execute(CardRuleContext ctx, HashSet<Card> matched)
        {
            _action?.Invoke(ctx, matched);
        }
    }
}