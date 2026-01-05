namespace EasyPack.InventorySystem
{
    /// <summary>
    ///     物品查找缓存结果
    /// </summary>
    public struct ItemLookupResult
    {
        public IItem Item;
        public int FirstSlotIndex;
        public int TotalCount;
        public bool Found => Item != null;
    }   
}