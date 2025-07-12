namespace EasyPack
{
    public interface ISlot
    {
        int Index { get; }
        IItem Item { get; }
        int ItemCount { get; }
        bool IsOccupied { get; } // 是否有物品
        bool HasMultiSlotItem { get; } // 是否是多格物品
        ItemCondition SlotCondition { get; }
        bool CanAcceptItem(IItem item);
        bool AddItem(IItem item, int count = 1);
        bool RemoveItem(int count = 1);

    }
}