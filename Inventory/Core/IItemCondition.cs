namespace EasyPack
{
    /// <summary>
    /// ��Ʒ�������ӿ�
    /// </summary>
    public interface IItemCondition
    {
        /// <summary>
        /// �����Ʒ�����Ƿ�����
        /// </summary>
        /// <param name="item">Ҫ������Ʒ</param>
        /// <returns>������������򷵻�true�����򷵻�false</returns>
        bool IsCondition(IItem item);
    }
}