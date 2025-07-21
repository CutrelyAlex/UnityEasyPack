using EasyPack;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LinerContainer : Container
{
    public override bool IsGrid => false;

    public override Vector2 Grid => new(-1,-1);


    /// <summary>
    /// ����һ����������
    /// </summary>
    /// <param name="id">����ID</param>
    /// <param name="name">��������</param>
    /// <param name="type">��������</param>
    /// <param name="capacity">������-1��ʾ����</param>
    public LinerContainer(string id, string name, string type, int capacity = -1)
        : base(id, name, type, capacity)
    {
        // ��ʼ����λ
        if (capacity > 0)
        {
            for (int i = 0; i < capacity; i++)
            {
                var slot = new Slot
                {
                    Index = i,
                    Container = this
                };
                _slots.Add(slot);
            }
        }
    }

    /// <summary>
    /// ��Ʒ�ƶ�����
    /// </summary>
    /// <param name="sourceSlotIndex">Դ��λ����</param>
    /// <param name="targetContainer">Ŀ������</param>
    /// <returns>�ƶ����</returns>
    public bool MoveItemToContainer(int sourceSlotIndex, IContainer targetContainer)
    {
        // ������֤�Ż�
        if (sourceSlotIndex < 0 || sourceSlotIndex >= _slots.Count)
            return false;

        var sourceSlot = _slots[sourceSlotIndex];
        if (!sourceSlot.IsOccupied || sourceSlot.Item == null)
            return false;

        var sourceItem = sourceSlot.Item;
        int sourceCount = sourceSlot.ItemCount;

        if (targetContainer is Container targetContainerImpl)
        {
            if (!targetContainerImpl.ValidateItemCondition(sourceItem))
                return false;
        }

        // ������ӵ�Ŀ������
        var (result, addedCount) = targetContainer.AddItems(sourceItem, sourceCount);

        // ������ӽ������Դ��λ
        if (result == AddItemResult.Success && addedCount > 0)
        {
            if (addedCount == sourceCount)
            {
                // ��ȫ�ƶ�ֱ�������λ
                sourceSlot.ClearSlot();

                UpdateEmptySlotCache(sourceSlotIndex, true);
                UpdateItemCache(sourceItem.ID, sourceSlotIndex, false);
                UpdateItemTypeCache(sourceItem.Type, sourceSlotIndex, false);
                UpdateItemCountCache(sourceItem.ID, -sourceCount);
                TriggerItemTotalCountChanged(sourceItem.ID);
            }
            else
            {
                // �����ƶ���������
                int remainingCount = sourceCount - addedCount;
                sourceSlot.SetItem(sourceItem, remainingCount);

                UpdateItemCountCache(sourceItem.ID, -addedCount);
                TriggerItemTotalCountChanged(sourceItem.ID, sourceItem);

                OnSlotQuantityChanged(sourceSlotIndex, sourceItem, sourceCount, remainingCount);
            }

            return true;
        }

        return false;
    }
    /// <summary>
    /// ��������
    /// </summary>
    public void SortInventory()
    {
        if (_slots.Count < 2)
            return;

        var occupiedSlots = new List<(int index, IItem item, int count)>();
        for (int i = 0; i < _slots.Count; i++)
        {
            var slot = _slots[i];
            if (slot.IsOccupied && slot.Item != null)
            {
                occupiedSlots.Add((i, slot.Item, slot.ItemCount));
            }
        }

        if (occupiedSlots.Count < 2)
            return;

        occupiedSlots.Sort((a, b) => {
            int typeCompare = string.Compare(a.item.Type, b.item.Type);
            if (typeCompare != 0)
                return typeCompare;
            return string.Compare(a.item.Name, b.item.Name);
        });

        var originalData = new (IItem item, int count)[_slots.Count];
        for (int i = 0; i < _slots.Count; i++)
        {
            var slot = _slots[i];
            originalData[i] = (slot.Item, slot.ItemCount);
        }

        BeginBatchUpdate();

        try
        {
            // ��������в�λ
            foreach (var slot in _slots)
            {
                slot.ClearSlot();
            }

            // ��������˳������λ
            for (int i = 0; i < occupiedSlots.Count; i++)
            {
                var (_, item, count) = occupiedSlots[i];
                _slots[i].SetItem(item, count);
            }

            RebuildCaches();
        }
        catch (Exception ex)
        {
            Debug.LogError($"���򱳰�ʱ��������: {ex.Message}�����ڻָ�ԭʼ����");
            for (int i = 0; i < _slots.Count && i < originalData.Length; i++)
            {
                if (originalData[i].item != null)
                    _slots[i].SetItem(originalData[i].item, originalData[i].count);
                else
                    _slots[i].ClearSlot();
            }

            RebuildCaches();
        }
        finally
        {
            EndBatchUpdate();
        }
    }
    /// <summary>
    /// �ϲ���ͬ��Ʒ�����ٵĲ�λ��
    /// </summary>
    public void ConsolidateItems()
    {
        if (_slots.Count < 2)
            return;

        // ��¼ԭʼ�����Է�����ʱ�ָ�
        var originalData = new (IItem item, int count)[_slots.Count];
        for (int i = 0; i < _slots.Count; i++)
        {
            var slot = _slots[i];
            originalData[i] = (slot.Item, slot.ItemCount);
        }

        BeginBatchUpdate();

        try
        {
            // ����ƷID�����ռ����пɶѵ���Ʒ
            var itemGroups = new Dictionary<string, List<(int slotIndex, IItem item, int count)>>();

            for (int i = 0; i < _slots.Count; i++)
            {
                var slot = _slots[i];
                if (slot.IsOccupied && slot.Item != null && slot.Item.IsStackable)
                {
                    string itemId = slot.Item.ID;
                    if (!itemGroups.ContainsKey(itemId))
                        itemGroups[itemId] = new List<(int, IItem, int)>();

                    itemGroups[itemId].Add((i, slot.Item, slot.ItemCount));
                }
            }

            // ��ÿ����Ʒ���кϲ�
            foreach (var kvp in itemGroups)
            {
                var itemSlots = kvp.Value;
                if (itemSlots.Count < 2) continue; // ֻ��һ����λ����Ʒ����Ҫ�ϲ�

                var firstSlot = itemSlots[0];
                IItem item = firstSlot.item;
                int maxStackCount = item.MaxStackCount;

                // ����������
                int totalCount = itemSlots.Sum(slot => slot.count);

                // ���������ز�λ
                foreach (var (slotIndex, _, _) in itemSlots)
                {
                    _slots[slotIndex].ClearSlot();
                }

                // ���·��䵽�������ٵĲ�λ
                var targetSlots = itemSlots.OrderBy(s => s.slotIndex).ToList();
                int remainingCount = totalCount;
                int targetIndex = 0;

                while (remainingCount > 0 && targetIndex < targetSlots.Count)
                {
                    int slotIndex = targetSlots[targetIndex].slotIndex;

                    // ȷ������λ���õ�����
                    int countForThisSlot;
                    if (maxStackCount <= 0) // ���޶ѵ�
                    {
                        countForThisSlot = remainingCount;
                        remainingCount = 0;
                    }
                    else
                    {
                        countForThisSlot = Math.Min(remainingCount, maxStackCount);
                        remainingCount -= countForThisSlot;
                    }

                    // ������Ʒ����λ
                    _slots[slotIndex].SetItem(item, countForThisSlot);
                    targetIndex++;
                }
            }

            // �ؽ�����
            RebuildCaches();
        }
        catch (Exception ex)
        {
            Debug.LogError($"�ϲ���Ʒʱ��������: {ex.Message}�����ڻָ�ԭʼ����");

            // �ָ�ԭʼ����
            for (int i = 0; i < _slots.Count && i < originalData.Length; i++)
            {
                if (originalData[i].item != null)
                    _slots[i].SetItem(originalData[i].item, originalData[i].count);
                else
                    _slots[i].ClearSlot();
            }

            RebuildCaches();
        }
        finally
        {
            EndBatchUpdate();
        }
    }
    /// <summary>
    /// �������������� + �ϲ���
    /// </summary>
    public void OrganizeInventory()
    {
        // �Ⱥϲ���ͬ��Ʒ
        ConsolidateItems();

        // �ٽ�����������
        SortInventory();
    }
}