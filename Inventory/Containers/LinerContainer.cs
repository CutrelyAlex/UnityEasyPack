using EasyPack;
using System;
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
                TriggerItemTotalCountChanged(sourceItem.ID);
            }
            else
            {
                // 部分移动更新数量
                int remainingCount = sourceCount - addedCount;
                sourceSlot.SetItem(sourceItem, remainingCount);

                UpdateItemCountCache(sourceItem.ID, -addedCount);
                TriggerItemTotalCountChanged(sourceItem.ID, sourceItem);

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
        if (_slots.Count < 2)
            return;

        var occupiedSlots = new List<(int index, IItem item, int count)>();
        for (int i = 0; i < _slots.Count; i++)
        {
            var slot = _slots[i];
            if (slot.IsOccupied && slot.Item != null)
            {
                occupiedSlots.Add((i, slot.Item, slot.ItemCount));
            }
        }

        if (occupiedSlots.Count < 2)
            return;

        occupiedSlots.Sort((a, b) => {
            int typeCompare = string.Compare(a.item.Type, b.item.Type);
            if (typeCompare != 0)
                return typeCompare;
            return string.Compare(a.item.Name, b.item.Name);
        });

        var originalData = new (IItem item, int count)[_slots.Count];
        for (int i = 0; i < _slots.Count; i++)
        {
            var slot = _slots[i];
            originalData[i] = (slot.Item, slot.ItemCount);
        }

        BeginBatchUpdate();

        try
        {
            // 先清空所有槽位
            foreach (var slot in _slots)
            {
                slot.ClearSlot();
            }

            // 按排序后的顺序填充槽位
            for (int i = 0; i < occupiedSlots.Count; i++)
            {
                var (_, item, count) = occupiedSlots[i];
                _slots[i].SetItem(item, count);
            }

            RebuildCaches();
        }
        catch (Exception ex)
        {
            Debug.LogError($"排序背包时发生错误: {ex.Message}，正在恢复原始数据");
            for (int i = 0; i < _slots.Count && i < originalData.Length; i++)
            {
                if (originalData[i].item != null)
                    _slots[i].SetItem(originalData[i].item, originalData[i].count);
                else
                    _slots[i].ClearSlot();
            }

            RebuildCaches();
        }
        finally
        {
            EndBatchUpdate();
        }
    }
}