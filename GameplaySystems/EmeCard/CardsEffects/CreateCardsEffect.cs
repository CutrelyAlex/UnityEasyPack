using System.Collections.Generic;

namespace EasyPack
{
    /// <summary>
    /// 产卡效果：在上下文容器中创建指定ID的新卡牌。
    /// 依赖 CardRuleContext.Factory；若未设置工厂则不生效。
    /// </summary>
    public sealed class CreateCardsEffect : IRuleEffect
    {
        /// <summary>要创建的卡牌ID列表。</summary>
        public List<string> CardIds { get; set; } = new List<string>();

        /// <summary>每个ID创建数量，默认1。</summary>
        public int CountPerId { get; set; } = 1;

        public void Execute(CardRuleContext ctx, IReadOnlyList<Card> matched)
        {
            if (ctx.Factory == null || ctx.Container == null || CardIds == null || CardIds.Count == 0 || CountPerId <= 0)
                return;

            foreach (var id in CardIds)
            {
                for (int i = 0; i < CountPerId; i++)
                {
                    var card = ctx.Factory.Create(id);
                    if (card != null)
                        ctx.Container.AddChild(card);
                }
            }
        }
    }
}