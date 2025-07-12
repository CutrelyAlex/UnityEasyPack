using System;

namespace EasyPack
{

    /// <summary>
    /// ��Ʒ�������ӿڣ�ʹ��ί��ģʽʵ��������֤
    /// </summary>
    public class ItemCondition
    {
        /// <summary>
        /// ������֤��Ʒ������ί��
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
        /// �����Ʒ�����Ƿ�����
        /// </summary>
        /// <param name="item">Ҫ������Ʒ</param>
        /// <returns>������������򷵻�true�����򷵻�false</returns>
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