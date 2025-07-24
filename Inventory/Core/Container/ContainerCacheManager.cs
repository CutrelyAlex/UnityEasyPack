using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EasyPack
{
    /// <summary>
    /// ���������������������������ĸ��ֻ������
    /// </summary>
    public class ContainerCacheManager
    {
        #region �����ֶ�
        // ��Ʒ��������
        private readonly Dictionary<string, HashSet<int>> _itemSlotIndexCache = new();

        // �ղ�λ����
        private readonly SortedSet<int> _emptySlotIndices = new();

        // ��Ʒ������������
        private readonly Dictionary<string, HashSet<int>> _itemTypeIndexCache = new();

        // ��Ʒ��������
        private readonly Dictionary<string, int> _itemCountCache = new();

        // ��Ʒ���û���
        private readonly Dictionary<string, WeakReference<IItem>> _itemReferenceCache = new();
        #endregion

        #region ���캯��
        public ContainerCacheManager(int capacity)
        {
            if (capacity > 500)
            {
                // Ϊ����������Ԥ���仺��ռ�
                var itemSlotCache = new Dictionary<string, HashSet<int>>(100);
                var itemTypeCache = new Dictionary<string, HashSet<int>>(100 / 4);
                var itemCountCache = new Dictionary<string, int>(100);
            }
        }
        #endregion

        #region ������·���
        public void UpdateItemSlotIndexCache(string itemId, int slotIndex, bool isAdding)
        {
            if (string.IsNullOrEmpty(itemId))
                return;

            if (isAdding)
            {
                if (!_itemSlotIndexCache.ContainsKey(itemId))
                    _itemSlotIndexCache[itemId] = new HashSet<int>();

                if (!_itemSlotIndexCache[itemId].Contains(slotIndex))
                    _itemSlotIndexCache[itemId].Add(slotIndex);
            }
            else
            {
                if (_itemSlotIndexCache.ContainsKey(itemId) && _itemSlotIndexCache[itemId].Contains(slotIndex))
                    _itemSlotIndexCache[itemId].Remove(slotIndex);

                if (_itemSlotIndexCache.ContainsKey(itemId) && _itemSlotIndexCache[itemId].Count == 0)
                    _itemSlotIndexCache.Remove(itemId);
            }
        }

        public void UpdateEmptySlotCache(int slotIndex, bool isEmpty)
        {
            if (isEmpty)
            {
                if (!_emptySlotIndices.Contains(slotIndex))
                    _emptySlotIndices.Add(slotIndex);
            }
            else
            {
                _emptySlotIndices.Remove(slotIndex);
            }
        }

        public void UpdateItemTypeCache(string itemType, int slotIndex, bool isAdding)
        {
            if (string.IsNullOrEmpty(itemType))
                return;

            if (isAdding)
            {
                if (!_itemTypeIndexCache.ContainsKey(itemType))
                    _itemTypeIndexCache[itemType] = new HashSet<int>();

                _itemTypeIndexCache[itemType].Add(slotIndex);
            }
            else
            {
                if (_itemTypeIndexCache.ContainsKey(itemType))
                {
                    _itemTypeIndexCache[itemType].Remove(slotIndex);
                    if (_itemTypeIndexCache[itemType].Count == 0)
                        _itemTypeIndexCache.Remove(itemType);
                }
            }
        }

        public void UpdateItemCountCache(string itemId, int delta)
        {
            if (string.IsNullOrEmpty(itemId))
                return;

            if (_itemCountCache.ContainsKey(itemId))
            {
                _itemCountCache[itemId] += delta;
                if (_itemCountCache[itemId] <= 0)
                    _itemCountCache.Remove(itemId);
            }
            else if (delta > 0)
            {
                _itemCountCache[itemId] = delta;
            }
        }

        public void UpdateItemReferenceCache(string itemId, IItem item = null)
        {
            if (string.IsNullOrEmpty(itemId))
                return;

            if (item != null)
            {
                _itemReferenceCache[itemId] = new WeakReference<IItem>(item);
            }
            else
            {
                _itemReferenceCache.Remove(itemId);
            }
        }
        #endregion

        #region �����ѯ����
        public bool HasItemInCache(string itemId)
        {
            return _itemSlotIndexCache.ContainsKey(itemId) && _itemSlotIndexCache[itemId].Count > 0;
        }

        public bool TryGetItemSlotIndices(string itemId, out HashSet<int> indices)
        {
            return _itemSlotIndexCache.TryGetValue(itemId, out indices);
        }

        public bool TryGetItemTypeIndices(string itemType, out HashSet<int> indices)
        {
            return _itemTypeIndexCache.TryGetValue(itemType, out indices);
        }

        public bool TryGetItemCount(string itemId, out int count)
        {
            return _itemCountCache.TryGetValue(itemId, out count);
        }

        public IItem GetCachedItemReference(string itemId, System.Func<string, IItem> fallbackGetter)
        {
            if (_itemReferenceCache.TryGetValue(itemId, out var weakRef) &&
                weakRef.TryGetTarget(out var item))
            {
                return item;
            }

            var newItem = fallbackGetter?.Invoke(itemId);
            if (newItem != null)
            {
                _itemReferenceCache[itemId] = new WeakReference<IItem>(newItem);
            }
            else
            {
                _itemReferenceCache.Remove(itemId);
            }

            return newItem;
        }

        public SortedSet<int> GetEmptySlotIndices()
        {
            return _emptySlotIndices;
        }

        public int GetCachedItemCount()
        {
            return _itemSlotIndexCache.Count;
        }

        public Dictionary<string, int> GetAllItemCounts()
        {
            return new Dictionary<string, int>(_itemCountCache);
        }
        #endregion

        #region ����ά������
        public void RefreshItemReferenceCache(IReadOnlyList<ISlot> slots, string itemId = null)
        {
            if (itemId != null)
            {
                // ˢ���ض���Ʒ
                var item = GetItemReferenceFromSlots(slots, itemId);
                UpdateItemReferenceCache(itemId, item);
            }
            else
            {
                // ˢ��������Ʒ���û���
                _itemReferenceCache.Clear();

                var processedItems = new HashSet<string>();
                foreach (var slot in slots)
                {
                    if (slot.IsOccupied && slot.Item != null && !processedItems.Contains(slot.Item.ID))
                    {
                        _itemReferenceCache[slot.Item.ID] = new WeakReference<IItem>(slot.Item);
                        processedItems.Add(slot.Item.ID);
                    }
                }
            }
        }

        private IItem GetItemReferenceFromSlots(IReadOnlyList<ISlot> slots, string itemId)
        {
            if (_itemSlotIndexCache.TryGetValue(itemId, out var indices) && indices.Count > 0)
            {
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

        public void RebuildCaches(IReadOnlyList<ISlot> slots)
        {
            // ������л���
            ClearAllCaches();

            var processedItems = new HashSet<string>();
            // �ؽ�����
            for (int i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                if (slot.IsOccupied && slot.Item != null)
                {
                    string itemId = slot.Item.ID;

                    // ������Ʒ��������
                    UpdateItemSlotIndexCache(slot.Item.ID, i, true);

                    // ������Ʒ���ͻ���
                    UpdateItemTypeCache(slot.Item.Type, i, true);

                    // ������Ʒ��������
                    UpdateItemCountCache(slot.Item.ID, slot.ItemCount);

                    // ������Ʒ���û���
                    if (!processedItems.Contains(itemId))
                    {
                        _itemReferenceCache[itemId] = new WeakReference<IItem>(slot.Item);
                        processedItems.Add(itemId);
                    }
                }
                else
                {
                    // ���¿ղ�λ����
                    UpdateEmptySlotCache(i, true);
                }
            }
        }

        public void ValidateCaches(IReadOnlyList<ISlot> slots)
        {
            // ��֤��Ʒ��������
            var itemsToRemove = new List<string>();
            foreach (var kvp in _itemSlotIndexCache)
            {
                var validIndices = new HashSet<int>();
                foreach (int index in kvp.Value)
                {
                    if (index < slots.Count && slots[index].IsOccupied &&
                        slots[index].Item != null && slots[index].Item.ID == kvp.Key)
                    {
                        validIndices.Add(index);
                    }
                }

                if (validIndices.Count == 0)
                    itemsToRemove.Add(kvp.Key);
                else
                    _itemSlotIndexCache[kvp.Key] = validIndices;
            }

            foreach (string itemId in itemsToRemove)
            {
                _itemSlotIndexCache.Remove(itemId);
            }

            // ��֤�ղ�λ����
            var emptyToRemove = new List<int>();
            foreach (int index in _emptySlotIndices)
            {
                if (index >= slots.Count || slots[index].IsOccupied)
                    emptyToRemove.Add(index);
            }

            foreach (int index in emptyToRemove)
            {
                _emptySlotIndices.Remove(index);
            }

            CleanupItemReferenceCache();
        }

        private void CleanupItemReferenceCache()
        {
            var keysToRemove = new List<string>();

            foreach (var kvp in _itemReferenceCache)
            {
                if (!kvp.Value.TryGetTarget(out _))
                {
                    keysToRemove.Add(kvp.Key);
                }
            }

            foreach (var key in keysToRemove)
            {
                _itemReferenceCache.Remove(key);
            }
        }

        public void ClearAllCaches()
        {
            _itemSlotIndexCache.Clear();
            _emptySlotIndices.Clear();
            _itemTypeIndexCache.Clear();
            _itemCountCache.Clear();
            _itemReferenceCache.Clear();
        }
        #endregion
    }
}