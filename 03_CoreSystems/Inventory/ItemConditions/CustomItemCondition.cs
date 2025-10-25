using System;

namespace EasyPack
{

    /// <summary>
    /// 物品条件检查接口，使用委托模式实现自定义验证
    /// </summary>
    public class CustomItemCondition : IItemCondition
    {
        /// <summary>
        /// 用于验证物品条件的委托
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

        public bool CheckCondition(IItem item)
        {
            if (Condition == null)
            {
                return false;
            }
            return Condition(item);
        }
    }
}