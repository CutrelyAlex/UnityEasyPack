using System;
using System.Collections.Generic;
using UnityEngine;

namespace EasyPack.EmeCardSystem
{
    /// <summary>
    /// 规则流式构建器
    /// 提供核心方法和便捷语法糖
    /// </summary>
    public sealed partial class CardRuleBuilder
    {
        private readonly CardRule _rule = new()
        {
            Requirements = new List<IRuleRequirement>(),
            Effects = new List<IRuleEffect>(),
            Policy = new RulePolicy { DistinctMatched = true }
        };

        #region 基础配置
        /// <summary>设置事件触发类型</summary>
        public CardRuleBuilder On(CardEventType eventType, string customId = null)
        {
            _rule.Trigger = eventType;
            _rule.CustomId = customId;
            return this;
        }

        /// <summary>设置容器锚点（0=Self, 1=Owner, -1=Root, N>1=向上N层）</summary>
        public CardRuleBuilder OwnerHops(int hops)
        {
            _rule.OwnerHops = hops;
            return this;
        }

        /// <summary>以自身为容器（OwnerHops=0）</summary>
        public CardRuleBuilder AtSelf() => OwnerHops(0);

        /// <summary>以直接父级为容器（OwnerHops=1）</summary>
        public CardRuleBuilder AtParent() => OwnerHops(1);

        /// <summary>以根容器为容器（OwnerHops=-1）</summary>
        public CardRuleBuilder AtRoot() => OwnerHops(-1);

        /// <summary>设置递归最大深度</summary>
        public CardRuleBuilder MaxDepth(int depth)
        {
            _rule.MaxDepth = depth;
            return this;
        }

        /// <summary>设置规则优先级（数值越小越优先）</summary>
        public CardRuleBuilder Priority(int priority)
        {
            _rule.Priority = priority;
            return this;
        }

        /// <summary>是否对匹配结果去重</summary>
        public CardRuleBuilder DistinctMatched(bool enabled = true)
        {
            _rule.Policy.DistinctMatched = enabled;
            return this;
        }

        /// <summary>执行后中止事件传播</summary>
        public CardRuleBuilder StopPropagation(bool stop = true)
        {
            _rule.Policy.StopEventOnSuccess = stop;
            return this;
        }
        #endregion

        #region 条件要求 - 核心
        /// <summary>添加条件判断</summary>
        public CardRuleBuilder When(Func<CardRuleContext, bool> predicate)
        {
            if (predicate != null)
                _rule.Requirements.Add(new ConditionRequirement(predicate));
            return this;
        }

        /// <summary>添加卡牌需求：需要从指定根选择特定卡牌</summary>
        public CardRuleBuilder Need(
            SelectionRoot root,
            TargetScope scope,
            CardFilterMode filter = CardFilterMode.None,
            string filterValue = null,
            int minCount = 1,
            int maxMatched = -1,
            int? maxDepth = null)
        {
            _rule.Requirements.Add(new CardsRequirement
            {
                Root = root,
                Scope = scope,
                FilterMode = filter,
                FilterValue = filterValue,
                MinCount = minCount,
                MaxMatched = maxMatched,
                MaxDepth = maxDepth
            });
            return this;
        }

        /// <summary>添加自定义要求</summary>
        public CardRuleBuilder AddRequirement(IRuleRequirement requirement)
        {
            if (requirement != null)
                _rule.Requirements.Add(requirement);
            return this;
        }
        #endregion

        #region 条件要求 - When语法糖
        /// <summary>要求源卡牌的类别为指定类别</summary>
        public CardRuleBuilder WhenSourceCategory(CardCategory category)
            => When(ctx => ctx.Source?.Category == category);

        /// <summary>要求源卡牌为对象类别</summary>
        public CardRuleBuilder WhenSourceIsObject()
            => When(ctx => ctx.Source?.Category == CardCategory.Object);

        /// <summary>要求源卡牌为动作类别</summary>
        public CardRuleBuilder WhenSourceIsAction()
            => When(ctx => ctx.Source?.Category == CardCategory.Action);

        /// <summary>要求源卡牌为属性类别</summary>
        public CardRuleBuilder WhenSourceIsAttribute()
            => When(ctx => ctx.Source?.Category == CardCategory.Attribute);

        /// <summary>要求源卡牌有指定标签</summary>
        public CardRuleBuilder WhenSourceHasTag(string tag)
            => When(ctx => ctx.Source != null && ctx.Source.HasTag(tag));

        /// <summary>要求源卡牌没有指定标签</summary>
        public CardRuleBuilder WhenSourceNotHasTag(string tag)
            => When(ctx => ctx.Source != null && !ctx.Source.HasTag(tag));

        /// <summary>要求源卡牌的ID为指定值</summary>
        public CardRuleBuilder WhenSourceId(string id)
            => When(ctx => ctx.Source != null && string.Equals(ctx.Source.Id, id, StringComparison.Ordinal));

        /// <summary>要求容器的类别为指定类别</summary>
        public CardRuleBuilder WhenContainerCategory(CardCategory category)
            => When(ctx => ctx.Container?.Category == category);

        /// <summary>要求容器为对象类别</summary>
        public CardRuleBuilder WhenContainerIsObject()
            => When(ctx => ctx.Container?.Category == CardCategory.Object);

        /// <summary>要求容器为动作类别</summary>
        public CardRuleBuilder WhenContainerIsAction()
            => When(ctx => ctx.Container?.Category == CardCategory.Action);

        /// <summary>要求容器为属性类别</summary>
        public CardRuleBuilder WhenContainerIsAttribute()
            => When(ctx => ctx.Container?.Category == CardCategory.Attribute);

        /// <summary>要求容器有指定标签</summary>
        public CardRuleBuilder WhenContainerHasTag(string tag)
            => When(ctx => ctx.Container != null && ctx.Container.HasTag(tag));

        /// <summary>要求容器没有指定标签</summary>
        public CardRuleBuilder WhenContainerNotHasTag(string tag)
            => When(ctx => ctx.Container != null && !ctx.Container.HasTag(tag));

        /// <summary>要求事件数据为指定类型</summary>
        public CardRuleBuilder WhenEventDataIs<T>() where T : class
            => When(ctx => ctx.Event.Data is T);

        /// <summary>要求事件数据不为空</summary>
        public CardRuleBuilder WhenEventDataNotNull()
            => When(ctx => ctx.Event.Data != null);
        #endregion

        #region 条件要求 - 便捷语法糖
        /// <summary>需要容器的直接子卡中有指定标签的卡牌</summary>
        public CardRuleBuilder NeedTag(string tag, int minCount = 1, int maxMatched = -1)
            => Need(SelectionRoot.Container, TargetScope.Children, CardFilterMode.ByTag, tag, minCount, maxMatched);

        /// <summary>需要容器的直接子卡中有指定ID的卡牌</summary>
        public CardRuleBuilder NeedId(string id, int minCount = 1, int maxMatched = -1)
            => Need(SelectionRoot.Container, TargetScope.Children, CardFilterMode.ById, id, minCount, maxMatched);

        /// <summary>需要容器的直接子卡中有指定类别的卡牌</summary>
        public CardRuleBuilder NeedCategory(CardCategory category, int minCount = 1, int maxMatched = -1)
            => Need(SelectionRoot.Container, TargetScope.Children, CardFilterMode.ByCategory, category.ToString(), minCount, maxMatched);

        /// <summary>需要容器的所有后代中有指定标签的卡牌</summary>
        public CardRuleBuilder NeedTagRecursive(string tag, int minCount = 1, int maxMatched = -1, int? maxDepth = null)
            => Need(SelectionRoot.Container, TargetScope.Descendants, CardFilterMode.ByTag, tag, minCount, maxMatched, maxDepth);

        /// <summary>需要容器的所有后代中有指定ID的卡牌</summary>
        public CardRuleBuilder NeedIdRecursive(string id, int minCount = 1, int maxMatched = -1, int? maxDepth = null)
            => Need(SelectionRoot.Container, TargetScope.Descendants, CardFilterMode.ById, id, minCount, maxMatched, maxDepth);

        /// <summary>需要容器的所有后代中有指定类别的卡牌</summary>
        public CardRuleBuilder NeedCategoryRecursive(CardCategory category, int minCount = 1, int maxMatched = -1, int? maxDepth = null)
            => Need(SelectionRoot.Container, TargetScope.Descendants, CardFilterMode.ByCategory, category.ToString(), minCount, maxMatched, maxDepth);

        /// <summary>需要源卡的直接子卡中有指定标签的卡牌</summary>
        public CardRuleBuilder NeedSourceTag(string tag, int minCount = 1, int maxMatched = -1)
            => Need(SelectionRoot.Source, TargetScope.Children, CardFilterMode.ByTag, tag, minCount, maxMatched);

        /// <summary>需要源卡的直接子卡中有指定ID的卡牌</summary>
        public CardRuleBuilder NeedSourceId(string id, int minCount = 1, int maxMatched = -1)
            => Need(SelectionRoot.Source, TargetScope.Children, CardFilterMode.ById, id, minCount, maxMatched);

        /// <summary>需要源卡的所有后代中有指定标签的卡牌</summary>
        public CardRuleBuilder NeedSourceTagRecursive(string tag, int minCount = 1, int maxMatched = -1, int? maxDepth = null)
            => Need(SelectionRoot.Source, TargetScope.Descendants, CardFilterMode.ByTag, tag, minCount, maxMatched, maxDepth);
        #endregion

        #region 效果执行 - 核心
        /// <summary>添加自定义效果</summary>
        public CardRuleBuilder Do(IRuleEffect effect)
        {
            if (effect != null)
                _rule.Effects.Add(effect);
            return this;
        }
        /// <summary>添加多个自定义效果</summary>
        public CardRuleBuilder Do(params IRuleEffect[] effects)
        {
            if (effects != null)
            {
                foreach (var effect in effects)
                {
                    if (effect != null)
                        _rule.Effects.Add(effect);
                }
            }
            return this;
        }

        /// <summary>添加多个自定义效果</summary>
        public CardRuleBuilder Do(IEnumerable<IRuleEffect> effects)
        {
            if (effects != null)
            {
                foreach (var effect in effects)
                {
                    if (effect != null)
                        _rule.Effects.Add(effect);
                }
            }
            return this;
        }

        /// <summary>移除卡牌效果</summary>
        public CardRuleBuilder DoRemove(
            SelectionRoot root = SelectionRoot.Container,
            TargetScope scope = TargetScope.Matched,
            CardFilterMode filter = CardFilterMode.None,
            string filterValue = null,
            int? take = null,
            int? maxDepth = null)
        {
            _rule.Effects.Add(new RemoveCardsEffect
            {
                Root = root,
                Scope = scope,
                Filter = filter,
                FilterValue = filterValue,
                Take = take,
                MaxDepth = maxDepth
            });
            return this;
        }

        /// <summary>修改属性效果</summary>
        public CardRuleBuilder DoModify(
            string propertyName,
            float value,
            ModifyPropertyEffect.Mode mode = ModifyPropertyEffect.Mode.AddToBase,
            SelectionRoot root = SelectionRoot.Container,
            TargetScope scope = TargetScope.Matched,
            CardFilterMode filter = CardFilterMode.None,
            string filterValue = null,
            int? take = null,
            int? maxDepth = null)
        {
            _rule.Effects.Add(new ModifyPropertyEffect
            {
                PropertyName = propertyName,
                Value = value,
                ApplyMode = mode,
                Root = root,
                Scope = scope,
                Filter = filter,
                FilterValue = filterValue,
                Take = take,
                MaxDepth = maxDepth
            });
            return this;
        }

        /// <summary>添加标签效果</summary>
        public CardRuleBuilder DoAddTag(
            string tag,
            SelectionRoot root = SelectionRoot.Container,
            TargetScope scope = TargetScope.Matched,
            CardFilterMode filter = CardFilterMode.None,
            string filterValue = null,
            int? take = null,
            int? maxDepth = null)
        {
            _rule.Effects.Add(new AddTagEffect
            {
                Tag = tag,
                Root = root,
                Scope = scope,
                Filter = filter,
                FilterValue = filterValue,
                Take = take,
                MaxDepth = maxDepth
            });
            return this;
        }

        /// <summary>创建卡牌效果</summary>
        public CardRuleBuilder DoCreate(string cardId, int count = 1)
        {
            _rule.Effects.Add(new CreateCardsEffect
            {
                CardIds = new List<string> { cardId },
                CountPerId = count
            });
            return this;
        }

        /// <summary>执行自定义逻辑</summary>
        public CardRuleBuilder DoInvoke(Action<CardRuleContext, IReadOnlyList<Card>> action)
        {
            if (action != null)
                _rule.Effects.Add(new InvokeEffect(action));
            return this;
        }
        #endregion

        #region 效果执行 - 便捷语法糖
        /// <summary>移除匹配结果中指定标签的卡牌</summary>
        public CardRuleBuilder DoRemoveByTag(string tag, int? take = null)
            => DoRemove(SelectionRoot.Container, TargetScope.Matched, CardFilterMode.ByTag, tag, take);

        /// <summary>移除匹配结果中指定ID的卡牌</summary>
        public CardRuleBuilder DoRemoveById(string id, int? take = null)
            => DoRemove(SelectionRoot.Container, TargetScope.Matched, CardFilterMode.ById, id, take);

        /// <summary>移除容器子卡中指定标签的卡牌</summary>
        public CardRuleBuilder DoRemoveInChildByTag(string tag, int? take = null)
            => DoRemove(SelectionRoot.Container, TargetScope.Children, CardFilterMode.ByTag, tag, take);

        /// <summary>移除容器子卡中指定ID的卡牌</summary>
        public CardRuleBuilder DoRemoveInChildById(string id, int? take = null)
            => DoRemove(SelectionRoot.Container, TargetScope.Children, CardFilterMode.ById, id, take);

        /// <summary>给匹配结果添加标签</summary>
        public CardRuleBuilder DoAddTagToMatched(string tag)
            => DoAddTag(tag, SelectionRoot.Container, TargetScope.Matched);

        /// <summary>给源卡牌自身添加标签</summary>
        public CardRuleBuilder DoAddTagToSource(string tag)
            => DoInvoke((ctx, matched) => ctx.Source.AddTag(tag));

        /// <summary>给容器卡牌自身添加标签</summary>
        public CardRuleBuilder DoAddTagToContainer(string tag)
            => DoInvoke((ctx, matched) => ctx.Container.AddTag(tag));

        /// <summary>给容器子卡中指定标签的卡牌添加新标签</summary>
        public CardRuleBuilder DoAddTagToTag(string targetTag, string newTag, int? take = null)
            => DoAddTag(newTag, SelectionRoot.Container, TargetScope.Children, CardFilterMode.ByTag, targetTag, take);

        /// <summary>给容器子卡中指定ID的卡牌添加标签</summary>
        public CardRuleBuilder DoAddTagToId(string targetId, string newTag, int? take = null)
            => DoAddTag(newTag, SelectionRoot.Container, TargetScope.Children, CardFilterMode.ById, targetId, take);

        /// <summary>给源卡牌自身移除标签</summary>
        public CardRuleBuilder DoRemoveTagToSource(string tag)
            => DoInvoke((ctx, matched) => ctx.Source.RemoveTag(tag));

        /// <summary>给容器卡牌自身移除标签</summary>
        public CardRuleBuilder DoRemoveTagToContainer(string tag)
            => DoInvoke((ctx, matched) => ctx.Container.RemoveTag(tag));

        /// <summary>修改匹配结果中指定标签的卡牌的属性</summary>
        public CardRuleBuilder DoModifyTag(
            string tag,
            string propertyName,
            float value,
            ModifyPropertyEffect.Mode mode = ModifyPropertyEffect.Mode.AddToBase,
            int? take = null)
            => DoModify(propertyName, value, mode, SelectionRoot.Container, TargetScope.Children, CardFilterMode.ByTag, tag, take);

        /// <summary>修改匹配结果的属性</summary>
        public CardRuleBuilder DoModifyMatched(
            string propertyName,
            float value,
            ModifyPropertyEffect.Mode mode = ModifyPropertyEffect.Mode.AddToBase)
            => DoModify(propertyName, value, mode, SelectionRoot.Container, TargetScope.Matched);

        /// <summary>批量触发匹配卡牌的自定义事件</summary>
        public CardRuleBuilder DoBatchCustom(string eventId, Func<CardRuleContext, object> data = null, bool haveSource = true)
        {
            return DoInvoke((ctx, matched) =>
            {
                object newData = data == null ? ctx : data.Invoke(ctx);
                if (haveSource)
                {
                    ctx.Source.Custom(eventId, newData);
                }

                foreach (var card in matched)
                {
                    card.Custom(eventId, newData);
                }
            });
        }
        #endregion

        #region Debug

        public CardRuleBuilder PrintContext() => DoInvoke(((context, list) => Debug.Log(context.ToString())));

        #endregion

        /// <summary>构建规则</summary>
        public CardRule Build() => _rule;
    }

    /// <summary>
    /// 规则注册扩展方法
    /// </summary>
    public static class RuleRegistrationExtensions
    {
        /// <summary>注册规则</summary>
        public static CardRule RegisterRule(this CardEngine engine, Action<CardRuleBuilder> configure)
        {
            var builder = new CardRuleBuilder();
            configure?.Invoke(builder);
            var rule = builder.Build();
            engine.RegisterRule(rule);
            return rule;
        }
        /// <summary>注册规则集</summary>
        public static void RegisterRule(this CardEngine engine, IReadOnlyList<Action<CardRuleBuilder>> configures)
        {
            foreach (var configure in configures)
            {
                RegisterRule(engine, configure);
            }
        }
    }
}
