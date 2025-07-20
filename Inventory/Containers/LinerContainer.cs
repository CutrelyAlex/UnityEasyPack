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
        if (sourceSlotIndex < 0 || sourceSlotIndex >= _slots.Count)
            return false;

        var sourceSlot = _slots[sourceSlotIndex];
        if (!sourceSlot.IsOccupied || sourceSlot.Item == null)
            return false;

        IItem sourceItem = sourceSlot.Item;
        int sourceCount = sourceSlot.ItemCount;

        bool isStackable = sourceItem.IsStackable;

        IItem itemToAdd;
        if (!isStackable)
        {
            if (sourceItem is Item itemObj)
            {
                itemToAdd = itemObj.Clone();
            }
            else
            {
                itemToAdd = new Item
                {
                    ID = sourceItem.ID,
                    Name = sourceItem.Name,
                    Type = sourceItem.Type,
                    Description = sourceItem.Description,
                    Weight = sourceItem.Weight,
                    IsStackable = false,
                    MaxStackCount = sourceItem.MaxStackCount,
                    IsMultiSlot = sourceItem.IsMultiSlot,
                    Size = sourceItem.Size
                };

                if (sourceItem.Attributes != null)
                {
                    ((Item)itemToAdd).Attributes = new Dictionary<string, object>(sourceItem.Attributes);
                }
            }
        }
        else
        {
            itemToAdd = sourceItem;
        }

        var (result, addedCount) = targetContainer.AddItems(itemToAdd, sourceCount);

        if (result == AddItemResult.Success)
        {
            // ��ȫ�ƶ�
            if (addedCount == sourceCount)
            {
                // ���Դ��λ
                sourceSlot.ClearSlot();
                return true;
            }
            // �����ƶ�
            else if (addedCount > 0)
            {
                // ����Դ��λ����Ʒ����
                sourceSlot.SetItem(sourceItem, sourceCount - addedCount);
                return true;
            }
        }

        return true;
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