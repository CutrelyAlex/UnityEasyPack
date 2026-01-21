namespace EasyPack.InventorySystem
{
    public enum AddItemResult
    {
        Success,
        ItemIsNull,
        ContainerIsFull,
        StackLimitReached,
        SlotNotFound,
        ItemConditionNotMet,
        NoSuitableSlotFound,
        AddNothingLOL,
        InvalidCount,
        /// <summary>
        /// ItemFactory未设置，无法通过ItemData/itemId创建物品
        /// </summary>
        FactoryNotAvailable,
        /// <summary>
        /// ItemFactory创建物品失败（可能ItemData配置问题）
        /// </summary>
        FactoryCreateFailed,
        /// <summary>
        /// 指定的itemId未在ItemFactory中注册
        /// </summary>
        ItemNotFound,
    }
}