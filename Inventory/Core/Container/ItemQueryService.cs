using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EasyPack
{
    /// <summary>
    /// ��Ʒ��ѯ����ӿ�
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
    /// ��Ʒ��ѯ����ʵ��
    /// </summary>
    public class ItemQueryService : IItemQueryService
    {
        private readonly IReadOnlyList<ISlot> _slots;
        private readonly ContainerCacheManager _cacheManager;

        public ItemQueryService(IReadOnlyList<ISlot> slots, ContainerCacheManager cacheManager)
        {
            _slots = slots ?? throw new ArgumentNullException(nameof(slots));
            _cacheManager = cacheManager ?? throw new ArgumentNullException(nameof(cacheManager));
        }

        #region ������ѯ

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
            // ���ȳ���ʹ����������
            if (_cacheManager.TryGetItemCount(itemId, out int cachedCount))
            {
                return cachedCount;
            }

            // �������δ���У�ʹ�������������
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

                // ���»���
                if (totalCount > 0)
                    _cacheManager.UpdateItemCountCache(itemId, totalCount);

                return totalCount;
            }

            // ���˵���ͳ����
            int count = 0;
            for (int i = 0; i < _slots.Count; i++)
            {
                var slot = _slots[i];
                if (slot.IsOccupied && slot.Item != null && slot.Item.ID == itemId)
                {
                    count += slot.ItemCount;
                    // ���»���
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

        #region ������ѯ

        public List<int> FindSlotIndices(string itemId)
        {
            // ʹ�û���
            if (_cacheManager.TryGetItemSlotIndices(itemId, out var indices))
            {
                // ��֤������Ч��
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

                // �����Ҫ���»���
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

            // ����δ���У�ʹ��ԭʼ���������»���
            var result = new List<int>();
            for (int i = 0; i < _slots.Count; i++)
            {
                var slot = _slots[i];
                if (slot.IsOccupied && slot.Item != null && slot.Item.ID == itemId)
                {
                    result.Add(i);
                    // ���»���
                    _cacheManager.UpdateItemSlotIndexCache(itemId, i, true);
                }
            }
            return result;
        }

        public int FindFirstSlotIndex(string itemId)
        {
            // ʹ�û�����ٲ���
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

            // ���˵���ͳ����
            for (int i = 0; i < _slots.Count; i++)
            {
                var slot = _slots[i];
                if (slot.IsOccupied && slot.Item != null && slot.Item.ID == itemId)
                {
                    // ���»���
                    _cacheManager.UpdateItemSlotIndexCache(itemId, i, true);
                    return i;
                }
            }

            return -1;
        }

        #endregion

        #region ���Ӳ�ѯ

        public List<(int slotIndex, IItem item, int count)> GetItemsByType(string itemType)
        {
            var result = new List<(int slotIndex, IItem item, int count)>();

            // ʹ��������������
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

            // ����δ����
            for (int i = 0; i < _slots.Count; i++)
            {
                var slot = _slots[i];
                if (slot.IsOccupied && slot.Item != null && slot.Item.Type == itemType)
                {
                    result.Add((i, slot.Item, slot.ItemCount));
                    // �������ͻ���
                    _cacheManager.UpdateItemTypeCache(itemType, i, true);
                }
            }

            return result;
        }

        public List<(int slotIndex, IItem item, int count)> GetItemsByAttribute(string attributeName, object attributeValue)
        {
            var result = new List<(int slotIndex, IItem item, int count)>();
            int slotCount = _slots.Count;

            // �����λ�����ϴ�ʹ�ò��д���
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
                // С��ģ����ʹ�õ��߳�
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

            // �����λ�����ϴ�ʹ�ò��д���
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
                // С��ģ����ʹ�õ��߳�
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

        #region �ۺϲ�ѯ

        public Dictionary<string, int> GetAllItemCountsDict()
        {
            // �����������������ֱ�ӷ��ػ��渱��
            var cachedCounts = _cacheManager.GetAllItemCounts();
            if (cachedCounts.Count > 0)
            {
                var result = new Dictionary<string, int>(cachedCounts);

                // ��֤�����Ƿ�����
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