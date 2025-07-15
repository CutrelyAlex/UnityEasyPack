using UnityEngine;

namespace EasyPack
{
    public class Slot : ISlot
    {
        public int Index { get; set; }

        public IItem Item { get; set; }

        public int ItemCount { get; set; }

        public bool IsOccupied { get; set; }

        public bool HasMultiSlotItem { get; set; } // �Ƿ��Ƕ����Ʒռ��
        public Vector2Int MultiSlotItemPosition { get; private set; } = new Vector2Int(0, 0);
        public CustomItemCondition SlotCondition { get; set; }
        public IContainer Container { get; set; }

        public bool SetItem(IItem item, int count = 1)
        {

            if (IsOccupied && Item != null && item != null && Item.ID == item.ID)
            {
                ItemCount = count;
                return true;
            }

            if (item != null && item.IsMultiSlot)
            {
                Debug.LogWarning($"Falied to Set {item.ID} at {Container.ID}.Use SetAsMultiSlotPart for multislot items Instead.");
                return false;
            }

            if (item == null || count <= 0)
                return false;

            // ������Ʒ������Ϣ
            Item = item;
            ItemCount = count;
            IsOccupied = true;

            return true;
        }

        /// <summary>
        /// ���ò�λΪ���λ��Ʒ��һ���֣�����¼���������λ��λ��
        /// </summary>
        /// <param name="item">���λ��Ʒ</param>
        /// <param name="position">���������λ��λ��</param>
        public void SetAsMultiSlotPart(IItem item, Vector2Int position,int count = 1)
        {
            Item = item;
            HasMultiSlotItem = true;
            MultiSlotItemPosition = position;
            ItemCount = count;
        }

        public int GetItemCount()
        {
            return ItemCount;
        }


        public bool CheckSlotCondition(IItem item)
        {
            if (item == null)
                return false;

            //if (IsOccupied)
            //    return false;

            if (SlotCondition != null && !SlotCondition.IsCondition(item))
                return false;


            return true;
        }
        public void ClearSlot()
        {
            Item = null;
            ItemCount = 0;
            IsOccupied = false;
            HasMultiSlotItem = false;
            MultiSlotItemPosition = Vector2Int.zero;
        }
    }
}