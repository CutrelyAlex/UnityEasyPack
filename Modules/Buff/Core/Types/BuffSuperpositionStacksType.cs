namespace EasyPack.BuffSystem
{
    /// <summary>
    /// Buff 堆叠层数叠加策略
    /// </summary>
    public enum BuffSuperpositionStacksType
    {
        /// <summary>
        /// 叠加堆叠层数
        /// </summary>
        Add,

        /// <summary>
        /// 重置堆叠层数后再叠加
        /// </summary>
        ResetThenAdd,

        /// <summary>
        /// 重置堆叠层数为 1
        /// </summary>
        Reset,

        /// <summary>
        /// 保持原有堆叠层数不变
        /// </summary>
        Keep
    }
}
