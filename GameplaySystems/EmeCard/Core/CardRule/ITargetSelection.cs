namespace EasyPack
{
    /// <summary>
    /// 效果选择的根锚点：决定以谁为根进行选择。
    /// </summary>
    public enum EffectRoot
    {
        /// <summary>以上下文容器（ctx.Container）为根。</summary>
        Container,
        /// <summary>以触发源（ctx.Source）为根。</summary>
        Source
    }

    /// <summary>
    /// 目标选择配置
    /// 供效果声明目标类型/过滤值/数量上限。
    /// </summary>
    public interface ITargetSelection
    {
        /// <summary>目标选择起点。</summary>
        EffectRoot Root { get; set; }

        /// <summary>目标类型（例如 Matched/ByTag/ById/Children/Descendants...）。</summary>
        TargetKind TargetKind { get; set; }

        /// <summary>目标过滤值（ByTag/ById 等时生效）。</summary>
        string TargetValueFilter { get; set; }

        /// <summary>仅作用前 N 个目标（<=0 表示不限制）。</summary>
        int Take { get; set; }
    }
}