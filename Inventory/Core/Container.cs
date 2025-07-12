using EasyPack;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class Container : IContainer
{
    #region 基本属性
    public string ID { get; }
    public string Name { get; }
    public string Type { get; set; }
    public int Capacity { get; set; } // -1表示无限容量
    public abstract bool IsGrid { get; } // 子类实现，决定是否为网格容器
    public List<IItemCondition> ContainerCondition { get; set; }
    protected List<ISlot> _slots = new List<ISlot>();
    public IReadOnlyList<ISlot> Slots => _slots.AsReadOnly();

    public Container(string id, string name, string type, int capacity = -1)
    {
        ID = id;
        Name = name;
        Type = type;
        Capacity = capacity;
        ContainerCondition = new List<IItemCondition>();
    }
    #endregion

    #region 状态检查
    /// <summary>
    /// 检查容器是否已满
    /// 仅当所有槽位都被占用，且每个占用的槽位物品都不可堆叠或已达到堆叠上限时，容器才被认为是满的
    /// </summary>
    public virtual bool Full
    {
        get
        {
            // 如果是无限容量，则永远不会满
            if (Capacity <= 0)
                return false;

            // 如果槽位数量小于容量，容器不满
            if (_slots.Count < Capacity)
                return false;

            // 检查是否所有槽位都已被占用
            foreach (var slot in _slots)
            {
                if (!slot.IsOccupied)
                    return false;

                // 如果物品可堆叠且未达到堆叠上限，容器不满
                if (slot.Item.IsStackable && (slot.Item.MaxStackCount <= 0 || slot.ItemCount < slot.Item.MaxStackCount))
                    return false;
            }

            return true;
        }
    }

    /// <summary>
    /// 检查物品是否满足容器条件
    /// </summary>
    protected bool CheckContainerCondition(IItem item)
    {
        if (item == null)
        {
            Debug.LogWarning("CheckContainerCondition: item is null.");
            return false;
        }

        if (ContainerCondition != null && ContainerCondition.Count > 0)
        {
            foreach (var condition in ContainerCondition)
            {
                if (!condition.IsCondition(item))
                    return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 检查是否可以添加物品到容器
    /// </summary>
    /// <param name="item">要添加的物品</param>
    /// <returns>添加结果，如果可以添加返回Success，否则返回对应的错误原因</returns>
    protected virtual AddItemResult CanAddItem(IItem item)
    {
        if (item == null)
            return AddItemResult.ItemIsNull;

        if (!CheckContainerCondition(item))
            return AddItemResult.ItemConditionNotMet;

        // 如果容器已满，需要检查是否有可堆叠的槽位
        if (Full)
        {
            // 如果物品可堆叠，检查是否有相同物品且未达到堆叠上限的槽位
            if (item.IsStackable)
            {
                bool hasStackableSlot = false;
                foreach (var slot in _slots)
                {
                    if (slot.IsOccupied && slot.Item.ID == item.ID &&
                        slot.Item.IsStackable && (slot.Item.MaxStackCount <= 0 || slot.ItemCount < slot.Item.MaxStackCount))
                    {
                        hasStackableSlot = true;
                        break;
                    }
                }

                // 没有可堆叠的槽位
                if (!hasStackableSlot)
                    return AddItemResult.StackLimitReached;
            }
            else
            {
                // 物品不可堆叠且容器已满
                return AddItemResult.ContainerIsFull;
            }
        }

        return AddItemResult.Success;
    }
    #endregion

    #region 物品操作
    /// <summary>
    /// 尝试将物品堆叠到已有相同物品的槽位
    /// </summary>
    protected virtual bool TryStackItem(IItem item, out AddItemResult result)
    {
        result = AddItemResult.NoSuitableSlotFound;

        if (!item.IsStackable)
            return false;

        foreach (var slot in _slots)
        {
            if (slot.IsOccupied && slot.Item != null && slot.Item.ID == item.ID && !slot.HasMultiSlotItem)
            {
                if (slot.Item.MaxStackCount <= 0 || slot.ItemCount < slot.Item.MaxStackCount) // 使用槽位中物品的MaxStackCount
                {
                    IItem existingItem = slot.Item;
                    if (slot.SetItem(existingItem, slot.ItemCount + 1))
                    {
                        result = AddItemResult.Success;
                        return true;
                    }
                }
            }
        }

        return false;
    }

    /// <summary>
    /// 尝试将指定数量的物品堆叠到已有相同物品的槽位
    /// </summary>
    /// <param name="item">要堆叠的物品</param>
    /// <param name="count">要堆叠的数量</param>
    /// <param name="result">堆叠结果</param>
    /// <param name="exceededCount">返回无法堆叠的数量</param>
    /// <returns>成功堆叠的数量</returns>
    protected virtual int TryStackItemWithCount(IItem item, int count, out AddItemResult result, out int exceededCount)
    {
        result = AddItemResult.NoSuitableSlotFound;
        exceededCount = 0;

        if (!item.IsStackable || count <= 0)
            return 0;

        int remainingCount = count;
        bool anyStacked = false;

        foreach (var slot in _slots)
        {
            if (slot.IsOccupied && slot.Item != null && slot.Item.ID == item.ID && !slot.HasMultiSlotItem)
            {
                if (slot.Item.MaxStackCount <= 0 || slot.ItemCount < slot.Item.MaxStackCount)
                {
                    int canAddCount;

                    if (slot.Item.MaxStackCount <= 0)
                    {
                        canAddCount = remainingCount; // 无限堆叠
                    }
                    else
                    {
                        canAddCount = Mathf.Min(remainingCount, slot.Item.MaxStackCount - slot.ItemCount);
                    }

                    if (canAddCount > 0)
                    {
                        IItem existingItem = slot.Item;
                        if (slot.SetItem(existingItem, slot.ItemCount + canAddCount))
                        {
                            remainingCount -= canAddCount;
                            anyStacked = true;

                            if (remainingCount <= 0)
                            {
                                result = AddItemResult.Success;
                                return count;
                            }
                        }
                    }
                }
            }
        }

        if (anyStacked)
        {
            result = AddItemResult.Success;
            return count - remainingCount;
        }

        return 0;
    }

    /// <summary>
    /// 检查容器中是否包含指定ID的物品
    /// </summary>
    /// <param name="itemId">物品ID</param>
    /// <returns>如果包含返回true，否则返回false</returns>
    public bool ContainsItem(string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
            return false;

        foreach (var slot in _slots)
        {
            if (slot.IsOccupied && slot.Item != null && slot.Item.ID == itemId)
                return true;
        }

        return false;
    }

    /// <summary>
    /// 获取容器中指定ID物品的总数量
    /// </summary>
    /// <param name="itemId">物品ID</param>
    /// <returns>物品总数量</returns>
    public int GetItemCount(string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
            return 0;

        int totalCount = 0;

        foreach (var slot in _slots)
        {
            if (slot.IsOccupied && slot.Item != null && slot.Item.ID == itemId)
            {
                totalCount += slot.ItemCount;
            }
        }

        return totalCount;
    }

    /// <summary>
    /// 移除指定ID的物品
    /// </summary>
    /// <param name="itemId">物品ID</param>
    /// <param name="count">移除数量</param>
    /// <returns>移除结果</returns>
    public RemoveItemResult RemoveItem(string itemId, int count = 1)
    {
        if (string.IsNullOrEmpty(itemId))
            return RemoveItemResult.InvalidItemId;

        int remainingCount = count;

        for (int i = 0; i < _slots.Count; i++)
        {
            var slot = _slots[i];
            if (slot.IsOccupied && slot.Item != null && slot.Item.ID == itemId)
            {
                int removeAmount = Mathf.Min(slot.ItemCount, remainingCount);

                if (removeAmount == slot.ItemCount)
                {
                    slot.ClearSlot();
                }
                else
                {
                    slot.SetItem(slot.Item, slot.ItemCount - removeAmount);
                }

                remainingCount -= removeAmount;

                if (remainingCount <= 0)
                    return RemoveItemResult.Success;
            }
        }

        // 如果移除了部分物品但数量不足
        if (remainingCount < count)
            return RemoveItemResult.InsufficientQuantity;

        // 如果没有找到任何物品
        return RemoveItemResult.ItemNotFound;
    }

    /// <summary>
    /// 从指定槽位移除物品
    /// </summary>
    /// <param name="index">槽位索引</param>
    /// <param name="count">移除数量</param>
    /// <returns>移除结果</returns>
    public virtual RemoveItemResult RemoveItemAtIndex(int index, int count = 1, string expectedItemId = null)
    {
        if (index < 0 || index >= _slots.Count)
            return RemoveItemResult.SlotNotFound;

        var slot = _slots[index];

        if (!slot.IsOccupied || slot.Item == null)
            return RemoveItemResult.ItemNotFound;

        // 如果提供了预期的物品ID，则验证
        if (!string.IsNullOrEmpty(expectedItemId) && slot.Item.ID != expectedItemId)
            return RemoveItemResult.InvalidItemId;

        if (slot.ItemCount < count)
            return RemoveItemResult.InsufficientQuantity;

        if (slot.ItemCount - count <= 0)
        {
            slot.ClearSlot();
        }
        else
        {
            slot.SetItem(slot.Item, slot.ItemCount - count);
        }

        return RemoveItemResult.Success;
    }
    #endregion


    /// <summary>
    /// 添加指定数量的物品到容器
    /// </summary>
    /// <param name="item">要添加的物品</param>
    /// <param name="count">要添加的数量</param>
    /// <param name="slotIndex">指定的槽位索引，-1表示自动寻找合适的槽位</param>
    /// <param name="exceededCount">超出堆叠上限的数量</param>
    /// <returns>添加结果和成功添加的数量</returns>
    public virtual (AddItemResult result, int addedCount) AddItems(IItem item, out int exceededCount, int count = 1, int slotIndex = -1)
    {
        exceededCount = 0;

        if (item == null)
            return (AddItemResult.ItemIsNull, 0);

        if (count <= 0)
            return (AddItemResult.Success, 0);

        if (!CheckContainerCondition(item))
            return (AddItemResult.ItemConditionNotMet, 0);

        int totalAdded = 0;
        int remainingCount = count;

        // 如果物品可堆叠，优先尝试堆叠
        if (item.IsStackable && slotIndex == -1)
        {
            int stackedCount = TryStackItemWithCount(item, remainingCount, out AddItemResult stackResult, out exceededCount);

            if (stackedCount > 0)
            {
                totalAdded += stackedCount;
                remainingCount -= stackedCount;

                if (remainingCount <= 0)
                    return (AddItemResult.Success, totalAdded);
            }
        }

        // 如果指定了槽位索引且还有剩余物品需要添加
        if (slotIndex >= 0 && remainingCount > 0)
        {
            if (slotIndex >= _slots.Count)
                return (AddItemResult.SlotNotFound, totalAdded);

            var targetSlot = _slots[slotIndex];

            if (!targetSlot.CheckSlotCondition(item))
                return (AddItemResult.ItemConditionNotMet, totalAdded);

            // 尝试设置物品到槽位
            int addCount = item.IsStackable && item.MaxStackCount > 0 ?
                           Mathf.Min(remainingCount, item.MaxStackCount) :
                           remainingCount;

            if (targetSlot.SetItem(item, addCount))
            {
                totalAdded += addCount;
                remainingCount -= addCount;

                if (remainingCount <= 0)
                    return (AddItemResult.Success, totalAdded);
                // 否则继续处理剩余物品
            }
            else
            {
                return (AddItemResult.NoSuitableSlotFound, totalAdded);
            }
        }

        // 处理剩余物品或不可堆叠物品，尝试放入空槽位，或者创建新槽位
        while (remainingCount > 0)
        {
            bool foundSlot = false;

            // 寻找空闲槽位
            foreach (var slot in _slots)
            {
                if (!slot.IsOccupied && slot.CheckSlotCondition(item))
                {
                    int addCount;

                    if (item.IsStackable && item.MaxStackCount > 0)
                    {
                        addCount = Mathf.Min(remainingCount, item.MaxStackCount);
                    }
                    else
                    {
                        addCount = 1; // 不可堆叠物品
                    }

                    if (slot.SetItem(item, addCount))
                    {
                        totalAdded += addCount;
                        remainingCount -= addCount;
                        foundSlot = true;

                        if (remainingCount <= 0)
                            return (AddItemResult.Success, totalAdded);

                        break; // 继续查找下一个槽位
                    }
                }
            }

            // 如果没有找到合适的槽位，但可以创建新槽位
            if (!foundSlot && (Capacity <= 0 || _slots.Count < Capacity))
            {
                var newSlot = new Slot
                {
                    Index = _slots.Count,
                    Container = this
                };

                int addCount;

                if (item.IsStackable && item.MaxStackCount > 0)
                {
                    addCount = Mathf.Min(remainingCount, item.MaxStackCount);
                }
                else
                {
                    addCount = 1; // 不可堆叠物品
                }

                if (newSlot.CheckSlotCondition(item) && newSlot.SetItem(item, addCount))
                {
                    _slots.Add(newSlot);
                    totalAdded += addCount;
                    remainingCount -= addCount;
                    foundSlot = true;

                    if (remainingCount <= 0)
                        return (AddItemResult.Success, totalAdded);
                }
            }

            // 如果没有找到合适的槽位，也不能创建新槽位，则中断添加
            if (!foundSlot)
            {
                if (totalAdded > 0)
                {
                    exceededCount = remainingCount;
                    return (AddItemResult.ContainerIsFull, totalAdded);
                }
                else
                {
                    exceededCount = count;
                    // 检查是否所有现有槽位都已被占用
                    bool noEmptySlots = !_slots.Any(s => !s.IsOccupied);
                    return (noEmptySlots ? AddItemResult.ContainerIsFull : AddItemResult.NoSuitableSlotFound, 0);
                }
            }
        }

        return (AddItemResult.Success, totalAdded);
    }

    /// <summary>
    /// 添加指定数量的物品到容器
    /// </summary>
    /// <param name="item">要添加的物品</param>
    /// <param name="count">要添加的数量</param>
    /// <param name="slotIndex">指定的槽位索引，-1表示自动寻找合适的槽位</param>
    /// <returns>添加结果和成功添加的数量</returns>
    public virtual (AddItemResult result, int addedCount) AddItems(IItem item, int count = 1, int slotIndex = -1)
    {
        return AddItems(item, out _, count, slotIndex);
    }
}