using System;
using System.Collections.Generic;
using EasyPack.Serialization;

namespace EasyPack.EmeCardSystem
{
    /// <summary>
    ///     Card 的 CategoryManager 状态数据
    /// </summary>
    [Serializable]
    public class CardCategoryManagerState : ISerializable
    {
        public bool IncludeEntities;
        public List<SerializedEntity> Entities;
        public List<SerializedCategory> Categories;
        public List<SerializedTag> Tags;
        public List<SerializedMetadata> Metadata;

        [Serializable]
        public class SerializedEntity
        {
            public string KeyJson; // long UID 序列化为 JSON
            public SerializableCard Entity; // 直接使用 SerializableCard 对象
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
            public List<string> EntityKeyJsons; // long UID 序列化为 JSON 的列表
        }

        [Serializable]
        public class SerializedMetadata
        {
            public string EntityKeyJson; // long UID 序列化为 JSON
            public string MetadataJson;
        }
    }
}
