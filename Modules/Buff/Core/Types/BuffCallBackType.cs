namespace EasyPack.BuffSystem
{
    /// <summary>
    /// Buff 回调类型，定义 Buff 生命周期中的各个事件点
    /// </summary>
    public enum BuffCallBackType
    {
        /// <summary>
        /// Buff 创建时触发
        /// </summary>
        OnCreate,

        /// <summary>
        /// Buff 移除时触发
        /// </summary>
        OnRemove,

        /// <summary>
        /// Buff 堆叠层数增加时触发
        /// </summary>
        OnAddStack,

        /// <summary>
        /// Buff 堆叠层数减少时触发
        /// </summary>
        OnReduceStack,

        /// <summary>
        /// Buff 每帧更新时触发
        /// </summary>
        OnUpdate,

        /// <summary>
        /// Buff 定时触发时触发
        /// </summary>
        OnTick,

        /// <summary>
        /// 条件触发
        /// </summary>
        Condition,

        /// <summary>
        /// 自定义回调类型
        /// </summary>
        Custom
    }
}