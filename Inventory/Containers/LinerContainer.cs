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
}