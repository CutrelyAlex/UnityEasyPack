using System;
using System.Collections.Generic;
using System.Linq;
using EasyPack.Architecture;
using EasyPack.CustomData;
using EasyPack.Serialization;
using UnityEngine;

namespace EasyPack.Category
{
    /// <summary>
    ///     CategoryManager JSON 双泛型序列化器
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    /// <typeparam name="TKey">键类型</typeparam>
    public class CategoryManagerJsonSerializer<T, TKey> :
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
            if (typeof(TKey) == typeof(long) || typeof(TKey) == typeof(Guid))
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
                data.Categories.Add(new() { Name = category });

                var entities = manager.GetByCategory(category);
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

                    data.Entities.Add(new() { KeyJson = keyJson, EntityJson = entityJson, Category = category });
                }
            }

            // 序列化标签
            var tagIndex = manager.GetTagIndex();
            foreach (var kvp in tagIndex)
            {
                data.Tags.Add(new() { TagName = kvp.Key, EntityKeyJsons = kvp.Value.Select(SerializeKey).ToList() });
            }

            // 序列化元数据
            var metadataStore = manager.GetMetadataStore();
            foreach (var kvp in metadataStore)
            {
                data.Metadata.Add(new()
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

            foreach (SerializableCategoryManagerState<T, TKey>.SerializedEntity serializedEntity in data.Entities)
            {
                try
                {
                    // 使用 SerializationService 反序列化实体
                    T entity = _serializationService != null
                        ? _serializationService.DeserializeFromJson<T>(serializedEntity.EntityJson)
                        : JsonUtility.FromJson<T>(serializedEntity.EntityJson);

                    // TKey key = DeserializeKey(serializedEntity.KeyJson);
                    string keyJson = serializedEntity.KeyJson;

                    // 查找实体的标签
                    var tags = data.Tags
                        .Where(t => t.EntityKeyJsons.Contains(keyJson))
                        .Select(t => t.TagName)
                        .ToList();

                    // 查找实体的元数据
                    CustomDataCollection metadata = null;
                    SerializableCategoryManagerState<T, TKey>.SerializedMetadata metadataEntry =
                        data.Metadata.FirstOrDefault(m => m.EntityKeyJson == keyJson);
                    if (metadataEntry != null)
                    {
                        var wrapper = JsonUtility.FromJson<CustomDataCollectionWrapper>(metadataEntry.MetadataJson);
                        metadata = new();
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
        public string ToJson(SerializableCategoryManagerState<T, TKey> dto) =>
            dto == null ? null : JsonUtility.ToJson(dto, true);

        /// <summary>
        ///     从 JSON 转为可序列化对象
        /// </summary>
        public SerializableCategoryManagerState<T, TKey> FromJson(string json) =>
            string.IsNullOrEmpty(json)
                ? throw new InvalidOperationException("JSON 字符串为空")
                : JsonUtility.FromJson<SerializableCategoryManagerState<T, TKey>>(json);

        /// <summary>
        ///     序列化 CategoryManager 为 JSON 字符串
        /// </summary>
        public string SerializeToJson(CategoryManager<T, TKey> manager)
        {
            SerializableCategoryManagerState<T, TKey> dto = ToSerializable(manager);
            return ToJson(dto);
        }

        /// <summary>
        ///     从 JSON 字符串反序列化为 CategoryManager
        /// </summary>
        public CategoryManager<T, TKey> DeserializeFromJson(string json)
        {
            SerializableCategoryManagerState<T, TKey> dto = FromJson(json);
            return FromSerializable(dto);
        }

        #endregion

        #region 辅助类

        [Serializable]
        private class CustomDataCollectionWrapper
        {
            public List<CustomDataEntry> Entries;
        }

        #endregion
    }
}