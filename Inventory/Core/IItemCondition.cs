namespace EasyPack
{
    /// <summary>
    /// 物品条件检查接口
    /// </summary>
    public interface IItemCondition
    {
        /// <summary>
        /// 检查物品条件是否满足
        /// </summary>
        /// <param name="item">要检查的物品</param>
        /// <returns>如果条件满足则返回true，否则返回false</returns>
        bool IsCondition(IItem item);
    }
}