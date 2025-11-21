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
    /// CategoryManager 状态数据的可序列化表示
    /// Entities 序列化由 SerializationService 管理
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    [Serializable]
    public class SerializableCategoryManagerState<T> : ISerializable
    {
        public int Version;
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
    /// CategoryManager JSON 序列化器
    /// 继承 EasyPack 序列化基类，支持完整状态的序列化和反序列化
    /// Entities 序列化由 SerializationService 管理，支持注册的序列化器和自动 [Serializable] 处理
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    public class CategoryManagerJsonSerializer<T> : JsonSerializerBase<CategoryManager<T>>
    {
        private readonly Func<T, string> _idExtractor;
        private ISerializationService _serializationService;

        /// <summary>
        /// 序列化数据版本号
        /// </summary>
        private const int CurrentVersion = 1;

        public CategoryManagerJsonSerializer(Func<T, string> idExtractor)
        {
            _idExtractor = idExtractor ?? throw new ArgumentNullException(nameof(idExtractor));
        }

        /// <summary>
        /// 获取或初始化 SerializationService
        /// </summary>
        private async void EnsureSerializationService()
        {
            _serializationService ??= await EasyPackArchitecture.Instance.ResolveAsync<ISerializationService>();
        }

        /// <summary>
        /// 序列化 CategoryManager 为 JSON 字符串
        /// 实体序列化由 SerializationService 管理
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
                    Version = CurrentVersion,
                    Entities = new List<SerializableCategoryManagerState<T>.SerializedEntity>(),
                    Categories = new List<SerializableCategoryManagerState<T>.SerializedCategory>(),
                    Tags = new List<SerializableCategoryManagerState<T>.SerializedTag>(),
                    Metadata = new List<SerializableCategoryManagerState<T>.SerializedMetadata>()
                };

                // 序列化实体和分类
                var categories = manager.GetAllCategories();
                foreach (var category in categories)
                {
                    var entities = manager.GetByCategory(category, includeChildren: false);
                    
                    data.Categories.Add(new SerializableCategoryManagerState<T>.SerializedCategory
                    {
                        Name = category
                    });

                    foreach (var entity in entities)
                    {
                        var id = _idExtractor(entity);
                        
                        // 避免重复添加实体
                        if (!data.Entities.Any(e => e.Id == id))
                        {
                            // 使用 SerializationService 序列化实体
                            string entityJson = _serializationService != null
                                ? _serializationService.SerializeToJson(entity)
                                : JsonUtility.ToJson(entity);

                            data.Entities.Add(new SerializableCategoryManagerState<T>.SerializedEntity
                            {
                                Id = id,
                                EntityJson = entityJson,
                                Category = category
                            });
                        }
                    }
                }

                // 序列化标签
                var tagIndex = manager.GetTagIndex();
                foreach (var kvp in tagIndex)
                {
                    data.Tags.Add(new SerializableCategoryManagerState<T>.SerializedTag
                    {
                        TagName = kvp.Key,
                        EntityIds = kvp.Value.ToList()
                    });
                }

                // 序列化元数据
                var metadataStore = manager.GetMetadataStore();
                foreach (var kvp in metadataStore)
                {
                    data.Metadata.Add(new SerializableCategoryManagerState<T>.SerializedMetadata
                    {
                        EntityId = kvp.Key,
                        MetadataJson = JsonUtility.ToJson(new CustomDataCollectionWrapper { Entries = kvp.Value.ToList() })
                    });
                }

                return JsonUtility.ToJson(data, prettyPrint: true);
            }
            catch (Exception ex)
            {
                Debug.LogError($"CategoryService 序列化失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 从 JSON 字符串反序列化为 CategoryManager
        /// 实体反序列化由 SerializationService 管理
        /// 注意：此方法创建新的 CategoryManager 实例
        /// </summary>
        /// <param name="json">JSON 字符串</param>
        /// <returns>反序列化的 CategoryManager 实例</returns>
        public override CategoryManager<T> DeserializeFromJson(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                throw new InvalidOperationException("JSON 字符串为空");
            }

            try
            {
                EnsureSerializationService();

                var data = JsonUtility.FromJson<SerializableCategoryManagerState<T>>(json);

                // 版本兼容性检查
                if (data.Version > CurrentVersion)
                {
                    throw new InvalidOperationException(
                        $"不支持的序列化版本: {data.Version} (当前版本: {CurrentVersion})");
                }

                // 创建新的 CategoryManager 实例
                var manager = new CategoryManager<T>(_idExtractor);

                // 反序列化实体
                var entityRegistrations = new List<(T entity, string category, List<string> tags, CustomDataCollection metadata)>();

                foreach (var serializedEntity in data.Entities)
                {
                    try
                    {
                        // 使用 SerializationService 反序列化实体
                        T entity;
                        entity = _serializationService != null
                            ? _serializationService.DeserializeFromJson<T>(serializedEntity.EntityJson)
                            : JsonUtility.FromJson<T>(serializedEntity.EntityJson);
                        
                        // 查找实体的标签
                        var tags = data.Tags
                            .Where(t => t.EntityIds.Contains(serializedEntity.Id))
                            .Select(t => t.TagName)
                            .ToList();

                        // 查找实体的元数据
                        CustomDataCollection metadata = null;
                        var metadataEntry = data.Metadata.FirstOrDefault(m => m.EntityId == serializedEntity.Id);
                        if (metadataEntry != null)
                        {
                            var wrapper = JsonUtility.FromJson<CustomDataCollectionWrapper>(metadataEntry.MetadataJson);
                            metadata = new CustomDataCollection();
                            foreach (var entry in wrapper.Entries)
                            {
                                metadata.Add(entry);
                            }
                        }

                        entityRegistrations.Add((entity, serializedEntity.Category, tags, metadata));
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"反序列化实体 '{serializedEntity.Id}' 失败: {ex.Message}");
                    }
                }

                // 注册所有实体
                foreach (var (entity, category, tags, metadata) in entityRegistrations)
                {
                    var registration = manager.RegisterEntity(entity, category);
                    
                    if (tags is { Count: > 0 })
                    {
                        registration.WithTags(tags.ToArray());
                    }

                    if (metadata != null)
                    {
                        registration.WithMetadata(metadata);
                    }
                    
                    var result = registration.Complete();
                    
                    if (!result.IsSuccess)
                    {
                        Debug.LogWarning($"注册实体失败: {result.ErrorMessage}");
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
