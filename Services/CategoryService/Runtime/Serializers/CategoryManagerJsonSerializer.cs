using EasyPack.Architecture;
using EasyPack.CustomData;
using EasyPack.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EasyPack.Category
{
    /// <summary>
    ///     CategoryManager 状态数据的可序列化表示
    ///     Entities 序列化由 SerializationService 管理
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    [Serializable]
    public class SerializableCategoryManagerState<T> : ISerializable
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
            public string Id;
            public string EntityJson;
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
            public List<string> EntityIds;
        }

        [Serializable]
        public class SerializedMetadata
        {
            public string EntityId;
            public string MetadataJson;
        }
    }

    /// <summary>
    ///     CategoryManager JSON 双泛型序列化器
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    public class CategoryManagerJsonSerializer<T> :
        JsonSerializerBase<CategoryManager<T>>,
        ITypeSerializer<CategoryManager<T>, SerializableCategoryManagerState<T>>
    {
        private readonly Func<T, string> _idExtractor;
        private ISerializationService _serializationService;

        public CategoryManagerJsonSerializer(Func<T, string> idExtractor) =>
            _idExtractor = idExtractor ?? throw new ArgumentNullException(nameof(idExtractor));

        /// <summary>
        ///     获取或初始化 SerializationService
        /// </summary>
        private async void EnsureSerializationService()
        {
            try
            {
                _serializationService ??= await EasyPackArchitecture.Instance.ResolveAsync<ISerializationService>();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"初始化 SerializationService 失败: {e.Message}");
            }
        }

        #region 实现

        /// <summary>
        ///     将 CategoryManager 转换为可序列化的状态对象
        /// </summary>
        public SerializableCategoryManagerState<T> ToSerializable(CategoryManager<T> manager)
        {
            if (manager == null) return null;

            EnsureSerializationService();

            var data = new SerializableCategoryManagerState<T>
            {
                Entities = new(),
                Categories = new(),
                Tags = new(),
                Metadata = new(),
            };

            // 序列化实体和分类
            var categories = manager.GetCategoriesNodes();
            foreach (string category in categories)
            {
                data.Categories.Add(new()
                {
                    Name = category,
                });

                var entities = manager.GetByCategory(category, false);
                foreach (T entity in entities)
                {
                    string id = _idExtractor(entity);

                    // 避免重复添加实体
                    if (data.Entities.Any(e => e.Id == id)) continue;

                    // 使用 SerializationService 序列化实体
                    string entityJson = _serializationService != null
                        ? _serializationService.SerializeToJson(entity)
                        : JsonUtility.ToJson(entity);

                    data.Entities.Add(new()
                    {
                        Id = id,
                        EntityJson = entityJson,
                        Category = category,
                    });
                }
            }

            // 序列化标签
            var tagIndex = manager.GetTagIndex();
            foreach (var kvp in tagIndex)
                data.Tags.Add(new()
                {
                    TagName = kvp.Key,
                    EntityIds = kvp.Value.ToList(),
                });

            // 序列化元数据
            var metadataStore = manager.GetMetadataStore();
            foreach (var kvp in metadataStore)
                data.Metadata.Add(new()
                {
                    EntityId = kvp.Key,
                    MetadataJson = JsonUtility.ToJson(new CustomDataCollectionWrapper { Entries = kvp.Value.ToList() }),
                });

            return data;
        }

        /// <summary>
        ///     从可序列化的状态对象还原 CategoryManager
        /// </summary>
        public CategoryManager<T> FromSerializable(SerializableCategoryManagerState<T> data)
        {
            if (data == null) throw new InvalidOperationException("序列化数据为空");

            EnsureSerializationService();

            var manager = new CategoryManager<T>(_idExtractor);

            var entityRegistrations =
                new List<(T entity, string category, List<string> tags, CustomDataCollection metadata)>();

            foreach (SerializableCategoryManagerState<T>.SerializedEntity serializedEntity in data.Entities)
                try
                {
                    // 使用 SerializationService 反序列化实体
                    T entity = _serializationService != null
                        ? _serializationService.DeserializeFromJson<T>(serializedEntity.EntityJson)
                        : JsonUtility.FromJson<T>(serializedEntity.EntityJson);

                    // 查找实体的标签
                    var tags = data.Tags
                        .Where(t => t.EntityIds.Contains(serializedEntity.Id))
                        .Select(t => t.TagName)
                        .ToList();

                    // 查找实体的元数据
                    CustomDataCollection metadata = null;
                    SerializableCategoryManagerState<T>.SerializedMetadata metadataEntry =
                        data.Metadata.FirstOrDefault(m => m.EntityId == serializedEntity.Id);
                    if (metadataEntry != null)
                    {
                        var wrapper = JsonUtility.FromJson<CustomDataCollectionWrapper>(metadataEntry.MetadataJson);
                        metadata = new();
                        foreach (CustomDataEntry entry in wrapper.Entries) metadata.Add(entry);
                    }

                    entityRegistrations.Add((entity, serializedEntity.Category, tags, metadata));
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"反序列化实体 '{serializedEntity.Id}' 失败: {ex.Message}");
                }

            // 注册所有实体
            foreach ((T entity, string category, var tags, CustomDataCollection metadata) in entityRegistrations)
            {
                IEntityRegistration registration = manager.RegisterEntitySafe(entity, category);

                if (tags is { Count: > 0 }) registration.WithTags(tags.ToArray());

                if (metadata != null) registration.WithMetadata(metadata);

                OperationResult result = registration.Complete();

                if (!result.IsSuccess) Debug.LogWarning($"注册实体失败: {result.ErrorMessage}");
            }

            return manager;
        }

        /// <summary>
        ///     将可序列化对象转为 JSON
        /// </summary>
        public string ToJson(SerializableCategoryManagerState<T> dto) =>
            dto == null ? null : JsonUtility.ToJson(dto, true);

        /// <summary>
        ///     从 JSON 转为可序列化对象
        /// </summary>
        public SerializableCategoryManagerState<T> FromJson(string json) =>
            string.IsNullOrEmpty(json)
                ? throw new InvalidOperationException("JSON 字符串为空")
                : JsonUtility.FromJson<SerializableCategoryManagerState<T>>(json);

        #endregion

        /// <summary>
        ///     序列化 CategoryManager 为 JSON 字符串
        ///     实体序列化由 SerializationService 管理
        /// </summary>
        /// <param name="manager">要序列化的 CategoryManager 实例</param>
        /// <returns>JSON 字符串</returns>
        public override string SerializeToJson(CategoryManager<T> manager)
        {
            if (manager == null) return null;

            try
            {
                EnsureSerializationService();

                var data = new SerializableCategoryManagerState<T>
                {
                    Entities = new(),
                    Categories = new(),
                    Tags = new(),
                    Metadata = new(),
                };

                manager.GetOptimizedSerializationIndices(out var entityTagIndex, out var entityMetadataIndex);

                var serializedEntityIds = new HashSet<string>(StringComparer.Ordinal);

                var categories = manager.GetCategoriesNodes();
                var tagIndex = manager.GetTagIndex();

                foreach (string category in categories)
                {
                    var entities = manager.GetByCategory(category, false);

                    data.Categories.Add(new()
                    {
                        Name = category,
                    });

                    foreach (T entity in entities)
                    {
                        string id = _idExtractor(entity);
                        if (serializedEntityIds.Contains(id)) continue;
                        serializedEntityIds.Add(id);

                        // 尝试序列化实体，如果失败则保留空EntityJson
                        string entityJson;
                        try
                        {
                            entityJson = _serializationService != null
                                ? _serializationService.SerializeToJson(entity)
                                : JsonUtility.ToJson(entity);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning($"序列化实体 '{id}' 失败: {ex.Message}，将保留空EntityJson");
                            entityJson = string.Empty;
                        }

                        data.Entities.Add(new()
                        {
                            Id = id,
                            EntityJson = entityJson,
                            Category = category,
                        });
                    }
                }

                // 序列化 Tag
                foreach (var kvp in tagIndex)
                    data.Tags.Add(new()
                    {
                        TagName = kvp.Key,
                        EntityIds = new(kvp.Value),
                    });

                // 序列化元数据 
                foreach (var kvp in entityMetadataIndex)
                    data.Metadata.Add(new()
                    {
                        EntityId = kvp.Key,
                        MetadataJson = JsonUtility.ToJson(new CustomDataCollectionWrapper
                            { Entries = kvp.Value.ToList() }),
                    });

                return JsonUtility.ToJson(data, true);
            }
            catch (Exception ex)
            {
                Debug.LogError($"CategoryService 序列化失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        ///     从 JSON 字符串反序列化为 CategoryManager
        ///     实体反序列化由 SerializationService 管理
        ///     注意：此方法创建新的 CategoryManager 实例
        /// </summary>
        /// <param name="json">JSON 字符串</param>
        /// <returns>反序列化的 CategoryManager 实例</returns>
        public override CategoryManager<T> DeserializeFromJson(string json)
        {
            if (string.IsNullOrEmpty(json)) throw new InvalidOperationException("JSON 字符串为空");

            try
            {
                EnsureSerializationService();

                var data = JsonUtility.FromJson<SerializableCategoryManagerState<T>>(json);

                // 检查并初始化可能为 null 的集合字段
                if (data == null) throw new InvalidOperationException("无法反序列化 JSON 数据");

                data.Entities ??= new();
                data.Categories ??= new();
                data.Tags ??= new();
                data.Metadata ??= new();

                // 预构建 EntityId -> Tags 字典
                var entityTagIndex = new Dictionary<string, List<string>>(StringComparer.Ordinal);
                foreach (SerializableCategoryManagerState<T>.SerializedTag tag in data.Tags)
                foreach (string entityId in tag.EntityIds)
                {
                    if (!entityTagIndex.TryGetValue(entityId, out var tags))
                    {
                        tags = new();
                        entityTagIndex[entityId] = tags;
                    }

                    tags.Add(tag.TagName);
                }

                // 预构建 EntityId -> Metadata 字典
                var entityMetadataIndex = new Dictionary<string, CustomDataCollection>(StringComparer.Ordinal);
                foreach (SerializableCategoryManagerState<T>.SerializedMetadata metadataEntry in data.Metadata)
                {
                    var wrapper = JsonUtility.FromJson<CustomDataCollectionWrapper>(metadataEntry.MetadataJson);
                    if (wrapper?.Entries != null)
                    {
                        var metadata = new CustomDataCollection();
                        foreach (CustomDataEntry entry in wrapper.Entries) metadata.Add(entry);
                        entityMetadataIndex[metadataEntry.EntityId] = metadata;
                    }
                }

                // 创建新的 CategoryManager 实例
                var manager = new CategoryManager<T>(_idExtractor);

                // 反序列化实体
                var entityRegistrations =
                    new List<(string id, T entity, string category, List<string> tags, CustomDataCollection metadata
                        )>();

                foreach (SerializableCategoryManagerState<T>.SerializedEntity serializedEntity in data.Entities)
                    try
                    {
                        T entity = default;

                        // 仅当EntityJson非空时才尝试反序列化实体
                        if (!string.IsNullOrEmpty(serializedEntity.EntityJson))
                            try
                            {
                                // 使用 SerializationService 反序列化实体
                                entity = _serializationService != null
                                    ? _serializationService.DeserializeFromJson<T>(serializedEntity.EntityJson)
                                    : JsonUtility.FromJson<T>(serializedEntity.EntityJson);

                                // 检查反序列化是否成功
                                if (entity == null)
                                {
                                    Debug.LogWarning($"反序列化实体 '{serializedEntity.Id}' 失败: 反序列化结果为 null");
                                    continue;
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.LogWarning($"反序列化实体 '{serializedEntity.Id}' 失败: {ex.Message}");
                                continue;
                            }

                        var tags = entityTagIndex.TryGetValue(serializedEntity.Id, out var entityTags)
                            ? entityTags
                            : new();

                        CustomDataCollection metadata =
                            entityMetadataIndex.TryGetValue(serializedEntity.Id,
                                out CustomDataCollection entityMetadata)
                                ? entityMetadata
                                : null;

                        entityRegistrations.Add(
                            (serializedEntity.Id, entity, serializedEntity.Category, tags, metadata));
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"处理实体 '{serializedEntity.Id}' 时发生异常: {ex.Message}");
                    }

                // 注册所有实体
                foreach ((string id, T entity, string category, var tags, CustomDataCollection metadata) in
                         entityRegistrations)
                {
                    // 检查entity是否为默认值
                    bool hasEntity = !EqualityComparer<T>.Default.Equals(entity, default);

                    if (!hasEntity)
                    {
                        OperationResult result = manager.RegisterCategoryAssociationOnly(id, category, tags, metadata);
                        if (result.IsSuccess) continue;
                        Debug.LogWarning($"注册分类关联失败: {result.ErrorMessage}");
                    }
                    else
                    {
                        // 正常注册实体
                        IEntityRegistration registration = manager.RegisterEntitySafeWithId(entity, id, category);

                        if (tags is { Count: > 0 }) registration.WithTags(tags.ToArray());

                        if (metadata != null) registration.WithMetadata(metadata);

                        OperationResult result = registration.Complete();

                        if (!result.IsSuccess) Debug.LogWarning($"注册实体失败: {result.ErrorMessage}");
                    }
                }

                return manager;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"JSON 反序列化失败: {ex.Message}", ex);
            }
        }

        #region 辅助类

        [Serializable]
        private class CustomDataCollectionWrapper
        {
            public List<CustomDataEntry> Entries;
        }

        #endregion
    }
}