using System.Collections.Generic;

namespace EasyPack.EmeCardSystem
{
    /// <summary>
    /// 委托调用效果：在效果阶段执行一段自定义委托逻辑
    /// <para></para>
    /// 典型用途：
    /// <para></para>
    /// - 打印/调试日志、复杂流程编排
    /// <para></para>
    /// - 自定义派发事件（如 <see cref="Card.Custom(string, object)"/>）
    /// <para></para>
    /// - 对 matched 做额外处理或跨系统桥接
    /// <para>注意</para>
    /// - 该效果不进行目标选择，直接接收匹配阶段的 <paramref name="matched"/>；
    /// </summary>
    public class InvokeEffect : IRuleEffect
    {
        /// <summary>
        /// 要执行的委托，签名为 (ctx, matched)。
        /// </summary>
        private readonly System.Action<CardRuleContext, IReadOnlyList<Card>> _action;

        /// <summary>
        /// 构造一个委托调用效果。
        /// </summary>
        /// <param name="action">要执行的委托（允许为 null，执行时将被忽略）。</param>
        public InvokeEffect(System.Action<CardRuleContext, IReadOnlyList<Card>> action) => _action = action;

        /// <summary>
        /// 执行委托。
        /// </summary>
        /// <param name="ctx">规则上下文。</param>
        /// <param name="matched">匹配阶段的命中集合。</param>
        public void Execute(CardRuleContext ctx, IReadOnlyList<Card> matched) => _action?.Invoke(ctx, matched);
    }
}
