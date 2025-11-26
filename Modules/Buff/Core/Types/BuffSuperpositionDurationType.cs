namespace EasyPack.BuffSystem
{
    /// <summary>
    ///     Buff 持续时间叠加策略
    /// </summary>
    public enum BuffSuperpositionDurationType
    {
        /// <summary>
        ///     叠加持续时间
        /// </summary>
        Add,

        /// <summary>
        ///     重置持续时间后再叠加
        /// </summary>
        ResetThenAdd,

        /// <summary>
        ///     重置持续时间
        /// </summary>
        Reset,

        /// <summary>
        ///     保持原有持续时间不变
        /// </summary>
        Keep,
    }
}