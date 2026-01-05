namespace EasyPack.InventorySystem
{
public partial class InventoryService
    {
        #region 全局物品搜索

        /// <summary>
        ///     全局物品搜索结果
        /// </summary>
        public struct GlobalItemResult
        {
            public string ContainerId;
            public int SlotIndex;
            public IItem Item;
            public int IndexCount;

            public GlobalItemResult(string containerId, int slotIndex, IItem item, int count)
            {
                ContainerId = containerId;
                SlotIndex = slotIndex;
                Item = item;
                IndexCount = count;
            }
        }

        #endregion
    }
}