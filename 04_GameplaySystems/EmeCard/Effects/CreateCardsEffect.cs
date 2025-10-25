using System.Collections.Generic;

namespace EasyPack.EmeCardSystem
{
    /// <summary>
    /// 产卡效果：在上下文容器（<see cref="CardRuleContext.Container"/>）中创建指定 ID 的新卡。
    /// <para></para>
    /// - 必须存在 <see cref="CardRuleContext.Factory"/>，否则不生效；
    /// <para></para>
    /// - 每个 ID 可创建 <see cref="CountPerId"/> 份；创建失败（工厂无此 ID）将被跳过；
    /// <para></para>
    /// - 成功 AddChild 后会向新卡派发 AddedToOwner 事件。
    /// </summary>
    public class CreateCardsEffect : IRuleEffect
    {
        /// <summary>
        /// 要创建的卡牌 ID 列表（依赖工厂注册）。
        /// </summary>
        public List<string> CardIds { get; set; } = new List<string>();

        /// <summary>
        /// 每个 ID 的创建数量（默认 1；&lt;=0 时不创建）。
        /// </summary>
        public int CountPerId { get; set; } = 1;

        /// <summary>
        /// 执行产卡。
        /// </summary>
        /// <param name="ctx">规则上下文（需包含 Factory 与 Container）。</param>
        /// <param name="matched">匹配结果（本效果不使用）。</param>
        public void Execute(CardRuleContext ctx, IReadOnlyList<Card> matched)
        {
            if (ctx.Factory == null || ctx.Container == null || CardIds == null || CardIds.Count == 0 || CountPerId <= 0)
                return;

            foreach (var id in CardIds)
            {
                for (int i = 0; i < CountPerId; i++)
                {
                    var card = ctx.Factory.Owner.CreateCard(id);
                    if (card != null)
                        ctx.Container.AddChild(card);
                }
            }
        }
    }
}
