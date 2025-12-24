using System;
using EasyPack.Category;
using EasyPack.Serialization;

namespace EasyPack.GamePropertySystem
{
    /// <summary>
    ///     GamePropertyManager 的可序列化数据传输对象
    /// </summary>
    [Serializable]
    public class PropertyManagerDTO : ISerializable
    {
        /// <summary>
        ///     属性条目列表
        /// </summary>
        public PropertyEntry[] Properties = Array.Empty<PropertyEntry>();

        /// <summary>
        ///     元数据条目列表（显示信息）
        /// </summary>
        public PropertyDisplayInfoEntry[] PropertyDisplayInfo = Array.Empty<PropertyDisplayInfoEntry>();

        /// <summary>
        ///     分类系统状态（分类、标签、自定义数据）
        /// </summary>
        public SerializableCategoryManagerState<GameProperty, long> CategoryState;
    }

    /// <summary>
    ///     属性条目
    ///     存储属性 ID 和序列化的属性数据
    /// </summary>
    [Serializable]
    public struct PropertyEntry
    {
        /// <summary>
        ///     属性唯一标识符
        /// </summary>
        public string ID;

        /// <summary>
        ///     序列化的 GameProperty JSON 字符串
        /// </summary>
        public string SerializedProperty;
    }

    /// <summary>
    ///     元数据条目
    ///     存储属性显示相关的元数据信息
    /// </summary>
    [Serializable]
    public struct PropertyDisplayInfoEntry
    {
        /// <summary>
        ///     对应的属性 ID
        /// </summary>
        public string PropertyID;

        /// <summary>
        ///     显示名称
        /// </summary>
        public string DisplayName;

        /// <summary>
        ///     详细描述
        /// </summary>
        public string Description;

        /// <summary>
        ///     图标资源路径
        /// </summary>
        public string IconPath;
    }
}