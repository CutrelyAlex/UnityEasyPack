using System;

namespace EasyPack
{

    /// <summary>
    /// 物品条件检查接口，使用委托模式实现条件验证
    /// </summary>
    public class ItemCondition
    {
        /// <summary>
        /// 用于验证物品的条件委托
        /// </summary>
        Func<IItem, bool> Condition { get; set; }

        public ItemCondition(Func<IItem, bool> condition)
        {
            Condition = condition ?? throw new ArgumentNullException(nameof(condition), "Condition delegate cannot be null.");
        }

        public void SetItemCondition(Func<IItem, bool> condition)
        {
            Condition = condition ?? throw new ArgumentNullException(nameof(condition), "Condition delegate cannot be null.");
        }


        /// <summary>
        /// 检查物品条件是否满足
        /// </summary>
        /// <param name="item">要检查的物品</param>
        /// <returns>如果条件满足则返回true，否则返回false</returns>
        public bool IsCondition(IItem item)
        { 
            if(Condition == null)
            {
                throw new InvalidOperationException("Condition delegate is not set.");
            }
            return Condition(item);
        }
    }
}