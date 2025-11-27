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
            public List<string> EntityKeyJsons; // TKey 序列化为 JSON 的列表
        }

        [Serializable]
        public class SerializedMetadata
        {
            public string EntityKeyJson; // TKey 序列化为 JSON
            public string MetadataJson;
        }
    }

    /// <summary>
    ///     CategoryManager JSON 双泛型序列化器
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    /// <typeparam name="TKey">键类型</typeparam>
    public class CategoryManagerJsonSerializer<T, TKey> :
        JsonSerializerBase<CategoryManager<T, TKey>>,
        ITypeSerializer<CategoryManager<T, TKey>, SerializableCategoryManagerState<T, TKey>>
        where TKey : IEquatable<TKey>
    {
        private readonly Func<T, TKey> _keyExtractor;
        private ISerializationService _serializationService;

        public CategoryManagerJsonSerializer(Func<T, TKey> keyExtractor) =>
            _keyExtractor = keyExtractor ?? throw new ArgumentNullException(nameof(keyExtractor));

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

        /// <summary>
        ///     序列化键为 JSON
        /// </summary>
        private string SerializeKey(TKey key)
        {
            // 对于基元类型直接转换
            if (typeof(TKey) == typeof(int))
                return key.ToString();
            if (typeof(TKey) == typeof(string))
                return (string)(object)key;
            if (typeof(TKey) == typeof(long))
                return key.ToString();
            if (typeof(TKey) == typeof(Guid))
                return key.ToString();

            // 其他类型使用 JSON 序列化
            return JsonUtility.ToJson(key);
        }

        /// <summary>
        ///     从 JSON 反序列化键
        /// </summary>
        private TKey DeserializeKey(string keyJson)
        {
            if (string.IsNullOrEmpty(keyJson))
                return default;

            // 对于基元类型直接转换
            if (typeof(TKey) == typeof(int))
                return (TKey)(object)int.Parse(keyJson);
            if (typeof(TKey) == typeof(string))
                return (TKey)(object)keyJson;
            if (typeof(TKey) == typeof(long))
                return (TKey)(object)long.Parse(keyJson);
            if (typeof(TKey) == typeof(Guid))
                return (TKey)(object)Guid.Parse(keyJson);

            // 其他类型使用 JSON 反序列化
            return JsonUtility.FromJson<TKey>(keyJson);
        }

        #region 实现

        /// <summary>
        ///     将 CategoryManager 转换为可序列化的状态对象
        /// </summary>
        public SerializableCategoryManagerState<T, TKey> ToSerializable(CategoryManager<T, TKey> manager)
        {
            if (manager == null) return null;

            EnsureSerializationService();

            var data = new SerializableCategoryManagerState<T, TKey>
            {
                Entities = new(), Categories = new(), Tags = new(), Metadata = new(),
            };

            // 序列化实体和分类
            var categories = manager.GetCategoriesNodes();
            foreach (string category in categories)
            {
                data.Categories.Add(new SerializableCategoryManagerState<T, TKey>.SerializedCategory { Name = category });

                var entities = manager.GetByCategory(category, false);
                foreach (T entity in entities)
                {
                    TKey key = _keyExtractor(entity);
                    string keyJson = SerializeKey(key);

                    // 避免重复添加实体
                    if (data.Entities.Any(e => e.KeyJson == keyJson)) continue;

                    // 使用 SerializationService 序列化实体
                    string entityJson = _serializationService != null
                        ? _serializationService.SerializeToJson(entity)
                        : JsonUtility.ToJson(entity);

                    data.Entities.Add(new SerializableCategoryManagerState<T, TKey>.SerializedEntity 
                    { 
                        KeyJson = keyJson, 
                        EntityJson = entityJson, 
                        Category = category 
                    });
                }
            }

            // 序列化标签
            var tagIndex = manager.GetTagIndex();
            foreach (var kvp in tagIndex)
            {
                data.Tags.Add(new SerializableCategoryManagerState<T, TKey>.SerializedTag 
                { 
                    TagName = kvp.Key, 
                    EntityKeyJsons = kvp.Value.Select(SerializeKey).ToList() 
                });
            }

            // 序列化元数据
            var metadataStore = manager.GetMetadataStore();
            foreach (var kvp in metadataStore)
            {
                data.Metadata.Add(new SerializableCategoryManagerState<T, TKey>.SerializedMetadata
                {
                    EntityKeyJson = SerializeKey(kvp.Key),
                    MetadataJson =
                        JsonUtility.ToJson(new CustomDataCollectionWrapper { Entries = kvp.Value.ToList() }),
                });
            }

            return data;
        }

        /// <summary>
        ///     从可序列化的状态对象还原 CategoryManager
        /// </summary>
        public CategoryManager<T, TKey> FromSerializable(SerializableCategoryManagerState<T, TKey> data)
        {
            if (data == null) throw new InvalidOperationException("序列化数据为空");

            EnsureSerializationService();

            var manager = new CategoryManager<T, TKey>(_keyExtractor);

            var entityRegistrations =
                new List<(T entity, string category, List<string> tags, CustomDataCollection metadata)>();

            foreach (var serializedEntity in data.Entities)
            {
                try
                {
                    // 使用 SerializationService 反序列化实体
                    T entity = _serializationService != null
                        ? _serializationService.DeserializeFromJson<T>(serializedEntity.EntityJson)
                        : JsonUtility.FromJson<T>(serializedEntity.EntityJson);

                    TKey key = DeserializeKey(serializedEntity.KeyJson);
                    string keyJson = serializedEntity.KeyJson;

                    // 查找实体的标签
                    var tags = data.Tags
                        .Where(t => t.EntityKeyJsons.Contains(keyJson))
                        .Select(t => t.TagName)
                        .ToList();

                    // 查找实体的元数据
                    CustomDataCollection metadata = null;
                    var metadataEntry = data.Metadata.FirstOrDefault(m => m.EntityKeyJson == keyJson);
                    if (metadataEntry != null)
                    {
                        var wrapper = JsonUtility.FromJson<CustomDataCollectionWrapper>(metadataEntry.MetadataJson);
                        metadata = new CustomDataCollection();
                        foreach (CustomDataEntry entry in wrapper.Entries)
                        {
                            metadata.Add(entry);
                        }
                    }

                    entityRegistrations.Add((entity, serializedEntity.Category, tags, metadata));
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"反序列化实体 '{serializedEntity.KeyJson}' 失败: {ex.Message}");
                }
            }

            // 注册所有实体
            foreach ((T entity, string category, var tags, CustomDataCollection metadata) in entityRegistrations)
            {
                var registration = manager.RegisterEntitySafe(entity, category);

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
        public string ToJson(SerializableCategoryManagerState<T, TKey> dto) =>
            dto == null ? null : JsonUtility.ToJson(dto, true);

        /// <summary>
        ///     从 JSON 转为可序列化对象
        /// </summary>
        public SerializableCategoryManagerState<T, TKey> FromJson(string json) =>
            string.IsNullOrEmpty(json)
                ? throw new InvalidOperationException("JSON 字符串为空")
                : JsonUtility.FromJson<SerializableCategoryManagerState<T, TKey>>(json);

        #endregion

        /// <summary>
        ///     序列化 CategoryManager 为 JSON 字符串
        ///     实体序列化由 SerializationService 管理
        /// </summary>
        /// <param name="manager">要序列化的 CategoryManager 实例</param>
        /// <returns>JSON 字符串</returns>
        public override string SerializeToJson(CategoryManager<T, TKey> manager)
        {
            if (manager == null) return null;

            try
            {
                EnsureSerializationService();

                var data = new SerializableCategoryManagerState<T, TKey>
                {
                    Entities = new(), Categories = new(), Tags = new(), Metadata = new(),
                };

                manager.GetOptimizedSerializationIndices(out var entityTagIndex, out var entityMetadataIndex);

                var serializedEntityKeys = new HashSet<string>(StringComparer.Ordinal);

                var categories = manager.GetCategoriesNodes();
                var tagIndex = manager.GetTagIndex();

                foreach (string category in categories)
                {
                    var entities = manager.GetByCategory(category, false);

                    data.Categories.Add(new SerializableCategoryManagerState<T, TKey>.SerializedCategory { Name = category });

                    foreach (T entity in entities)
                    {
                        TKey key = _keyExtractor(entity);
                        string keyJson = SerializeKey(key);

                        if (serializedEntityKeys.Contains(keyJson)) continue;
                        serializedEntityKeys.Add(keyJson);

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
                            Debug.LogWarning($"序列化实体 '{keyJson}' 失败: {ex.Message}，将保留空EntityJson");
                            entityJson = string.Empty;
                        }

                        data.Entities.Add(new SerializableCategoryManagerState<T, TKey>.SerializedEntity 
                        { 
                            KeyJson = keyJson, 
                            EntityJson = entityJson, 
                            Category = category 
                        });
                    }
                }

                // 序列化 Tag
                foreach (var kvp in tagIndex)
                {
                    data.Tags.Add(new SerializableCategoryManagerState<T, TKey>.SerializedTag 
                    { 
                        TagName = kvp.Key, 
                        EntityKeyJsons = kvp.Value.Select(SerializeKey).ToList() 
                    });
                }

                // 序列化元数据 
                foreach (var kvp in entityMetadataIndex)
                {
                    data.Metadata.Add(new SerializableCategoryManagerState<T, TKey>.SerializedMetadata
                    {
                        EntityKeyJson = SerializeKey(kvp.Key),
                        MetadataJson = JsonUtility.ToJson(new CustomDataCollectionWrapper
                        {
                            Entries = kvp.Value.ToList(),
                        }),
                    });
                }

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
        public override CategoryManager<T, TKey> DeserializeFromJson(string json)
        {
            if (string.IsNullOrEmpty(json)) throw new InvalidOperationException("JSON 字符串为空");

            try
            {
                EnsureSerializationService();

                var data = JsonUtility.FromJson<SerializableCategoryManagerState<T, TKey>>(json);

                // 检查并初始化可能为 null 的集合字段
                if (data == null) throw new InvalidOperationException("无法反序列化 JSON 数据");

                data.Entities ??= new();
                data.Categories ??= new();
                data.Tags ??= new();
                data.Metadata ??= new();

                // 预构建 EntityKeyJson -> Tags 字典
                var entityTagIndex = new Dictionary<string, List<string>>(StringComparer.Ordinal);
                foreach (var tag in data.Tags)
                foreach (string keyJson in tag.EntityKeyJsons)
                {
                    if (!entityTagIndex.TryGetValue(keyJson, out var tags))
                    {
                        tags = new List<string>();
                        entityTagIndex[keyJson] = tags;
                    }

                    tags.Add(tag.TagName);
                }

                // 预构建 EntityKeyJson -> Metadata 字典
                var entityMetadataIndex = new Dictionary<string, CustomDataCollection>(StringComparer.Ordinal);
                foreach (var metadataEntry in data.Metadata)
                {
                    var wrapper = JsonUtility.FromJson<CustomDataCollectionWrapper>(metadataEntry.MetadataJson);
                    if (wrapper?.Entries != null)
                    {
                        var metadata = new CustomDataCollection();
                        foreach (CustomDataEntry entry in wrapper.Entries)
                        {
                            metadata.Add(entry);
                        }

                        entityMetadataIndex[metadataEntry.EntityKeyJson] = metadata;
                    }
                }

                // 创建新的 CategoryManager 实例
                var manager = new CategoryManager<T, TKey>(_keyExtractor);

                // 反序列化实体
                var entityRegistrations =
                    new List<(TKey key, T entity, string category, List<string> tags, CustomDataCollection metadata)>();

                foreach (var serializedEntity in data.Entities)
                {
                    try
                    {
                        T entity = default;
                        TKey key = DeserializeKey(serializedEntity.KeyJson);

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
                                    Debug.LogWarning($"反序列化实体 '{serializedEntity.KeyJson}' 失败: 反序列化结果为 null");
                                    continue;
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.LogWarning($"反序列化实体 '{serializedEntity.KeyJson}' 失败: {ex.Message}");
                                continue;
                            }

                        var tags = entityTagIndex.TryGetValue(serializedEntity.KeyJson, out var entityTags)
                            ? entityTags
                            : new List<string>();

                        CustomDataCollection metadata =
                            entityMetadataIndex.TryGetValue(serializedEntity.KeyJson,
                                out CustomDataCollection entityMetadata)
                                ? entityMetadata
                                : null;

                        entityRegistrations.Add((key, entity, serializedEntity.Category, tags, metadata));
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"处理实体 '{serializedEntity.KeyJson}' 时发生异常: {ex.Message}");
                    }
                }

                // 注册所有实体
                foreach ((TKey key, T entity, string category, var tags, CustomDataCollection metadata) in
                         entityRegistrations)
                {
                    // 检查entity是否为默认值
                    bool hasEntity = !EqualityComparer<T>.Default.Equals(entity, default);

                    if (!hasEntity)
                    {
                        OperationResult result = manager.RegisterCategoryAssociationOnly(key, category, tags, metadata);
                        if (result.IsSuccess) continue;
                        Debug.LogWarning($"注册分类关联失败: {result.ErrorMessage}");
                    }
                    else
                    {
                        // 正常注册实体
                        var registration = manager.RegisterEntitySafeWithKey(entity, key, category);

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

    #region 单泛型版本（用于 CategoryManager<T>，使用 string 作为 ID）

    [Obsolete("单泛型版本已过时，请使用双泛型版本 CategoryManagerJsonSerializer<T, TKey>，并指定键类型 TKey。")]
    /// <summary>
    ///     CategoryManager 状态数据的可序列化表示（单泛型版本，使用 string ID）
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    [Serializable]
    public class SerializableCategoryManagerState<T> : ISerializable
    {
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
    ///     CategoryManager JSON 单泛型序列化器（用于 CategoryManager&lt;T&gt;，使用 string 作为 ID）
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

        public SerializableCategoryManagerState<T> ToSerializable(CategoryManager<T> manager)
        {
            if (manager == null) return null;
            EnsureSerializationService();

            var data = new SerializableCategoryManagerState<T>
            {
                Entities = new(), Categories = new(), Tags = new(), Metadata = new(),
            };

            var categories = manager.GetCategoriesNodes();
            foreach (string category in categories)
            {
                data.Categories.Add(new SerializableCategoryManagerState<T>.SerializedCategory { Name = category });
                var entities = manager.GetByCategory(category, false);
                foreach (T entity in entities)
                {
                    string id = _idExtractor(entity);
                    if (data.Entities.Any(e => e.Id == id)) continue;

                    string entityJson = _serializationService != null
                        ? _serializationService.SerializeToJson(entity)
                        : JsonUtility.ToJson(entity);

                    data.Entities.Add(new SerializableCategoryManagerState<T>.SerializedEntity
                    {
                        Id = id, EntityJson = entityJson, Category = category
                    });
                }
            }

            var tagIndex = manager.GetTagIndex();
            foreach (var kvp in tagIndex)
            {
                data.Tags.Add(new SerializableCategoryManagerState<T>.SerializedTag
                {
                    TagName = kvp.Key, EntityIds = kvp.Value.ToList()
                });
            }

            var metadataStore = manager.GetMetadataStore();
            foreach (var kvp in metadataStore)
            {
                data.Metadata.Add(new SerializableCategoryManagerState<T>.SerializedMetadata
                {
                    EntityId = kvp.Key,
                    MetadataJson = JsonUtility.ToJson(new SingleGenericCustomDataWrapper { Entries = kvp.Value.ToList() }),
                });
            }

            return data;
        }

        public CategoryManager<T> FromSerializable(SerializableCategoryManagerState<T> data)
        {
            if (data == null) throw new InvalidOperationException("序列化数据为空");
            EnsureSerializationService();

            var manager = new CategoryManager<T>(_idExtractor);
            var registrations = new List<(T entity, string category, List<string> tags, CustomDataCollection metadata)>();

            foreach (var serializedEntity in data.Entities)
            {
                try
                {
                    T entity = _serializationService != null
                        ? _serializationService.DeserializeFromJson<T>(serializedEntity.EntityJson)
                        : JsonUtility.FromJson<T>(serializedEntity.EntityJson);

                    var tags = data.Tags.Where(t => t.EntityIds.Contains(serializedEntity.Id)).Select(t => t.TagName).ToList();
                    CustomDataCollection metadata = null;
                    var metadataEntry = data.Metadata.FirstOrDefault(m => m.EntityId == serializedEntity.Id);
                    if (metadataEntry != null)
                    {
                        var wrapper = JsonUtility.FromJson<SingleGenericCustomDataWrapper>(metadataEntry.MetadataJson);
                        metadata = new CustomDataCollection();
                        foreach (var entry in wrapper.Entries) metadata.Add(entry);
                    }

                    registrations.Add((entity, serializedEntity.Category, tags, metadata));
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"反序列化实体 '{serializedEntity.Id}' 失败: {ex.Message}");
                }
            }

            foreach ((T entity, string category, var tags, CustomDataCollection metadata) in registrations)
            {
                var registration = manager.RegisterEntitySafe(entity, category);
                if (tags is { Count: > 0 }) registration.WithTags(tags.ToArray());
                if (metadata != null) registration.WithMetadata(metadata);
                registration.Complete();
            }

            return manager;
        }

        public string ToJson(SerializableCategoryManagerState<T> dto) =>
            dto == null ? null : JsonUtility.ToJson(dto, true);

        public SerializableCategoryManagerState<T> FromJson(string json) =>
            string.IsNullOrEmpty(json)
                ? throw new InvalidOperationException("JSON 字符串为空")
                : JsonUtility.FromJson<SerializableCategoryManagerState<T>>(json);

        public override string SerializeToJson(CategoryManager<T> manager)
        {
            if (manager == null) return null;
            try
            {
                EnsureSerializationService();
                var data = ToSerializable(manager);
                return JsonUtility.ToJson(data, true);
            }
            catch (Exception ex)
            {
                Debug.LogError($"CategoryService 序列化失败: {ex.Message}");
                throw;
            }
        }

        public override CategoryManager<T> DeserializeFromJson(string json)
        {
            if (string.IsNullOrEmpty(json)) throw new InvalidOperationException("JSON 字符串为空");
            try
            {
                EnsureSerializationService();
                var data = JsonUtility.FromJson<SerializableCategoryManagerState<T>>(json);
                return FromSerializable(data);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"JSON 反序列化失败: {ex.Message}", ex);
            }
        }

        [Serializable]
        private class SingleGenericCustomDataWrapper
        {
            public List<CustomDataEntry> Entries;
        }
    }

    #endregion
}