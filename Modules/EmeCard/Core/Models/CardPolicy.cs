namespace EasyPack.EmeCardSystem
{

    // 引擎默认策略
    public sealed class EnginePolicy
    {
        // 是否只执行一条命中的规则（跨所有规则）
        public bool FirstMatchOnly { get; set; } = false;

        // 命中规则的裁决方式
        public RuleSelectionMode RuleSelection { get; set; } = RuleSelectionMode.RegistrationOrder;

        // ===== 分帧处理配置 =====

        /// <summary>
        /// 是否启用分帧事件处理（默认开启）
        /// </summary>
        public bool EnableFrameDistribution { get; set; } = true;

        /// <summary>
        /// 每帧最小处理事件数（至少处理这么多，即使超时）
        /// 适用于：PumpFrameWithBudget()、PumpTimeLimitedBatch()
        /// 默认值：1
        /// </summary>
        public int MinEventsPerFrame { get; set; } = 1;

        /// <summary>
        /// 每帧最大处理事件数
        /// 适用于：PumpFrameWithBudget()、PumpTimeLimitedBatch()
        /// 默认值：3500
        /// </summary>
        public int MaxEventsPerFrame { get; set; } = 3500;

        /// <summary>
        /// 每帧时间预算（毫秒）
        /// 适用于：PumpFrameWithBudget()、PumpTimeLimitedBatch()
        /// 默认值：10ms
        /// </summary>
        public float FrameBudgetMs { get; set; } = 10f;

        /// <summary>
        /// 批次处理的总时间限制（秒）
        /// 仅用于PumpTimeLimitedBatch()方法
        /// 默认值：0.8s
        /// </summary>
        public float BatchTimeLimitSeconds { get; set; } = 0.8f;
    }

    // 规则策略
    public sealed class RulePolicy
    {
        // 是否对聚合的 matched 去重
        public bool DistinctMatched { get; set; } = true;

        // 该规则命中并执行后，是否中止本次事件的后续规则
        public bool StopEventOnSuccess { get; set; } = false;
    }
}
