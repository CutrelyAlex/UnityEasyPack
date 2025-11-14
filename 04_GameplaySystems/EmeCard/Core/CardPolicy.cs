namespace EasyPack.EmeCardSystem
{
    // 规则选择模式
    public enum RuleSelectionMode
    {
        RegistrationOrder,  // 按注册顺序
        Priority            // 按规则优先级（数值越小优先）
    }

    // 引擎默认策略
    public sealed class EnginePolicy
    {
        // 是否只执行一条命中的规则（跨所有规则）
        public bool FirstMatchOnly { get; set; } = false;

        // 命中规则的裁决方式
        public RuleSelectionMode RuleSelection { get; set; } = RuleSelectionMode.RegistrationOrder;

        // ===== 分帧处理配置 =====

        /// <summary>
        /// 是否启用分帧事件处理（默认关闭）
        /// </summary>
        public bool EnableFrameDistribution { get; set; } = false;

        /// <summary>
        /// 每帧事件处理的时间预算（毫秒），默认3ms
        /// </summary>
        public float FrameBudgetMs { get; set; } = 3f;

        /// <summary>
        /// 每帧最小处理事件数，即使超时也至少处理这么多
        /// </summary>
        public int MinEventsPerFrame { get; set; } = 1;

        /// <summary>
        /// 每帧最大处理事件数
        /// </summary>
        public int MaxEventsPerFrame { get; set; } = 100;
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
