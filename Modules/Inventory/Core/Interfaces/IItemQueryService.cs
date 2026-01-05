using System;
using System.Collections.Generic;

namespace EasyPack.InventorySystem
{
    /// <summary>
    ///     物品查询服务接口
    /// </summary>
    public interface IItemQueryService
    {
        bool HasItem(string itemId);
        IItem GetItemReference(string itemId);
        int GetItemTotalCount(string itemId);
        bool HasEnoughItems(string itemId, int requiredCount);
        List<int> FindSlotIndices(string itemId);
        int FindFirstSlotIndex(string itemId);

        List<(int slotIndex, IItem item, int count)> GetItemsByType(string itemType);
        List<(int slotIndex, IItem item, int count)> GetItemsByAttribute(string attributeName, object attributeValue);
        List<(int slotIndex, IItem item, int count)> GetItemsByName(string namePattern);
        List<(int slotIndex, IItem item, int count)> GetItemsWhere(Func<IItem, bool> condition);

        Dictionary<string, int> GetAllItemCountsDict();
        List<(int slotIndex, IItem item, int count)> GetAllItems();
        int GetUniqueItemCount();
        bool IsEmpty();
        float GetTotalWeight();
    }
}