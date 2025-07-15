using EasyPack;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Pool;

public abstract class Container : IContainer
{
    #region ��������
    public string ID { get; }
    public string Name { get; }
    public string Type { get; set; } = "";
    public int Capacity { get; set; } // -1��ʾ��������
    public abstract bool IsGrid { get; } // ����ʵ�֣������Ƿ�Ϊ��������

    public abstract Vector2 Grid {  get; } // ����������״

    public List<IItemCondition> ContainerCondition { get; set; }
    protected List<ISlot> _slots = new();
    public IReadOnlyList<ISlot> Slots => _slots.AsReadOnly();

    public Container(string id, string name, string type, int capacity = -1)
    {
        ID = id;
        Name = name;
        Type = type;
        Capacity = capacity;
        ContainerCondition = new List<IItemCondition>();

        _emptySlotIndices.Clear();
        _itemSlotIndexCache.Clear();
        _itemMaxStackCache.Clear();
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
    protected virtual void RaiseSlotItemCountChangedEvent(int slotIndex, IItem item, int oldCount, int newCount)
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
    protected virtual void CheckAndRaiseItemTotalCountChanged(string itemId, IItem itemRef = null)
    {
        // ��ȡ��������
        int newTotal = GetItemCount(itemId);

        // ��ȡ������������ֵ��в�������Ϊ0
        int oldTotal = _itemTotalCounts.ContainsKey(itemId) ? _itemTotalCounts[itemId] : 0;

        // ֻ�������б仯�Ŵ����¼�
        if (newTotal != oldTotal)
        {
            // ���û�д�����Ʒ���ã����Դӱ������ҵ�һ��
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

            // �����¼� (ȷ���ȴ����¼����ٸ��¼�¼)
            OnItemTotalCountChanged?.Invoke(itemId, itemRef, oldTotal, newTotal);

            // ���¼�¼
            if (newTotal > 0)
                _itemTotalCounts[itemId] = newTotal;
            else
                _itemTotalCounts.Remove(itemId);
        }
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
    /// ����Ƿ���������Ʒ������
    /// </summary>
    /// <param name="item">Ҫ��ӵ���Ʒ</param>
    /// <returns>��ӽ�������������ӷ���Success�����򷵻ض�Ӧ�Ĵ���ԭ��</returns>
    protected virtual AddItemResult CanAddItem(IItem item)
    {
        if (item == null)
            return AddItemResult.ItemIsNull;

        if (!CheckContainerCondition(item))
            return AddItemResult.ItemConditionNotMet;

        // ���������������Ҫ����Ƿ��пɶѵ��Ĳ�λ
        if (Full)
        {
            // �����Ʒ�ɶѵ�������Ƿ�����ͬ��Ʒ��δ�ﵽ�ѵ����޵Ĳ�λ
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

    #region ����
    // ��Ʒ->index�б���
    private readonly Dictionary<string, List<int>> _itemSlotIndexCache = new();
    private void UpdateItemCache(string itemId, int slotIndex, bool isAdding)
    {
        if (string.IsNullOrEmpty(itemId))
            return;

        if (isAdding)
        {
            if (!_itemSlotIndexCache.ContainsKey(itemId))
                _itemSlotIndexCache[itemId] = new List<int>();

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


    // �ղ�λ����
    private readonly List<int> _emptySlotIndices = new();
    private void UpdateEmptySlotCache(int slotIndex, bool isEmpty)
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

    // ��Ʒ���ѵ�������
    private readonly Dictionary<string, int> _itemMaxStackCache = new();
    /// <summary>
    /// ��ʼ�����ؽ����л���
    /// </summary>
    protected void RebuildCaches()
    {
        // ������л���
        _itemSlotIndexCache.Clear();
        _emptySlotIndices.Clear();
        _itemMaxStackCache.Clear();

        // �ؽ�����
        for (int i = 0; i < _slots.Count; i++)
        {
            var slot = _slots[i];
            if (slot.IsOccupied && slot.Item != null)
            {
                // ������Ʒ��������
                UpdateItemCache(slot.Item.ID, i, true);

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

    #endregion

    #region ��Ʒ��ѯ
    /// <summary>
    /// ����������Ƿ����ָ��ID����Ʒ
    /// </summary>
    /// <param name="itemId">��ƷID</param>
    /// <returns>�����������true�����򷵻�false</returns>
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
    /// ��ȡ������ָ��ID��Ʒ��������
    /// </summary>
    /// <param name="itemId">��ƷID</param>
    /// <returns>��Ʒ������</returns>
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
    /// �����Ͳ�ѯ��Ʒ
    /// </summary>
    /// <param name="itemType">��Ʒ����</param>
    /// <returns>�������͵���Ʒ�б�������λ��������Ʒ���ú�����</returns>
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
    /// �����Բ�ѯ��Ʒ
    /// </summary>
    /// <param name="attributeName">��������</param>
    /// <param name="attributeValue">����ֵ</param>
    /// <returns>����������������Ʒ�б�������λ��������Ʒ���ú�����</returns>
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
    /// ��������ѯ��Ʒ
    /// </summary>
    /// <param name="condition">����ί��</param>
    /// <returns>������������Ʒ�б�������λ��������Ʒ���ú�����</returns>
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
    /// ��ȡ��Ʒ���ڵĲ�λ����
    /// </summary>
    /// <param name="itemId">��ƷID</param>
    /// <returns>��Ʒ���ڵĲ�λ�����б�</returns>
    public List<int> GetItemSlotIndices(string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
            return new List<int>();

        // ʹ�û���
        if (_itemSlotIndexCache.TryGetValue(itemId, out var indices))
        {
            // ��֤������Ч��
            var validIndices = new List<int>(indices.Count);
            foreach (int idx in indices)
            {
                if (idx < _slots.Count)  // ȷ��������Ч
                {
                    var slot = _slots[idx];
                    if (slot.IsOccupied && slot.Item != null && slot.Item.ID == itemId)
                    {
                        validIndices.Add(idx);
                    }
                    else
                    {
                        // ��������Ч���ӻ������Ƴ�
                        UpdateItemCache(itemId, idx, false);
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
    /// ��ȡ������������Ʒ��ID��������
    /// </summary>
    /// <returns>��ƷID�����������ֵ�</returns>
    public Dictionary<string, int> GetAllItemCountsDict()
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
        foreach (var slot in _slots)
        {
            if (slot.IsOccupied && slot.Item != null && slot.ItemCount > 0)
                return false;
        }
        return true;
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
        return GetItemCount(itemId) >= requiredCount;
    }

    /// <summary>
    /// ͨ������ģ����ѯ��Ʒ
    /// </summary>
    /// <param name="namePattern">����ģʽ��֧�ֲ���ƥ��</param>
    /// <returns>��������ģʽ����Ʒ�б�</returns>
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

    #region �Ƴ���Ʒ
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
        int totalCount = GetItemCount(itemId);
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

                if (removeAmount == slot.ItemCount)
                {
                    slot.ClearSlot();
                    UpdateEmptySlotCache(slotIndex, true);
                    UpdateItemCache(itemId, slotIndex, false);
                }
                else
                {
                    slot.SetItem(slot.Item, slot.ItemCount - removeAmount);
                }

                affectedSlots.Add(slotIndex);

                // ��λ��Ʒ��������¼�
                RaiseSlotItemCountChangedEvent(slotIndex, item, oldCount, slot.ItemCount);
            }

            // �Ƴ��ɹ��¼�
            OnItemRemoved?.Invoke(itemId, count, affectedSlots);
            CheckAndRaiseItemTotalCountChanged(itemId);
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
        }
        else
        {
            slot.SetItem(item, slot.ItemCount - count);
        }

        // ������Ʒ��������¼�
        RaiseSlotItemCountChangedEvent(index, item, oldCount, slot.ItemCount);

        // ������Ʒ�Ƴ��¼�
        OnItemRemoved?.Invoke(itemId, count, new List<int> { index });
        CheckAndRaiseItemTotalCountChanged(itemId, item);

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
    public virtual (AddItemResult result, int addedCount) AddItemsWithCount(IItem item, out int exceededCount, int count = 1, int slotIndex = -1)
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
            return (AddItemResult.Success, 0);

        if (!CheckContainerCondition(item))
        {
            OnItemAddFailed?.Invoke(item, count, AddItemResult.ItemConditionNotMet);
            return (AddItemResult.ItemConditionNotMet, 0);
        }

        int totalAdded = 0;
        int remainingCount = count;



        // 1. �����Ʒ�ɶѵ�����δָ����λ�����ȳ��Զѵ�
        if (item.IsStackable && slotIndex == -1)
        {
            var (stackedCount, stackedSlots, slotChanges) = TryStackItems(item, remainingCount);

            if (stackedCount > 0)
            {
                totalAdded += stackedCount;
                remainingCount -= stackedCount;
                affectedSlots.AddRange(stackedSlots);

                // ������������¼�
                foreach (var change in slotChanges)
                {
                    int slotIdx = change.Key;
                    var slot = _slots[slotIdx];
                    RaiseSlotItemCountChangedEvent(slotIdx, slot.Item, change.Value.oldCount, change.Value.newCount);
                }

                CheckAndRaiseItemTotalCountChanged(item.ID, item);

                if (remainingCount <= 0)
                {
                    // ȫ���ѵ��ɹ�
                    OnItemAdded?.Invoke(item, totalAdded, affectedSlots);
                    return (AddItemResult.Success, totalAdded);
                }
            }
        }

        // 2. ���ָ���˲�λ�����һ���ʣ����Ʒ
        if (slotIndex >= 0 && remainingCount > 0)
        {
            var (success, addedCount, newRemaining) = TryAddToSpecificSlot(item, slotIndex, remainingCount);

            if (success)
            {
                totalAdded += addedCount;
                remainingCount = newRemaining;
                affectedSlots.Add(slotIndex);

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

        // 3. ����ʣ����Ʒ�����Է���ղ�λ�򴴽��²�λ
        while (remainingCount > 0)
        {
            // ������ӵ��ղ�λ
            var (emptySlotSuccess, emptyAddedCount, emptyRemaining, emptySlotIndex) =
                TryAddToEmptySlot(item, remainingCount);

            if (emptySlotSuccess)
            {
                totalAdded += emptyAddedCount;
                remainingCount = emptyRemaining;
                affectedSlots.Add(emptySlotIndex);

                if (remainingCount <= 0)
                {
                    OnItemAdded?.Invoke(item, totalAdded, affectedSlots);
                    CheckAndRaiseItemTotalCountChanged(item.ID, item);
                    return (AddItemResult.Success, totalAdded);
                }

                continue; // ����������һ����λ
            }

            // ���Դ����²�λ
            var (newSlotSuccess, newAddedCount, newRemaining, newSlotIndex) =
                TryAddToNewSlot(item, remainingCount);

            if (newSlotSuccess)
            {
                totalAdded += newAddedCount;
                remainingCount = newRemaining;
                affectedSlots.Add(newSlotIndex);

                CheckAndRaiseItemTotalCountChanged(item.ID, item);

                if (remainingCount <= 0)
                {
                    OnItemAdded?.Invoke(item, totalAdded, affectedSlots);
                    return (AddItemResult.Success, totalAdded);
                }

                continue; // �������ʣ����Ʒ
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
                // ����Ƿ��������в�λ���ѱ�ռ��
                bool noEmptySlots = !_slots.Any(s => !s.IsOccupied);
                AddItemResult result = noEmptySlots ? AddItemResult.ContainerIsFull : AddItemResult.NoSuitableSlotFound;
                OnItemAddFailed?.Invoke(item, count, result);
                return (result, 0);
            }
        }

        // ������Ʒ���ɹ����
        OnItemAdded?.Invoke(item, totalAdded, affectedSlots);
        CheckAndRaiseItemTotalCountChanged(item.ID, item);
        return (AddItemResult.Success, totalAdded);
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
    #endregion

    #region �м䴦��API

    /// <summary>
    /// ���Խ���Ʒ�ѵ���������ͬ��Ʒ�Ĳ�λ��
    /// </summary>
    protected virtual (int stackedCount, List<int> affectedSlots, Dictionary<int, (int oldCount, int newCount)> changes)
        TryStackItems(IItem item, int remainingCount)
    {
        int stackedCount = 0;
        List<int> affectedSlots = new(8);
        Dictionary<int, (int oldCount, int newCount)> slotChanges = new();

        // ��Ʒ�������
        if (_itemSlotIndexCache.TryGetValue(item.ID, out var slotIndices) && slotIndices.Count > 0)
        {
            // �������и���Ʒ�Ĳ�λ
            foreach (int i in slotIndices)
            {
                if (remainingCount <= 0) break;

                var slot = _slots[i];
                if (slot.IsOccupied && slot.Item != null && !slot.HasMultiSlotItem)
                {
                    // ��ȡ�򻺴����ѵ���
                    int maxStack;
                    if (!_itemMaxStackCache.TryGetValue(item.ID, out maxStack))
                    {
                        maxStack = slot.Item.MaxStackCount;
                        _itemMaxStackCache[item.ID] = maxStack;
                    }

                    if (maxStack <= 0 || slot.ItemCount < maxStack)
                    {
                        int oldCount = slot.ItemCount;
                        int canAddCount = maxStack <= 0 ? remainingCount :
                                          Mathf.Min(remainingCount, maxStack - slot.ItemCount);

                        if (canAddCount > 0)
                        {
                            IItem existingItem = slot.Item;
                            if (slot.SetItem(existingItem, slot.ItemCount + canAddCount))
                            {
                                remainingCount -= canAddCount;
                                stackedCount += canAddCount;
                                affectedSlots.Add(i);
                                slotChanges[i] = (oldCount, slot.ItemCount);
                            }
                        }
                    }
                }
            }
        }
        else
        {
            // ����δ����
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
                            canAddCount = remainingCount; // ���޶ѵ�
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
                                slotChanges[i] = (oldCount, slot.ItemCount);

                                // ���»���
                                UpdateItemCache(item.ID, i, true);
                                if (!_itemMaxStackCache.ContainsKey(item.ID))
                                    _itemMaxStackCache[item.ID] = existingItem.MaxStackCount;
                            }
                        }
                    }
                }
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
                RaiseSlotItemCountChangedEvent(slotIndex, targetSlot.Item, oldCount, targetSlot.ItemCount);
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
                RaiseSlotItemCountChangedEvent(slotIndex, targetSlot.Item, 0, targetSlot.ItemCount);
                return (true, addCount, remainingCount - addCount);
            }
        }

        return (false, 0, remainingCount);
    }

    /// <summary>
    /// �����ҵ����õĿղ�λ�������Ʒ
    /// </summary>
    protected virtual (bool success, int addedCount, int remainingCount, int slotIndex)
        TryAddToEmptySlot(IItem item, int remainingCount)
    {
        // �ղ�λ�������
        foreach (var i in _emptySlotIndices.ToArray())
        {
            if (i < _slots.Count)
            {
                var slot = _slots[i];
                if (!slot.IsOccupied && slot.CheckSlotCondition(item))
                {
                    // ��ȡ�򻺴����ѵ���
                    int maxStack;
                    if (!_itemMaxStackCache.TryGetValue(item.ID, out maxStack))
                    {
                        maxStack = item.MaxStackCount;
                        _itemMaxStackCache[item.ID] = maxStack;
                    }

                    int addCount = item.IsStackable && maxStack > 0 ?
                                  Mathf.Min(remainingCount, maxStack) : 1;

                    if (slot.SetItem(item, addCount))
                    {
                        // ���»���
                        _emptySlotIndices.Remove(i);
                        UpdateItemCache(item.ID, i, true);

                        // �����������
                        RaiseSlotItemCountChangedEvent(i, slot.Item, 0, slot.ItemCount);
                        return (true, addCount, remainingCount - addCount, i);
                    }
                }
            }
        }

        // �������δ���У����˵���������
        for (int i = 0; i < _slots.Count; i++)
        {
            if (!_emptySlotIndices.Contains(i)) // �����Ѽ��Ŀղ�λ
            {
                var slot = _slots[i];
                if (!slot.IsOccupied && slot.CheckSlotCondition(item))
                {
                    int addCount = item.IsStackable && item.MaxStackCount > 0 ?
                                  Mathf.Min(remainingCount, item.MaxStackCount) : 1;

                    if (slot.SetItem(item, addCount))
                    {
                        // ���»���
                        UpdateItemCache(item.ID, i, true);

                        // �����������
                        RaiseSlotItemCountChangedEvent(i, slot.Item, 0, slot.ItemCount);
                        return (true, addCount, remainingCount - addCount, i);
                    }
                }
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
                // �����������
                RaiseSlotItemCountChangedEvent(newSlotIndex, newSlot.Item, 0, newSlot.ItemCount);
                return (true, addCount, remainingCount - addCount, newSlotIndex);
            }
        }

        return (false, 0, remainingCount, -1);
    }
    #endregion
}