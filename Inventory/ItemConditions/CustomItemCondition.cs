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