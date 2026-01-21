namespace EasyPack.InventorySystem
{
    /// <summary>
    /// 物品移动操作结果
    /// </summary>
    public enum MoveItemResult
    {
        /// <summary>
        /// 移动成功
        /// </summary>
        Success,
        
        /// <summary>
        /// 部分成功（部分数量已移动，但未能完全移动）
        /// </summary>
        PartialSuccess,
        
        /// <summary>
        /// 源槽位索引无效
        /// </summary>
        SourceSlotNotFound,
        
        /// <summary>
        /// 源槽位为空
        /// </summary>
        SourceSlotEmpty,
        
        /// <summary>
        /// 目标容器为null
        /// </summary>
        TargetContainerNull,
        
        /// <summary>
        /// 目标容器已满
        /// </summary>
        TargetContainerFull,
        
        /// <summary>
        /// 目标槽位索引无效
        /// </summary>
        TargetSlotNotFound,
        
        /// <summary>
        /// 目标槽位已被占用且无法堆叠
        /// </summary>
        TargetSlotOccupied,
        
        /// <summary>
        /// 物品不满足目标容器条件
        /// </summary>
        ConditionNotMet,
        
        /// <summary>
        /// 未知错误
        /// </summary>
        Failed
    }
}
