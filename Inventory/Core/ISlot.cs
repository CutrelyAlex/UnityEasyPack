using System.Numerics;
using UnityEngine;

namespace EasyPack
{
    public interface ISlot
    {
        int Index { get; }
        IItem Item { get; }
        int ItemCount { get; }
        bool IsOccupied { get; } // �Ƿ�����Ʒ
        bool HasMultiSlotItem { get; } // �Ƿ񱻶����Ʒռ��
        Vector2Int MultiSlotItemPosition { get; } // ��Ӧ�ö����Ʒ������λ���������
        public IContainer Container { get; set; } // ��������
        CustomItemCondition SlotCondition { get; }
        bool CheckSlotCondition(IItem item);
        bool SetItem(IItem item, int count = 1);
        void ClearSlot();

    }
}