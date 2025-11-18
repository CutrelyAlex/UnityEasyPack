namespace EasyPack.EmeCardSystem
{
    /// <summary>
    /// 统一的选择根锚点：决定以谁为根进行选择。
    /// 同时适用于效果（ITargetSelection）和要求项（CardsRequirement）。
    /// </summary>
    public enum SelectionRoot
    {
        /// <summary>以上下文容器（ctx.Container）为根。</summary>
        Container,
        /// <summary>以触发源（ctx.Source）为根。</summary>
        Source
    }

    /// <summary>
    /// 目标选择范围：决定选择的作用域。
    /// </summary>
    public enum TargetScope
    {
        /// <summary>来自"所有要求项"返回的匹配卡集合的聚合。</summary>
        Matched,
        /// <summary>选定根的一层子卡（不递归）。</summary>
        Children,
        /// <summary>选定根的所有后代（递归）。</summary>
        Descendants
    }

    /// <summary>
    /// 过滤模式：决定如何过滤目标。
    /// </summary>
    public enum CardFilterMode
    {
        /// <summary>不过滤（返回所有目标）。</summary>
        None,
        /// <summary>按标签过滤。</summary>
        ByTag,
        /// <summary>按ID过滤。</summary>
        ById,
        /// <summary>按类别过滤。</summary>
        ByCategory
    }

    /// <summary>
    /// 目标选择配置
    /// 供效果声明目标类型/过滤值/数量上限。
    /// </summary>
    public interface ITargetSelection
    {
        /// <summary>目标选择起点（默认 Container）。</summary>
        SelectionRoot Root { get; set; }

        /// <summary>选择范围（默认 Matched）。</summary>
        TargetScope Scope { get; set; }

        /// <summary>过滤模式（默认 None）。</summary>
        CardFilterMode Filter { get; set; }

        /// <summary>目标过滤值（ByTag/ById/ByCategory 时生效）。</summary>
        string FilterValue { get; set; }

        /// <summary>仅作用前 N 个目标（null 表示不限制）。</summary>
        int? Take { get; set; }

        /// <summary>递归深度限制（仅对 Scope=Descendants 生效，null 或 &lt;=0 表示不限制）。</summary>
        int? MaxDepth { get; set; }
    }
}
