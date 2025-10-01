using System;
using System.Collections.Generic;
using UnityEngine;

namespace EasyPack
{
    /// <summary>
    /// 规则流式构建语法糖
    /// </summary>
    public sealed class CardRuleBuilder
    {
        private readonly CardRule _rule = new CardRule
        {
            Requirements = new List<IRuleRequirement>(),
            Effects = new List<IRuleEffect>(),
            Policy = new RulePolicy { DistinctMatched = true }
        };

        /// <summary>
        /// 设置事件触发类型及过滤器
        /// </summary>
        /// <param name="type">事件触发类型</param>
        /// <param name="customId">过滤器</param>
        public CardRuleBuilder Trigger(CardEventType type, string customId = null)
        {
            _rule.Trigger = type;
            _rule.CustomId = customId;
            return this;
        }

        #region 事件便捷
        /// <summary>
        /// 自定义事件便捷设置
        /// </summary>
        public CardRuleBuilder TriggerCustom(string id)
        {
            return Trigger(CardEventType.Custom, id);
        }

        /// <summary>按时事件便捷设置</summary>
        public CardRuleBuilder TriggerTick() => Trigger(CardEventType.Tick);

        /// <summary>主动使用事件便捷设置</summary>
        public CardRuleBuilder TriggerUse() => Trigger(CardEventType.Use);

        /// <summary>加入持有者事件便捷设置</summary>
        public CardRuleBuilder TriggerAddedToOwner() => Trigger(CardEventType.AddedToOwner);

        /// <summary>从持有者移除事件便捷设置</summary>
        public CardRuleBuilder TriggerRemovedFromOwner() => Trigger(CardEventType.RemovedFromOwner);
        #endregion

        #region 容器锚点便捷
        /// <summary>
        /// 容器锚点选择
        /// 0=Self，1=Owner（默认），N>1 上溯，-1=Root。
        /// </summary>
        /// <param name="hops">0=Self，1=Owner（默认），N>1 上溯，-1=Root。</param>
        public CardRuleBuilder OwnerHops(int hops) { _rule.OwnerHops = hops; return this; }
        /// <summary>选择自身作为容器（OwnerHops=0）。</summary>
        public CardRuleBuilder Self() { return OwnerHops(0); }
        /// <summary>选择直接父级作为容器（OwnerHops=1）。</summary>
        public CardRuleBuilder Owner() { return OwnerHops(1); }
        /// <summary>选择根作为容器（OwnerHops=-1）。</summary>
        public CardRuleBuilder Root() { return OwnerHops(-1); }
        #endregion

        #region 策略配置
        /// <summary>
        /// /// <summary>递归选择的最大深度（仅对递归类 TargetKind 生效）。</summary>
        /// </summary>
        /// <param name="depth">最大深度</param>
        public CardRuleBuilder MaxDepth(int depth) { _rule.MaxDepth = depth; return this; }

        /// <summary>设置规则优先级（数值越小优先）。</summary>
        public CardRuleBuilder Priority(int priority) { _rule.Priority = priority; return this; }
        public CardRuleBuilder Policy(Action<RulePolicy> configure) { configure?.Invoke(_rule.Policy); return this; }
        /// <summary>
        /// 是否对聚合的 matched 去重（仅影响 TargetKind=Matched 的效果）
        /// </summary>
        public CardRuleBuilder DistinctMatched(bool enabled = true)
        {
            _rule.Policy.DistinctMatched = enabled;
            return this;
        }
        /// <summary>
        /// 该规则命中并执行后，是否中止本次事件的后续规则（可选，用于强短路）
        /// </summary>
        public CardRuleBuilder StopAfterThisRule() { _rule.Policy.StopEventOnSuccess = true; return this; }
        #endregion

        #region Requirements When
        /// <summary>
        /// 添加自定义条件要求
        /// </summary>
        /// <param name="predicate">委托</param>
        public CardRuleBuilder Where(Func<CardRuleContext, bool> predicate)
        {
            _rule.Requirements.Add(new ConditionRequirement(predicate));
            return this;
        }
        /// <summary>
        /// 检查触发源标签
        /// </summary>
        /// <param name="tag">目标标签</param>
        public CardRuleBuilder WhenSourceTag(string tag) => Where(ctx => ctx.Source != null && ctx.Source.HasTag(tag));
        /// <summary>
        /// 检查触发源Id
        /// </summary>
        /// <param name="id">目标Id</param>
        public CardRuleBuilder WhenSourceId(string id) => Where(ctx => ctx.Source != null && ctx.Source.Id == id);
        /// <summary>
        /// 检查触发源类型
        /// </summary>
        /// <param name="category"></param>
        public CardRuleBuilder WhenSourceCategory(CardCategory category) => Where(ctx => ctx.Source != null && ctx.Source.Category == category);
        /// <summary>
        /// 检查容器对应卡牌标签
        /// </summary>
        /// <param name="tag">标签</param>
        public CardRuleBuilder WhenContainerTag(string tag) => Where(ctx => ctx.Container != null && ctx.Container.HasTag(tag));
        /// <summary>
        /// 检查容器对应卡牌Id
        /// </summary>
        /// <param name="id">卡牌Id</param>
        public CardRuleBuilder WhenContainerId(string id) => Where(ctx => ctx.Container != null && ctx.Container.Id == id);
        /// <summary>
        /// 检查容器对应卡牌类型
        /// </summary>
        /// <param name="category">卡牌类型</param>
        public CardRuleBuilder WhenContainerCategory(CardCategory category) => Where(ctx => ctx.Container != null && ctx.Container.Category == category);
        #endregion

        #region Requirements Need (Container)
        /// <summary>
        /// 根据标签获取一层子卡牌
        /// </summary>
        /// <param name="tag">标签</param>
        /// <param name="min">所需及获取数量，为0则获取全部</param>
        /// <returns></returns>
        public CardRuleBuilder NeedContainerTag(string tag, int min = 0) => NeedCard(RequirementRoot.Container, TargetKind.ByTag, tag, min);
        /// <summary>
        /// 根据Id获取一层子卡牌
        /// </summary>
        /// <param name="id">卡牌Id</param>
        /// <param name="min">所需及获取数量，为0则获取全部</param>
        /// <returns></returns>
        public CardRuleBuilder NeedContainerId(string id, int min = 0) => NeedCard(RequirementRoot.Container, TargetKind.ById, id, min);
        /// <summary>
        /// 根据类型获取一层子卡牌
        /// </summary>
        /// <param name="category">卡牌类型</param>
        /// <param name="min">所需及获取数量，为0则获取全部</param>
        /// <returns></returns>
        public CardRuleBuilder NeedContainerCategory(CardCategory category, int min = 0) => NeedCard(RequirementRoot.Container, TargetKind.ByCategory, category.ToString(), min);
        /// <summary>
        /// 根据标签递归获取子卡牌
        /// </summary>
        /// <param name="tag">标签</param>
        /// <param name="min">所需及获取数量，为0则获取全部</param>
        public CardRuleBuilder NeedContainerDescendantsTag(string tag, int min = 0) => NeedCard(RequirementRoot.Container, TargetKind.ByTagRecursive, tag, min);
        /// <summary>
        /// 根据Id递归获取子卡牌
        /// </summary>
        public CardRuleBuilder NeedContainerDescendantsId(string id, int min = 0) => NeedCard(RequirementRoot.Container, TargetKind.ByIdRecursive, id, min);
        /// <summary>
        /// 根据类型递归获取子卡牌
        /// </summary>
        public CardRuleBuilder NeedContainerDescendantsCategory(CardCategory category, int min = 0) => NeedCard(RequirementRoot.Container, TargetKind.ByCategoryRecursive, category.ToString(), min);
        /// <summary>
        /// 获取容器的一层子卡（不带筛选）
        /// </summary>
        public CardRuleBuilder NeedContainerChildren(int min = 1) => NeedCard(RequirementRoot.Container, TargetKind.ContainerChildren, null, min);
        #endregion

        #region Requirements Need (Source)
        /// <summary>
        /// 根据标签获取源卡的一层子卡牌
        /// </summary>
        public CardRuleBuilder NeedSourceTag(string tag, int min = 0) => NeedCard(RequirementRoot.Source, TargetKind.ByTag, tag, min);
        /// <summary>
        /// 根据Id获取源卡的一层子卡牌
        /// </summary>
        public CardRuleBuilder NeedSourceId(string id, int min = 0) => NeedCard(RequirementRoot.Source, TargetKind.ById, id, min);
        /// <summary>
        /// 根据类型获取源卡的一层子卡牌
        /// </summary>
        public CardRuleBuilder NeedSourceCategory(CardCategory category, int min = 0) => NeedCard(RequirementRoot.Source, TargetKind.ByCategory, category.ToString(), min);
        /// <summary>
        /// 根据标签递归获取源卡子卡牌
        /// </summary>
        public CardRuleBuilder NeedSourceDescendantsTag(string tag, int min = 0) => NeedCard(RequirementRoot.Source, TargetKind.ByTagRecursive, tag, min);
        /// <summary>
        /// 获取源卡的一层子卡（不带筛选）
        /// </summary>
        public CardRuleBuilder NeedSourceChildren(int min = 1) => NeedCard(RequirementRoot.Source, TargetKind.ContainerChildren, null, min);
        #endregion

        #region Requirements 自定义
        /// <summary>
        /// 添加自定义卡牌匹配器
        /// </summary>
        /// <param name="root">目标根</param>
        /// <param name="kind">作用对象域</param>
        /// <param name="filter">过滤器</param>
        /// <param name="min">阈值</param>
        public CardRuleBuilder NeedCard(RequirementRoot root, TargetKind kind, string filter = null, int min = 1)
        {
            _rule.Requirements.Add(new CardsRequirement { Root = root, TargetKind = kind, Filter = filter, MinCount = min });
            return this;
        }
        /// <summary>
        /// 添加自定义匹配器
        /// </summary>
        /// <param name="requirement">匹配器</param>
        public CardRuleBuilder AddRequirement(IRuleRequirement requirement)
        {
            if (requirement != null) _rule.Requirements.Add(requirement);
            return this;
        }
        /// <summary>
        /// 添加多个自定义匹配器
        /// </summary>
        /// <param name="requirements">匹配器们</param>
        public CardRuleBuilder AddRequirements(IEnumerable<IRuleRequirement> requirements)
        {
            if (requirements != null)
                foreach (var r in requirements) if (r != null) _rule.Requirements.Add(r);
            return this;
        }
        #endregion

        #region Effects 便捷
        /// <summary>
        /// 添加自定义效果
        /// </summary>
        /// <param name="effect">效果</param>
        public CardRuleBuilder AddEffect(IRuleEffect effect) { if (effect != null) _rule.Effects.Add(effect); return this; }
        /// <summary>
        /// 添加多个自定义效果
        /// </summary>
        /// <param name="effects">效果们</param>
        public CardRuleBuilder AddEffects(IEnumerable<IRuleEffect> effects)
        {
            if (effects != null) foreach (var e in effects) if (e != null) _rule.Effects.Add(e);
            return this;
        }

        #region Effects Remove
        /// <summary>
        /// 按标签移除卡牌
        /// </summary>
        /// <param name="tag">标签</param>
        /// <param name="take">作用数量</param>
        public CardRuleBuilder DoRemoveByTag(string tag, int take = 0) { _rule.Effects.Add(new RemoveCardsEffect { TargetKind = TargetKind.ByTag, TargetValueFilter = tag, Take = take }); return this; }
        /// <summary>
        /// 按Id移除卡牌
        /// </summary>
        /// <param name="id">卡牌Id</param>
        /// <param name="take">作用数量</param>
        public CardRuleBuilder DoRemoveById(string id, int take = 0) { _rule.Effects.Add(new RemoveCardsEffect { TargetKind = TargetKind.ById, TargetValueFilter = id, Take = take }); return this; }
        #endregion

        #region Effects Modify
        /// <summary>
        /// 按标签修改卡牌自定义值
        /// </summary>
        /// <param name="tag">标签</param>
        /// <param name="value">传入值</param>
        /// <param name="mode">修改选项</param>
        /// <param name="take">作用数量</param>
        /// <param name="propertyName">要修改的属性名（空为全部属性）</param>
        public CardRuleBuilder DoModifyByTag(string tag, float value, ModifyPropertyEffect.Mode mode = ModifyPropertyEffect.Mode.AddToBase, int take = 0, string propertyName = "")
        {
            _rule.Effects.Add(new ModifyPropertyEffect { TargetKind = TargetKind.ByTag, TargetValueFilter = tag, ApplyMode = mode, Value = value, Take = take, PropertyName = propertyName ?? string.Empty });
            return this;
        }
        #endregion

        #region Effects AddTag/Create/Invoke
        /// <summary>
        /// 按标签添加标签
        /// </summary>
        /// <param name="targetTagFilter">过滤标签</param>
        /// <param name="addTag">要添加的标签</param>
        /// <param name="take">作用数量</param>
        public CardRuleBuilder DoAddTagToByTag(string targetTagFilter, string addTag, int take = 0)
        {
            _rule.Effects.Add(new AddTagEffect { TargetKind = TargetKind.ByTag, TargetValueFilter = targetTagFilter, Tag = addTag, Take = take });
            return this;
        }
        /// <summary>
        /// 创建新卡牌到容器
        /// </summary>
        /// <param name="id">新卡牌的Id</param>
        /// <param name="count">创建数量</param>
        public CardRuleBuilder DoCreate(string id, int count = 1) { _rule.Effects.Add(new CreateCardsEffect { CardIds = new List<string> { id }, CountPerId = count }); return this; }
        /// <summary>
        /// 执行事件效果
        /// </summary>
        /// <param name="action">要触发的事件</param>
        public CardRuleBuilder DoInvoke(Action<CardRuleContext, IReadOnlyList<Card>> action) { _rule.Effects.Add(new InvokeEffect(action)); return this; }

        /// <summary>
        /// 批量触发匹配卡牌自定义事件
        /// </summary>
        /// <param name="eventId">事件Id</param>
        public CardRuleBuilder BatchCustom(string eventId)
        {
            //居然能运行，好神奇
            DoInvoke((context, list) =>
            {
                foreach (Card target in list)
                {
                    Debug.Log(target.Id);
                    target.Custom(eventId);
                }
            });
            return this;
        }
        #endregion
        #endregion

        public CardRule Build() => _rule;
    }

    public static class RuleRegistrationExtensions
    {
        public static CardRule RegisterRule(this CardEngine engine, CardRuleBuilder builder)
        {
            var rule = builder?.Build();
            if (rule != null) engine.RegisterRule(rule);
            return rule;
        }

        public static CardRule RegisterRule(this CardEngine engine, Action<CardRuleBuilder> configure)
        {
            var b = new CardRuleBuilder();
            configure?.Invoke(b);
            var rule = b.Build();
            engine.RegisterRule(rule);
            return rule;
        }
    }
}