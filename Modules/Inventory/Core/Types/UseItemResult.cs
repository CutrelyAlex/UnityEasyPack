namespace EasyPack.InventorySystem
{
    public enum UseItemResult
    {
        /// <summary>使用成功</summary>
        Success,

        /// <summary>物品为 null</summary>
        ItemIsNull,

        /// <summary>物品没有绑定任何 UseModule</summary>
        NoModule,

        /// <summary>CanUse 检查未通过（前置条件不满足）</summary>
        ConditionNotMet,

        /// <summary>物品数量不足（如消耗品数量为0）</summary>
        InsufficientCount,

        /// <summary>模块内部执行失败</summary>
        Failed,
    }
}
