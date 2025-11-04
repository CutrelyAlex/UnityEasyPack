using System;
using EasyPack.GamePropertySystem;

namespace EasyPack
{
    /// <summary>
    /// GamePropertyManager 的可序列化数据传输对象
    /// </summary>
    [Serializable]
    public class PropertyManagerDTO : ISerializable
    {
        /// <summary>
        /// 属性条目列表
        /// </summary>
        public PropertyEntry[] Properties = Array.Empty<PropertyEntry>();

        /// <summary>
        /// 元数据条目列表
        /// </summary>
        public MetadataEntry[] Metadata = Array.Empty<MetadataEntry>();
    }

    /// <summary>
    /// 属性条目
    /// 存储属性 ID、分类和序列化的属性数据
    /// </summary>
    [Serializable]
    public struct PropertyEntry
    {
        /// <summary>
        /// 属性唯一标识符
        /// </summary>
        public string ID;

        /// <summary>
        /// 属性所属分类（用于重建分类索引）
        /// </summary>
        public string Category;

        /// <summary>
        /// 序列化的 GameProperty JSON 字符串
        /// </summary>
        public string SerializedProperty;
    }

    /// <summary>
    /// 元数据条目
    /// 存储属性元数据信息
    /// </summary>
    [Serializable]
    public struct MetadataEntry
    {
        /// <summary>
        /// 对应的属性 ID
        /// </summary>
        public string PropertyID;

        /// <summary>
        /// 显示名称
        /// </summary>
        public string DisplayName;

        /// <summary>
        /// 详细描述
        /// </summary>
        public string Description;

        /// <summary>
        /// 图标资源路径
        /// </summary>
        public string IconPath;

        /// <summary>
        /// 标签数组（用于重建标签索引）
        /// </summary>
        public string[] Tags;

        /// <summary>
        /// 自定义扩展数据的 JSON 字符串
        /// </summary>
        public string CustomDataJson;
    }
}

