using EasyPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public abstract class Container : IContainer
{
    #region ��������
    public string ID { get; }
    public string Name { get; }
    public string Type { get; set; } = "";
    public int Capacity { get; set; } // -1��ʾ��������
    public abstract bool IsGrid { get; } // ����ʵ�֣������Ƿ�Ϊ��������
    public abstract Vector2 Grid { get; } // ����������״

    public List<IItemCondition> ContainerCondition { get; set; }
    protected List<ISlot> _slots = new();
    public IReadOnlyList<ISlot> Slots => _slots.AsReadOnly();

    // ���������
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

    #region �����¼�

    /// <summary>
    /// �����Ʒ��������¼���ͳһ����ɹ���ʧ�ܣ�
    /// </summary>
    /// <param name="item">��������Ʒ</param>
    /// <param name="requestedCount">������ӵ�����</param>
    /// <param name="actualCount">ʵ����ӵ�����</param>
    /// <param name="result">�������</param>
    /// <param name="affectedSlots">�漰�Ĳ�λ�����б�ʧ��ʱΪ���б�</param>
    public event System.Action<IItem, int, int, AddItemResult, List<int>> OnItemAddResult;

    /// <summary>
    /// �Ƴ���Ʒ��������¼���ͳһ����ɹ���ʧ�ܣ�
    /// </summary>
    /// <param name="itemId">��������ƷID</param>
    /// <param name="requestedCount">�����Ƴ�������</param>
    /// <param name="actualCount">ʵ���Ƴ�������</param>
    /// <param name="result">�������</param>
    /// <param name="affectedSlots">�漰�Ĳ�λ�����б�ʧ��ʱΪ���б�</param>
    public event System.Action<string, int, int, RemoveItemResult, List<int>> OnItemRemoveResult;

    /// <summary>
    /// ��λ��������¼�
    /// </summary>
    /// <param name="slotIndex">����Ĳ�λ����</param>
    /// <param name="item">�������Ʒ</param>
    /// <param name="oldCount">ԭ����</param>
    /// <param name="newCount">������</param>
    public event System.Action<int, IItem, int, int> OnSlotCountChanged;

    /// <summary>
    /// ������λ��Ʒ��������¼�
    /// </summary>
    protected virtual void OnSlotQuantityChanged(int slotIndex, IItem item, int oldCount, int newCount)
    {
        OnSlotCountChanged?.Invoke(slotIndex, item, oldCount, newCount);
    }

    /// <summary>
    /// ��Ʒ��������¼�
    /// </summary>
    /// <param name="itemId">��ƷID</param>
    /// <param name="item">��Ʒ���ã�����Ϊnull�������Ʒ����ȫ�Ƴ���</param>
    /// <param name="oldTotalCount">������</param>
    /// <param name="newTotalCount">������</param>
    public event System.Action<string, IItem, int, int> OnItemTotalCountChanged;

    private readonly Dictionary<string, int> _itemTotalCounts = new();

    /// <summary>
    /// ������Ʒ�������
    /// </summary>
    protected void TriggerItemTotalCountChanged(string itemId, IItem itemRef = null)
    {
        int newTotal = GetItemTotalCount(itemId);
        
        int oldTotal = _itemTotalCounts.TryGetValue(itemId, out int value) ? value : 0;

        // ֻ�������б仯�ż�������
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

    #region ������
    private readonly HashSet<string> _pendingTotalCountUpdates = new();
    private readonly Dictionary<string, IItem> _itemRefCache = new();
    private bool _batchUpdateMode = false;

    /// <summary>
    /// ��ʼ��������ģʽ
    /// </summary>
    protected void BeginBatchUpdate()
    {
        _batchUpdateMode = true;
        _pendingTotalCountUpdates.Clear();
        _itemRefCache.Clear();
    }

    /// <summary>
    /// ������������ģʽ���������д�������
    /// </summary>
    protected void EndBatchUpdate()
    {
        if (_batchUpdateMode && _pendingTotalCountUpdates.Count > 0)
        {
            // �����������д����µ���Ʒ
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

    #region ״̬���
    // <summary>
    /// ��������Ƿ�����
    /// �������в�λ����ռ�ã���ÿ��ռ�õĲ�λ��Ʒ�����ɶѵ����Ѵﵽ�ѵ�����ʱ�������ű���Ϊ������
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

                // �����Ʒ�ɶѵ���δ�ﵽ�ѵ����ޣ���������
                if (slot.Item.IsStackable && (slot.Item.MaxStackCount <= 0 || slot.ItemCount < slot.Item.MaxStackCount))
                    return false;
            }

            return true;
        }
    }

    /// <summary>
    /// �����Ʒ�Ƿ�������������
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
    /// ����Ƿ���������Ʒ������
    /// </summary>
    /// <param name="item">Ҫ��ӵ���Ʒ</param>
    /// <returns>��ӽ�������������ӷ���Success�����򷵻ض�Ӧ�Ĵ���ԭ��</returns>
    protected virtual AddItemResult CanAddItem(IItem item)
    {
        if (item == null)
            return AddItemResult.ItemIsNull;

        if (!ValidateItemCondition(item))
            return AddItemResult.ItemConditionNotMet;

        // ���������������Ҫ����Ƿ��пɶѵ��Ĳ�λ
        if (Full)
        {
            // �����Ʒ�ɶѵ�������Ƿ�����ͬ��Ʒ��δ�ﵽ�ѵ����޵Ĳ�λ
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

    #region ������ط���
    /// <summary>
    /// ˢ����Ʒ���û���
    /// </summary>
    /// <param name="itemId">�ض���ƷID��null��ʾˢ������</param>
    public void RefreshItemReferenceCache(string itemId = null)
    {
        _cacheManager.RefreshItemReferenceCache(_slots.AsReadOnly(), itemId);
    }

    /// <summary>
    /// ��ʼ�����ؽ����л���
    /// </summary>
    public void RebuildCaches()
    {
        _cacheManager.RebuildCaches(_slots.AsReadOnly());
    }

    /// <summary>
    /// ��������е���Ч��Ŀ
    /// </summary>
    public void ValidateCaches()
    {
        _cacheManager.ValidateCaches(_slots.AsReadOnly());
    }
    #endregion

    #region ��Ʒ��ѯ
    /// <summary>
    /// ����������Ƿ����ָ��ID����Ʒ
    /// </summary>
    /// <param name="itemId">��ƷID</param>
    /// <returns>�����������true�����򷵻�false</returns>
    public bool HasItem(string itemId)
    {
        return _cacheManager.HasItemInCache(itemId);
    }

    /// <summary>
    /// ��ȡ��Ʒ����
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
    /// ��ȡ������ָ��ID��Ʒ��������
    /// </summary>
    /// <param name="itemId">��ƷID</param>
    /// <returns>��Ʒ������</returns>
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

    /// <summary>
    /// �����Ͳ�ѯ��Ʒ
    /// </summary>
    /// <param name="itemType">��Ʒ����</param>
    /// <returns>�������͵���Ʒ�б�������λ��������Ʒ���ú�����</returns>
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

    /// <summary>
    /// �����Բ�ѯ��Ʒ
    /// </summary>
    /// <param name="attributeName">��������</param>
    /// <param name="attributeValue">����ֵ</param>
    /// <returns>����������������Ʒ�б�������λ��������Ʒ���ú�����</returns>
    public List<(int slotIndex, IItem item, int count)> GetItemsByAttribute(string attributeName, object attributeValue)
    {
        var result = new List<(int slotIndex, IItem item, int count)>();

        var slots = _slots;
        int slotCount = slots.Count;

        // �����λ�����ϴ�ʹ�ò��д���
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
            // С��ģ����ʹ�õ��߳�
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
    /// ��������ѯ��Ʒ
    /// </summary>
    /// <param name="condition">����ί��</param>
    /// <returns>������������Ʒ�б�������λ��������Ʒ���ú�����</returns>
    public List<(int slotIndex, IItem item, int count)> GetItemsWhere(System.Func<IItem, bool> condition)
    {
        var result = new List<(int slotIndex, IItem item, int count)>();

        var slots = _slots;
        int slotCount = slots.Count;

        // �����λ�����ϴ�ʹ�ò��д���
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
            // С��ģ����ʹ�õ��߳�
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
    /// ��ȡ��Ʒ���ڵĲ�λ����
    /// </summary>
    /// <param name="itemId">��ƷID</param>
    /// <returns>��Ʒ���ڵĲ�λ�����б�</returns>
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

    /// <summary>
    /// ��ȡ��һ������ָ����ƷID�Ĳ�λ����
    /// </summary>
    /// <param name="itemId">��ƷID</param>
    /// <returns>�ҵ��Ĳ�λ���������û�ҵ�����-1</returns>
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

    /// <summary>
    /// ��ȡ������������Ʒ��ID��������
    /// </summary>
    /// <returns>��ƷID�����������ֵ�</returns>
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

        // ���¼��㲢���»���
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
    /// ��ȡ���������е���Ʒ������λ˳��
    /// </summary>
    /// <returns>��λ��������Ʒ���������б�</returns>
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
    /// ��ȡ���������в�ͬ���͵���Ʒ����
    /// </summary>
    /// <returns>��ͬ���͵���Ʒ����</returns>
    public int GetUniqueItemCount()
    {
        return GetAllItemCountsDict().Count;
    }

    /// <summary>
    /// ��������Ƿ�Ϊ��
    /// </summary>
    /// <returns>�������Ϊ�շ���true�����򷵻�false</returns>
    public bool IsEmpty()
    {
        return _cacheManager.GetCachedItemCount() == 0;
    }

    /// <summary>
    /// ��ȡ������ǰռ�õ�������
    /// </summary>
    /// <returns>������</returns>
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
    /// ����������Ƿ����㹻������ָ����Ʒ
    /// </summary>
    /// <param name="itemId">��ƷID</param>
    /// <param name="requiredCount">��Ҫ������</param>
    /// <returns>������㹻��������true�����򷵻�false</returns>
    public bool HasEnoughItems(string itemId, int requiredCount)
    {
        return GetItemTotalCount(itemId) >= requiredCount;
    }

    /// <summary>
    /// ͨ������ģ����ѯ��Ʒ
    /// </summary>
    /// <param name="namePattern">����ģʽ��֧�ֲ���ƥ��</param>
    /// <returns>��������ģʽ����Ʒ�б�</returns>
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

    #region �Ƴ���Ʒ
    /// <summary>
    /// �Ƴ�ָ��ID����Ʒ
    /// </summary>
    /// <param name="itemId">��ƷID</param>
    /// <param name="count">�Ƴ�����</param>
    /// <returns>�Ƴ����</returns>
    public virtual RemoveItemResult RemoveItem(string itemId, int count = 1)
    {
        var emptySlots = new List<int>();
        if (string.IsNullOrEmpty(itemId))
        {
            OnItemRemoveResult?.Invoke(itemId, count, 0, RemoveItemResult.InvalidItemId, emptySlots);
            return RemoveItemResult.InvalidItemId;
        }

        // �ȼ����Ʒ�����Ƿ��㹻
        int totalCount = GetItemTotalCount(itemId);
        if (totalCount < count && totalCount != 0)
        {
            OnItemRemoveResult?.Invoke(itemId, count, 0, RemoveItemResult.InsufficientQuantity, emptySlots);
            return RemoveItemResult.InsufficientQuantity;
        }

        // �����Ʒ������
        if (totalCount == 0)
        {
            OnItemRemoveResult?.Invoke(itemId, count, 0, RemoveItemResult.ItemNotFound, emptySlots);
            return RemoveItemResult.ItemNotFound;
        }

        // ֻ��ȷ���ܹ���ȫ�Ƴ�ָ����������Ʒʱ����ִ���Ƴ�����
        int remainingCount = count;
        List<(ISlot slot, int removeAmount, int slotIndex)> removals = new();

        // ��һ��������Ҫ��ÿ����λ�Ƴ�������
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
        // �ڶ�����ȷ�Ͽ�����ȫ�Ƴ�ָ��������ִ��ʵ�ʵ��Ƴ�����
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

                // ������������
                _cacheManager.UpdateItemCountCache(itemId, -removeAmount);

                affectedSlots.Add(slotIndex);

                // ��λ��Ʒ��������¼�
                OnSlotQuantityChanged(slotIndex, item, oldCount, slot.ItemCount);
            }
            // �����Ʒ��ȫ�Ƴ���������û���
            if (itemCompletelyRemoved)
            {
                _cacheManager.UpdateItemReferenceCache(itemId, null);
            }

            // �Ƴ��ɹ��¼�
            OnItemRemoveResult?.Invoke(itemId, count, count, RemoveItemResult.Success, affectedSlots);
            TriggerItemTotalCountChanged(itemId);
            return RemoveItemResult.Success;
        }

        // ����δ֪���󣬲��ܱ��ҵ��ߵ�����һ��
        OnItemRemoveResult?.Invoke(itemId, count, 0, RemoveItemResult.Failed, emptySlots);
        return RemoveItemResult.Failed;
    }

    /// <summary>
    /// ��ָ����λ�Ƴ���Ʒ
    /// </summary>
    /// <param name="index">��λ����</param>
    /// <param name="count">�Ƴ�����</param>
    /// <param name="expectedItemId">Ԥ����ƷID��������֤</param>
    /// <returns>�Ƴ����</returns>
    public virtual RemoveItemResult RemoveItemAtIndex(int index, int count = 1, string expectedItemId = null)
    {
        var emptySlots = new List<int>();
        // ����λ�����Ƿ���Ч
        if (index < 0 || index >= _slots.Count)
        {
            OnItemRemoveResult?.Invoke(expectedItemId ?? "unknown", count, 0, RemoveItemResult.SlotNotFound, emptySlots);
            return RemoveItemResult.SlotNotFound;
        }

        var slot = _slots[index];

        // ����λ�Ƿ�����Ʒ
        if (!slot.IsOccupied || slot.Item == null)
        {
            OnItemRemoveResult?.Invoke(expectedItemId ?? "unknown", count, 0, RemoveItemResult.ItemNotFound, emptySlots);
            return RemoveItemResult.ItemNotFound;
        }

        // ������Ʒ���ú�ID
        IItem item = slot.Item;
        string itemId = item.ID;
        string itemType = item.Type;

        // ����ṩ��Ԥ�ڵ���ƷID������֤
        if (!string.IsNullOrEmpty(expectedItemId) && itemId != expectedItemId)
        {
            OnItemRemoveResult?.Invoke(expectedItemId, count, 0, RemoveItemResult.InvalidItemId, emptySlots);
            return RemoveItemResult.InvalidItemId;
        }

        // �����Ʒ�����Ƿ��㹻
        if (slot.ItemCount < count)
        {
            OnItemRemoveResult?.Invoke(itemId, count, 0, RemoveItemResult.InsufficientQuantity, emptySlots);
            return RemoveItemResult.InsufficientQuantity;
        }

        // ��¼������
        int oldCount = slot.ItemCount;

        // ���м�鶼ͨ����ִ���Ƴ�����
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

        // ������������
        _cacheManager.UpdateItemCountCache(itemId, -count);

        // ������Ʒ��������¼�
        OnSlotQuantityChanged(index, item, oldCount, slot.ItemCount);

        // ������Ʒ�Ƴ��¼�
        var affectedSlots = new List<int> { index };
        OnItemRemoveResult?.Invoke(itemId, count, count, RemoveItemResult.Success, affectedSlots);
        TriggerItemTotalCountChanged(itemId, item);

        return RemoveItemResult.Success;
    }
    #endregion

    #region �����Ʒ
    /// <summary>
    /// ���ָ����������Ʒ������
    /// </summary>
    /// <param name="item">Ҫ��ӵ���Ʒ</param>
    /// <param name="count">Ҫ��ӵ�����</param>
    /// <param name="slotIndex">ָ���Ĳ�λ������-1��ʾ�Զ�Ѱ�Һ��ʵĲ�λ</param>
    /// <param name="exceededCount">�����ѵ����޵�����</param>
    /// <returns>��ӽ���ͳɹ���ӵ�����</returns>
    public virtual (AddItemResult result, int addedCount)
    AddItemsWithCount(IItem item, out int exceededCount, int count = 1, int slotIndex = -1)
    {
        exceededCount = 0;
        List<int> affectedSlots = new(12);
        var emptySlots = new List<int>();

        // ������֤
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
        // ��ʼ��������ģʽ
        BeginBatchUpdate();

        try
        {
            int totalAdded = 0;
            int remainingCount = count;

            // ����Ʒ��ӵ��������б�������Ʒ����
            _pendingTotalCountUpdates.Add(item.ID);
            _itemRefCache[item.ID] = item;

            // 1. �ѵ�����
            if (item.IsStackable && slotIndex == -1)
            {
                var (stackedCount, stackedSlots, slotChanges) = TryStackItems(item, remainingCount);

                if (stackedCount > 0)
                {
                    totalAdded += stackedCount;
                    remainingCount -= stackedCount;
                    affectedSlots.AddRange(stackedSlots);

                    _cacheManager.UpdateItemCountCache(item.ID, stackedCount);

                    // �����¼�����
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

            // 2. ָ����λ����
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

            // 3. �ղ�λ���²�λ����
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

                // �޷��������
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
            // �����������£�ͳһ�������д�������
            EndBatchUpdate();
        }
    }

    /// <summary>
    /// ���ָ����������Ʒ������
    /// </summary>
    /// <param name="item">Ҫ��ӵ���Ʒ</param>
    /// <param name="count">Ҫ��ӵ�����</param>
    /// <param name="slotIndex">ָ���Ĳ�λ������-1��ʾ�Զ�Ѱ�Һ��ʵĲ�λ</param>
    /// <returns>��ӽ���ͳɹ���ӵ�����</returns>
    public virtual (AddItemResult result, int addedCount) AddItems(IItem item, int count = 1, int slotIndex = -1)
    {
        return AddItemsWithCount(item, out _, count, slotIndex);
    }

    /// <summary>
    /// �첽�����Ʒ
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
    /// ������Ӷ�����Ʒ
    /// </summary>
    /// <param name="itemsToAdd">Ҫ��ӵ���Ʒ�������б�</param>
    /// <returns>ÿ����Ʒ����ӽ��</returns>
    public virtual List<(IItem item, AddItemResult result, int addedCount, int exceededCount)> AddItemsBatch(
        List<(IItem item, int count)> itemsToAdd)
    {
        var results = new List<(IItem item, AddItemResult result, int addedCount, int exceededCount)>();

        if (itemsToAdd == null || itemsToAdd.Count == 0)
            return results;

        // ��ʼ��������ģʽ
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
            // �����������£�ͳһ�������д�������
            EndBatchUpdate();
        }

        return results;
    }
    #endregion

    #region �м䴦��API

    /// <summary>
    /// ���Խ���Ʒ�ѵ���������ͬ��Ʒ�Ĳ�λ�� - �����Ż��汾
    /// </summary>
    protected virtual (int stackedCount, List<int> affectedSlots, Dictionary<int, (int oldCount, int newCount)> changes)
        TryStackItems(IItem item, int remainingCount)
    {
        // �����˳�
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

        // ����ػ�
        int estimatedSize = Math.Min(indices.Count, 16);
        var affectedSlots = new List<int>(estimatedSize);
        var slotChanges = new Dictionary<int, (int oldCount, int newCount)>(estimatedSize);

        // �ռ���Ч��λ��Ϣ
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
            // �����ÿռ併����������������ռ��λ
            stackableSlots.Sort((a, b) => b.space.CompareTo(a.space));
        }

        // �ѵ�ʵ��
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
    /// ���Խ���Ʒ��ӵ�ָ����λ
    /// </summary>
    protected virtual (bool success, int addedCount, int remainingCount)
    TryAddToSpecificSlot(IItem item, int slotIndex, int remainingCount)
    {
        if (slotIndex >= _slots.Count)
        {
            return (false, 0, remainingCount);
        }

        var targetSlot = _slots[slotIndex];

        // �����λ�ѱ�ռ�ã�����Ƿ���Զѵ���Ʒ
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

            // ������������
            int oldCount = targetSlot.ItemCount;
            int canAddCount;

            if (item.MaxStackCount <= 0)
            {
                canAddCount = remainingCount; // ���޶ѵ�
            }
            else
            {
                // ���ǲ�λ����������ȷ�����������ѵ���
                canAddCount = Mathf.Min(remainingCount, item.MaxStackCount - targetSlot.ItemCount);
                if (canAddCount <= 0)
                {
                    return (false, 0, remainingCount); // �Ѵﵽ���ѵ���
                }
            }

            // ������Ʒ
            if (targetSlot.SetItem(targetSlot.Item, targetSlot.ItemCount + canAddCount))
            {
                // �����������
                OnSlotQuantityChanged(slotIndex, targetSlot.Item, oldCount, targetSlot.ItemCount);
                return (true, canAddCount, remainingCount - canAddCount);
            }
        }
        else
        {
            // ��λΪ�գ�ֱ�����
            if (!targetSlot.CheckSlotCondition(item))
            {
                return (false, 0, remainingCount);
            }

            int addCount = item.IsStackable && item.MaxStackCount > 0 ?
                           Mathf.Min(remainingCount, item.MaxStackCount) :
                           remainingCount;

            if (targetSlot.SetItem(item, addCount))
            {
                // �����������
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

        // ������ӵ�����
        int addCount = isStackable && maxStack > 0
            ? Math.Min(remainingCount, maxStack)
            : (isStackable ? remainingCount : 1);

        var emptySlotIndices = _cacheManager.GetEmptySlotIndices();

        // ʹ�ÿղ�λ����
        foreach (int i in emptySlotIndices)
        {
            if (i >= _slots.Count) continue;

            var slot = _slots[i];
            if (slot.IsOccupied) continue;

            if (!slot.CheckSlotCondition(item)) continue;

            if (slot.SetItem(item, addCount))
            {
                // �������»���
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
            if (emptySlotSet.Contains(i)) continue; // ���ڸղż����Ĳ�λ����

            var slot = _slots[i];
            if (slot.IsOccupied || !slot.CheckSlotCondition(item)) continue;

            if (slot.SetItem(item, addCount))
            {
                // ���»���״̬
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
    /// ���Դ����²�λ�������Ʒ
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
                          1; // ���ɶѵ���Ʒ

            if (newSlot.CheckSlotCondition(item) && newSlot.SetItem(item, addCount))
            {
                _slots.Add(newSlot);

                // ���»���
                _cacheManager.UpdateItemSlotIndexCache(item.ID, newSlotIndex, true);
                _cacheManager.UpdateItemTypeCache(item.Type, newSlotIndex, true);
                _cacheManager.UpdateItemReferenceCache(item.ID, item);

                // �����������
                OnSlotQuantityChanged(newSlotIndex, newSlot.Item, 0, newSlot.ItemCount);
                return (true, addCount, remainingCount - addCount, newSlotIndex);
            }
        }

        return (false, 0, remainingCount, -1);
    }
    #endregion
}