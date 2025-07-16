using System;

namespace EasyPack
{

    /// <summary>
    /// ��Ʒ�������ӿڣ�ʹ��ί��ģʽʵ��������֤
    /// </summary>
    public class CustomItemCondition : IItemCondition
    {
        /// <summary>
        /// ������֤��Ʒ������ί��
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