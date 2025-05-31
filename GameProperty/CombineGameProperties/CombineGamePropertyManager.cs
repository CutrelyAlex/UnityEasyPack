using System.Collections.Generic;

namespace RPGPack
{
    /// <summary>
    /// ������� ICombineGameProperty ����ɾ���
    /// </summary>
    public class CombineGamePropertyManager
    {
        private readonly Dictionary<string, ICombineGameProperty> _properties = new();

        /// <summary>
        /// ���������һ�� ICombineGameProperty
        /// </summary>
        public void AddOrUpdate(ICombineGameProperty property)
        {
            _properties[property.ID] = property;
        }

        /// <summary>
        /// ����ID���� ICombineGameProperty
        /// </summary>
        public ICombineGameProperty Get(string id)
        {
            _properties.TryGetValue(id, out var property);
            return property;
        }

        /// <summary>
        /// ɾ��ָ��ID�� ICombineGameProperty
        /// </summary>
        public bool Remove(string id)
        {
            return _properties.Remove(id);
        }

        /// <summary>
        /// ��ȡ���� ICombineGameProperty
        /// </summary>
        public IEnumerable<ICombineGameProperty> GetAll()
        {
            return _properties.Values;
        }

        /// <summary>
        /// �����������
        /// </summary>
        public void Clear()
        {
            _properties.Clear();
        }
    }
}