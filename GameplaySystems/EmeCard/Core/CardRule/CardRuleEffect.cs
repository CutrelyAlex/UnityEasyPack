using System.Collections.Generic;

namespace EasyPack
{
    // 规则效果接口
    public interface IRuleEffect
    {
        void Execute(CardRuleContext ctx, IReadOnlyList<Card> matched);
    }

    /// <summary>
    /// 目标选择配置
    /// 供效果声明目标类型/过滤值/数量上限。
    /// </summary>
    public interface ITargetSelection
    {
        /// <summary>目标类型（例如 Matched/ByTag/ById/Container...）。</summary>
        TargetKind TargetKind { get; set; }

        /// <summary>目标过滤值（ByTag/ById 等时生效）。</summary>
        string TargetValueFilter { get; set; }

        /// <summary>仅作用前 N 个目标（<=0 表示不限制）。</summary>
        int Take { get; set; }
    }
}