namespace EasyPack.InventorySystem
{
    /// <summary>
    ///     物品工厂接口，负责ItemData注册和Item实例创建。
    /// </summary>
    public interface IItemFactory
    {
        /// <summary>
        ///     注册ItemData模板
        /// </summary>
        /// <param name="itemId">物品ID</param>
        /// <param name="itemData">物品模板数据</param>
        void Register(string itemId, ItemData itemData);

        /// <summary>
        ///     根据ItemData创建Item实例
        /// </summary>
        /// <param name="itemData">物品模板数据</param>
        /// <param name="count">物品数量（默认1）</param>
        /// <returns>创建的Item实例</returns>
        IItem CreateItem(ItemData itemData, int count = 1);

        /// <summary>
        ///     根据物品ID创建Item实例
        /// </summary>
        /// <param name="itemId">物品ID</param>
        /// <param name="count">物品数量（默认1）</param>
        /// <returns>创建的Item实例，若物品未注册则返回null或抛异常</returns>
        IItem CreateItem(string itemId, int count = 1);

        /// <summary>
        ///     克隆物品，生成新的Item实例
        /// </summary>
        /// <param name="sourceItem">源物品</param>
        /// <param name="count">克隆后的数量（默认保持源物品的count）</param>
        /// <returns>克隆的Item实例（ItemUID重置为-1）</returns>
        IItem CloneItem(IItem sourceItem, int count = -1);

        /// <summary>
        ///     分配新的唯一ItemUID
        /// </summary>
        /// <returns>新的ItemUID</returns>
        long AllocateItemUID();

        /// <summary>
        ///     重置UID计数器（用于反序列化或重新初始化）
        /// </summary>
        /// <param name="maxUID">设定的最大UID（后续分配从maxUID+1开始）</param>
        void ResetUIDCounter(long maxUID);
    }
}
