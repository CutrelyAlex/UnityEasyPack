namespace EasyPack.EmeCardSystem
{
    /// <summary>
    /// 分帧处理配置数据容器
    /// </summary>
    public sealed class FrameDistributionConfig
    {
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

        /// <summary>
        /// 获取当前配置的描述
        /// </summary>
        public override string ToString()
        {
            return $"FrameDistributionConfig[FrameBudget={FrameBudgetMs}ms, " +
                   $"MinEvents={MinEventsPerFrame}, MaxEvents={MaxEventsPerFrame}, " +
                   $"BatchTimeLimit={BatchTimeLimitSeconds}s]";
        }
    }
}
