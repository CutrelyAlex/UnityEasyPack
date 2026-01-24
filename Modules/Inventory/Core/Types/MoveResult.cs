namespace EasyPack.InventorySystem
{
    /// <summary>
    ///     移动操作结果
    /// </summary> 
    public enum MoveResult
    {
        Success,
        SourceContainerNotFound,
        TargetContainerNotFound,
        SourceSlotEmpty,
        SourceSlotNotFound,
        TargetSlotNotFound,
        ItemNotFound,
        InsufficientQuantity,
        TargetContainerFull,
        ItemConditionNotMet,
        Failed,
    }
}