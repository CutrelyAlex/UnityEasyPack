namespace EasyPack.InventorySystem
{
    public enum AddItemResult
    {
        Success,
        ItemIsNull,
        ContainerIsFull,
        StackLimitReached,
        SlotNotFound,
        ItemConditionNotMet,
        NoSuitableSlotFound,
        AddNothingLOL,
        InvalidCount,
    }
}