using System;
using System.Collections.Generic;

namespace EasyPack
{
    /// <summary>
    /// 规则流式构建器：减少样板代码，提升可读性。
    /// </summary>
    public sealed class CardRuleBuilder
    {
        private readonly CardRule _rule = new CardRule
        {
            Requirements = new List<IRuleRequirement>(),
            Effects = new List<IRuleEffect>(),
            Policy = new RulePolicy { DistinctMatched = true }
        };
        #region 基本配置

        public CardRuleBuilder Trigger(CardEventType type, string customId = null)
        {
            _rule.Trigger = type;
            _rule.CustomId = customId;
            return this;
        }

        public CardRuleBuilder OwnerHops(int hops)
        {
            _rule.OwnerHops = hops;
            return this;
        }

        public CardRuleBuilder MaxDepth(int depth)
        {
            _rule.MaxDepth = depth;
            return this;
        }
        #endregion

        #region 策略配置
        public CardRuleBuilder DistinctMatched(bool enabled = true)
        {
            _rule.Policy.DistinctMatched = enabled;
            return this;
        }
        /// <summary>
        /// 配置规则策略（RulePolicy）语法糖
        /// 例如：.Policy(p => { p.DistinctMatched = true; p.SpecificityOverride = 100; })
        /// </summary>
        public CardRuleBuilder Policy(Action<RulePolicy> configure)
        {
            configure?.Invoke(_rule.Policy);
            return this;
        }

        public CardRuleBuilder SpecificityOverride(int score)
        {
            _rule.Policy.SpecificityOverride = score;
            return this;
        }
        #endregion

        #region 预设Requirements
        public CardRuleBuilder WhenSourceTag(string tag)
        {
            return Where(ctx => ctx.Source != null && ctx.Source.HasTag(tag));
        }

        public CardRuleBuilder Where(Func<CardRuleContext, bool> predicate)
        {
            _rule.Requirements.Add(new ConditionRequirement(predicate));
            return this;
        }

        public CardRuleBuilder NeedContainerTag(string tag, int min = 1)
        {
            return NeedCard(RequirementRoot.Container, TargetKind.ByTag, tag, min);
        }

        public CardRuleBuilder NeedContainerId(string id, int min = 1)
        {
            return NeedCard(RequirementRoot.Container, TargetKind.ById, id, min);
        }

        public CardRuleBuilder NeedContainerCategory(CardCategory category, int min = 1)
        {
            return NeedCard(RequirementRoot.Container, TargetKind.ByCategory, category.ToString(), min);
        }

        /// <summary>
        /// 以 Root 为锚点，使用 TargetKind + Filter 选择目标，至少命中 MinCount 个。
        /// </summary>
        /// <param name="root"></param>
        /// <param name="kind"></param>
        /// <param name="filter"></param>
        /// <param name="min"></param>
        /// <returns></returns>
        public CardRuleBuilder NeedCard(RequirementRoot root, TargetKind kind, string filter = null, int min = 1)
        {
            _rule.Requirements.Add(new CardRequirement
            {
                Root = root,
                TargetKind = kind,
                Filter = filter,
                MinCount = min
            });
            return this;
        }

        #endregion

        #region Requirements添加
        /// <summary>
        /// 注入自定义的 Requirement（IRuleRequirement）。
        /// </summary>
        public CardRuleBuilder AddRequirement(IRuleRequirement requirement)
        {
            if (requirement != null) _rule.Requirements.Add(requirement);
            return this;
        }

        /// <summary>
        /// 批量注入自定义 Requirements。
        /// </summary>
        public CardRuleBuilder AddRequirements(IEnumerable<IRuleRequirement> requirements)
        {
            if (requirements != null)
            {
                foreach (var r in requirements)
                    if (r != null) _rule.Requirements.Add(r);
            }
            return this;
        }
        #endregion

        #region 预设Effects
        public CardRuleBuilder AddEffect(IRuleEffect effect)
        {
            if (effect != null) _rule.Effects.Add(effect);
            return this;
        }

        public CardRuleBuilder AddEffects(IEnumerable<IRuleEffect> effects)
        {
            if (effects != null)
            {
                foreach (var e in effects)
                    if (e != null) _rule.Effects.Add(e);
            }
            return this;
        }

        public CardRuleBuilder DoRemoveByTag(string tag, int take = 0)
        {
            _rule.Effects.Add(new RemoveCardsEffect { TargetKind = TargetKind.ByTag, TargetValueFilter = tag, Take = take });
            return this;
        }

        public CardRuleBuilder DoRemoveById(string id, int take = 0)
        {
            _rule.Effects.Add(new RemoveCardsEffect { TargetKind = TargetKind.ById, TargetValueFilter = id, Take = take });
            return this;
        }

        public CardRuleBuilder DoModifyByTag(string tag, float value, ModifyPropertyEffect.Mode mode = ModifyPropertyEffect.Mode.AddToBase, int take = 0)
        {
            _rule.Effects.Add(new ModifyPropertyEffect
            {
                TargetKind = TargetKind.ByTag,
                TargetValueFilter = tag,
                ApplyMode = mode,
                Value = value,
                Take = take
            });
            return this;
        }

        public CardRuleBuilder DoAddTagToByTag(string targetTagFilter, string addTag, int take = 0)
        {
            _rule.Effects.Add(new AddTagEffect
            {
                TargetKind = TargetKind.ByTag,
                TargetValueFilter = targetTagFilter,
                Tag = addTag,
                Take = take
            });
            return this;
        }

        public CardRuleBuilder DoCreate(string id, int count = 1)
        {
            _rule.Effects.Add(new CreateCardsEffect { CardIds = new List<string> { id }, CountPerId = count });
            return this;
        }

        public CardRuleBuilder DoInvoke(Action<CardRuleContext, IReadOnlyList<Card>> action)
        {
            _rule.Effects.Add(new InvokeEffect(action));
            return this;
        }

        #endregion
        public CardRule Build() => _rule;
    }

    /// <summary>
    /// 以 DSL/Builder 方式注册规则。
    /// </summary>
    public static class RuleRegistrationExtensions
    {
        public static CardRule RegisterRule(this CardRuleEngine engine, CardRuleBuilder builder)
        {
            var rule = builder?.Build();
            if (rule != null) engine.RegisterRule(rule);
            return rule;
        }

        public static CardRule RegisterRule(this CardRuleEngine engine, Action<CardRuleBuilder> configure)
        {
            var b = new CardRuleBuilder();
            configure?.Invoke(b);
            var rule = b.Build();
            engine.RegisterRule(rule);
            return rule;
        }
    }
}