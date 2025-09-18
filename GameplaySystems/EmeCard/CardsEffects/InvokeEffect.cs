using System.Collections.Generic;

namespace EasyPack
{
        // 执行任意委托的效果，便于演示链式事件触发/日志等
        public sealed class InvokeEffect : IRuleEffect
        {
            private readonly System.Action<CardRuleContext, IReadOnlyList<Card>> _action;
            public InvokeEffect(System.Action<CardRuleContext, IReadOnlyList<Card>> action) => _action = action;
            public void Execute(CardRuleContext ctx, IReadOnlyList<Card> matched) => _action?.Invoke(ctx, matched);
        }
}