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
        // 早期验证优化
        if (sourceSlotIndex < 0 || sourceSlotIndex >= _slots.Count)
            return false;

        var sourceSlot = _slots[sourceSlotIndex];
        if (!sourceSlot.IsOccupied || sourceSlot.Item == null)
            return false;

        var sourceItem = sourceSlot.Item;
        int sourceCount = sourceSlot.ItemCount;

        if (targetContainer is Container targetContainerImpl)
        {
            if (!targetContainerImpl.ValidateItemCondition(sourceItem))
                return false;
        }

        // 尝试添加到目标容器
        var (result, addedCount) = targetContainer.AddItems(sourceItem, sourceCount);

        // 根据添加结果更新源槽位
        if (result == AddItemResult.Success && addedCount > 0)
        {
            if (addedCount == sourceCount)
            {
                // 完全移动直接清除槽位
                sourceSlot.ClearSlot();

                UpdateEmptySlotCache(sourceSlotIndex, true);
                UpdateItemCache(sourceItem.ID, sourceSlotIndex, false);
                UpdateItemTypeCache(sourceItem.Type, sourceSlotIndex, false);
                UpdateItemCountCache(sourceItem.ID, -sourceCount);
                UpdateItemTotalCount(sourceItem.ID);
            }
            else
            {
                // 部分移动更新数量
                int remainingCount = sourceCount - addedCount;
                sourceSlot.SetItem(sourceItem, remainingCount);

                UpdateItemCountCache(sourceItem.ID, -addedCount);
                UpdateItemTotalCount(sourceItem.ID, sourceItem);

                OnSlotQuantityChanged(sourceSlotIndex, sourceItem, sourceCount, remainingCount);
            }

            return true;
        }

        return false;
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