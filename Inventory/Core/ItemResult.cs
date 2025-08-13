namespace EasyPack
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
        AddNothingLOL
    }

    public enum RemoveItemResult
    {
        Success,
        InvalidItemId,
        ItemNotFound,
        SlotNotFound,
        InsufficientQuantity,
        Failed
    }
}