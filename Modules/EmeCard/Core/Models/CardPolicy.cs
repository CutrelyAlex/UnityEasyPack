namespace EasyPack.EmeCardSystem
{
    // 引擎默认策略
    public sealed class EnginePolicy
    {
        // 是否只执行一条命中的规则（跨所有规则）
        public bool FirstMatchOnly { get; set; } = false;

        // 命中规则的裁决方式（默认按优先级排序，数值越小优先级越高）
        public RuleSelectionMode RuleSelection { get; set; } = RuleSelectionMode.Priority;

        // ===== 并行处理配置 =====

        /// <summary>
        ///     是否启用并行 Requirement 评估。
        ///     <para>当规则数量超过 ParallelThreshold 时，使用并行评估</para>
        ///     <remarks>默认值：false</remarks>
        /// </summary>
        public bool EnableParallelMatching { get; set; } = false;

        /// <summary>
        ///     启用并行评估的规则数量阈值。
        ///     <para>仅当 EnableParallelMatching=true 且规则数≥此值时启用并行。</para>
        ///     <remarks>默认值：10</remarks>
        /// </summary>
        public int ParallelThreshold { get; set; } = 10;

        /// <summary>
        ///     并行评估的最大并发度。
        ///     <para>-1 表示使用系统默认（CPU 核心数）</para>
        ///     <remarks>默认值：-1</remarks>
        /// </summary>
        public int MaxDegreeOfParallelism { get; set; } = -1;

        // ===== 分帧处理配置 =====

        /// <summary>
        ///     是否启用分帧事件处理（默认开启）
        /// </summary>
        public bool EnableFrameDistribution { get; set; } = true;

        /// <summary>
        ///     每帧最小处理事件数（至少处理这么多，即使超时）
        ///     适用于：PumpFrameWithBudget()、PumpTimeLimitedBatch()
        ///     默认值：1
        /// </summary>
        public int MinEventsPerFrame { get; set; } = 1;

        /// <summary>
        ///     每帧最大处理事件数
        ///     适用于：PumpFrameWithBudget()、PumpTimeLimitedBatch()
        ///     <remarks>默认值：3500</remarks>
        /// </summary>
        public int MaxEventsPerFrame { get; set; } = 3500;

        /// <summary>
        ///     每帧时间预算（毫秒）
        ///     适用于：PumpFrameWithBudget()、PumpTimeLimitedBatch()
        ///     默认值：10ms
        /// </summary>
        public float FrameBudgetMs { get; set; } = 10f;

        /// <summary>
        ///     批次处理的总时间限制（秒）
        ///     仅用于PumpTimeLimitedBatch()方法
        ///     默认值：0.8s
        /// </summary>
        public float BatchTimeLimitSeconds { get; set; } = 0.8f;

        // ===== 全局效果池配置 =====

        /// <summary>
        ///     是否启用全局效果池。
        ///     <para>
        ///         启用后，所有规则效果会先收集到全局池中，
        ///         然后按全局优先级排序后统一执行。
        ///         这确保了跨事件的效果执行顺序一致性，避免并发执行效果。
        ///     </para>
        ///     <remarks>默认值：true </remarks>
        /// </summary>
        public bool EnableEffectPool { get; set; } = true;

        /// <summary>
        ///     效果池刷新模式。
        ///     <para>
        ///         - AfterPump: 每次 Pump() 调用结束时刷新（处理完所有排队事件后）
        ///         - AfterFrame: 每帧结束时刷新（配合 PumpFrame 系列方法）
        ///         - Manual: 手动调用 FlushEffectPool() 刷新
        ///     </para>
        ///     <remarks>默认值：AfterPump</remarks>
        /// </summary>
        public EffectPoolFlushMode EffectPoolFlushMode { get; set; } = EffectPoolFlushMode.AfterPump;
    }

    /// <summary>
    ///     效果池刷新模式。
    /// </summary>
    public enum EffectPoolFlushMode
    {
        /// <summary>每次 Pump() 调用结束时刷新（处理完所有排队事件后）</summary>
        AfterPump,

        /// <summary>每帧结束时刷新（配合 PumpFrame 系列方法）</summary>
        AfterFrame,

        /// <summary>手动调用 FlushEffectPool() 刷新</summary>
        Manual,
    }

    // 规则策略
    public sealed class RulePolicy
    {
        /// <summary>
        ///     是否对聚合的 matched 去重
        ///     <remarks>默认值：true</remarks>
        /// </summary>
        public bool DistinctMatched { get; set; } = true;

        /// <summary>
        ///     该规则命中并执行后，是否中止本次事件的后续规则
        ///     <remarks>默认值：false</remarks>
        /// </summary>
        public bool StopEventOnSuccess { get; set; }
    }
}