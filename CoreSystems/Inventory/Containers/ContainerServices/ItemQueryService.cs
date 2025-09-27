using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EasyPack
{
    /// <summary>
    /// 物品查询服务接口
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

    /// <summary>
    /// 物品查询服务实现
    /// </summary>
    public class ItemQueryService : IItemQueryService
    {
        private readonly IReadOnlyList<ISlot> _slots;
        private readonly ContainerCacheService _cacheManager;

        public ItemQueryService(IReadOnlyList<ISlot> slots, ContainerCacheService cacheManager)
        {
            _slots = slots ?? throw new ArgumentNullException(nameof(slots));
            _cacheManager = cacheManager ?? throw new ArgumentNullException(nameof(cacheManager));
        }

        #region 基础查询

        public bool HasItem(string itemId)
        {
            return _cacheManager.HasItemInCache(itemId);
        }

        public IItem GetItemReference(string itemId)
        {
            if (_cacheManager.TryGetItemSlotIndices(itemId, out var indices) && indices.Count > 0)
            {
                foreach (int index in indices)
                {
                    if (index < _slots.Count)
                    {
                        var slot = _slots[index];
                        if (slot.IsOccupied && slot.Item?.ID == itemId)
                        {
                            return slot.Item;
                        }
                    }
                }
            }
            return null;
        }

        public int GetItemTotalCount(string itemId)
        {
            // 首先尝试使用数量缓存
            if (_cacheManager.TryGetItemCount(itemId, out int cachedCount))
            {
                return cachedCount;
            }

            // 如果缓存未命中，使用槽位索引缓存
            if (_cacheManager.TryGetItemSlotIndices(itemId, out var indices))
            {
                int totalCount = 0;

                foreach (int index in indices)
                {
                    if (index < _slots.Count)
                    {
                        var slot = _slots[index];
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

            // 降级到遍历统计
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

        public bool HasEnoughItems(string itemId, int requiredCount)
        {
            return GetItemTotalCount(itemId) >= requiredCount;
        }

        #endregion

        #region 位置查询

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

            // 缓存未命中，使用原始遍历并更新缓存
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

            // 降级到遍历统计
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

        #endregion

        #region 高级查询

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

        public List<(int slotIndex, IItem item, int count)> GetItemsByAttribute(string attributeName, object attributeValue)
        {
            var result = new List<(int slotIndex, IItem item, int count)>();
            int slotCount = _slots.Count;

            // 如果槽位数量较大使用并行处理
            if (slotCount > 100)
            {
                var lockObject = new object();
                Parallel.For(0, slotCount, i =>
                {
                    var slot = _slots[i];
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
                    var slot = _slots[i];
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

        public List<(int slotIndex, IItem item, int count)> GetItemsWhere(Func<IItem, bool> condition)
        {
            var result = new List<(int slotIndex, IItem item, int count)>();
            int slotCount = _slots.Count;

            // 如果槽位数量较大使用并行处理
            if (slotCount > 100)
            {
                var lockObject = new object();
                Parallel.For(0, slotCount, i =>
                {
                    var slot = _slots[i];
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
                    var slot = _slots[i];
                    if (slot.IsOccupied && slot.Item != null && condition(slot.Item))
                    {
                        result.Add((i, slot.Item, slot.ItemCount));
                    }
                }
            }

            return result;
        }

        #endregion

        #region 聚合查询

        public Dictionary<string, int> GetAllItemCountsDict()
        {
            // 如果缓存存在且完整，直接返回缓存副本
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

            return counts;
        }

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

        public int GetUniqueItemCount()
        {
            return GetAllItemCountsDict().Count;
        }

        public bool IsEmpty()
        {
            return _cacheManager.GetCachedItemCount() == 0;
        }

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

        #endregion
    }
}