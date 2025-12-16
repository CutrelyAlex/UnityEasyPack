using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace EasyPack.EmeCardSystem
{
    /// <summary>
    ///     卡牌引擎 - 事件处理、规则匹配与效果执行
    /// </summary>
    public sealed partial class CardEngine
    {
        #region Rule与效果缓存

        /// <summary>
        ///     下一个可用的 RuleUID。
        /// </summary>
        private int _nextRuleUID;

        /// <summary>
        ///     RuleUID -> Rule 的快速查找映射。
        /// </summary>
        private readonly Dictionary<int, CardRule> _rulesByUID = new();

        // 规则表（按事件类型字符串索引）
        private readonly Dictionary<string, List<CardRule>> _rules = new();

        // 全局效果池，跨事件收集效果，确保全局优先级排序
        private readonly EffectPool _globalEffectPool = new();

        // 全局效果池的规则顺序计数器
        private int _globalOrderIndex;

        /// <summary>
        ///     根据 RuleUID 获取规则。
        /// </summary>
        /// <param name="ruleUID">规则的唯一标识符。</param>
        /// <returns>找到的规则，如果未找到返回 null。</returns>
        public CardRule GetRuleByUID(int ruleUID)
        {
            _rulesByUID.TryGetValue(ruleUID, out CardRule rule);
            return rule;
        }

        #endregion
        
        #region 对象池与缓存比较器
        [ThreadStatic] private static List<(CardRule, HashSet<Card>, CardRuleContext, int)> t_evals;
        [ThreadStatic] private static HashSet<Card> t_distinctSet;

        // 缓存排序比较器
        private static readonly Comparison<(CardRule rule, HashSet<Card> matched, CardRuleContext ctx, int orderIndex)>
            s_priorityComparison = (a, b) =>
            {
                int cmp = a.rule.Priority.CompareTo(b.rule.Priority);
                return cmp != 0 ? cmp : a.orderIndex.CompareTo(b.orderIndex);
            };

        private static readonly Comparison<(CardRule rule, HashSet<Card> matched, CardRuleContext ctx, int orderIndex)>
            s_orderComparison = (a, b) => a.orderIndex.CompareTo(b.orderIndex);

        /// <summary>
        ///     从线程局部池获取或创建 evals 列表
        /// </summary>
        private static List<(CardRule, HashSet<Card>, CardRuleContext, int)> RentEvalsList(int capacity = 8)
        {
            var list = t_evals;
            if (list == null)
            {
                t_evals = new List<(CardRule, HashSet<Card>, CardRuleContext, int)>(capacity);
                return t_evals;
            }
            list.Clear();
            if (list.Capacity < capacity)
                list.Capacity = capacity;
            return list;
        }

        #endregion

        #region 规则注册

        /// <summary>
        ///     注册一条规则到引擎。
        ///     <para>自动分配唯一的 RuleUID 并建立索引。</para>
        /// </summary>
        /// <param name="rule">规则实例。</param>
        public void RegisterRule(CardRule rule)
        {
            if (rule == null) throw new ArgumentNullException(nameof(rule));
            if (string.IsNullOrEmpty(rule.EventType))
                throw new ArgumentException("Rule must have an EventType", nameof(rule));

            // 分配 RuleUID（如果尚未分配）
            if (rule.RuleUID < 0)
            {
                rule.RuleUID = _nextRuleUID++;
            }

            // 添加到 RuleUID 索引
            _rulesByUID[rule.RuleUID] = rule;

            // 确保事件类型的规则列表存在
            if (!_rules.TryGetValue(rule.EventType, out var ruleList))
            {
                ruleList = new();
                _rules[rule.EventType] = ruleList;
            }

            ruleList.Add(rule);
        }

        /// <summary>
        ///     使用 CardRuleBuilder 注册规则。
        /// </summary>
        /// <param name="configure">规则配置委托。</param>
        /// <returns>已注册的规则实例。</returns>
        public CardRule RegisterRule(Action<CardRuleBuilder> configure)
        {
            if (configure == null) throw new ArgumentNullException(nameof(configure));

            var builder = new CardRuleBuilder();
            configure(builder);
            CardRule rule = builder.Build();
            RegisterRule(rule);
            return rule;
        }

        /// <summary>
        ///     批量注册规则集。
        /// </summary>
        /// <param name="configures">规则配置委托集合。</param>
        public void RegisterRules(IReadOnlyList<Action<CardRuleBuilder>> configures)
        {
            if (configures == null) throw new ArgumentNullException(nameof(configures));

            foreach (Action<CardRuleBuilder> configure in configures)
            {
                if (configure != null)
                {
                    RegisterRule(configure);
                }
            }
        }

        /// <summary>
        ///     从引擎中注销一条规则。
        /// </summary>
        /// <param name="rule">要注销的规则实例。</param>
        /// <returns>如果成功注销返回true，否则返回false。</returns>
        public bool UnregisterRule(CardRule rule)
        {
            if (rule == null || string.IsNullOrEmpty(rule.EventType)) return false;

            bool removed = false;
            if (_rules.TryGetValue(rule.EventType, out var ruleList)) removed = ruleList.Remove(rule);

            // 从 RuleUID 索引移除
            if (removed && rule.RuleUID >= 0)
            {
                _rulesByUID.Remove(rule.RuleUID);
            }

            return removed;
        }

        #endregion
       
        #region 规则评估

        /// <summary>
        ///     串行评估规则（默认模式）。
        /// </summary>
        private List<(CardRule rule, HashSet<Card> matched, CardRuleContext ctx, int orderIndex)>
            EvaluateRulesSerial(List<CardRule> rules, Card source, ICardEvent evt)
        {
            // 使用线程局部缓存复用 evals 列表
            var evals = RentEvalsList(rules.Count);

            for (int i = 0; i < rules.Count; i++)
            {
                CardRule rule = rules[i];
                CardRuleContext context = BuildContext(rule, source, evt);
                if (context == null) continue;

                if (!EvaluateRequirements(context, rule.Requirements, out var matched))
                    continue;

                evals.Add((rule, matched, context, i));
            }

            return evals;
        }

        /// <summary>
        ///     并行评估规则
        ///     <para>
        ///         注意：Requirements 实现必须是线程安全的才能使用此模式。
        ///         建议仅在 Requirements 不修改共享状态时启用。
        ///     </para>
        /// </summary>
        private List<(CardRule rule, HashSet<Card> matched, CardRuleContext ctx, int orderIndex)>
            EvaluateRulesParallel(List<CardRule> rules, Card source, ICardEvent evt)
        {
            var results = new ConcurrentBag<(CardRule rule, HashSet<Card> matched, CardRuleContext ctx, int orderIndex)>();

            var options = new ParallelOptions();
            if (Policy.MaxDegreeOfParallelism > 0)
                options.MaxDegreeOfParallelism = Policy.MaxDegreeOfParallelism;

            Parallel.For(0, rules.Count, options, i =>
            {
                CardRule rule = rules[i];

                CardRuleContext ctx = BuildContext(rule, source, evt);
                if (ctx == null) return;

                if (EvaluateRequirements(ctx, rule.Requirements, out var matched))
                {
                    results.Add((rule, matched, ctx, i));
                }
            });

            return results.ToList();
        }

        /// <summary>
        ///     评估规则的所有条件要求。
        /// </summary>
        private bool EvaluateRequirements(CardRuleContext ctx, List<IRuleRequirement> requirements,
                                          out HashSet<Card> matchedAll)
        {
            matchedAll = new HashSet<Card>();

            if (requirements == null || requirements.Count == 0)
                return true;

            foreach (IRuleRequirement req in requirements)
            {
                if (req == null)
                {
                    matchedAll = null;
                    return false;
                }

                if (!req.TryMatch(ctx, out var picks))
                {
                    matchedAll = null;
                    return false;
                }

                if (picks is { Count: > 0 })
                {
                    foreach (Card card in picks)
                        matchedAll.Add(card);
                }
            }

            return true;
        }

        /// <summary>
        ///     构建规则上下文。
        /// </summary>
        private CardRuleContext BuildContext(CardRule rule, Card source, ICardEvent evt)
        {
            // 解析匹配根和效果根
            Card matchRoot = CardRule.ResolveRootCard(source, rule.MatchRootHops);
            Card effectRoot = CardRule.ResolveRootCard(source, rule.EffectRootHops);

            if (matchRoot == null) return null;

            // 直接创建新的 CardRuleContext
            return new CardRuleContext(
                source,
                matchRoot,
                effectRoot,
                evt,
                this,
                rule.MaxDepth,
                rule
            );
        }

        #endregion
    }
}
