using System.Collections.Generic;

namespace RPGPack
{
    /// <summary>
    /// 管理各类 ICombineGameProperty 的增删查改
    /// </summary>
    public class CombineGamePropertyManager
    {
        private readonly Dictionary<string, ICombineGameProperty> _properties = new();

        /// <summary>
        /// 新增或更新一个 ICombineGameProperty
        /// </summary>
        public void AddOrUpdate(ICombineGameProperty property)
        {
            _properties[property.ID] = property;
        }

        /// <summary>
        /// 根据ID查找 ICombineGameProperty
        /// </summary>
        public ICombineGameProperty Get(string id)
        {
            _properties.TryGetValue(id, out var property);
            return property;
        }

        /// <summary>
        /// 删除指定ID的 ICombineGameProperty
        /// </summary>
        public bool Remove(string id)
        {
            return _properties.Remove(id);
        }

        /// <summary>
        /// 获取所有 ICombineGameProperty
        /// </summary>
        public IEnumerable<ICombineGameProperty> GetAll()
        {
            return _properties.Values;
        }

        /// <summary>
        /// 清空所有属性
        /// </summary>
        public void Clear()
        {
            _properties.Clear();
        }
    }
}