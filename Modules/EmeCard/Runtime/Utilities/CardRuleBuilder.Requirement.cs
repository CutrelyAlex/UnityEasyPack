using System;
using System.Collections.Generic;

namespace EasyPack.EmeCardSystem
{
    /// <summary>
    ///     CardRuleBuilder - 条件要求部分
    /// </summary>
    public sealed partial class CardRuleBuilder
    {
        #region 条件要求 - When语法糖

        /// <summary>要求源卡牌的默认分类为指定分类</summary>
        public CardRuleBuilder WhenSourceCategory(string category)
        {
            return When(ctx => string.Equals(ctx.Source?.Data?.Category, category, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>要求源卡牌为对象类别</summary>
        public CardRuleBuilder WhenSourceIsObject() => WhenSourceCategory("Card.Object");

        /// <summary>要求源卡牌为动作类别</summary>
        public CardRuleBuilder WhenSourceIsAction() => WhenSourceCategory("Card.Action");

        /// <summary>要求源卡牌为属性类别</summary>
        public CardRuleBuilder WhenSourceIsAttribute() => WhenSourceCategory("Card.Attribute");

        /// <summary>要求源卡牌有指定标签</summary>
        public CardRuleBuilder WhenSourceHasTag(string tag)
        {
            return When(ctx => ctx.Source?.HasTag(tag) ?? false);
        }

        /// <summary>要求源卡牌没有指定标签</summary>
        public CardRuleBuilder WhenSourceNotHasTag(string tag)
        {
            return When(ctx => !(ctx.Source?.HasTag(tag) ?? false));
        }

        /// <summary>要求源卡牌的ID为指定值</summary>
        public CardRuleBuilder WhenSourceId(string id)
        {
            return When(ctx => string.Equals(ctx.Source?.Id, id, StringComparison.Ordinal));
        }

        /// <summary>要求容器的默认分类为指定分类</summary>
        public CardRuleBuilder WhenMatchRootCategory(string category)
        {
            return When(ctx =>
                string.Equals(ctx.MatchRoot?.Data?.Category, category, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>要求容器为对象类别</summary>
        public CardRuleBuilder WhenMatchRootIsObject() => WhenMatchRootCategory("Card.Object");

        /// <summary>要求容器为动作类别</summary>
        public CardRuleBuilder WhenMatchRootIsAction() => WhenMatchRootCategory("Card.Action");

        /// <summary>要求容器为属性类别</summary>
        public CardRuleBuilder WhenMatchRootIsAttribute() => WhenMatchRootCategory("Card.Attribute");

        /// <summary>要求容器有指定标签</summary>
        public CardRuleBuilder WhenMatchRootHasTag(string tag)
        {
            return When(ctx => ctx.MatchRoot?.HasTag(tag) ?? false);
        }

        /// <summary>要求容器没有指定标签</summary>
        public CardRuleBuilder WhenMatchRootNotHasTag(string tag)
        {
            return When(ctx => !(ctx.MatchRoot?.HasTag(tag) ?? false));
        }

        /// <summary>要求事件数据为指定类型</summary>
        public CardRuleBuilder WhenEventDataIs<T>() where T : class
        {
            return When(ctx => ctx.Event.DataObject is T);
        }

        /// <summary>要求事件数据不为空</summary>
        public CardRuleBuilder WhenEventDataNotNull()
        {
            return When(ctx => ctx.Event.DataObject != null);
        }

        #endregion

        #region 条件要求 - 便捷语法糖

        /// <summary>需要容器的直接子卡中有指定标签的卡牌</summary>
        public CardRuleBuilder NeedMatchRootTag(string tag, int minCount = 1, int maxMatched = -1) => Need(
            SelectionRoot.MatchRoot, TargetScope.Children, CardFilterMode.ByTag, tag, minCount, maxMatched);

        /// <summary>需要容器的直接子卡中有指定ID的卡牌</summary>
        public CardRuleBuilder NeedMatchRootId(string id, int minCount = 1, int maxMatched = -1) => Need(
            SelectionRoot.MatchRoot,
            TargetScope.Children, CardFilterMode.ById, id, minCount, maxMatched);

        /// <summary>需要容器的直接子卡中有指定类别的卡牌</summary>
        public CardRuleBuilder NeedMatchRootCategory(string category, int minCount = 1, int maxMatched = -1) =>
            Need(SelectionRoot.MatchRoot, TargetScope.Children, CardFilterMode.ByCategory, category,
                minCount, maxMatched);

        /// <summary>需要容器的所有后代中有指定标签的卡牌</summary>
        public CardRuleBuilder NeedMatchRootTagRecursive(string tag, int minCount = 1, int maxMatched = -1,
                                                         int? maxDepth = null) =>
            Need(SelectionRoot.MatchRoot, TargetScope.Descendants, CardFilterMode.ByTag, tag, minCount,
                maxMatched, maxDepth);

        /// <summary>需要容器的所有后代中有指定ID的卡牌</summary>
        public CardRuleBuilder
            NeedIdRecursive(string id, int minCount = 1, int maxMatched = -1, int? maxDepth = null) =>
            Need(SelectionRoot.MatchRoot, TargetScope.Descendants, CardFilterMode.ById, id, minCount, maxMatched,
                maxDepth);

        /// <summary>需要容器的所有后代中有指定类别的卡牌</summary>
        public CardRuleBuilder NeedCategoryRecursive(string category, int minCount = 1, int maxMatched = -1,
                                                     int? maxDepth = null) =>
            Need(SelectionRoot.MatchRoot, TargetScope.Descendants, CardFilterMode.ByCategory,
                category, minCount, maxMatched, maxDepth);

        /// <summary>需要源卡的直接子卡中有指定标签的卡牌</summary>
        public CardRuleBuilder NeedSourceTag(string tag, int minCount = 1, int maxMatched = -1) =>
            Need(SelectionRoot.Source, TargetScope.Children, CardFilterMode.ByTag, tag, minCount, maxMatched);

        /// <summary>需要源卡的两层子卡中有指定标签的卡牌</summary>
        public CardRuleBuilder NeedSourceTagTwo(string tag, int minCount = 1, int maxMatched = -1) =>
            Need(SelectionRoot.Source, TargetScope.Descendants, CardFilterMode.ByTag, tag, minCount, maxMatched, 2);

        /// <summary>需要源卡的直接子卡中有指定ID的卡牌</summary>
        public CardRuleBuilder NeedSourceId(string id, int minCount = 1, int maxMatched = -1) =>
            Need(SelectionRoot.Source, TargetScope.Children, CardFilterMode.ById, id, minCount, maxMatched);

        /// <summary>需要源卡的所有后代中有指定标签的卡牌</summary>
        public CardRuleBuilder NeedSourceTagRecursive(string tag, int minCount = 1, int maxMatched = -1,
                                                      int? maxDepth = null) =>
            Need(SelectionRoot.Source, TargetScope.Descendants, CardFilterMode.ByTag, tag, minCount, maxMatched,
                maxDepth);

        /// <summary>需要源卡或直接子卡中有指定标签的卡牌</summary>
        public CardRuleBuilder NeedSourceOrChildHasTag(string tag) => AddRequirement(new AnyRequirement
        {
            Children =
            {
                new ConditionRequirement(context =>
                {
                    if (context.Source.HasTag(tag))
                    {
                        return (true, new() { context.Source });
                    }

                    return (false, null);
                }),
                new CardsRequirement(SelectionRoot.Source, TargetScope.Children, CardFilterMode.ByTag, tag,
                    1, 0, 1),
            },
        });

        /// <summary>需要源卡或直接子卡中没有指定标签的卡牌</summary>
        public CardRuleBuilder NeedSourceOrChildNotHasTag(string tag) => AddRequirement(new NotRequirement
        {
            Inner = new AnyRequirement
            {
                Children =
                {
                    new ConditionRequirement(context =>
                    {
                        if (context.Source.HasTag(tag))
                        {
                            return (true, new() { context.Source });
                        }

                        return (false, null);
                    }),
                    new CardsRequirement(SelectionRoot.Source, CardFilterMode.ByTag, tag,
                        1, TargetScope.Children),
                },
            },
        });
        
        /// <summary>需要容器或直接子卡中有指定标签的卡牌</summary>
        public CardRuleBuilder NeedContainerOrChildHasTag(string tag) => AddRequirement(new AnyRequirement
        {
            Children =
            {
                new ConditionRequirement(context =>
                {
                    if (context.MatchRoot.HasTag(tag))
                    {
                        return (true, new() { context.Source });
                    }

                    return (false, null);
                }),
                new CardsRequirement(SelectionRoot.MatchRoot, TargetScope.Children, CardFilterMode.ByTag, tag,
                    1, 0, 1),
            },
        });

        #endregion
    }
}