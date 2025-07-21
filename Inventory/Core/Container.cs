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

    public Container(string id, string name, string type, int capacity = -1, int BIGCAPACITYNUM = 1000, int PRECACHE = 1000)
    {
        ID = id;
        Name = name;
        Type = type;
        Capacity = capacity;
        ContainerCondition = new List<IItemCondition>();

        RebuildCaches();
        if (capacity > BIGCAPACITYNUM)
        {
            _itemSlotIndexCache = new Dictionary<string, HashSet<int>>(PRECACHE);
            _itemTypeIndexCache = new Dictionary<string, HashSet<int>>(PRECACHE / 4);
            _itemCountCache = new Dictionary<string, int>(PRECACHE);
        }
    }
    #endregion

    #region �����¼�

    /// <summary>
    /// �����Ʒ�ɹ��¼�
    /// </summary>
    /// <param name="item">��ӵ���Ʒ</param>
    /// <param name="count">��ӵ�����</param>
    /// <param name="slotIndices">�漰�Ĳ�λ�����б�</param>
    public event System.Action<IItem, int, List<int>> OnItemAdded;

    /// <summary>
    /// �����Ʒʧ���¼�
    /// </summary>
    /// <param name="item">������ӵ���Ʒ</param>
    /// <param name="count">������ӵ�����</param>
    /// <param name="result">ʧ�ܵĽ��</param>
    public event System.Action<IItem, int, AddItemResult> OnItemAddFailed;

    /// <summary>
    /// �Ƴ���Ʒ�ɹ��¼�
    /// </summary>
    /// <param name="itemId">���Ƴ���Ʒ��ID</param>
    /// <param name="count">�Ƴ�������</param>
    /// <param name="slotIndices">�漰�Ĳ�λ�����б�</param>
    public event System.Action<string, int, List<int>> OnItemRemoved;

    /// <summary>
    /// �Ƴ���Ʒʧ���¼�
    /// </summary>
    /// <param name="itemId">�����Ƴ�����ƷID</param>
    /// <param name="count">�����Ƴ�������</param>
    /// <param name="result">ʧ�ܵĽ��</param>
    public event System.Action<string, int, RemoveItemResult> OnItemRemoveFailed;

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
    /// ��鲢������Ʒ�����仯�¼�
    /// </summary>
    protected virtual void TriggerItemTotalCountChanged(string itemId, IItem itemRef = null)
    {

        // ��ȡ��������
        int newTotal = GetItemTotalCount(itemId);

        // ��ȡ������������ֵ��в�������Ϊ0
        int oldTotal = _itemTotalCounts.ContainsKey(itemId) ? _itemTotalCounts[itemId] : 0;

        // ֻ�������б仯�Ŵ����¼�
        if (newTotal != oldTotal)
        {
            // ���û�д�����Ʒ���ã����Դӱ������ҵ�һ��
            if (itemRef == null && newTotal > 0)
            {
                if (_itemSlotIndexCache.TryGetValue(itemId, out var indices) && indices.Count > 0)
                {
                    int firstIndex = indices.First();
                    if (firstIndex < _slots.Count)
                    {
                        var slot = _slots[firstIndex];
                        if (slot.IsOccupied && slot.Item != null && slot.Item.ID == itemId)
                        {
                            itemRef = slot.Item;
                        }
                    }
                }
            }

            // �����¼�
            OnItemTotalCountChanged?.Invoke(itemId, itemRef, oldTotal, newTotal);

            // ���¼�¼
            if (newTotal > 0)
                _itemTotalCounts[itemId] = newTotal;
            else
                _itemTotalCounts.Remove(itemId);
        }
    }

    /// <summary>
    /// ����������Ʒ�������
    /// </summary>
    private void TriggerItemTotalCountChangedImmediate(string itemId, IItem itemRef = null)
    {
        int newTotal;
        if (_itemCountCache.TryGetValue(itemId, out newTotal))
        {
            // �������У�ֱ��ʹ��
        }
        else
        {
            newTotal = GetItemTotalCount(itemId);
        }

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
                TriggerItemTotalCountChangedImmediate(itemId,
                    _itemRefCache.TryGetValue(itemId, out var itemRef) ? itemRef : null);
            }

            _pendingTotalCountUpdates.Clear();
            _itemRefCache.Clear();
        }
        _batchUpdateMode = false;
    }
    #endregion

    #region ״̬���
    /// <summary>
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

            if (_emptySlotIndices.Count > 0)
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
                if (_itemSlotIndexCache.TryGetValue(item.ID, out var indices))
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

    #region ����
    // ��Ʒ��������
    private readonly Dictionary<string, HashSet<int>> _itemSlotIndexCache = new();

    // �ղ�λ����
    private readonly SortedSet<int> _emptySlotIndices = new();

    // ��Ʒ������������
    private readonly Dictionary<string, HashSet<int>> _itemTypeIndexCache = new();

    // ��Ʒ��������
    private readonly Dictionary<string, int> _itemCountCache = new();

    // ��Ʒ���ѵ�������
    private readonly Dictionary<string, int> _itemMaxStackCache = new();

    // ��Ʒ���û���
    private readonly Dictionary<string, WeakReference<IItem>> _itemReferenceCache = new();

    protected void UpdateItemCache(string itemId, int slotIndex, bool isAdding)
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

    protected void UpdateEmptySlotCache(int slotIndex, bool isEmpty)
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

    // ������Ʒ������������
    protected void UpdateItemTypeCache(string itemType, int slotIndex, bool isAdding)
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

    // ������Ʒ��������
    protected void UpdateItemCountCache(string itemId, int delta)
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

    /// <summary>
    /// ��ʼ�����ؽ����л���
    /// </summary>
    public void RebuildCaches()
    {
        // ������л���
        _itemSlotIndexCache.Clear();
        _emptySlotIndices.Clear();
        _itemMaxStackCache.Clear();
        _itemTypeIndexCache.Clear();
        _itemCountCache.Clear();

        // �ؽ�����
        for (int i = 0; i < _slots.Count; i++)
        {
            var slot = _slots[i];
            if (slot.IsOccupied && slot.Item != null)
            {
                // ������Ʒ��������
                UpdateItemCache(slot.Item.ID, i, true);

                // ������Ʒ���ͻ���
                UpdateItemTypeCache(slot.Item.Type, i, true);

                // ������Ʒ��������
                UpdateItemCountCache(slot.Item.ID, slot.ItemCount);

                // ������Ʒ���ѵ�����
                if (!_itemMaxStackCache.ContainsKey(slot.Item.ID))
                    _itemMaxStackCache[slot.Item.ID] = slot.Item.MaxStackCount;
            }
            else
            {
                // ���¿ղ�λ����
                UpdateEmptySlotCache(i, true);
            }
        }
    }

    /// <summary>
    /// ��������е���Ч��Ŀ
    /// </summary>
    public void ValidateCaches()
    {
        // ��֤��Ʒ��������
        var itemsToRemove = new List<string>();
        foreach (var kvp in _itemSlotIndexCache)
        {
            var validIndices = new HashSet<int>();
            foreach (int index in kvp.Value)
            {
                if (index < _slots.Count && _slots[index].IsOccupied &&
                    _slots[index].Item != null && _slots[index].Item.ID == kvp.Key)
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
            if (index >= _slots.Count || _slots[index].IsOccupied)
                emptyToRemove.Add(index);
        }

        foreach (int index in emptyToRemove)
        {
            _emptySlotIndices.Remove(index);
        }
    }

    /// <summary>
    /// ����ʧЧ����Ʒ���û���
    /// </summary>
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

    #endregion

    #region ��Ʒ��ѯ
    /// <summary>
    /// ����������Ƿ����ָ��ID����Ʒ
    /// </summary>
    /// <param name="itemId">��ƷID</param>
    /// <returns>�����������true�����򷵻�false</returns>
    public bool HasItem(string itemId)
    {
        return _itemSlotIndexCache.ContainsKey(itemId) && _itemSlotIndexCache[itemId].Count > 0;
    }

    /// <summary>
    /// ��ȡ��Ʒ����
    /// </summary>
    private IItem GetItemReference(string itemId)
    {
        if (_itemSlotIndexCache.TryGetValue(itemId, out var indices) && indices.Count > 0)
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
    /// ��ȡ�������Ʒ����
    /// </summary>
    private IItem GetCachedItemReference(string itemId)
    {
        if (_itemReferenceCache.TryGetValue(itemId, out var weakRef) &&
            weakRef.TryGetTarget(out var item))
        {
            return item;
        }

        var newItem = GetItemReference(itemId);
        if (newItem != null)
        {
            _itemReferenceCache[itemId] = new WeakReference<IItem>(newItem);
        }

        return newItem;
    }

    /// <summary>
    /// ��ȡ������ָ��ID��Ʒ��������
    /// </summary>
    /// <param name="itemId">��ƷID</param>
    /// <returns>��Ʒ������</returns>
    public int GetItemTotalCount(string itemId)
    {
        // ���ȳ���ʹ����������
        if (_itemCountCache.TryGetValue(itemId, out int cachedCount))
        {
            return cachedCount;
        }

        // �������δ���У�ʹ�������������
        if (_itemSlotIndexCache.TryGetValue(itemId, out var indices))
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
                _itemCountCache[itemId] = totalCount;

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
                UpdateItemCache(itemId, i, true);
            }
        }

        if (count > 0)
            _itemCountCache[itemId] = count;

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
        if (_itemTypeIndexCache.TryGetValue(itemType, out var indices))
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
                UpdateItemTypeCache(itemType, i, true);
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
        if (_itemSlotIndexCache.TryGetValue(itemId, out var indices))
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
                _itemSlotIndexCache[itemId] = new HashSet<int>(validIndices);
                if (validIndices.Count == 0)
                    _itemSlotIndexCache.Remove(itemId);
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
                UpdateItemCache(itemId, i, true);
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
        if (_itemSlotIndexCache.TryGetValue(itemId, out var indices) && indices.Count > 0)
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
                UpdateItemCache(itemId, i, true);
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
        if (_itemCountCache.Count > 0)
        {
            var result = new Dictionary<string, int>(_itemCountCache);

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

        // ���»���
        _itemCountCache.Clear();
        foreach (var kvp in counts)
        {
            _itemCountCache[kvp.Key] = kvp.Value;
        }

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
        return _itemSlotIndexCache.Count == 0;
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
    /// <summary>
    /// �Ƴ�ָ��ID����Ʒ
    /// </summary>
    /// <param name="itemId">��ƷID</param>
    /// <param name="count">�Ƴ�����</param>
    /// <returns>�Ƴ����</returns>
    public virtual RemoveItemResult RemoveItem(string itemId, int count = 1)
    {
        if (string.IsNullOrEmpty(itemId))
        {
            OnItemRemoveFailed?.Invoke(itemId, count, RemoveItemResult.InvalidItemId);
            return RemoveItemResult.InvalidItemId;
        }

        // �ȼ����Ʒ�����Ƿ��㹻
        int totalCount = GetItemTotalCount(itemId);
        if (totalCount < count && totalCount != 0)
        {
            OnItemRemoveFailed?.Invoke(itemId, count, RemoveItemResult.InsufficientQuantity);
            return RemoveItemResult.InsufficientQuantity;
        }

        // �����Ʒ������
        if (totalCount == 0)
        {
            OnItemRemoveFailed?.Invoke(itemId, count, RemoveItemResult.ItemNotFound);
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

        // �ڶ�����ȷ�Ͽ�����ȫ�Ƴ�ָ��������ִ��ʵ�ʵ��Ƴ�����
        if (remainingCount == 0)
        {
            var affectedSlots = new List<int>();

            foreach (var (slot, removeAmount, slotIndex) in removals)
            {
                int oldCount = slot.ItemCount;
                var item = slot.Item;
                string itemType = item.Type;

                if (removeAmount == slot.ItemCount)
                {
                    slot.ClearSlot();
                    UpdateEmptySlotCache(slotIndex, true);
                    UpdateItemCache(itemId, slotIndex, false);
                    UpdateItemTypeCache(itemType, slotIndex, false);
                }
                else
                {
                    slot.SetItem(slot.Item, slot.ItemCount - removeAmount);
                }

                // ������������
                UpdateItemCountCache(itemId, -removeAmount);

                affectedSlots.Add(slotIndex);

                // ��λ��Ʒ��������¼�
                OnSlotQuantityChanged(slotIndex, item, oldCount, slot.ItemCount);
            }

            // �Ƴ��ɹ��¼�
            OnItemRemoved?.Invoke(itemId, count, affectedSlots);
            TriggerItemTotalCountChanged(itemId);
            return RemoveItemResult.Success;
        }

        // ����δ֪���󣬲��ܱ��ҵ��ߵ�����һ��
        OnItemRemoveFailed?.Invoke(itemId, count, RemoveItemResult.Failed);
        return RemoveItemResult.Failed;
    }

    /// <summary>
    /// ��ָ����λ�Ƴ���Ʒ
    /// </summary>
    /// <param name="index">��λ����</param>
    /// <param name="count">�Ƴ�����</param>
    /// <param name="expectedItemId">Ԥ����ƷID��������֤</param>
    /// <returns>�Ƴ����</returns>
    /// <summary>
    /// ��ָ����λ�Ƴ���Ʒ
    /// </summary>
    /// <param name="index">��λ����</param>
    /// <param name="count">�Ƴ�����</param>
    /// <param name="expectedItemId">Ԥ����ƷID��������֤</param>
    /// <returns>�Ƴ����</returns>
    public virtual RemoveItemResult RemoveItemAtIndex(int index, int count = 1, string expectedItemId = null)
    {
        // ����λ�����Ƿ���Ч
        if (index < 0 || index >= _slots.Count)
        {
            OnItemRemoveFailed?.Invoke(expectedItemId ?? "unknown", count, RemoveItemResult.SlotNotFound);
            return RemoveItemResult.SlotNotFound;
        }

        var slot = _slots[index];

        // ����λ�Ƿ�����Ʒ
        if (!slot.IsOccupied || slot.Item == null)
        {
            OnItemRemoveFailed?.Invoke(expectedItemId ?? "unknown", count, RemoveItemResult.ItemNotFound);
            return RemoveItemResult.ItemNotFound;
        }

        // ������Ʒ���ú�ID
        IItem item = slot.Item;
        string itemId = item.ID;
        string itemType = item.Type;

        // ����ṩ��Ԥ�ڵ���ƷID������֤
        if (!string.IsNullOrEmpty(expectedItemId) && itemId != expectedItemId)
        {
            OnItemRemoveFailed?.Invoke(expectedItemId, count, RemoveItemResult.InvalidItemId);
            return RemoveItemResult.InvalidItemId;
        }

        // �����Ʒ�����Ƿ��㹻
        if (slot.ItemCount < count)
        {
            OnItemRemoveFailed?.Invoke(itemId, count, RemoveItemResult.InsufficientQuantity);
            return RemoveItemResult.InsufficientQuantity;
        }

        // ��¼������
        int oldCount = slot.ItemCount;

        // ���м�鶼ͨ����ִ���Ƴ�����
        if (slot.ItemCount - count <= 0)
        {
            slot.ClearSlot();
            UpdateEmptySlotCache(index, true);
            UpdateItemCache(itemId, index, false);
            UpdateItemTypeCache(itemType, index, false);
        }
        else
        {
            slot.SetItem(item, slot.ItemCount - count);
        }

        // ������������
        UpdateItemCountCache(itemId, -count);

        // ������Ʒ��������¼�
        OnSlotQuantityChanged(index, item, oldCount, slot.ItemCount);

        // ������Ʒ�Ƴ��¼�
        OnItemRemoved?.Invoke(itemId, count, new List<int> { index });
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

        // ������֤
        if (item == null)
        {
            OnItemAddFailed?.Invoke(item, count, AddItemResult.ItemIsNull);
            return (AddItemResult.ItemIsNull, 0);
        }

        if (count <= 0)
            return (AddItemResult.AddNothingLOL, 0);

        if (!ValidateItemCondition(item))
        {
            OnItemAddFailed?.Invoke(item, count, AddItemResult.ItemConditionNotMet);
            return (AddItemResult.ItemConditionNotMet, 0);
        }
        // ��ʼ��������ģʽ
        BeginBatchUpdate();

        try
        {
            int totalAdded = 0;
            int remainingCount = count;

            // 1. �ѵ�����
            if (item.IsStackable && slotIndex == -1)
            {
                var (stackedCount, stackedSlots, slotChanges) = TryStackItems(item, remainingCount);

                if (stackedCount > 0)
                {
                    totalAdded += stackedCount;
                    remainingCount -= stackedCount;
                    affectedSlots.AddRange(stackedSlots);

                    UpdateItemCountCache(item.ID, stackedCount);

                    // �����¼�����
                    foreach (var change in slotChanges)
                    {
                        int slotIdx = change.Key;
                        var slot = _slots[slotIdx];
                        OnSlotQuantityChanged(slotIdx, slot.Item, change.Value.oldCount, change.Value.newCount);
                    }

                    // �ӳٸ�������
                    TriggerItemTotalCountChanged(item.ID, item);

                    if (remainingCount <= 0)
                    {
                        OnItemAdded?.Invoke(item, totalAdded, affectedSlots);
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

                    UpdateItemCountCache(item.ID, addedCount);
                    UpdateItemCache(item.ID, slotIndex, true);
                    UpdateItemTypeCache(item.Type, slotIndex, true);
                    UpdateEmptySlotCache(slotIndex, false);

                    TriggerItemTotalCountChanged(item.ID, item);

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

                    UpdateItemCountCache(item.ID, emptyAddedCount);
                    UpdateItemCache(item.ID, emptySlotIndex, true);
                    UpdateItemTypeCache(item.Type, emptySlotIndex, true);

                    if (remainingCount <= 0)
                    {
                        OnItemAdded?.Invoke(item, totalAdded, affectedSlots);
                        TriggerItemTotalCountChanged(item.ID, item);
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

                    UpdateItemCountCache(item.ID, newAddedCount);
                    UpdateItemCache(item.ID, newSlotIndex, true);
                    UpdateItemTypeCache(item.Type, newSlotIndex, true);

                    TriggerItemTotalCountChanged(item.ID, item);

                    if (remainingCount <= 0)
                    {
                        OnItemAdded?.Invoke(item, totalAdded, affectedSlots);
                        return (AddItemResult.Success, totalAdded);
                    }
                    continue;
                }

                // �޷��������
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
                    bool noEmptySlots = _emptySlotIndices.Count == 0;
                    AddItemResult result = noEmptySlots ? AddItemResult.ContainerIsFull : AddItemResult.NoSuitableSlotFound;
                    OnItemAddFailed?.Invoke(item, count, result);
                    return (result, 0);
                }
            }

            OnItemAdded?.Invoke(item, totalAdded, affectedSlots);
            TriggerItemTotalCountChanged(item.ID, item);
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
    #endregion

    #region �м䴦��API

    /// <summary>
    /// ���Խ���Ʒ�ѵ���������ͬ��Ʒ�Ĳ�λ��
    /// </summary>
    /// <summary>
    /// ���Խ���Ʒ�ѵ���������ͬ��Ʒ�Ĳ�λ�� - �����Ż��汾
    /// </summary>
    protected virtual (int stackedCount, List<int> affectedSlots, Dictionary<int, (int oldCount, int newCount)> changes)
        TryStackItems(IItem item, int remainingCount)
    {
        // �����˳�
        if (remainingCount <= 0 || !item.IsStackable)
            return (0, new List<int>(0), new Dictionary<int, (int oldCount, int newCount)>(0));

        int maxStack = _itemMaxStackCache.TryGetValue(item.ID, out int cachedMaxStack)
            ? cachedMaxStack
            : (_itemMaxStackCache[item.ID] = item.MaxStackCount);

        if (maxStack <= 1 || !_itemSlotIndexCache.TryGetValue(item.ID, out var indices) || indices.Count == 0)
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
        int maxStack = isStackable ? (_itemMaxStackCache.TryGetValue(item.ID, out var cachedMax)
            ? cachedMax : (_itemMaxStackCache[item.ID] = item.MaxStackCount)) : 1;

        // ������ӵ�����
        int addCount = isStackable && maxStack > 0
            ? Math.Min(remainingCount, maxStack)
            : (isStackable ? remainingCount : 1);

        // ʹ�ÿղ�λ����
        foreach (int i in _emptySlotIndices)
        {
            if (i >= _slots.Count) continue;

            var slot = _slots[i];
            if (slot.IsOccupied) continue;

            if (!slot.CheckSlotCondition(item)) continue;

            if (slot.SetItem(item, addCount))
            {
                // �������»���
                _emptySlotIndices.Remove(i);
                UpdateItemCache(item.ID, i, true);
                UpdateItemTypeCache(item.Type, i, true);

                OnSlotQuantityChanged(i, slot.Item, 0, slot.ItemCount);
                return (true, addCount, remainingCount - addCount, i);
            }
        }

        for (int i = 0; i < _slots.Count; i++)
        {
            if (_emptySlotIndices.Contains(i)) continue; // ���ڸղż����Ĳ�λ����

            var slot = _slots[i];
            if (slot.IsOccupied || !slot.CheckSlotCondition(item)) continue;

            if (slot.SetItem(item, addCount))
            {
                // ���»���״̬
                UpdateItemCache(item.ID, i, true);
                UpdateItemTypeCache(item.Type, i, true);

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
                UpdateItemCache(item.ID, newSlotIndex, true);
                UpdateItemTypeCache(item.Type, newSlotIndex, true);

                // �����������
                OnSlotQuantityChanged(newSlotIndex, newSlot.Item, 0, newSlot.ItemCount);
                return (true, addCount, remainingCount - addCount, newSlotIndex);
            }
        }

        return (false, 0, remainingCount, -1);
    }
    #endregion
}