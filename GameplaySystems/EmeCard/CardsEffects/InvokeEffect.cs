using System.Collections.Generic;

namespace EasyPack
{
        /// <summary>
        /// 执行任意委托的效果
        /// </summary>
        public class InvokeEffect : IRuleEffect
        {
            private readonly System.Action<CardRuleContext, IReadOnlyList<Card>> _action;
            public InvokeEffect(System.Action<CardRuleContext, IReadOnlyList<Card>> action) => _action = action;
            public void Execute(CardRuleContext ctx, IReadOnlyList<Card> matched) => _action?.Invoke(ctx, matched);
        }
}