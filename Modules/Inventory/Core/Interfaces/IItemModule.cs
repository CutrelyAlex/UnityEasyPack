namespace EasyPack.InventorySystem
{
    /// <summary>
    ///     物品行为模块接口
    /// </summary>
    public interface IItemModule
    {
        /// <summary>
        ///     模块唯一标识符
        /// </summary>
        string ModuleId { get; }

        /// <summary>
        ///     检查当前物品是否满足使用条件
        /// </summary>
        /// <param name="item">要使用的物品实例</param>
        /// <param name="context">可选的上下文数据（如使用者、目标等）</param>
        /// <returns>是否可以使用</returns>
        bool CanUse(IItem item, object context = null);

        /// <summary>
        ///     执行物品使用逻辑
        /// </summary>
        /// <param name="item">要使用的物品实例</param>
        /// <param name="service">关联的库存服务，用于移除消耗品等操作</param>
        /// <param name="context">可选的上下文数据（如使用者、目标等）</param>
        /// <returns>使用结果</returns>
        UseItemResult Use(IItem item, IInventoryService service, object context = null);
    }
}
