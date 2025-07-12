using EasyPack;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class Container : IContainer
{
    #region ��������
    public string ID { get; }
    public string Name { get; }
    public string Type { get; set; }
    public int Capacity { get; set; } // -1��ʾ��������
    public abstract bool IsGrid { get; } // ����ʵ�֣������Ƿ�Ϊ��������
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

    #region ״̬���
    /// <summary>
    /// ��������Ƿ�����
    /// �������в�λ����ռ�ã���ÿ��ռ�õĲ�λ��Ʒ�����ɶѵ����Ѵﵽ�ѵ�����ʱ�������ű���Ϊ������
    /// </summary>
    public virtual bool Full
    {
        get
        {
            // �������������������Զ������
            if (Capacity <= 0)
                return false;

            // �����λ����С����������������
            if (_slots.Count < Capacity)
                return false;

            // ����Ƿ����в�λ���ѱ�ռ��
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

                // û�пɶѵ��Ĳ�λ
                if (!hasStackableSlot)
                    return AddItemResult.StackLimitReached;
            }
            else
            {
                // ��Ʒ���ɶѵ�����������
                return AddItemResult.ContainerIsFull;
            }
        }

        return AddItemResult.Success;
    }
    #endregion

    #region ��Ʒ����
    /// <summary>
    /// ���Խ���Ʒ�ѵ���������ͬ��Ʒ�Ĳ�λ
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
                if (slot.Item.MaxStackCount <= 0 || slot.ItemCount < slot.Item.MaxStackCount) // ʹ�ò�λ����Ʒ��MaxStackCount
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
    /// ���Խ�ָ����������Ʒ�ѵ���������ͬ��Ʒ�Ĳ�λ
    /// </summary>
    /// <param name="item">Ҫ�ѵ�����Ʒ</param>
    /// <param name="count">Ҫ�ѵ�������</param>
    /// <param name="result">�ѵ����</param>
    /// <param name="exceededCount">�����޷��ѵ�������</param>
    /// <returns>�ɹ��ѵ�������</returns>
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
    /// �Ƴ�ָ��ID����Ʒ
    /// </summary>
    /// <param name="itemId">��ƷID</param>
    /// <param name="count">�Ƴ�����</param>
    /// <returns>�Ƴ����</returns>
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

        // ����Ƴ��˲�����Ʒ����������
        if (remainingCount < count)
            return RemoveItemResult.InsufficientQuantity;

        // ���û���ҵ��κ���Ʒ
        return RemoveItemResult.ItemNotFound;
    }

    /// <summary>
    /// ��ָ����λ�Ƴ���Ʒ
    /// </summary>
    /// <param name="index">��λ����</param>
    /// <param name="count">�Ƴ�����</param>
    /// <returns>�Ƴ����</returns>
    public virtual RemoveItemResult RemoveItemAtIndex(int index, int count = 1, string expectedItemId = null)
    {
        if (index < 0 || index >= _slots.Count)
            return RemoveItemResult.SlotNotFound;

        var slot = _slots[index];

        if (!slot.IsOccupied || slot.Item == null)
            return RemoveItemResult.ItemNotFound;

        // ����ṩ��Ԥ�ڵ���ƷID������֤
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

        if (item == null)
            return (AddItemResult.ItemIsNull, 0);

        if (count <= 0)
            return (AddItemResult.Success, 0);

        if (!CheckContainerCondition(item))
            return (AddItemResult.ItemConditionNotMet, 0);

        int totalAdded = 0;
        int remainingCount = count;

        // �����Ʒ�ɶѵ������ȳ��Զѵ�
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

        // ���ָ���˲�λ�����һ���ʣ����Ʒ��Ҫ���
        if (slotIndex >= 0 && remainingCount > 0)
        {
            if (slotIndex >= _slots.Count)
                return (AddItemResult.SlotNotFound, totalAdded);

            var targetSlot = _slots[slotIndex];

            if (!targetSlot.CheckSlotCondition(item))
                return (AddItemResult.ItemConditionNotMet, totalAdded);

            // ����������Ʒ����λ
            int addCount = item.IsStackable && item.MaxStackCount > 0 ?
                           Mathf.Min(remainingCount, item.MaxStackCount) :
                           remainingCount;

            if (targetSlot.SetItem(item, addCount))
            {
                totalAdded += addCount;
                remainingCount -= addCount;

                if (remainingCount <= 0)
                    return (AddItemResult.Success, totalAdded);
                // �����������ʣ����Ʒ
            }
            else
            {
                return (AddItemResult.NoSuitableSlotFound, totalAdded);
            }
        }

        // ����ʣ����Ʒ�򲻿ɶѵ���Ʒ�����Է���ղ�λ�����ߴ����²�λ
        while (remainingCount > 0)
        {
            bool foundSlot = false;

            // Ѱ�ҿ��в�λ
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
                        addCount = 1; // ���ɶѵ���Ʒ
                    }

                    if (slot.SetItem(item, addCount))
                    {
                        totalAdded += addCount;
                        remainingCount -= addCount;
                        foundSlot = true;

                        if (remainingCount <= 0)
                            return (AddItemResult.Success, totalAdded);

                        break; // ����������һ����λ
                    }
                }
            }

            // ���û���ҵ����ʵĲ�λ�������Դ����²�λ
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
                    addCount = 1; // ���ɶѵ���Ʒ
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

            // ���û���ҵ����ʵĲ�λ��Ҳ���ܴ����²�λ�����ж����
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
                    // ����Ƿ��������в�λ���ѱ�ռ��
                    bool noEmptySlots = !_slots.Any(s => !s.IsOccupied);
                    return (noEmptySlots ? AddItemResult.ContainerIsFull : AddItemResult.NoSuitableSlotFound, 0);
                }
            }
        }

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
}