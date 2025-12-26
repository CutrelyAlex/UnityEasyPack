using System;
using System.Collections.Generic;
using UnityEngine;

namespace EasyPack.EmeCardSystem
{
    /// <summary>
    ///     卡牌引擎 - 事件处理与 Pump 驱动
    /// </summary>
    public sealed partial class CardEngine
    {
        #region 事件队列与状态

        // 事件队列
        private readonly Queue<(Card source, ICardEvent evt)> _eventQueue = new();
        private readonly Queue<(Card source, ICardEvent evt)> _startEventQueue = new();
        private readonly Queue<(Card source, ICardEvent evt)> _endEventQueue = new();

        #endregion

        #region 引擎策略与状态

        /// <summary>
        ///     引擎全局策略。
        /// </summary>
        public EnginePolicy Policy { get; } = new();

        // Pump 状态标志
        private bool _isPumping;
        private bool _isFlushingEffectPool;

        /// <summary>
        ///     用来设置指定事件的泵入层级，不过目前End只能在Normal层级已有事件时才能延迟执行，如果泵入时Normal或Start内无事件则不会延迟
        /// </summary>
        private EEventPumpType _currentPumpState;

        // 分帧处理相关
        private float _frameStartTime;
        private int _frameProcessedCount;

        // 时间限制批次处理
        private float _batchStartTime;
        private float _batchTimeLimit;

        /// <summary>
        ///     是否正在进行批次处理。
        /// </summary>
        public bool IsBatchProcessing { get; private set; }

        #endregion

        #region 事件入队

        /// <summary>
        ///     卡牌事件回调，入队并驱动事件处理。
        /// </summary>
        private void OnCardEvent(Card source, ICardEvent evt)
        {
            switch (evt.PumpType)
            {
                case EEventPumpType.Start:
                    if (_currentPumpState > EEventPumpType.Start) return;
                    _startEventQueue.Enqueue((source, evt));
                    break;
                case EEventPumpType.Normal:
                    if (_currentPumpState > EEventPumpType.Normal) return;
                    _eventQueue.Enqueue((source, evt));
                    break;
                case EEventPumpType.End:
                    _endEventQueue.Enqueue((source, evt));
                    break;
            }

            // 非分帧模式立即处理
            if (!_isPumping && !Policy.EnableFrameDistribution) Pump();
        }

        #endregion

        #region Pump 事件驱动

        /// <summary>
        ///     同步一次性处理所有事件（无分帧）。
        /// </summary>
        public void Pump()
        {
            if (_isPumping) return;
            _isPumping = true;

            try
            {
                _currentPumpState = EEventPumpType.Start;
                ProcessPumpLifecycleEvent(CardEventTypes.PUMP_START);

                const int MaxIterations = 1000;
                int iteration = 0;

                while (iteration < MaxIterations)
                {
                    // 处理队列中的所有事件
                    while (_startEventQueue.Count > 0)
                    {
                        _currentPumpState = EEventPumpType.Start;
                        (Card source, ICardEvent evt) = _startEventQueue.Dequeue();
                        Process(source, evt);
                    }

                    while (_eventQueue.Count > 0)
                    {
                        _currentPumpState = EEventPumpType.Normal;
                        (Card source, ICardEvent evt) = _eventQueue.Dequeue();
                        Process(source, evt);
                    }

                    while (_endEventQueue.Count > 0)
                    {
                        _currentPumpState = EEventPumpType.End;
                        (Card source, ICardEvent evt) = _endEventQueue.Dequeue();
                        Process(source, evt);
                    }

                    // 刷新效果池
                    if (Policy.EnableEffectPool && Policy.EffectPoolFlushMode == EffectPoolFlushMode.AfterPump)
                    {
                        FlushEffectPool();
                    }

                    if (_eventQueue.Count == 0) break;
                    iteration++;
                }

                if (iteration >= MaxIterations) Debug.LogWarning($"[CardEngine] Pump 达到最大迭代次数 {MaxIterations}");

                _currentPumpState = EEventPumpType.End;
                ProcessPumpLifecycleEvent(CardEventTypes.PUMP_END);
            }
            finally
            {
                _isPumping = false;
                _currentPumpState = EEventPumpType.Start;
            }
        }

        /// <summary>
        ///     处理 Pump 生命周期事件（PUMP_START/PUMP_END）。
        /// </summary>
        private void ProcessPumpLifecycleEvent(string eventType)
        {
            if (!_rules.TryGetValue(eventType, out var ruleList) || ruleList.Count == 0) return;

            var lifecycleEvent = new CardEvent<object>(eventType, null, eventType);
            var cardSnapshot = new List<Card>(_cardsByUID.Values);

            foreach (Card card in cardSnapshot)
            {
                if (_cardsByUID.ContainsKey(card.UID)) ProcessCore(card, lifecycleEvent);
            }

            if (Policy.EnableEffectPool) FlushEffectPool();
        }

        /// <summary>
        ///     按帧时间预算处理事件。
        /// </summary>
        public void PumpFrameWithBudget()
        {
            if (_isPumping) return;
            _isPumping = true;
            int processed = 0;

            try
            {
                _frameStartTime = Time.realtimeSinceStartup * 1000f;
                _frameProcessedCount = 0;

                float frameBudget = Policy.FrameBudgetMs;
                int maxEvents = Policy.MaxEventsPerFrame;
                int minEvents = Policy.MinEventsPerFrame;

                while (_eventQueue.Count > 0 && processed < maxEvents)
                {
                    if (_frameProcessedCount >= minEvents)
                    {
                        float elapsed = Time.realtimeSinceStartup * 1000f - _frameStartTime;
                        if (elapsed >= frameBudget) break;
                    }

                    (Card source, ICardEvent evt) = _eventQueue.Dequeue();
                    Process(source, evt);
                    processed++;
                    _frameProcessedCount++;
                }
            }
            finally
            {
                _isPumping = false;

                if (Policy.EnableEffectPool && Policy.EffectPoolFlushMode == EffectPoolFlushMode.AfterFrame)
                {
                    try
                    {
                        FlushEffectPool();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[CardEngine] PumpFrameWithBudget 错误: {ex}");
                    }
                }
            }
        }

        /// <summary>
        ///     初始化时间限制批次处理。
        /// </summary>
        public void BeginTimeLimitedBatch(float? timeLimitSeconds = null)
        {
            if (IsBatchProcessing) return;

            IsBatchProcessing = true;
            _batchStartTime = Time.realtimeSinceStartup;
            _batchTimeLimit = timeLimitSeconds ?? Policy.BatchTimeLimitSeconds;
        }

        /// <summary>
        ///     驱动时间限制批次处理（在 Update 中调用）。
        /// </summary>
        public void PumpTimeLimitedBatch()
        {
            if (!IsBatchProcessing) return;

            float elapsed = Time.realtimeSinceStartup - _batchStartTime;
            float remaining = _batchTimeLimit - elapsed;
            bool batchCompleted = false;

            if (remaining > 0)
            {
                float frameStart = Time.realtimeSinceStartup;
                float frameBudgetSec = Policy.FrameBudgetMs / 1000f;
                int maxEvents = Policy.MaxEventsPerFrame;
                int processedInFrame = 0;

                while (_eventQueue.Count > 0 &&
                       Time.realtimeSinceStartup - frameStart < frameBudgetSec &&
                       processedInFrame < maxEvents)
                {
                    (Card source, ICardEvent evt) = _eventQueue.Dequeue();
                    Process(source, evt);
                    processedInFrame++;
                }

                if (_eventQueue.Count == 0)
                {
                    IsBatchProcessing = false;
                    batchCompleted = true;
                }
                else if (Policy.EnableEffectPool && Policy.EffectPoolFlushMode == EffectPoolFlushMode.AfterFrame)
                {
                    FlushEffectPool();
                }
            }
            else
            {
                // 超时：同步完成所有
                while (_eventQueue.Count > 0)
                {
                    (Card source, ICardEvent evt) = _eventQueue.Dequeue();
                    Process(source, evt);
                }

                IsBatchProcessing = false;
                batchCompleted = true;
            }

            if (batchCompleted && Policy.EnableEffectPool &&
                Policy.EffectPoolFlushMode == EffectPoolFlushMode.AfterPump)
            {
                FlushEffectPool();
            }
        }

        #endregion

        #region 事件处理核心

        /// <summary>
        ///     处理单个事件。
        /// </summary>
        private void Process(Card source, ICardEvent evt)
        {
            // 跳过 Pump 生命周期事件
            if (CardEventTypes.IsPumpLifecycle(evt)) return;

            ProcessCore(source, evt);
        }

        /// <summary>
        ///     核心事件处理逻辑。
        /// </summary>
        private void ProcessCore(Card source, ICardEvent evt)
        {
            if (!_rules.TryGetValue(evt.EventType, out var typeRules) || typeRules.Count == 0) return;

            // 评估规则
            var evals = Policy.EnableParallelMatching && typeRules.Count >= Policy.ParallelThreshold
                ? EvaluateRulesParallel(typeRules, source, evt)
                : EvaluateRulesSerial(typeRules, source, evt);

            if (evals.Count == 0) return;

            // 直接执行模式需要排序
            if (!Policy.EnableEffectPool)
            {
                evals.Sort(Policy.RuleSelection == RuleSelectionMode.Priority
                    ? s_priorityComparison
                    : s_orderComparison);
            }

            ProcessRulesEffects(evals);
        }

        #endregion

        #region 效果执行

        /// <summary>
        ///     执行规则效果。
        /// </summary>
        private void ProcessRulesEffects(
            List<(CardRule rule, HashSet<Card> matched, CardRuleContext ctx, int orderIndex)> evals)
        {
            if (Policy.EnableEffectPool)
            {
                CollectEffectsToPool(evals);
            }
            else
            {
                ExecuteRulesDirect(evals);
            }
        }

        /// <summary>
        ///     直接执行规则效果。
        /// </summary>
        private void ExecuteRulesDirect(
            List<(CardRule rule, HashSet<Card> matched, CardRuleContext ctx, int orderIndex)> evals)
        {
            if (evals.Count == 0) return;

            if (Policy.FirstMatchOnly)
            {
                ExecuteOne(evals[0]);
            }
            else
            {
                foreach (var eval in evals)
                {
                    if (ExecuteOne(eval)) break;
                }
            }
        }

        /// <summary>
        ///     收集效果到全局效果池。
        /// </summary>
        private void CollectEffectsToPool(
            List<(CardRule rule, HashSet<Card> matched, CardRuleContext ctx, int orderIndex)> evals)
        {
            if (evals.Count == 0) return;

            int currentEventIndex = _globalOrderIndex;

            foreach ((CardRule rule, var matched, CardRuleContext ctx, int orderIndex) in evals)
            {
                if (matched == null || rule.Effects == null || rule.Effects.Count == 0)
                {
                    continue;
                }

                _globalEffectPool.AddRuleEffects(rule, ctx, matched, currentEventIndex, orderIndex);
            }

            _globalOrderIndex++;
        }

        /// <summary>
        ///     刷新全局效果池。
        /// </summary>
        public int FlushEffectPool()
        {
            if (_isFlushingEffectPool || _globalEffectPool.Count == 0) return 0;

            _isFlushingEffectPool = true;
            try
            {
                int count = _globalEffectPool.ExecuteWithStopEventOnSuccess();
                _globalEffectPool.Clear();
                _globalOrderIndex = 0;
                return count;
            }
            finally
            {
                _isFlushingEffectPool = false;
            }
        }

        /// <summary>
        ///     执行单个规则的效果。
        /// </summary>
        private bool ExecuteOne((CardRule rule, HashSet<Card> matched, CardRuleContext ctx, int orderIndex) e)
        {
            if (e.matched == null || e.rule.Effects == null || e.rule.Effects.Count == 0)
            {
                return false;
            }

            foreach (IRuleEffect eff in e.rule.Effects)
            {
                eff.Execute(e.ctx, e.matched);
            }

            return e.rule.Policy?.StopEventOnSuccess == true;
        }

        #endregion
    }
}