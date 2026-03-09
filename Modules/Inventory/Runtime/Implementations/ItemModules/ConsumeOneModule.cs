namespace EasyPack.InventorySystem
{
    /// <summary>
    ///     默认消耗模块：使用物品时从所在容器消耗自身一个。
    /// </summary>
    public class ConsumeOneModule : IItemModule
    {
        public static readonly ConsumeOneModule Default = new();

        public string ModuleId => "consume_one";

        /// <inheritdoc/>
        public bool CanUse(IItem item, object context = null) =>
            item != null && item.Count > 0;

        /// <inheritdoc/>
        public UseItemResult Use(IItem item, IInventoryService service, object context = null)
        {
            if (item == null)    return UseItemResult.ItemIsNull;
            if (item.Count <= 0) return UseItemResult.InsufficientCount;

            var results = service.FindItemGlobally(item.ID);
            if (results == null || results.Count == 0)
                return UseItemResult.Failed;

            foreach (var result in results)
            {
                // 通过 UID 匹配；UID 未分配时使用第一个匹配槽位
                if (item.ItemUID >= 0 && result.Item.ItemUID != item.ItemUID)
                    continue;

                Container container = service.GetContainer(result.ContainerId);
                if (container == null) continue;

                RemoveItemResult removeResult =
                    container.RemoveItemAtIndex(result.SlotIndex, 1, item.ID);

                return removeResult == RemoveItemResult.Success
                    ? UseItemResult.Success
                    : UseItemResult.Failed;
            }

            return UseItemResult.Failed;
        }
    }
}
