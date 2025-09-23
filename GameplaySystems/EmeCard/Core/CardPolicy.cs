using System;

namespace EasyPack
{
    // 规则选择模式：仅保留“最具体”裁决
    public enum RuleSelectionMode
    {
        MostSpecific,
        // 待拓展
    }

    // 引擎默认策略
    public sealed class EnginePolicy
    {
        // 是否只执行一条命中的规则。只执行具体度最高的的一条
        public bool FirstMatchOnly { get; set; } = true;

        // 命中规则的裁决方式
        public RuleSelectionMode RuleSelection { get; set; } = RuleSelectionMode.MostSpecific;
    }

    // 规则策略
    public sealed class RulePolicy
    {
        // 是否对聚合的 matched 去重（仅影响 TargetKind=Matched 的效果）
        public bool DistinctMatched { get; set; } = true;

        // 覆盖具体度
        public int? SpecificityOverride { get; set; } = null;
    }
}