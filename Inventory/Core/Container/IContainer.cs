using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;


namespace EasyPack
{

    /// <summary>
    /// 添加物品操作的结果枚举
    /// </summary>
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
    public interface IContainer
    {
        // 容器属性
        string ID { get; }
        string Name { get; }
        string Type { get; set; }
        int Capacity { get; set; }
        bool IsGrid { get; }
        bool Full { get; }
        IReadOnlyList<ISlot> Slots { get; }
        List<IItemCondition> ContainerCondition { get; set; }

        RemoveItemResult RemoveItem(string itemID, int count = 1);
        RemoveItemResult RemoveItemAtIndex(int index, int count = 1, string itemID = null);
        bool HasItem(string itemID);
        int GetItemTotalCount(string itemID);

        // 添加物品方法
        (AddItemResult result, int addedCount) AddItems(IItem item, int count, int slotIndex = -1);
        (AddItemResult result, int addedCount) AddItemsWithCount(IItem item, out int exceededCount, int count = 1, int slotIndex = -1);
    }
}