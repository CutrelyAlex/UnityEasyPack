using EasyPack;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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

    private readonly Dictionary<string, int> _itemTotalCounts = new Dictionary<string, int>();

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
    /// <param name="skipEmptySlots">�Ƿ���������Ϊ0�Ĳ�λ</param>
    /// <returns>��Ʒ���ڵĲ�λ�����б�</returns>
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
        return GetAllItemCounts().Count;
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
        if (totalCount < count)
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
        List<(ISlot slot, int removeAmount, int slotIndex)> removals = new List<(ISlot, int, int)>();

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

        // �����Ʒ�ɶѵ������ȳ��Զѵ�
        if (item.IsStackable && slotIndex == -1)
        {
            int stackedCount = 0;
            Dictionary<int, (int oldCount, int newCount)> stackSlotChanges = new Dictionary<int, (int, int)>();

            // ���Զѵ���Ʒ
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
                                stackSlotChanges[i] = (oldCount, slot.ItemCount);

                                if (remainingCount <= 0)
                                    break;
                            }
                        }
                    }
                }
            }

            // ����ѵ��Ľ��
            if (stackedCount > 0)
            {
                totalAdded += stackedCount;

                // ������������Ͳ�λ����¼�
                foreach (var change in stackSlotChanges)
                {
                    int slotIdx = change.Key;
                    var slot = _slots[slotIdx];
                    RaiseSlotItemCountChangedEvent(slotIdx, slot.Item, change.Value.oldCount, change.Value.newCount);
                    CheckAndRaiseItemTotalCountChanged(item.ID, item);
                }

                if (remainingCount <= 0)
                {
                    // ȫ���ѵ��ɹ�
                    OnItemAdded?.Invoke(item, totalAdded, affectedSlots);
                    return (AddItemResult.Success, totalAdded);
                }
            }
        }

        // ���ָ���˲�λ�����һ���ʣ����Ʒ��Ҫ���
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

                // �����������
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

        // ����ʣ����Ʒ�򲻿ɶѵ���Ʒ�����Է���ղ�λ�����ߴ����²�λ
        while (remainingCount > 0)
        {
            bool foundSlot = false;

            // Ѱ�ҿ��в�λ
            for (int i = 0; i < _slots.Count; i++)
            {
                var slot = _slots[i];
                if (!slot.IsOccupied && slot.CheckSlotCondition(item))
                {
                    int oldCount = 0; // �ղ�λ������Ϊ0
                    int addCount;

                    if (item.IsStackable && item.MaxStackCount > 0)
                    {
                        addCount = Mathf.Min(remainingCount, item.MaxStackCount);
                    }
                    else
                    {
                        addCount = 1; // ���ɶѵ���Ʒ
                    }

                    if (slot.SetItem(item, addCount))
                    {
                        totalAdded += addCount;
                        remainingCount -= addCount;
                        foundSlot = true;
                        affectedSlots.Add(i);

                        // ������������Ͳ�λ����¼�
                        RaiseSlotItemCountChangedEvent(i, slot.Item, oldCount, slot.ItemCount);
                        // CheckAndRaiseItemTotalCountChanged(item.ID, item);

                        if (remainingCount <= 0)
                        {
                            OnItemAdded?.Invoke(item, totalAdded, affectedSlots);
                            return (AddItemResult.Success, totalAdded);
                        }

                        break; // ����������һ����λ
                    }
                }
            }

            // ���û���ҵ����ʵĲ�λ�������Դ����²�λ
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
                    addCount = 1; // ���ɶѵ���Ʒ
                }

                if (newSlot.CheckSlotCondition(item) && newSlot.SetItem(item, addCount))
                {
                    _slots.Add(newSlot);
                    totalAdded += addCount;
                    remainingCount -= addCount;
                    foundSlot = true;
                    affectedSlots.Add(newSlotIndex);

                    // ������������Ͳ�λ����¼�
                    RaiseSlotItemCountChangedEvent(newSlotIndex, newSlot.Item, 0, newSlot.ItemCount);
                    CheckAndRaiseItemTotalCountChanged(item.ID, item);

                    if (remainingCount <= 0)
                    {
                        OnItemAdded?.Invoke(item, totalAdded, affectedSlots);
                        return (AddItemResult.Success, totalAdded);
                    }
                }
            }

            // ���û���ҵ����ʵĲ�λ��Ҳ���ܴ����²�λ�����ж����
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
                    // ����Ƿ��������в�λ���ѱ�ռ��
                    bool noEmptySlots = !_slots.Any(s => !s.IsOccupied);
                    AddItemResult result = noEmptySlots ? AddItemResult.ContainerIsFull : AddItemResult.NoSuitableSlotFound;
                    OnItemAddFailed?.Invoke(item, count, result);
                    return (result, 0);
                }
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
        return AddItems(item, out _, count, slotIndex);
    }
    #endregion
}