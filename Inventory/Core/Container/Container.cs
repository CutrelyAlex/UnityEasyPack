using EasyPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public abstract class Container : IContainer
{
    #region 基本属性
    public string ID { get; }
    public string Name { get; }
    public string Type { get; set; } = "";
    public int Capacity { get; set; } // -1表示无限容量
    public abstract bool IsGrid { get; } // 子类实现，决定是否为网格容器
    public abstract Vector2 Grid { get; } // 网格容器形状

    public List<IItemCondition> ContainerCondition { get; set; }
    protected List<ISlot> _slots = new();
    public IReadOnlyList<ISlot> Slots => _slots.AsReadOnly();

    // 缓存管理器
    protected readonly ContainerCacheManager _cacheManager;

    public Container(string id, string name, string type, int capacity = -1)
    {
        ID = id;
        Name = name;
        Type = type;
        Capacity = capacity;
        ContainerCondition = new List<IItemCondition>();

        _cacheManager = new ContainerCacheManager(capacity);
        RebuildCaches();
    }
    #endregion

    #region 容器事件

    /// <summary>
    /// 添加物品操作结果事件（统一处理成功和失败）
    /// </summary>
    /// <param name="item">操作的物品</param>
    /// <param name="requestedCount">请求添加的数量</param>
    /// <param name="actualCount">实际添加的数量</param>
    /// <param name="result">操作结果</param>
    /// <param name="affectedSlots">涉及的槽位索引列表（失败时为空列表）</param>
    public event System.Action<IItem, int, int, AddItemResult, List<int>> OnItemAddResult;

    /// <summary>
    /// 移除物品操作结果事件（统一处理成功和失败）
    /// </summary>
    /// <param name="itemId">操作的物品ID</param>
    /// <param name="requestedCount">请求移除的数量</param>
    /// <param name="actualCount">实际移除的数量</param>
    /// <param name="result">操作结果</param>
    /// <param name="affectedSlots">涉及的槽位索引列表（失败时为空列表）</param>
    public event System.Action<string, int, int, RemoveItemResult, List<int>> OnItemRemoveResult;

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
    protected virtual void OnSlotQuantityChanged(int slotIndex, IItem item, int oldCount, int newCount)
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

    private readonly Dictionary<string, int> _itemTotalCounts = new();

    /// <summary>
    /// 触发物品总数变更
    /// </summary>
    protected void TriggerItemTotalCountChanged(string itemId, IItem itemRef = null)
    {
        int newTotal = GetItemTotalCount(itemId);
        
        int oldTotal = _itemTotalCounts.TryGetValue(itemId, out int value) ? value : 0;

        // 只有总数有变化才继续处理
        if (newTotal == oldTotal) return;

        if (itemRef == null && newTotal > 0)
        {
            itemRef = GetItemReference(itemId);
        }

        OnItemTotalCountChanged?.Invoke(itemId, itemRef, oldTotal, newTotal);

        if (newTotal > 0)
            _itemTotalCounts[itemId] = newTotal;
        else
            _itemTotalCounts.Remove(itemId);
    }

    #endregion

    #region 批操作
    private readonly HashSet<string> _pendingTotalCountUpdates = new();
    private readonly Dictionary<string, IItem> _itemRefCache = new();
    private bool _batchUpdateMode = false;

    /// <summary>
    /// 开始批量操作模式
    /// </summary>
    protected void BeginBatchUpdate()
    {
        _batchUpdateMode = true;
        _pendingTotalCountUpdates.Clear();
        _itemRefCache.Clear();
    }

    /// <summary>
    /// 结束批量操作模式并处理所有待更新项
    /// </summary>
    protected void EndBatchUpdate()
    {
        if (_batchUpdateMode && _pendingTotalCountUpdates.Count > 0)
        {
            // 批量处理所有待更新的物品
            foreach (string itemId in _pendingTotalCountUpdates)
            {
                TriggerItemTotalCountChanged(itemId,
                    _itemRefCache.TryGetValue(itemId, out var itemRef) ? itemRef : null);
            }

            _pendingTotalCountUpdates.Clear();
            _itemRefCache.Clear();
        }
        _batchUpdateMode = false;
    }
    #endregion

    #region 状态检查
    // <summary>
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

            if (_cacheManager.GetEmptySlotIndices().Count > 0)
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
    public bool ValidateItemCondition(IItem item)
    {
        if (item == null)
        {
            Debug.LogWarning("ValidateItemCondition: item is null.");
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

        if (!ValidateItemCondition(item))
            return AddItemResult.ItemConditionNotMet;

        // 如果容器已满，需要检查是否有可堆叠的槽位
        if (Full)
        {
            // 如果物品可堆叠，检查是否有相同物品且未达到堆叠上限的槽位
            if (item.IsStackable)
            {
                if (_cacheManager.TryGetItemSlotIndices(item.ID, out var indices))
                {
                    foreach (int slotIndex in indices)
                    {
                        if (slotIndex < _slots.Count)
                        {
                            var slot = _slots[slotIndex];
                            if (slot.IsOccupied && slot.Item.ID == item.ID &&
                                slot.Item.IsStackable && (slot.Item.MaxStackCount <= 0 || slot.ItemCount < slot.Item.MaxStackCount))
                            {
                                return AddItemResult.Success;
                            }
                        }
                    }
                }
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

    #region 缓存相关方法
    /// <summary>
    /// 刷新物品引用缓存
    /// </summary>
    /// <param name="itemId">特定物品ID，null表示刷新所有</param>
    public void RefreshItemReferenceCache(string itemId = null)
    {
        _cacheManager.RefreshItemReferenceCache(_slots.AsReadOnly(), itemId);
    }

    /// <summary>
    /// 初始化或重建所有缓存
    /// </summary>
    public void RebuildCaches()
    {
        _cacheManager.RebuildCaches(_slots.AsReadOnly());
    }

    /// <summary>
    /// 清除缓存中的无效条目
    /// </summary>
    public void ValidateCaches()
    {
        _cacheManager.ValidateCaches(_slots.AsReadOnly());
    }
    #endregion

    #region 物品查询
    /// <summary>
    /// 检查容器中是否包含指定ID的物品
    /// </summary>
    /// <param name="itemId">物品ID</param>
    /// <returns>如果包含返回true，否则返回false</returns>
    public bool HasItem(string itemId)
    {
        return _cacheManager.HasItemInCache(itemId);
    }

    /// <summary>
    /// 获取物品引用
    /// </summary>
    private IItem GetItemReference(string itemId)
    {
        if (_cacheManager.TryGetItemSlotIndices(itemId, out var indices) && indices.Count > 0)
        {
            var slots = _slots;

            foreach (int index in indices)
            {
                if (index < slots.Count)
                {
                    var slot = slots[index];
                    if (slot.IsOccupied && slot.Item?.ID == itemId)
                    {
                        return slot.Item;
                    }
                }
            }
        }
        return null;
    }

    /// <summary>
    /// 获取容器中指定ID物品的总数量
    /// </summary>
    /// <param name="itemId">物品ID</param>
    /// <returns>物品总数量</returns>
    public int GetItemTotalCount(string itemId)
    {
        // 首先尝试使用数量缓存
        if (_cacheManager.TryGetItemCount(itemId, out int cachedCount))
        {
            return cachedCount;
        }

        // 如果缓存未命中，使用索引缓存计算
        if (_cacheManager.TryGetItemSlotIndices(itemId, out var indices))
        {
            int totalCount = 0;
            var slots = _slots;

            foreach (int index in indices)
            {
                if (index < slots.Count)
                {
                    var slot = slots[index];
                    if (slot.IsOccupied && slot.Item != null && slot.Item.ID == itemId)
                    {
                        totalCount += slot.ItemCount;
                    }
                }
            }

            // 更新缓存
            if (totalCount > 0)
                _cacheManager.UpdateItemCountCache(itemId, totalCount);

            return totalCount;
        }

        // 回退到传统方法
        int count = 0;
        for (int i = 0; i < _slots.Count; i++)
        {
            var slot = _slots[i];
            if (slot.IsOccupied && slot.Item != null && slot.Item.ID == itemId)
            {
                count += slot.ItemCount;
                // 更新缓存
                _cacheManager.UpdateItemSlotIndexCache(itemId, i, true);
            }
        }

        if (count > 0)
            _cacheManager.UpdateItemCountCache(itemId, count);

        return count;
    }

    /// <summary>
    /// 按类型查询物品
    /// </summary>
    /// <param name="itemType">物品类型</param>
    /// <returns>符合类型的物品列表，包含槽位索引、物品引用和数量</returns>
    public List<(int slotIndex, IItem item, int count)> GetItemsByType(string itemType)
    {
        var result = new List<(int slotIndex, IItem item, int count)>();
        // 使用类型索引缓存
        if (_cacheManager.TryGetItemTypeIndices(itemType, out var indices))
        {
            foreach (int index in indices)
            {
                if (index < _slots.Count)
                {
                    var slot = _slots[index];
                    if (slot.IsOccupied && slot.Item != null && slot.Item.Type == itemType)
                    {
                        result.Add((index, slot.Item, slot.ItemCount));
                    }
                }
            }
            return result;
        }

        // 缓存未命中
        for (int i = 0; i < _slots.Count; i++)
        {
            var slot = _slots[i];
            if (slot.IsOccupied && slot.Item != null && slot.Item.Type == itemType)
            {
                result.Add((i, slot.Item, slot.ItemCount));
                // 更新类型缓存
                _cacheManager.UpdateItemTypeCache(itemType, i, true);
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
    public List<(int slotIndex, IItem item, int count)> GetItemsByAttribute(string attributeName, object attributeValue)
    {
        var result = new List<(int slotIndex, IItem item, int count)>();

        var slots = _slots;
        int slotCount = slots.Count;

        // 如果槽位数量较大，使用并行处理
        if (slotCount > 100)
        {
            var lockObject = new object();
            System.Threading.Tasks.Parallel.For(0, slotCount, i =>
            {
                var slot = slots[i];
                if (slot.IsOccupied && slot.Item != null &&
                    slot.Item.Attributes != null &&
                    slot.Item.Attributes.TryGetValue(attributeName, out var value) &&
                    (attributeValue == null || value.Equals(attributeValue)))
                {
                    lock (lockObject)
                    {
                        result.Add((i, slot.Item, slot.ItemCount));
                    }
                }
            });
        }
        else
        {
            // 小规模数据使用单线程
            for (int i = 0; i < slotCount; i++)
            {
                var slot = slots[i];
                if (slot.IsOccupied && slot.Item != null &&
                    slot.Item.Attributes != null &&
                    slot.Item.Attributes.TryGetValue(attributeName, out var value) &&
                    (attributeValue == null || value.Equals(attributeValue)))
                {
                    result.Add((i, slot.Item, slot.ItemCount));
                }
            }
        }

        return result;
    }

    /// <summary>
    /// 按条件查询物品
    /// </summary>
    /// <param name="condition">条件委托</param>
    /// <returns>符合条件的物品列表，包含槽位索引、物品引用和数量</returns>
    public List<(int slotIndex, IItem item, int count)> GetItemsWhere(System.Func<IItem, bool> condition)
    {
        var result = new List<(int slotIndex, IItem item, int count)>();

        var slots = _slots;
        int slotCount = slots.Count;

        // 如果槽位数量较大，使用并行处理
        if (slotCount > 100)
        {
            var lockObject = new object();
            System.Threading.Tasks.Parallel.For(0, slotCount, i =>
            {
                var slot = slots[i];
                if (slot.IsOccupied && slot.Item != null && condition(slot.Item))
                {
                    lock (lockObject)
                    {
                        result.Add((i, slot.Item, slot.ItemCount));
                    }
                }
            });
        }
        else
        {
            // 小规模数据使用单线程
            for (int i = 0; i < slotCount; i++)
            {
                var slot = slots[i];
                if (slot.IsOccupied && slot.Item != null && condition(slot.Item))
                {
                    result.Add((i, slot.Item, slot.ItemCount));
                }
            }
        }

        return result;
    }

    /// <summary>
    /// 获取物品所在的槽位索引
    /// </summary>
    /// <param name="itemId">物品ID</param>
    /// <returns>物品所在的槽位索引列表</returns>
    public List<int> FindSlotIndices(string itemId)
    {
        // 使用缓存
        if (_cacheManager.TryGetItemSlotIndices(itemId, out var indices))
        {
            // 验证缓存有效性
            var validIndices = new List<int>(indices.Count);
            bool needsUpdate = false;

            foreach (int idx in indices)
            {
                if (idx < _slots.Count)
                {
                    var slot = _slots[idx];
                    if (slot.IsOccupied && slot.Item != null && slot.Item.ID == itemId)
                    {
                        validIndices.Add(idx);
                    }
                    else
                    {
                        needsUpdate = true;
                    }
                }
                else
                {
                    needsUpdate = true;
                }
            }

            // 如果需要更新缓存
            if (needsUpdate)
            {
                foreach (int idx in indices)
                {
                    if (idx >= _slots.Count || !_slots[idx].IsOccupied ||
                        _slots[idx].Item == null || _slots[idx].Item.ID != itemId)
                    {
                        _cacheManager.UpdateItemSlotIndexCache(itemId, idx, false);
                    }
                }
            }

            return validIndices;
        }

        // 缓存未命中，使用原始方法并更新缓存
        var result = new List<int>();
        for (int i = 0; i < _slots.Count; i++)
        {
            var slot = _slots[i];
            if (slot.IsOccupied && slot.Item != null && slot.Item.ID == itemId)
            {
                result.Add(i);
                // 更新缓存
                _cacheManager.UpdateItemSlotIndexCache(itemId, i, true);
            }
        }
        return result;
    }

    /// <summary>
    /// 获取第一个包含指定物品ID的槽位索引
    /// </summary>
    /// <param name="itemId">物品ID</param>
    /// <returns>找到的槽位索引，如果没找到返回-1</returns>
    public int FindFirstSlotIndex(string itemId)
    {
        // 使用缓存快速查找
        if (_cacheManager.TryGetItemSlotIndices(itemId, out var indices) && indices.Count > 0)
        {
            int firstIndex = indices.Min();
            if (firstIndex < _slots.Count)
            {
                var slot = _slots[firstIndex];
                if (slot.IsOccupied && slot.Item != null && slot.Item.ID == itemId)
                {
                    return firstIndex;
                }
            }
        }

        // 回退到传统方法
        for (int i = 0; i < _slots.Count; i++)
        {
            var slot = _slots[i];
            if (slot.IsOccupied && slot.Item != null && slot.Item.ID == itemId)
            {
                // 更新缓存
                _cacheManager.UpdateItemSlotIndexCache(itemId, i, true);
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// 获取容器中所有物品的ID和总数量
    /// </summary>
    /// <returns>物品ID和总数量的字典</returns>
    public Dictionary<string, int> GetAllItemCountsDict()
    {
        // 如果数量缓存完整，直接返回缓存副本
        var cachedCounts = _cacheManager.GetAllItemCounts();
        if (cachedCounts.Count > 0)
        {
            var result = new Dictionary<string, int>(cachedCounts);

            // 验证缓存是否完整
            bool cacheComplete = true;
            foreach (var slot in _slots)
            {
                if (slot.IsOccupied && slot.Item != null)
                {
                    if (!result.ContainsKey(slot.Item.ID))
                    {
                        cacheComplete = false;
                        break;
                    }
                }
            }

            if (cacheComplete)
                return result;
        }

        // 重新计算并更新缓存
        var counts = new Dictionary<string, int>();
        foreach (var slot in _slots)
        {
            if (slot.IsOccupied && slot.Item != null)
            {
                string itemId = slot.Item.ID;
                int count = slot.ItemCount;

                if (counts.ContainsKey(itemId))
                {
                    counts[itemId] += count;
                }
                else
                {
                    counts[itemId] = count;
                }
            }
        }

        RebuildCaches();

        return counts;
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
        return GetAllItemCountsDict().Count;
    }

    /// <summary>
    /// 检查容器是否为空
    /// </summary>
    /// <returns>如果容器为空返回true，否则返回false</returns>
    public bool IsEmpty()
    {
        return _cacheManager.GetCachedItemCount() == 0;
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
        return GetItemTotalCount(itemId) >= requiredCount;
    }

    /// <summary>
    /// 通过名称模糊查询物品
    /// </summary>
    /// <param name="namePattern">名称模式，支持部分匹配</param>
    /// <returns>符合名称模式的物品列表</returns>
    public List<(int slotIndex, IItem item, int count)> GetItemsByName(string namePattern)
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
        var emptySlots = new List<int>();
        if (string.IsNullOrEmpty(itemId))
        {
            OnItemRemoveResult?.Invoke(itemId, count, 0, RemoveItemResult.InvalidItemId, emptySlots);
            return RemoveItemResult.InvalidItemId;
        }

        // 先检查物品总数是否足够
        int totalCount = GetItemTotalCount(itemId);
        if (totalCount < count && totalCount != 0)
        {
            OnItemRemoveResult?.Invoke(itemId, count, 0, RemoveItemResult.InsufficientQuantity, emptySlots);
            return RemoveItemResult.InsufficientQuantity;
        }

        // 如果物品不存在
        if (totalCount == 0)
        {
            OnItemRemoveResult?.Invoke(itemId, count, 0, RemoveItemResult.ItemNotFound, emptySlots);
            return RemoveItemResult.ItemNotFound;
        }

        // 只有确认能够完全移除指定数量的物品时，才执行移除操作
        int remainingCount = count;
        List<(ISlot slot, int removeAmount, int slotIndex)> removals = new();

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

        var affectedSlots = new List<int>();
        // 第二步：确认可以完全移除指定数量后，执行实际的移除操作
        if (remainingCount == 0)
        {
            bool itemCompletelyRemoved = false;

            foreach (var (slot, removeAmount, slotIndex) in removals)
            {
                int oldCount = slot.ItemCount;
                var item = slot.Item;
                string itemType = item.Type;

                if (removeAmount == slot.ItemCount)
                {
                    slot.ClearSlot();
                    _cacheManager.UpdateEmptySlotCache(slotIndex, true);
                    _cacheManager.UpdateItemSlotIndexCache(itemId, slotIndex, false);
                    _cacheManager.UpdateItemTypeCache(itemType, slotIndex, false);
                    if (!itemCompletelyRemoved)
                    {
                        itemCompletelyRemoved = !_cacheManager.HasItemInCache(itemId);
                    }
                }
                else
                {
                    slot.SetItem(slot.Item, slot.ItemCount - removeAmount);
                }

                // 更新数量缓存
                _cacheManager.UpdateItemCountCache(itemId, -removeAmount);

                affectedSlots.Add(slotIndex);

                // 槽位物品数量变更事件
                OnSlotQuantityChanged(slotIndex, item, oldCount, slot.ItemCount);
            }
            // 如果物品完全移除，清除引用缓存
            if (itemCompletelyRemoved)
            {
                _cacheManager.UpdateItemReferenceCache(itemId, null);
            }

            // 移除成功事件
            OnItemRemoveResult?.Invoke(itemId, count, count, RemoveItemResult.Success, affectedSlots);
            TriggerItemTotalCountChanged(itemId);
            return RemoveItemResult.Success;
        }

        // 发生未知错误，才能悲惨得走到了这一步
        OnItemRemoveResult?.Invoke(itemId, count, 0, RemoveItemResult.Failed, emptySlots);
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
        var emptySlots = new List<int>();
        // 检查槽位索引是否有效
        if (index < 0 || index >= _slots.Count)
        {
            OnItemRemoveResult?.Invoke(expectedItemId ?? "unknown", count, 0, RemoveItemResult.SlotNotFound, emptySlots);
            return RemoveItemResult.SlotNotFound;
        }

        var slot = _slots[index];

        // 检查槽位是否有物品
        if (!slot.IsOccupied || slot.Item == null)
        {
            OnItemRemoveResult?.Invoke(expectedItemId ?? "unknown", count, 0, RemoveItemResult.ItemNotFound, emptySlots);
            return RemoveItemResult.ItemNotFound;
        }

        // 保存物品引用和ID
        IItem item = slot.Item;
        string itemId = item.ID;
        string itemType = item.Type;

        // 如果提供了预期的物品ID，则验证
        if (!string.IsNullOrEmpty(expectedItemId) && itemId != expectedItemId)
        {
            OnItemRemoveResult?.Invoke(expectedItemId, count, 0, RemoveItemResult.InvalidItemId, emptySlots);
            return RemoveItemResult.InvalidItemId;
        }

        // 检查物品数量是否足够
        if (slot.ItemCount < count)
        {
            OnItemRemoveResult?.Invoke(itemId, count, 0, RemoveItemResult.InsufficientQuantity, emptySlots);
            return RemoveItemResult.InsufficientQuantity;
        }

        // 记录旧数量
        int oldCount = slot.ItemCount;

        // 所有检查都通过，执行移除操作
        if (slot.ItemCount - count <= 0)
        {
            slot.ClearSlot();
            _cacheManager.UpdateEmptySlotCache(index, true);
            _cacheManager.UpdateItemSlotIndexCache(itemId, index, false);
            _cacheManager.UpdateItemTypeCache(itemType, index, false);
        }
        else
        {
            slot.SetItem(item, slot.ItemCount - count);
        }

        // 更新数量缓存
        _cacheManager.UpdateItemCountCache(itemId, -count);

        // 触发物品数量变更事件
        OnSlotQuantityChanged(index, item, oldCount, slot.ItemCount);

        // 触发物品移除事件
        var affectedSlots = new List<int> { index };
        OnItemRemoveResult?.Invoke(itemId, count, count, RemoveItemResult.Success, affectedSlots);
        TriggerItemTotalCountChanged(itemId, item);

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
    public virtual (AddItemResult result, int addedCount)
    AddItemsWithCount(IItem item, out int exceededCount, int count = 1, int slotIndex = -1)
    {
        exceededCount = 0;
        List<int> affectedSlots = new(12);
        var emptySlots = new List<int>();

        // 基本验证
        if (item == null)
        {
            OnItemAddResult?.Invoke(item, count, 0, AddItemResult.ItemIsNull, emptySlots);
            return (AddItemResult.ItemIsNull, 0);
        }

        if (count <= 0)
            return (AddItemResult.AddNothingLOL, 0);

        if (!ValidateItemCondition(item))
        {
            OnItemAddResult?.Invoke(item, count, 0, AddItemResult.ItemConditionNotMet, emptySlots);
            return (AddItemResult.ItemConditionNotMet, 0);
        }
        // 开始批量更新模式
        BeginBatchUpdate();

        try
        {
            int totalAdded = 0;
            int remainingCount = count;

            // 将物品添加到待更新列表并缓存物品引用
            _pendingTotalCountUpdates.Add(item.ID);
            _itemRefCache[item.ID] = item;

            // 1. 堆叠处理
            if (item.IsStackable && slotIndex == -1)
            {
                var (stackedCount, stackedSlots, slotChanges) = TryStackItems(item, remainingCount);

                if (stackedCount > 0)
                {
                    totalAdded += stackedCount;
                    remainingCount -= stackedCount;
                    affectedSlots.AddRange(stackedSlots);

                    _cacheManager.UpdateItemCountCache(item.ID, stackedCount);

                    // 批量事件触发
                    foreach (var change in slotChanges)
                    {
                        int slotIdx = change.Key;
                        var slot = _slots[slotIdx];
                        OnSlotQuantityChanged(slotIdx, slot.Item, change.Value.oldCount, change.Value.newCount);
                    }

                    if (remainingCount <= 0)
                    {
                        OnItemAddResult?.Invoke(item, count, totalAdded, AddItemResult.Success, affectedSlots);
                        return (AddItemResult.Success, totalAdded);
                    }
                }
            }

            // 2. 指定槽位处理
            if (slotIndex >= 0 && remainingCount > 0)
            {
                var (success, addedCount, newRemaining) = TryAddToSpecificSlot(item, slotIndex, remainingCount);

                if (success)
                {
                    totalAdded += addedCount;
                    remainingCount = newRemaining;
                    affectedSlots.Add(slotIndex);

                    _cacheManager.UpdateItemCountCache(item.ID, addedCount);
                    _cacheManager.UpdateItemSlotIndexCache(item.ID, slotIndex, true);
                    _cacheManager.UpdateItemTypeCache(item.Type, slotIndex, true);
                    _cacheManager.UpdateEmptySlotCache(slotIndex, false);

                    if (remainingCount <= 0)
                    {
                        OnItemAddResult?.Invoke(item, count, totalAdded, AddItemResult.Success, affectedSlots);
                        return (AddItemResult.Success, totalAdded);
                    }
                }
                else
                {
                    if (totalAdded > 0)
                    {
                        OnItemAddResult?.Invoke(item, count, totalAdded, AddItemResult.Success, affectedSlots);
                    }
                    OnItemAddResult?.Invoke(item, remainingCount, 0, AddItemResult.NoSuitableSlotFound, emptySlots);
                    return (AddItemResult.NoSuitableSlotFound, totalAdded);
                }
            }

            // 3. 空槽位和新槽位处理
            while (remainingCount > 0)
            {
                var (emptySlotSuccess, emptyAddedCount, emptyRemaining, emptySlotIndex) =
                    TryAddToEmptySlot(item, remainingCount);

                if (emptySlotSuccess)
                {
                    totalAdded += emptyAddedCount;
                    remainingCount = emptyRemaining;
                    affectedSlots.Add(emptySlotIndex);

                    _cacheManager.UpdateItemCountCache(item.ID, emptyAddedCount);
                    _cacheManager.UpdateItemSlotIndexCache(item.ID, emptySlotIndex, true);
                    _cacheManager.UpdateItemTypeCache(item.Type, emptySlotIndex, true);

                    if (remainingCount <= 0)
                    {
                        OnItemAddResult?.Invoke(item, count, totalAdded, AddItemResult.Success, affectedSlots);

                        return (AddItemResult.Success, totalAdded);
                    }
                    continue;
                }

                var (newSlotSuccess, newAddedCount, newRemaining, newSlotIndex) =
                    TryAddToNewSlot(item, remainingCount);

                if (newSlotSuccess)
                {
                    totalAdded += newAddedCount;
                    remainingCount = newRemaining;
                    affectedSlots.Add(newSlotIndex);

                    _cacheManager.UpdateItemCountCache(item.ID, newAddedCount);
                    _cacheManager.UpdateItemSlotIndexCache(item.ID, newSlotIndex, true);
                    _cacheManager.UpdateItemTypeCache(item.Type, newSlotIndex, true);

                    if (remainingCount <= 0)
                    {
                        OnItemAddResult?.Invoke(item, count, totalAdded, AddItemResult.Success, affectedSlots);
                        return (AddItemResult.Success, totalAdded);
                    }
                    continue;
                }

                // 无法继续添加
                if (totalAdded > 0)
                {
                    exceededCount = remainingCount;
                    OnItemAddResult?.Invoke(item, count, totalAdded, AddItemResult.Success, affectedSlots);
                    OnItemAddResult?.Invoke(item, remainingCount, 0, AddItemResult.ContainerIsFull, emptySlots);
                    return (AddItemResult.ContainerIsFull, totalAdded);
                }
                else
                {
                    exceededCount = count;
                    bool noEmptySlots = _cacheManager.GetEmptySlotIndices().Count == 0;
                    AddItemResult result = noEmptySlots ? AddItemResult.ContainerIsFull : AddItemResult.NoSuitableSlotFound;
                    OnItemAddResult?.Invoke(item, count, 0, result, emptySlots);
                    return (result, 0);
                }
            }

            OnItemAddResult?.Invoke(item, count, totalAdded, AddItemResult.Success, affectedSlots);

            return (AddItemResult.Success, totalAdded);
        }
        finally
        {
            // 结束批量更新，统一处理所有待更新项
            EndBatchUpdate();
        }
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
        return AddItemsWithCount(item, out _, count, slotIndex);
    }

    /// <summary>
    /// 异步添加物品
    /// </summary>
    public async Task<(AddItemResult result, int addedCount)> AddItemsAsync(
        IItem item, int count, CancellationToken cancellationToken = default)
    {
        if (count > 10000 || _slots.Count > 100000)
        {
            return await Task.Run(() => AddItems(item, count), cancellationToken);
        }

        return AddItems(item, count);
    }

    /// <summary>
    /// 批量添加多种物品
    /// </summary>
    /// <param name="itemsToAdd">要添加的物品和数量列表</param>
    /// <returns>每个物品的添加结果</returns>
    public virtual List<(IItem item, AddItemResult result, int addedCount, int exceededCount)> AddItemsBatch(
        List<(IItem item, int count)> itemsToAdd)
    {
        var results = new List<(IItem item, AddItemResult result, int addedCount, int exceededCount)>();

        if (itemsToAdd == null || itemsToAdd.Count == 0)
            return results;

        // 开始批量更新模式
        BeginBatchUpdate();

        try
        {
            foreach (var (item, count) in itemsToAdd)
            {
                var (result, addedCount) = AddItemsWithCount(item, out int exceededCount, count);
                results.Add((item, result, addedCount, exceededCount));
            }
        }
        finally
        {
            // 结束批量更新，统一处理所有待更新项
            EndBatchUpdate();
        }

        return results;
    }
    #endregion

    #region 中间处理API

    /// <summary>
    /// 尝试将物品堆叠到已有相同物品的槽位中 - 极致优化版本
    /// </summary>
    protected virtual (int stackedCount, List<int> affectedSlots, Dictionary<int, (int oldCount, int newCount)> changes)
        TryStackItems(IItem item, int remainingCount)
    {
        // 早期退出
        if (remainingCount <= 0 || !item.IsStackable)
            return (0, new List<int>(0), new Dictionary<int, (int oldCount, int newCount)>(0));

        int maxStack;
        if (!_cacheManager.TryGetItemMaxStack(item.ID, out maxStack))
        {
            maxStack = item.MaxStackCount;
            _cacheManager.SetItemMaxStack(item.ID, maxStack);
        }

        if (maxStack <= 1 || !_cacheManager.TryGetItemSlotIndices(item.ID, out var indices) || indices.Count == 0)
            return (0, new List<int>(0), new Dictionary<int, (int oldCount, int newCount)>(0));

        // 数组池化
        int estimatedSize = Math.Min(indices.Count, 16);
        var affectedSlots = new List<int>(estimatedSize);
        var slotChanges = new Dictionary<int, (int oldCount, int newCount)>(estimatedSize);

        // 收集有效槽位信息
        bool isInfiniteStack = maxStack <= 0;
        var stackableSlots = new List<(int index, int space)>(Math.Min(indices.Count, 64));

        foreach (int idx in indices)
        {
            if (idx >= _slots.Count) continue;

            var slot = _slots[idx];
            if (!slot.IsOccupied || slot.Item == null) continue;

            int availSpace = isInfiniteStack ? remainingCount : (maxStack - slot.ItemCount);
            if (availSpace <= 0) continue;

            stackableSlots.Add((idx, availSpace));
        }

        if (stackableSlots.Count > 20)
        {
            // 按可用空间降序排序，优先填满大空间槽位
            stackableSlots.Sort((a, b) => b.space.CompareTo(a.space));
        }

        // 堆叠实现
        int stackedCount = 0;
        int currentRemaining = remainingCount;

        for (int i = 0; i < stackableSlots.Count && currentRemaining > 0; i++)
        {
            var (slotIndex, availSpace) = stackableSlots[i];
            var slot = _slots[slotIndex];

            int oldCount = slot.ItemCount;
            int actualAdd = Math.Min(availSpace, currentRemaining);

            if (slot.SetItem(slot.Item, oldCount + actualAdd))
            {
                currentRemaining -= actualAdd;
                stackedCount += actualAdd;
                affectedSlots.Add(slotIndex);
                slotChanges[slotIndex] = (oldCount, slot.ItemCount);
            }
        }

        return (stackedCount, affectedSlots, slotChanges);
    }

    /// <summary>
    /// 尝试将物品添加到指定槽位
    /// </summary>
    protected virtual (bool success, int addedCount, int remainingCount)
    TryAddToSpecificSlot(IItem item, int slotIndex, int remainingCount)
    {
        if (slotIndex >= _slots.Count)
        {
            return (false, 0, remainingCount);
        }

        var targetSlot = _slots[slotIndex];

        // 如果槽位已被占用，检查是否可以堆叠物品
        if (targetSlot.IsOccupied)
        {
            if (targetSlot.Item.ID != item.ID)
            {
                return (false, 0, remainingCount);
            }

            if (!item.IsStackable)
            {
                return (false, 0, remainingCount);
            }

            // 计算可添加数量
            int oldCount = targetSlot.ItemCount;
            int canAddCount;

            if (item.MaxStackCount <= 0)
            {
                canAddCount = remainingCount; // 无限堆叠
            }
            else
            {
                // 考虑槽位已有数量，确保不超过最大堆叠数
                canAddCount = Mathf.Min(remainingCount, item.MaxStackCount - targetSlot.ItemCount);
                if (canAddCount <= 0)
                {
                    return (false, 0, remainingCount); // 已达到最大堆叠数
                }
            }

            // 设置物品
            if (targetSlot.SetItem(targetSlot.Item, targetSlot.ItemCount + canAddCount))
            {
                // 触发数量变更
                OnSlotQuantityChanged(slotIndex, targetSlot.Item, oldCount, targetSlot.ItemCount);
                return (true, canAddCount, remainingCount - canAddCount);
            }
        }
        else
        {
            // 槽位为空，直接添加
            if (!targetSlot.CheckSlotCondition(item))
            {
                return (false, 0, remainingCount);
            }

            int addCount = item.IsStackable && item.MaxStackCount > 0 ?
                           Mathf.Min(remainingCount, item.MaxStackCount) :
                           remainingCount;

            if (targetSlot.SetItem(item, addCount))
            {
                // 触发数量变更
                OnSlotQuantityChanged(slotIndex, targetSlot.Item, 0, targetSlot.ItemCount);
                return (true, addCount, remainingCount - addCount);
            }
        }

        return (false, 0, remainingCount);
    }

    protected virtual (bool success, int addedCount, int remainingCount, int slotIndex)
        TryAddToEmptySlot(IItem item, int remainingCount)
    {
        bool isStackable = item.IsStackable;

        int maxStack;
        if (isStackable)
        {
            if (!_cacheManager.TryGetItemMaxStack(item.ID, out maxStack))
            {
                maxStack = item.MaxStackCount;
                _cacheManager.SetItemMaxStack(item.ID, maxStack);
            }
        }
        else
        {
            maxStack = 1;
        }

        // 可以添加的数量
        int addCount = isStackable && maxStack > 0
            ? Math.Min(remainingCount, maxStack)
            : (isStackable ? remainingCount : 1);

        var emptySlotIndices = _cacheManager.GetEmptySlotIndices();

        // 使用空槽位缓存
        foreach (int i in emptySlotIndices)
        {
            if (i >= _slots.Count) continue;

            var slot = _slots[i];
            if (slot.IsOccupied) continue;

            if (!slot.CheckSlotCondition(item)) continue;

            if (slot.SetItem(item, addCount))
            {
                // 批量更新缓存
                _cacheManager.UpdateEmptySlotCache(i, false);
                _cacheManager.UpdateItemSlotIndexCache(item.ID, i, true);
                _cacheManager.UpdateItemTypeCache(item.Type, i, true);
                _cacheManager.UpdateItemReferenceCache(item.ID, item);

                OnSlotQuantityChanged(i, slot.Item, 0, slot.ItemCount);
                return (true, addCount, remainingCount - addCount, i);
            }
        }

        var emptySlotSet = new HashSet<int>(emptySlotIndices);

        for (int i = 0; i < _slots.Count; i++)
        {
            if (emptySlotSet.Contains(i)) continue; // 已在刚才检查过的槽位跳过

            var slot = _slots[i];
            if (slot.IsOccupied || !slot.CheckSlotCondition(item)) continue;

            if (slot.SetItem(item, addCount))
            {
                // 更新缓存状态
                _cacheManager.UpdateItemSlotIndexCache(item.ID, i, true);
                _cacheManager.UpdateItemTypeCache(item.Type, i, true);
                _cacheManager.UpdateItemReferenceCache(item.ID, item);

                OnSlotQuantityChanged(i, slot.Item, 0, slot.ItemCount);
                return (true, addCount, remainingCount - addCount, i);
            }
        }

        return (false, 0, remainingCount, -1);
    }

    /// <summary>
    /// 尝试创建新槽位并添加物品
    /// </summary>
    protected virtual (bool success, int addedCount, int remainingCount, int slotIndex)
        TryAddToNewSlot(IItem item, int remainingCount)
    {
        if (Capacity <= 0 || _slots.Count < Capacity)
        {
            int newSlotIndex = _slots.Count;
            var newSlot = new Slot
            {
                Index = newSlotIndex,
                Container = this
            };

            int addCount = item.IsStackable && item.MaxStackCount > 0 ?
                          Mathf.Min(remainingCount, item.MaxStackCount) :
                          1; // 不可堆叠物品

            if (newSlot.CheckSlotCondition(item) && newSlot.SetItem(item, addCount))
            {
                _slots.Add(newSlot);

                // 更新缓存
                _cacheManager.UpdateItemSlotIndexCache(item.ID, newSlotIndex, true);
                _cacheManager.UpdateItemTypeCache(item.Type, newSlotIndex, true);
                _cacheManager.UpdateItemReferenceCache(item.ID, item);

                // 触发数量变更
                OnSlotQuantityChanged(newSlotIndex, newSlot.Item, 0, newSlot.ItemCount);
                return (true, addCount, remainingCount - addCount, newSlotIndex);
            }
        }

        return (false, 0, remainingCount, -1);
    }
    #endregion
}