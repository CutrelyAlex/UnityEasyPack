using EasyPack.CustomData;
using System;

namespace EasyPack.GamePropertySystem
{
    /// <summary>
    /// 属性元数据
    /// 存储属性的显示信息和扩展数据
    /// </summary>
    [Serializable]
    public class PropertyMetadata
    {
        /// <summary>显示名称</summary>
        public string DisplayName;

        /// <summary>详细描述</summary>
        public string Description;

        /// <summary>图标资源路径</summary>
        public string IconPath;

        /// <summary>
        /// 标签数组
        /// </summary>
        public string[] Tags;

        /// <summary>
        /// 自定义扩展数据
        /// </summary>
        public CustomDataCollection CustomData;

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public PropertyMetadata()
        {
            CustomData = new CustomDataCollection();
        }

        /// <summary>
        /// 获取自定义数据值
        /// </summary>
        public T GetCustomData<T>(string key, T defaultValue = default)
        {
            return CustomData.TryGetValue(key, out T value) ? value : defaultValue;
        }

        /// <summary>
        /// 设置自定义数据值
        /// </summary>
        public void SetCustomData(string key, object value)
        {
            // 查找是否已存在
            foreach (var entry in CustomData)
            {
                if (entry.Key == key)
                {
                    entry.SetValue(value);
                    return;
                }
            }

            // 不存在则添加新条目
            var newEntry = new CustomDataEntry { Key = key };
            newEntry.SetValue(value);
            CustomData.Add(newEntry);
        }
    }
}
