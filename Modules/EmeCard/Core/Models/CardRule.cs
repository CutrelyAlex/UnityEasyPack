using System;
using System.Collections.Generic;

namespace EasyPack.EmeCardSystem
{
    /// <summary>
    ///     数据驱动的卡牌规则。
    /// </summary>
    public class CardRule
    {
        /// <summary>
        ///     规则的唯一标识符（由 CardEngine 在注册时自动分配）。
        ///     <para>未注册的规则 RuleUID 为 -1。</para>
        /// </summary>
        public int RuleUID { get; internal set; } = -1;

        /// <summary>
        ///     事件触发类型（字符串标识符，如 "Tick"、"Use"、"Collision"）。
        ///     <para>
        ///         使用字符串，支持任意自定义事件类型。
        ///         标准事件类型定义在 <see cref="CardEventTypes" /> 中。
        ///     </para>
        /// </summary>
        public string EventType = CardEventTypes.ADDED_TO_OWNER;

        #region Root Hops 配置

        /// <summary>
        ///     匹配根跳数：确定 Requirements 匹配的起始卡牌。
        ///     <para>
        ///         0 = Self（持有此规则的卡牌）<br />
        ///         1 = Owner（持有者，默认）<br />
        ///         N &gt; 1 = 向上第 N 级 Owner<br />
        ///         -1 = Root（最顶层卡牌）
        ///     </para>
        /// </summary>
        public int MatchRootHops = 1;

        /// <summary>
        ///     效果根跳数：确定 Effects 执行的起始卡牌。
        ///     <para>
        ///         0 = Self（持有此规则的卡牌）<br />
        ///         1 = Owner（持有者，默认）<br />
        ///         N &gt; 1 = 向上第 N 级 Owner<br />
        ///         -1 = Root（最顶层卡牌）
        ///     </para>
        /// </summary>
        public int EffectRootHops = 1;

        #endregion

        /// <summary>递归选择的最大深度（仅对递归类 TargetKind 生效）。</summary>
        public int MaxDepth = int.MaxValue;

        /// <summary>
        ///     规则优先级（数值越小优先级越高）。当引擎Policy选择模式为 Priority 时生效。
        /// </summary>
        public int Priority = 0;

        /// <summary>匹配条件集合（与关系）。</summary>
        public List<IRuleRequirement> Requirements = new();

        /// <summary>命中后执行的效果管线。</summary>
        public List<IRuleEffect> Effects = new();

        /// <summary>规则执行策略。</summary>
        public RulePolicy Policy { get; set; } = new();

        /// <summary>
        ///     创建空规则。
        /// </summary>
        public CardRule() { }

        /// <summary>
        ///     创建指定事件类型的规则。
        /// </summary>
        /// <param name="eventType">事件类型标识符。</param>
        public CardRule(string eventType) => EventType = eventType;

        #region 辅助方法

        /// <summary>
        ///     根据跳数从指定卡牌解析目标根卡牌。
        /// </summary>
        /// <param name="sourceCard">起始卡牌（通常是持有此规则的卡牌）。</param>
        /// <param name="hops">跳数（0=Self, 1=Owner, N>1=向上N级, -1=Root）。</param>
        /// <returns>解析后的根卡牌；如果无法解析则返回 sourceCard 本身。</returns>
        public static Card ResolveRootCard(Card sourceCard, int hops)
        {
            if (sourceCard == null) return null;

            switch (hops)
            {
                // Self
                case 0:
                    return sourceCard;
                // Root (-1)
                case < 0:
                    {
                        return sourceCard.RootCard ?? sourceCard;
                    }
                case >= 1:
                    {
                        Card target = sourceCard;
                        // N 级 Owner
                        for (int i = 0; i < hops && target.Owner != null; i++)
                        {
                            target = target.Owner;
                        }

                        return target;
                    }
            }
        }

        /// <summary>
        ///     解析此规则的匹配根卡牌。
        /// </summary>
        /// <param name="ruleHolder">持有此规则的卡牌。</param>
        /// <returns>匹配操作的根卡牌。</returns>
        public Card ResolveMatchRoot(Card ruleHolder) => ResolveRootCard(ruleHolder, MatchRootHops);

        /// <summary>
        ///     解析此规则的效果根卡牌。
        /// </summary>
        /// <param name="ruleHolder">持有此规则的卡牌。</param>
        /// <returns>效果执行的根卡牌。</returns>
        public Card ResolveEffectRoot(Card ruleHolder) => ResolveRootCard(ruleHolder, EffectRootHops);

        #endregion
    }
}