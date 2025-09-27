using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace EasyPack
{
    public sealed class CardEngine
    {
        #region 基本属性
        private readonly Dictionary<CardEventType, List<CardRule>> _rules = new Dictionary<CardEventType, List<CardRule>>();
        public ICardFactory CardFactory { get; set; }

        /// <summary>引擎策略</summary>
        public EnginePolicy Policy { get; } = new EnginePolicy();

        private bool _isPumping = false;

        // 事件封装
        private struct EventEntry
        {
            public Card Source;
            public CardEvent Event;
            public EventEntry(Card s, CardEvent e) { Source = s; Event = e; }
        }
        private readonly Queue<EventEntry> _queue = new Queue<EventEntry>();

        public CardEngine(ICardFactory factory = null)
        {
            CardFactory = factory;
            foreach (CardEventType t in Enum.GetValues(typeof(CardEventType)))
                _rules[t] = new List<CardRule>();
        }

        #endregion

        #region 规则处理

        public void RegisterRule(CardRule rule)
        {
            if (rule == null) throw new ArgumentNullException(nameof(rule));
            _rules[rule.Trigger].Add(rule);
        }

        public CardEngine Attach(Card card) { card.OnEvent += OnCardEvent; return this; }
        public CardEngine Detach(Card card) { card.OnEvent -= OnCardEvent; return this; }

        private void OnCardEvent(Card source, CardEvent evt)
        {
            _queue.Enqueue(new EventEntry(source, evt));
            if (!_isPumping) Pump();
        }

        public void Pump(int maxEvents = int.MaxValue)
        {
            if (_isPumping) return;
            _isPumping = true;
            int processed = 0;
            try
            {
                while (_queue.Count > 0 && processed < maxEvents)
                {
                    var entry = _queue.Dequeue();
                    Process(entry.Source, entry.Event);
                    processed++;
                }
            }
            finally
            {
                _isPumping = false;
            }
        }

        private void Process(Card source, CardEvent evt)
        {
            var rules = _rules[evt.Type];
            if (rules == null || rules.Count == 0) return;

            // 评估阶段：记录所有命中与上下文快照（按注册顺序）
            var evals = new List<(CardRule rule, List<Card> matched, CardRuleContext ctx, int orderIndex)>();
            for (int i = 0; i < rules.Count; i++)
            {
                var rule = rules[i];

                if (evt.Type == CardEventType.Custom &&
                    !string.IsNullOrEmpty(rule.CustomId) &&
                    !string.Equals(rule.CustomId, evt.ID, StringComparison.Ordinal))
                {
                    continue;
                }

                var ctx = BuildContext(rule, source, evt);
                if (ctx == null) continue;

                if (EvaluateRequirements(ctx, rule.Requirements, out var matched))
                {
                    if ((rule.Policy?.DistinctMatched ?? true) && matched != null && matched.Count > 1)
                        matched = matched.Distinct().ToList();

                    evals.Add((rule, matched, ctx, i));
                }
            }

            if (evals.Count == 0) return;

            // 排序：按注册顺序或优先级
            IEnumerable<(CardRule rule, List<Card> matched, CardRuleContext ctx, int orderIndex)> ordered =
                Policy.RuleSelection == RuleSelectionMode.Priority
                    ? evals.OrderBy(e => e.rule.Priority).ThenBy(e => e.orderIndex)
                    : evals.OrderBy(e => e.orderIndex);

            if (Policy.FirstMatchOnly)
            {
                var first = ordered.First();
                ExecuteOne(first);
            }
            else
            {
                foreach (var e in ordered)
                {
                    if (ExecuteOne(e)) break; // 支持规则级 StopEventOnSuccess
                }
            }
        }

        private bool ExecuteOne((CardRule rule, List<Card> matched, CardRuleContext ctx, int orderIndex) e)
        {
            if (e.rule.Effects == null || e.rule.Effects.Count == 0) return false;
            foreach (var eff in e.rule.Effects)
                eff.Execute(e.ctx, e.matched);
            return e.rule.Policy?.StopEventOnSuccess == true;
        }

        private CardRuleContext BuildContext(CardRule rule, Card source, CardEvent evt)
        {
            var container = SelectContainer(rule.OwnerHops, source);
            if (container == null) return null;
            return new CardRuleContext
            {
                Source = source,
                Container = container,
                Event = evt,
                Factory = CardFactory,
                MaxDepth = rule.MaxDepth
            };
        }
        #endregion

        #region 容器方法
        // 基于 OwnerHops 选择容器：0=Self，1=Owner，N>1 上溯，-1=Root
        private static Card SelectContainer(int ownerHops, Card source)
        {
            if (source == null) return null;

            if (ownerHops == 0) return source;

            if (ownerHops < 0)
            {
                var curr = source;
                while (curr.Owner != null) curr = curr.Owner;
                return curr;
            }

            var node = source;
            int hops = ownerHops;
            while (hops > 0 && node.Owner != null)
            {
                node = node.Owner;
                hops--;
            }
            return node ?? source;
        }

        /// <summary>
        /// 评估一组规则要求（requirements）在给定上下文下是否全部成立，并聚合每个要求项命中的卡牌集合。
        /// </summary>
        /// <param name="ctx">规则执行上下文</param>
        /// <param name="requirements">要评估的要求项列表。如果为 null 或空，则视为匹配成功。</param>
        /// <param name="matchedAll">
        /// 输出参数：聚合所有要求项命中的卡牌集合。方法返回时该集合已被初始化（非 null）。
        /// 每个要求项如返回非空/非空集合，其元素将被追加到此聚合列表中。
        /// </param>
        /// <returns>
        /// 若所有要求项都匹配成功返回 true；如任一要求为 null 或 TryMatch 返回 false 则立即返回 false（短路）。
        /// </returns>
        private static bool EvaluateRequirements(CardRuleContext ctx, List<IRuleRequirement> requirements, out List<Card> matchedAll)
        {
            matchedAll = new List<Card>();
            if (requirements == null || requirements.Count == 0) return true;

            foreach (var req in requirements)
            {
                if (req == null) return false;
                if (!req.TryMatch(ctx, out var picks)) return false;
                if (picks != null && picks.Count > 0) matchedAll.AddRange(picks);
            }
            return true;
        }
        #endregion
    }
}