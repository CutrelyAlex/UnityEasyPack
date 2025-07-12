using System.Numerics;
using UnityEngine;

namespace EasyPack
{
    public interface ISlot
    {
        int Index { get; }
        IItem Item { get; }
        int ItemCount { get; }
        bool IsOccupied { get; } // 是否有物品
        bool HasMultiSlotItem { get; } // 是否被多格物品占据
        Vector2Int MultiSlotItemPosition { get; } // 对应该多格物品的索引位置相对坐标
        public IContainer Container { get; set; } // 所属容器
        CustomItemCondition SlotCondition { get; }
        bool CheckSlotCondition(IItem item);
        bool SetItem(IItem item, int count = 1);
        void ClearSlot();

    }
}