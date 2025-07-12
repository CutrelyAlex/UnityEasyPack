namespace EasyPack
{
    public interface ISlot
    {
        int Index { get; }
        IItem Item { get; }
        int ItemCount { get; }
        bool IsOccupied { get; } // �Ƿ�����Ʒ
        bool HasMultiSlotItem { get; } // �Ƿ��Ƕ����Ʒ
        ItemCondition SlotCondition { get; }
        bool CanAcceptItem(IItem item);
        bool AddItem(IItem item, int count = 1);
        bool RemoveItem(int count = 1);

    }
}