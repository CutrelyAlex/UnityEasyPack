using System;
using System.Collections.Generic;
using EasyPack.Serialization;

namespace EasyPack.Category
{
    /// <summary>
    ///     CategoryManager 状态数据的可序列化表示（双泛型版本）
    ///     Entities 序列化由 SerializationService 管理
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    /// <typeparam name="TKey">键类型</typeparam>
    [Serializable]
    public class SerializableCategoryManagerState<T, TKey> : ISerializable
        where TKey : IEquatable<TKey>
    {
        /// <summary>
        ///     标记此序列化数据是否包含 Entity 对象数据
        ///     为 false 时表示仅包含分类、标签、元数据等结构，不包含 Entity 对象
        /// </summary>
        public bool IncludeEntities;

        public List<SerializedEntity> Entities;
        public List<SerializedCategory> Categories;
        public List<SerializedTag> Tags;
        public List<SerializedMetadata> Metadata;

        [Serializable]
        public class SerializedEntity
        {
            public string KeyJson; // TKey 序列化为 JSON
            public T Entity;
            public string Category;
        }

        [Serializable]
        public class SerializedCategory
        {
            public string Name;
        }

        [Serializable]
        public class SerializedTag
        {
            public string TagName;
            public List<string> EntityKeyJsons; // TKey 序列化为 JSON 的列表
        }

        [Serializable]
        public class SerializedMetadata
        {
            public string EntityKeyJson; // TKey 序列化为 JSON
            public string MetadataJson;
        }
    }
}