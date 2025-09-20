using System.Collections.Generic;

namespace EasyPack
{
    // 决定效果作用对象
    public enum TargetKind
    {
        Matched,            // 匹配到的卡
        Source,             // 触发源
        Container,          // 匹配容器本体
        ContainerChildren,  // 容器内所有子卡
        ByTag,              // 按标签过滤容器内子卡
        ById                // 按ID过滤容器内子卡
    }

    // 规则效果接口
    public interface IRuleEffect
    {
        void Execute(CardRuleContext ctx, IReadOnlyList<Card> matched);
    }
}