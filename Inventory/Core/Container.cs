using EasyPack;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class Container : IContainer
{
    #region 基本属性
    public string ID { get; }
    public string Name { get; }
    public string Type { get; set; } = "";
    public int Capacity { get; set; } // -1表示无限容量
    public abstract bool IsGrid { get; } // 子类实现，决定是否为网格容器

    public abstract Vector2 Grid {  get; } // 网格容器形状

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

    #region 容器事件

    /// <summary>
    /// 添加物品成功事件
    /// </summary>
    /// <param name="item">添加的物品</param>
    /// <param name="count">添加的数量</param>
    /// <param name="slotIndices">涉及的槽位索引列表</param>
    public event System.Action<IItem, int, List<int>> OnItemAdded;

    /// <summary>
    /// 添加物品失败事件
    /// </summary>
    /// <param name="item">尝试添加的物品</param>
    /// <param name="count">尝试添加的数量</param>
    /// <param name="result">失败的结果</param>
    public event System.Action<IItem, int, AddItemResult> OnItemAddFailed;

    /// <summary>
    /// 移除物品成功事件
    /// </summary>
    /// <param name="itemId">被移除物品的ID</param>
    /// <param name="count">移除的数量</param>
    /// <param name="slotIndices">涉及的槽位索引列表</param>
    public event System.Action<string, int, List<int>> OnItemRemoved;

    /// <summary>
    /// 移除物品失败事件
    /// </summary>
    /// <param name="itemId">尝试移除的物品ID</param>
    /// <param name="count">尝试移除的数量</param>
    /// <param name="result">失败的结果</param>
    public event System.Action<string, int, RemoveItemResult> OnItemRemoveFailed;

    /// <summary>
    /// 槽位数量变更事件
    /// </summary>
    /// <param name="slotIndex">变更的槽位索引</param>
    /// <param name="item">变更的物品</param>
    /// <param name="oldCount">原数量</param>
    /// <param name="newCount">新数量</param>
    public event System.Action<int, IItem, int, int> OnSlotCountChanged;


    /// <summary>
    /// 触发槽位物品数量变更事件
    /// </summary>
    protected virtual void RaiseSlotItemCountChangedEvent(int slotIndex, IItem item, int oldCount, int newCount)
    {
        OnSlotCountChanged?.Invoke(slotIndex, item, oldCount, newCount);
    }


    /// <summary>
    /// 物品总数变更事件
    /// </summary>
    /// <param name="itemId">物品ID</param>
    /// <param name="item">物品引用（可能为null，如果物品已完全移除）</param>
    /// <param name="oldTotalCount">旧总数</param>
    /// <param name="newTotalCount">新总数</param>
    public event System.Action<string, IItem, int, int> OnItemTotalCountChanged;

    private readonly Dictionary<string, int> _itemTotalCounts = new Dictionary<string, int>();

    /// <summary>
    /// 检查并触发物品总数变化事件
    /// </summary>
    protected virtual void CheckAndRaiseItemTotalCountChanged(string itemId, IItem itemRef = null)
    {
        // 获取最新总数
        int newTotal = GetItemCount(itemId);

        // 获取旧总数，如果字典中不存在则为0
        int oldTotal = _itemTotalCounts.ContainsKey(itemId) ? _itemTotalCounts[itemId] : 0;

        // 只有总数有变化才触发事件
        if (newTotal != oldTotal)
        {
            // 如果没有传入物品引用，尝试从背包中找到一个
            if (itemRef == null && newTotal > 0)
            {
                foreach (var slot in _slots)
                {
                    if (slot.IsOccupied && slot.Item != null && slot.Item.ID == itemId)
                    {
                        itemRef = slot.Item;
                        break;
                    }
                }
            }

            // 触发事件 (确保先触发事件，再更新记录)
            OnItemTotalCountChanged?.Invoke(itemId, itemRef, oldTotal, newTotal);

            // 更新记录
            if (newTotal > 0)
                _itemTotalCounts[itemId] = newTotal;
            else
                _itemTotalCounts.Remove(itemId);
        }
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
            if (Capacity <= 0)
                return false;

            if (_slots.Count < Capacity)
                return false;

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

                if (!hasStackableSlot)
                    return AddItemResult.StackLimitReached;
            }
            else
            {
                return AddItemResult.ContainerIsFull;
            }
        }

        return AddItemResult.Success;
    }
    #endregion

    #region 物品查询
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
    /// 按类型查询物品
    /// </summary>
    /// <param name="itemType">物品类型</param>
    /// <returns>符合类型的物品列表，包含槽位索引、物品引用和数量</returns>
    public List<(int slotIndex, IItem item, int count)> FindItemsByType(string itemType)
    {
        var result = new List<(int slotIndex, IItem item, int count)>();

        if (string.IsNullOrEmpty(itemType))
            return result;

        for (int i = 0; i < _slots.Count; i++)
        {
            var slot = _slots[i];
            if (slot.IsOccupied && slot.Item != null && slot.Item.Type == itemType)
            {
                result.Add((i, slot.Item, slot.ItemCount));
            }
        }

        return result;
    }

    /// <summary>
    /// 按属性查询物品
    /// </summary>
    /// <param name="attributeName">属性名称</param>
    /// <param name="attributeValue">属性值</param>
    /// <returns>符合属性条件的物品列表，包含槽位索引、物品引用和数量</returns>
    public List<(int slotIndex, IItem item, int count)> FindItemsByAttribute(string attributeName, object attributeValue)
    {
        var result = new List<(int slotIndex, IItem item, int count)>();

        if (string.IsNullOrEmpty(attributeName))
            return result;

        for (int i = 0; i < _slots.Count; i++)
        {
            var slot = _slots[i];
            if (slot.IsOccupied && slot.Item != null &&
                slot.Item.Attributes != null &&
                slot.Item.Attributes.TryGetValue(attributeName, out var value) &&
                (attributeValue == null || value.Equals(attributeValue)))
            {
                result.Add((i, slot.Item, slot.ItemCount));
            }
        }

        return result;
    }

    /// <summary>
    /// 按条件查询物品
    /// </summary>
    /// <param name="condition">条件委托</param>
    /// <returns>符合条件的物品列表，包含槽位索引、物品引用和数量</returns>
    public List<(int slotIndex, IItem item, int count)> FindItems(System.Func<IItem, bool> condition)
    {
        var result = new List<(int slotIndex, IItem item, int count)>();

        if (condition == null)
            return result;

        for (int i = 0; i < _slots.Count; i++)
        {
            var slot = _slots[i];
            if (slot.IsOccupied && slot.Item != null && condition(slot.Item))
            {
                result.Add((i, slot.Item, slot.ItemCount));
            }
        }

        return result;
    }

    /// <summary>
    /// 获取物品所在的槽位索引
    /// </summary>
    /// <param name="itemId">物品ID</param>
    /// <param name="skipEmptySlots">是否跳过数量为0的槽位</param>
    /// <returns>物品所在的槽位索引列表</returns>
    public List<int> GetItemSlotIndices(string itemId, bool skipEmptySlots = true)
    {
        var result = new List<int>();

        if (string.IsNullOrEmpty(itemId))
            return result;

        for (int i = 0; i < _slots.Count; i++)
        {
            var slot = _slots[i];
            if (slot.IsOccupied && slot.Item != null && slot.Item.ID == itemId)
            {
                if (!skipEmptySlots || slot.ItemCount > 0)
                {
                    result.Add(i);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// 获取第一个包含指定物品ID的槽位索引
    /// </summary>
    /// <param name="itemId">物品ID</param>
    /// <returns>找到的槽位索引，如果没找到返回-1</returns>
    public int GetFirstItemSlotIndex(string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
            return -1;

        for (int i = 0; i < _slots.Count; i++)
        {
            var slot = _slots[i];
            if (slot.IsOccupied && slot.Item != null && slot.Item.ID == itemId)
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// 获取容器中所有物品的ID和总数量
    /// </summary>
    /// <returns>物品ID和总数量的字典</returns>
    public Dictionary<string, int> GetAllItemCounts()
    {
        var result = new Dictionary<string, int>();

        foreach (var slot in _slots)
        {
            if (slot.IsOccupied && slot.Item != null)
            {
                string itemId = slot.Item.ID;
                int count = slot.ItemCount;

                if (result.ContainsKey(itemId))
                {
                    result[itemId] += count;
                }
                else
                {
                    result[itemId] = count;
                }
            }
        }

        return result;
    }

    /// <summary>
    /// 获取容器中所有的物品，按槽位顺序
    /// </summary>
    /// <returns>槽位索引、物品和数量的列表</returns>
    public List<(int slotIndex, IItem item, int count)> GetAllItems()
    {
        var result = new List<(int slotIndex, IItem item, int count)>();

        for (int i = 0; i < _slots.Count; i++)
        {
            var slot = _slots[i];
            if (slot.IsOccupied && slot.Item != null)
            {
                result.Add((i, slot.Item, slot.ItemCount));
            }
        }

        return result;
    }

    /// <summary>
    /// 获取容器中所有不同类型的物品数量
    /// </summary>
    /// <returns>不同类型的物品总数</returns>
    public int GetUniqueItemCount()
    {
        return GetAllItemCounts().Count;
    }

    /// <summary>
    /// 检查容器是否为空
    /// </summary>
    /// <returns>如果容器为空返回true，否则返回false</returns>
    public bool IsEmpty()
    {
        foreach (var slot in _slots)
        {
            if (slot.IsOccupied && slot.Item != null && slot.ItemCount > 0)
                return false;
        }
        return true;
    }

    /// <summary>
    /// 获取容器当前占用的总重量
    /// </summary>
    /// <returns>总重量</returns>
    public float GetTotalWeight()
    {
        float totalWeight = 0;

        foreach (var slot in _slots)
        {
            if (slot.IsOccupied && slot.Item != null)
            {
                totalWeight += slot.Item.Weight * slot.ItemCount;
            }
        }

        return totalWeight;
    }

    /// <summary>
    /// 检查容器中是否有足够数量的指定物品
    /// </summary>
    /// <param name="itemId">物品ID</param>
    /// <param name="requiredCount">需要的数量</param>
    /// <returns>如果有足够数量返回true，否则返回false</returns>
    public bool HasEnoughItems(string itemId, int requiredCount)
    {
        return GetItemCount(itemId) >= requiredCount;
    }

    /// <summary>
    /// 通过名称模糊查询物品
    /// </summary>
    /// <param name="namePattern">名称模式，支持部分匹配</param>
    /// <returns>符合名称模式的物品列表</returns>
    public List<(int slotIndex, IItem item, int count)> FindItemsByName(string namePattern)
    {
        var result = new List<(int slotIndex, IItem item, int count)>();

        if (string.IsNullOrEmpty(namePattern))
            return result;

        for (int i = 0; i < _slots.Count; i++)
        {
            var slot = _slots[i];
            if (slot.IsOccupied && slot.Item != null &&
                slot.Item.Name != null && slot.Item.Name.Contains(namePattern))
            {
                result.Add((i, slot.Item, slot.ItemCount));
            }
        }

        return result;
    }
    #endregion

    #region 移除物品
    /// <summary>
    /// 移除指定ID的物品
    /// </summary>
    /// <param name="itemId">物品ID</param>
    /// <param name="count">移除数量</param>
    /// <returns>移除结果</returns>
    public virtual RemoveItemResult RemoveItem(string itemId, int count = 1)
    {
        if (string.IsNullOrEmpty(itemId))
        {
            OnItemRemoveFailed?.Invoke(itemId, count, RemoveItemResult.InvalidItemId);
            return RemoveItemResult.InvalidItemId;
        }

        // 先检查物品总数是否足够
        int totalCount = GetItemCount(itemId);
        if (totalCount < count)
        {
            OnItemRemoveFailed?.Invoke(itemId, count, RemoveItemResult.InsufficientQuantity);
            return RemoveItemResult.InsufficientQuantity;
        }

        // 如果物品不存在
        if (totalCount == 0)
        {
            OnItemRemoveFailed?.Invoke(itemId, count, RemoveItemResult.ItemNotFound);
            return RemoveItemResult.ItemNotFound;
        }

        // 只有确认能够完全移除指定数量的物品时，才执行移除操作
        int remainingCount = count;
        List<(ISlot slot, int removeAmount, int slotIndex)> removals = new List<(ISlot, int, int)>();

        // 第一步：计算要从每个槽位移除的数量
        for (int i = 0; i < _slots.Count && remainingCount > 0; i++)
        {
            var slot = _slots[i];
            if (slot.IsOccupied && slot.Item != null && slot.Item.ID == itemId)
            {
                int removeAmount = Mathf.Min(slot.ItemCount, remainingCount);
                removals.Add((slot, removeAmount, i));
                remainingCount -= removeAmount;
            }
        }

        // 第二步：确认可以完全移除指定数量后，执行实际的移除操作
        if (remainingCount == 0)
        {
            var affectedSlots = new List<int>();

            foreach (var (slot, removeAmount, slotIndex) in removals)
            {
                int oldCount = slot.ItemCount;
                var item = slot.Item;

                if (removeAmount == slot.ItemCount)
                {
                    slot.ClearSlot();
                }
                else
                {
                    slot.SetItem(slot.Item, slot.ItemCount - removeAmount);
                }

                affectedSlots.Add(slotIndex);

                // 槽位物品数量变更事件
                RaiseSlotItemCountChangedEvent(slotIndex, item, oldCount, slot.ItemCount);
            }

            // 移除成功事件
            OnItemRemoved?.Invoke(itemId, count, affectedSlots);
            CheckAndRaiseItemTotalCountChanged(itemId);
            return RemoveItemResult.Success;
        }

        // 发生未知错误，才能悲惨得走到了这一步
        OnItemRemoveFailed?.Invoke(itemId, count, RemoveItemResult.Failed);
        return RemoveItemResult.Failed;
    }

    /// <summary>
    /// 从指定槽位移除物品
    /// </summary>
    /// <param name="index">槽位索引</param>
    /// <param name="count">移除数量</param>
    /// <param name="expectedItemId">预期物品ID，用于验证</param>
    /// <returns>移除结果</returns>
    public virtual RemoveItemResult RemoveItemAtIndex(int index, int count = 1, string expectedItemId = null)
    {
        // 检查槽位索引是否有效
        if (index < 0 || index >= _slots.Count)
        {
            OnItemRemoveFailed?.Invoke(expectedItemId ?? "unknown", count, RemoveItemResult.SlotNotFound);
            return RemoveItemResult.SlotNotFound;
        }

        var slot = _slots[index];

        // 检查槽位是否有物品
        if (!slot.IsOccupied || slot.Item == null)
        {
            OnItemRemoveFailed?.Invoke(expectedItemId ?? "unknown", count, RemoveItemResult.ItemNotFound);
            return RemoveItemResult.ItemNotFound;
        }

        // 保存物品引用和ID
        IItem item = slot.Item;
        string itemId = item.ID;

        // 如果提供了预期的物品ID，则验证
        if (!string.IsNullOrEmpty(expectedItemId) && itemId != expectedItemId)
        {
            OnItemRemoveFailed?.Invoke(expectedItemId, count, RemoveItemResult.InvalidItemId);
            return RemoveItemResult.InvalidItemId;
        }

        // 检查物品数量是否足够
        if (slot.ItemCount < count)
        {
            OnItemRemoveFailed?.Invoke(itemId, count, RemoveItemResult.InsufficientQuantity);
            return RemoveItemResult.InsufficientQuantity;
        }

        // 记录旧数量
        int oldCount = slot.ItemCount;

        // 所有检查都通过，执行移除操作
        if (slot.ItemCount - count <= 0)
        {
            slot.ClearSlot();
        }
        else
        {
            slot.SetItem(item, slot.ItemCount - count);
        }

        // 触发物品数量变更事件
        RaiseSlotItemCountChangedEvent(index, item, oldCount, slot.ItemCount);

        // 触发物品移除事件
        OnItemRemoved?.Invoke(itemId, count, new List<int> { index });
        CheckAndRaiseItemTotalCountChanged(itemId, item);

        return RemoveItemResult.Success;
    }
    #endregion

    #region 添加物品
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
        List<int> affectedSlots = new List<int>();

        if (item == null)
        {
            OnItemAddFailed?.Invoke(item, count, AddItemResult.ItemIsNull);
            return (AddItemResult.ItemIsNull, 0);
        }

        if (count <= 0)
            return (AddItemResult.Success, 0);

        if (!CheckContainerCondition(item))
        {
            OnItemAddFailed?.Invoke(item, count, AddItemResult.ItemConditionNotMet);
            return (AddItemResult.ItemConditionNotMet, 0);
        }

        int totalAdded = 0;
        int remainingCount = count;

        // 如果物品可堆叠，优先尝试堆叠
        if (item.IsStackable && slotIndex == -1)
        {
            int stackedCount = 0;
            Dictionary<int, (int oldCount, int newCount)> stackSlotChanges = new Dictionary<int, (int, int)>();

            // 尝试堆叠物品
            for (int i = 0; i < _slots.Count && remainingCount > 0; i++)
            {
                var slot = _slots[i];
                if (slot.IsOccupied && slot.Item != null && slot.Item.ID == item.ID && !slot.HasMultiSlotItem)
                {
                    if (slot.Item.MaxStackCount <= 0 || slot.ItemCount < slot.Item.MaxStackCount)
                    {
                        int oldCount = slot.ItemCount;
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
                                stackedCount += canAddCount;
                                affectedSlots.Add(i);
                                stackSlotChanges[i] = (oldCount, slot.ItemCount);

                                if (remainingCount <= 0)
                                    break;
                            }
                        }
                    }
                }
            }

            // 处理堆叠的结果
            if (stackedCount > 0)
            {
                totalAdded += stackedCount;

                // 触发数量变更和槽位变更事件
                foreach (var change in stackSlotChanges)
                {
                    int slotIdx = change.Key;
                    var slot = _slots[slotIdx];
                    RaiseSlotItemCountChangedEvent(slotIdx, slot.Item, change.Value.oldCount, change.Value.newCount);
                    CheckAndRaiseItemTotalCountChanged(item.ID, item);
                }

                if (remainingCount <= 0)
                {
                    // 全部堆叠成功
                    OnItemAdded?.Invoke(item, totalAdded, affectedSlots);
                    return (AddItemResult.Success, totalAdded);
                }
            }
        }

        // 如果指定了槽位索引且还有剩余物品需要添加
        if (slotIndex >= 0 && remainingCount > 0)
        {
            if (slotIndex >= _slots.Count)
            {
                if (totalAdded > 0)
                {
                    OnItemAdded?.Invoke(item, totalAdded, affectedSlots);
                }
                OnItemAddFailed?.Invoke(item, remainingCount, AddItemResult.SlotNotFound);
                return (AddItemResult.SlotNotFound, totalAdded);
            }

            var targetSlot = _slots[slotIndex];

            if (!targetSlot.CheckSlotCondition(item))
            {
                if (totalAdded > 0)
                {
                    OnItemAdded?.Invoke(item, totalAdded, affectedSlots);
                }
                OnItemAddFailed?.Invoke(item, remainingCount, AddItemResult.ItemConditionNotMet);
                return (AddItemResult.ItemConditionNotMet, totalAdded);
            }

            int oldCount = targetSlot.ItemCount;
            int addCount = item.IsStackable && item.MaxStackCount > 0 ?
                           Mathf.Min(remainingCount, item.MaxStackCount) :
                           remainingCount;

            if (targetSlot.SetItem(item, addCount))
            {
                totalAdded += addCount;
                remainingCount -= addCount;
                affectedSlots.Add(slotIndex);

                // 触发数量变更
                RaiseSlotItemCountChangedEvent(slotIndex, targetSlot.Item, oldCount, targetSlot.ItemCount);
                CheckAndRaiseItemTotalCountChanged(item.ID, item);

                if (remainingCount <= 0)
                {
                    OnItemAdded?.Invoke(item, totalAdded, affectedSlots);
                    return (AddItemResult.Success, totalAdded);
                }
            }
            else
            {
                if (totalAdded > 0)
                {
                    OnItemAdded?.Invoke(item, totalAdded, affectedSlots);
                }
                OnItemAddFailed?.Invoke(item, remainingCount, AddItemResult.NoSuitableSlotFound);
                return (AddItemResult.NoSuitableSlotFound, totalAdded);
            }
        }

        // 处理剩余物品或不可堆叠物品，尝试放入空槽位，或者创建新槽位
        while (remainingCount > 0)
        {
            bool foundSlot = false;

            // 寻找空闲槽位
            for (int i = 0; i < _slots.Count; i++)
            {
                var slot = _slots[i];
                if (!slot.IsOccupied && slot.CheckSlotCondition(item))
                {
                    int oldCount = 0; // 空槽位旧数量为0
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
                        affectedSlots.Add(i);

                        // 触发数量变更和槽位变更事件
                        RaiseSlotItemCountChangedEvent(i, slot.Item, oldCount, slot.ItemCount);
                        // CheckAndRaiseItemTotalCountChanged(item.ID, item);

                        if (remainingCount <= 0)
                        {
                            OnItemAdded?.Invoke(item, totalAdded, affectedSlots);
                            return (AddItemResult.Success, totalAdded);
                        }

                        break; // 继续查找下一个槽位
                    }
                }
            }

            // 如果没有找到合适的槽位，但可以创建新槽位
            if (!foundSlot && (Capacity <= 0 || _slots.Count < Capacity))
            {
                int newSlotIndex = _slots.Count;
                var newSlot = new Slot
                {
                    Index = newSlotIndex,
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
                    affectedSlots.Add(newSlotIndex);

                    // 触发数量变更和槽位变更事件
                    RaiseSlotItemCountChangedEvent(newSlotIndex, newSlot.Item, 0, newSlot.ItemCount);
                    CheckAndRaiseItemTotalCountChanged(item.ID, item);

                    if (remainingCount <= 0)
                    {
                        OnItemAdded?.Invoke(item, totalAdded, affectedSlots);
                        return (AddItemResult.Success, totalAdded);
                    }
                }
            }

            // 如果没有找到合适的槽位，也不能创建新槽位，则中断添加
            if (!foundSlot)
            {
                if (totalAdded > 0)
                {
                    exceededCount = remainingCount;
                    OnItemAdded?.Invoke(item, totalAdded, affectedSlots);
                    OnItemAddFailed?.Invoke(item, remainingCount, AddItemResult.ContainerIsFull);
                    return (AddItemResult.ContainerIsFull, totalAdded);
                }
                else
                {
                    exceededCount = count;
                    // 检查是否所有现有槽位都已被占用
                    bool noEmptySlots = !_slots.Any(s => !s.IsOccupied);
                    AddItemResult result = noEmptySlots ? AddItemResult.ContainerIsFull : AddItemResult.NoSuitableSlotFound;
                    OnItemAddFailed?.Invoke(item, count, result);
                    return (result, 0);
                }
            }
        }

        // 所有物品都成功添加
        OnItemAdded?.Invoke(item, totalAdded, affectedSlots);
        CheckAndRaiseItemTotalCountChanged(item.ID, item);
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
    #endregion
}