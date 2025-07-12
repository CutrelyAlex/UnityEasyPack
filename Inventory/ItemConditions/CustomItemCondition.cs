using System;

namespace EasyPack
{

    /// <summary>
    /// 物品条件检查接口，使用委托模式实现条件验证
    /// </summary>
    public class CustomItemCondition : IItemCondition
    {
        /// <summary>
        /// 用于验证物品的条件委托
        /// </summary>
        Func<IItem, bool> Condition { get; set; }

        public CustomItemCondition(Func<IItem, bool> condition)
        {
            Condition = condition;
        }

        public void SetItemCondition(Func<IItem, bool> condition)
        {
            Condition = condition;
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
                return false;
            }
            return Condition(item);
        }
    }
}