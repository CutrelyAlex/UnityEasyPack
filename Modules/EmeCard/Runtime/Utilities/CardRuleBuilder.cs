using System;
using System.Collections.Generic;
using UnityEngine;

namespace EasyPack.EmeCardSystem
{
    /// <summary>
    ///     规则流式构建器
    ///     提供核心方法和便捷语法糖
    /// </summary>
    public sealed partial class CardRuleBuilder
    {
        private readonly CardRule _rule = new()
        {
            Requirements = new(), Effects = new(), Policy = new() { DistinctMatched = true },
        };

        #region 基础配置

        /// <summary>设置事件触发类型（使用字符串）</summary>
        public CardRuleBuilder On(string eventType)
        {
            _rule.EventType = eventType;
            return this;
        }

        /// <summary>设置事件触发类型（使用事件定义）</summary>
        public CardRuleBuilder On<T>(CardEventDefinition<T> eventDef)
        {
            _rule.EventType = eventDef?.EventType;
            return this;
        }

        /// <summary>监听 Tick 事件</summary>
        public CardRuleBuilder OnTick() => On(CardEventTypes.TICK);

        /// <summary>监听 Use 事件</summary>
        public CardRuleBuilder OnUse() => On(CardEventTypes.USE);

        /// <summary>监听 AddedToOwner 事件</summary>
        public CardRuleBuilder OnAddedToOwner() => On(CardEventTypes.ADDED_TO_OWNER);

        /// <summary>监听 RemovedFromOwner 事件</summary>
        public CardRuleBuilder OnRemovedFromOwner() => On(CardEventTypes.REMOVED_FROM_OWNER);

        /// <summary>
        ///     监听 PumpStart 事件。
        ///     <para>在 Pump 循环开始前触发，适用于需要在所有事件处理前执行的初始化逻辑。</para>
        ///     <para>注意：此事件仅在 Pump 边界处理，不会在普通事件处理中触发。</para>
        /// </summary>
        public CardRuleBuilder OnPumpStart() => On(CardEventTypes.PUMP_START);

        /// <summary>
        ///     监听 PumpEnd 事件。
        ///     <para>在 Pump 循环结束后触发，适用于延迟删除、资源清理等需要在所有事件处理完成后执行的逻辑。</para>
        ///     <para>注意：此事件仅在 Pump 边界处理，不会在普通事件处理中触发。</para>
        /// </summary>
        public CardRuleBuilder OnPumpEnd() => On(CardEventTypes.PUMP_END);

        #region 根节点跳数配置

        /// <summary>
        ///     设置匹配范围根节点跳数。
        ///     <para>0=Self, 1=Owner, -1=Root, N&gt;1=向上N层</para>
        /// </summary>
        /// <param name="hops">跳数值</param>
        /// <returns>构建器自身，用于链式调用</returns>
        public CardRuleBuilder MatchRootHops(int hops)
        {
            _rule.MatchRootHops = hops;
            return this;
        }

        /// <summary>
        ///     设置效果作用根节点跳数。
        ///     <para>0=Self, 1=Owner, -1=Root, N&gt;1=向上N层</para>
        /// </summary>
        /// <param name="hops">跳数值</param>
        /// <returns>构建器自身，用于链式调用</returns>
        public CardRuleBuilder EffectRootHops(int hops)
        {
            _rule.EffectRootHops = hops;
            return this;
        }

        /// <summary>
        ///     [已弃用] 设置容器锚点（同时设置 MatchRootHops 和 EffectRootHops）。
        ///     <para>请使用 <see cref="MatchRootHops"/> 和 <see cref="EffectRootHops"/> 分别设置。</para>
        /// </summary>
        /// <param name="hops">跳数值（0=Self, 1=Owner, -1=Root, N&gt;1=向上N层）</param>
        /// <returns>构建器自身，用于链式调用</returns>
        [Obsolete("使用 MatchRootHops() 和 EffectRootHops() 分别设置匹配和效果范围。")]
        public CardRuleBuilder OwnerHops(int hops)
        {
            _rule.OwnerHops = hops;
            return this;
        }

        /// <summary>匹配范围以自身为根（MatchRootHops=0）</summary>
        public CardRuleBuilder MatchRootAtSelf() => MatchRootHops(0);

        /// <summary>匹配范围以直接父级为根（MatchRootHops=1）</summary>
        public CardRuleBuilder MatchRootAtParent() => MatchRootHops(1);

        /// <summary>匹配范围以根容器为根（MatchRootHops=-1）</summary>
        public CardRuleBuilder MatchAtRoot() => MatchRootHops(-1);

        /// <summary>效果范围以自身为根（EffectRootHops=0）</summary>
        public CardRuleBuilder EffectAtSelf() => EffectRootHops(0);

        /// <summary>效果范围以直接父级为根（EffectRootHops=1）</summary>
        public CardRuleBuilder EffectAtParent() => EffectRootHops(1);

        /// <summary>效果范围以根容器为根（EffectRootHops=-1）</summary>
        public CardRuleBuilder EffectAtRoot() => EffectRootHops(-1);

        /// <summary>
        ///     [已弃用] 以自身为容器（OwnerHops=0）。
        ///     <para>请使用 <see cref="MatchRootAtSelf"/> 或 <see cref="EffectAtSelf"/>。</para>
        /// </summary>
        [Obsolete("使用 MatchAtSelf() 或 EffectAtSelf()")]
        public CardRuleBuilder AtSelf() => OwnerHops(0);

        /// <summary>
        ///     [已弃用] 以直接父级为容器（OwnerHops=1）。
        ///     <para>请使用 <see cref="MatchRootAtParent"/> 或 <see cref="EffectAtParent"/>。</para>
        /// </summary>
        [Obsolete("使用 MatchAtParent() 或 EffectAtParent()")]
        public CardRuleBuilder AtParent() => OwnerHops(1);

        /// <summary>
        ///     [已弃用] 以根容器为容器（OwnerHops=-1）。
        ///     <para>请使用 <see cref="MatchAtRoot"/> 或 <see cref="EffectAtRoot"/>。</para>
        /// </summary>
        [Obsolete("使用 MatchAtRoot() 或 EffectAtRoot()")]
        public CardRuleBuilder AtRoot() => OwnerHops(-1);

        #endregion

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

        /// <summary>设置规则优先级</summary>
        public CardRuleBuilder WithPriority(int priority) => Priority(priority);

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
        /// <param name="predicate">条件判断委托</param>
        /// <returns>构建器自身，用于链式调用</returns>
        public CardRuleBuilder When(Func<CardRuleContext, bool> predicate)
        {
            if (predicate != null)
                _rule.Requirements.Add(new ConditionRequirement(predicate));
            return this;
        }

        /// <summary>
        ///     添加条件判断和卡牌输出
        ///     <para>返回值格式：(matched: 是否匹配, cards: 匹配的卡牌列表)</para>
        ///     <para>优势：用户委托仅被调用一次，避免重复计算</para>
        /// </summary>
        /// <param name="predicate">返回匹配结果和卡牌列表的委托</param>
        /// <returns>构建器自身，用于链式调用</returns>
        public CardRuleBuilder WhenWithCards(
            Func<CardRuleContext, (bool matched, List<Card> cards)> predicate)
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
            _rule.Requirements.Add(new CardsRequirement(root: root, scope: scope, filterMode: filter,
                filterValue: filterValue, minCount: minCount, maxMatched: maxMatched, maxDepth: maxDepth));
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
                foreach (IRuleEffect effect in effects)
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
                foreach (IRuleEffect effect in effects)
                {
                    if (effect != null)
                        _rule.Effects.Add(effect);
                }
            }

            return this;
        }

        /// <summary>移除卡牌效果</summary>
        public CardRuleBuilder DoRemove(
            SelectionRoot root = SelectionRoot.MatchRoot,
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
                MaxDepth = maxDepth,
            });
            return this;
        }

        /// <summary>修改属性效果</summary>
        public CardRuleBuilder DoModify(
            string propertyName,
            float value,
            ModifyPropertyEffect.Mode mode = ModifyPropertyEffect.Mode.AddToBase,
            SelectionRoot root = SelectionRoot.MatchRoot,
            TargetScope scope = TargetScope.Matched,
            CardFilterMode filter = CardFilterMode.None,
            string filterValue = null,
            Func<CardRuleContext,float> valueFunc=null,
            int? take = null,
            int? maxDepth = null)
        {
            _rule.Effects.Add(new ModifyPropertyEffect
            {
                PropertyName = propertyName,
                ValueFunc = valueFunc,
                Value = value,
                ApplyMode = mode,
                Root = root,
                Scope = scope,
                Filter = filter,
                FilterValue = filterValue,
                Take = take,
                MaxDepth = maxDepth,
            });
            return this;
        }

        /// <summary>添加标签效果</summary>
        public CardRuleBuilder DoAddTag(
            string tag,
            SelectionRoot root = SelectionRoot.MatchRoot,
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
                MaxDepth = maxDepth,
            });
            return this;
        }

        /// <summary>创建卡牌效果</summary>
        public CardRuleBuilder DoCreate(string cardId, int count = 1)
        {
            _rule.Effects.Add(new CreateCardsEffect { CardIds = new() { cardId }, CountPerId = count });
            return this;
        }

        /// <summary>执行自定义逻辑</summary>
        public CardRuleBuilder DoInvoke(Action<CardRuleContext, HashSet<Card>> action)
        {
            if (action != null)
                _rule.Effects.Add(new InvokeEffect(action));
            return this;
        }

        #endregion

        #region Debug

#if UNITY_EDITOR

        public CardRuleBuilder LogContext()
        {
            return DoInvoke((context, list) =>
            {
                Debug.Log(
                    "[Rule Debug] 规则触发上下文：\n" +
                    $"- 规则名称：{context.EventId}\n" +
                    $"- 事件类型：{context.Event.EventType})\n" +
                    $"- 事件数据：{context.Event.DataObject}\n" +
                    $"- 源卡牌：{context.Source?.Name} (ID: {context.Source?.Id})\n" +
                    $"- 容器卡牌：{context.MatchRoot?.Name} (ID: {context.MatchRoot?.Id})\n" +
                    $"- 匹配卡牌数量：{list?.Count}");
            });
        }

        /// <summary>
        ///     调试时输出日志
        /// </summary>
        public CardRuleBuilder WhenLog(string message)
        {
            return When(context =>
            {
                Debug.Log(message);
                return true;
            });
        }

        /// <summary>
        ///     仅在编辑器中执行调试逻辑
        /// </summary>
        public CardRuleBuilder WhenDebugInvoke(Func<CardRuleContext, bool> debugAction = null)
        {
            return When(context =>
            {
                debugAction?.Invoke(context);
                return true;
            });
        }

        /// <summary>
        ///     仅在编辑器中执行调试效果
        /// </summary>
        public CardRuleBuilder DoDebugInvoke(Action<CardRuleContext, HashSet<Card>> debugAction = null)
        {
            if (debugAction != null) _rule.Effects.Add(new InvokeEffect(debugAction));

            return this;
        }
#endif

        #endregion

        /// <summary>构建规则</summary>
        public CardRule Build() => _rule;
    }
}