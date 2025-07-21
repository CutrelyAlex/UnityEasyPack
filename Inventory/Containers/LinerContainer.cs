using EasyPack;
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
                UpdateItemTotalCount(sourceItem.ID);
            }
            else
            {
                // �����ƶ���������
                int remainingCount = sourceCount - addedCount;
                sourceSlot.SetItem(sourceItem, remainingCount);

                UpdateItemCountCache(sourceItem.ID, -addedCount);
                UpdateItemTotalCount(sourceItem.ID, sourceItem);

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
        // 1. �ռ�������Ʒ
        var allItems = new List<(IItem item, int count)>();
        foreach (var slot in _slots.ToArray())
        {
            if (slot.IsOccupied && slot.Item != null)
            {
                allItems.Add((slot.Item, slot.ItemCount));
                slot.ClearSlot();
            }
        }

        // 2. ����ƷID���鲢����
        var groupedItems = allItems
            .GroupBy(x => x.item.ID)
            .OrderBy(g => g.First().item.Type)
            .ThenBy(g => g.First().item.Name)
            .ToList();

        // 3. ���·Żر���
        foreach (var group in groupedItems)
        {
            var itemTemplate = group.First().item;
            int totalCount = group.Sum(x => x.count);

            // ��ӻر���
            var result = AddItems(itemTemplate, totalCount);
            if (result.result != AddItemResult.Success)
            {
                Debug.LogWarning($"���򱳰�ʱ�޷����������Ʒ: {itemTemplate.Name}, ���: {result.result}");
            }
        }
    }
}