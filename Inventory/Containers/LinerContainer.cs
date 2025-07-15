using EasyPack;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LinerContainer : Container
{
    public override bool IsGrid => false;

    public override Vector2 Grid => new(-1,-1);


    /// <summary>
    /// 创建一个线性容器
    /// </summary>
    /// <param name="id">容器ID</param>
    /// <param name="name">容器名称</param>
    /// <param name="type">容器类型</param>
    /// <param name="capacity">容量，-1表示无限</param>
    public LinerContainer(string id, string name, string type, int capacity = -1)
        : base(id, name, type, capacity)
    {
        // 初始化槽位
        if (capacity > 0)
        {
            for (int i = 0; i < capacity; i++)
            {
                var slot = new Slot
                {
                    Index = i,
                    Container = this
                };
                _slots.Add(slot);
            }
        }
    }

    /// <summary>
    /// 物品移动处理
    /// </summary>
    /// <param name="sourceSlotIndex">源槽位索引</param>
    /// <param name="targetContainer">目标容器</param>
    /// <returns>移动结果</returns>
    public bool MoveItemToContainer(int sourceSlotIndex, IContainer targetContainer)
    {
        if (sourceSlotIndex < 0 || sourceSlotIndex >= _slots.Count)
            return false;

        var sourceSlot = _slots[sourceSlotIndex];
        if (!sourceSlot.IsOccupied || sourceSlot.Item == null)
            return false;

        var item = sourceSlot.Item;
        int count = sourceSlot.ItemCount;

        bool isStackable = item.IsStackable;

        IItem itemToAdd;
        if (!isStackable)
        {
            if (item is Item itemObj)
            {
                itemToAdd = itemObj.Clone();
            }
            else
            {
                itemToAdd = new Item
                {
                    ID = item.ID,
                    Name = item.Name,
                    Type = item.Type,
                    Description = item.Description,
                    Weight = item.Weight,
                    IsStackable = false,
                    MaxStackCount = item.MaxStackCount,
                    IsMultiSlot = item.IsMultiSlot,
                    Size = item.Size
                };

                if (item.Attributes != null)
                {
                    ((Item)itemToAdd).Attributes = new Dictionary<string, object>(item.Attributes);
                }
            }
        }
        else
        {
            itemToAdd = item;
        }

        var (result, addedCount) = targetContainer.AddItems(itemToAdd, count);

        if (result == AddItemResult.Success)
        {
            // 完全移动
            if (addedCount == count)
            {
                // 清除源槽位
                sourceSlot.ClearSlot();
                return true;
            }
            // 部分移动
            else if (addedCount > 0)
            {
                // 更新源槽位的物品数量
                sourceSlot.SetItem(item, count - addedCount);
                return true;
            }
        }

        return true;
    }
    /// <summary>
    /// 整理容器
    /// </summary>
    public void SortInventory()
    {
        // 1. 收集所有物品
        var allItems = new List<(IItem item, int count)>();
        foreach (var slot in _slots.ToArray())
        {
            if (slot.IsOccupied && slot.Item != null)
            {
                allItems.Add((slot.Item, slot.ItemCount));
                slot.ClearSlot();
            }
        }

        // 2. 按物品ID分组并排序
        var groupedItems = allItems
            .GroupBy(x => x.item.ID)
            .OrderBy(g => g.First().item.Type)
            .ThenBy(g => g.First().item.Name)
            .ToList();

        // 3. 重新放回背包
        foreach (var group in groupedItems)
        {
            var itemTemplate = group.First().item;
            int totalCount = group.Sum(x => x.count);

            // 添加回背包
            var result = AddItems(itemTemplate, totalCount);
            if (result.result != AddItemResult.Success)
            {
                Debug.LogWarning($"排序背包时无法重新添加物品: {itemTemplate.Name}, 结果: {result.result}");
            }
        }
    }
}