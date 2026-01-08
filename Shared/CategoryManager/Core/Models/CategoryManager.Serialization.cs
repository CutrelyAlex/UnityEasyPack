using System;
using System.Collections.Generic;
using System.Linq;
using EasyPack.CustomData;


namespace EasyPack.Category
{
    /// <summary>
    ///     双泛型分类管理系统，支持自定义键类型。
    ///     <para>T: 实体类型</para>
    ///     <para>TKey: 键类型（如 int, string, Guid 等）</para>
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    /// <typeparam name="TKey">键类型，必须实现 IEquatable&lt;TKey&gt;</typeparam>
    public partial class CategoryManager<T, TKey> : ICategoryManager<T, TKey>
        where TKey : IEquatable<TKey>
    {
        #region 序列化

        /// <summary>
        ///     序列化为 JSON 字符串。
        /// </summary>
        public string SerializeToJson()
        {
            var serializer = new CategoryManagerJsonSerializer<T, TKey>(_keyExtractor);
            return serializer.SerializeToJson(this);
        }

        /// <summary>
        ///     从 JSON 字符串加载数据。
        /// </summary>
        /// <param name="json">JSON 字符串。</param>
        /// <returns>操作结果。</returns>
        public OperationResult LoadFromJson(string json)
        {
            try
            {
                var serializer = new CategoryManagerJsonSerializer<T, TKey>(_keyExtractor);
                var newManager = serializer.DeserializeFromJson(json);

                _treeLock.EnterWriteLock();
                try
                {
                    Clear_NoLock();

                    // 复制数据
                    foreach (var kvp in newManager._entities)
                    {
                        _entities[kvp.Key] = kvp.Value;
                    }

                    foreach (var kvp in newManager._categoryNodes)
                    {
                        _categoryNodes[kvp.Key] = kvp.Value;
                    }

                    foreach (var kvp in newManager._categoryNameToId)
                    {
                        _categoryNameToId[kvp.Key] = kvp.Value;
                    }

                    foreach (var kvp in newManager._categoryIdToName)
                    {
                        _categoryIdToName[kvp.Key] = kvp.Value;
                    }

                    // 复制实体到分类的映射，并更新节点引用
                    foreach (var kvp in newManager._entityKeyToNode)
                    {
                        // 获取节点的 TermId，从当前管理器中获取对应的节点
                        int nodeTermId = kvp.Value.TermId;
                        if (_categoryNodes.TryGetValue(nodeTermId, out CategoryNode localNode))
                        {
                            _entityKeyToNode[kvp.Key] = localNode;
                        }
                    }

                    foreach (var kvp in newManager._tagToEntityKeys)
                    {
                        _tagToEntityKeys[kvp.Key] = new(kvp.Value, _keyComparer);
                    }

                    foreach (var kvp in newManager._entityToTagIds)
                    {
                        _entityToTagIds[kvp.Key] = new(kvp.Value);
                    }

                    foreach (var kvp in newManager._metadataStore)
                    {
                        _metadataStore[kvp.Key] = kvp.Value;
                    }

                    // 复制标签映射器状态
                    var tagSnapshot = newManager._tagMapper.GetSnapshot();
                    foreach (var kvp in tagSnapshot)
                    {
                        _tagMapper.GetOrAssignId(kvp.Value);
                    }

                    var categorySnapshot = newManager._categoryTermMapper.GetSnapshot();
                    foreach (var kvp in categorySnapshot)
                    {
                        _categoryTermMapper.GetOrAssignId(kvp.Value);
                    }
                }
                finally
                {
                    _treeLock.ExitWriteLock();
                }

                return OperationResult.Success();
            }
            catch (Exception ex)
            {
                return OperationResult.Failure(ErrorCode.InvalidCategory, $"反序列化失败: {ex.Message}");
            }
        }

        /// <summary>
        ///     从 JSON 字符串创建新的 CategoryManager 实例。
        /// </summary>
        /// <param name="json">JSON 字符串。</param>
        /// <param name="keyExtractor">键提取函数。</param>
        /// <returns>新的 CategoryManager 实例。</returns>
        public static CategoryManager<T, TKey> CreateFromJson(string json, Func<T, TKey> keyExtractor)
        {
            var serializer = new CategoryManagerJsonSerializer<T, TKey>(keyExtractor);
            return serializer.DeserializeFromJson(json);
        }

        /// <summary>
        ///     获取标签索引（标签名 -> 实体键列表）。
        /// </summary>
        public IReadOnlyDictionary<string, IReadOnlyList<TKey>> GetTagIndex()
        {
            var result = new Dictionary<string, IReadOnlyList<TKey>>();
            _treeLock.EnterReadLock();
            try
            {
                foreach (var kvp in _tagToEntityKeys)
                {
                    if (_tagMapper.TryGetString(kvp.Key, out string tagName)) result[tagName] = kvp.Value.ToList();
                }
            }
            finally
            {
                _treeLock.ExitReadLock();
            }

            return result;
        }

        /// <summary>
        ///     获取优化的序列化索引。
        /// </summary>
        public void GetOptimizedSerializationIndices(
            out Dictionary<TKey, List<string>> entityTagIndex,
            out Dictionary<TKey, CustomDataCollection> entityMetadataIndex)
        {
            entityTagIndex = new(_keyComparer);
            entityMetadataIndex = new(_keyComparer);

            _treeLock.EnterReadLock();
            try
            {
                // 构建实体 -> 标签列表
                foreach (var kvp in _entityToTagIds)
                {
                    var tags = new List<string>();
                    foreach (int tagId in kvp.Value)
                    {
                        if (_tagMapper.TryGetString(tagId, out string tagName)) tags.Add(tagName);
                    }

                    if (tags.Count > 0) entityTagIndex[kvp.Key] = tags;
                }
            }
            finally
            {
                _treeLock.ExitReadLock();
            }

            foreach (var kvp in _metadataStore)
            {
                entityMetadataIndex[kvp.Key] = kvp.Value;
            }
        }

        /// <summary>
        ///     仅注册分类关联（不注册实体对象）。
        /// </summary>
        public OperationResult RegisterCategoryAssociationOnly(
            TKey key, string category, List<string> tags, CustomDataCollection metadata)
        {
            _treeLock.EnterWriteLock();
            try
            {
                // 确保分类存在
                EnsureCategoryExists_NoLock(category);

                // 获取或创建分类节点
                if (!_categoryNameToId.TryGetValue(category, out int categoryId))
                {
                    return OperationResult.Failure(ErrorCode.InvalidCategory, $"分类 '{category}' 不存在");
                }

                CategoryNode node = _categoryNodes[categoryId];
                _entityKeyToNode[key] = node;

                // 添加标签
                if (tags != null)
                {
                    foreach (string tag in tags)
                    {
                        int tagId = _tagMapper.GetOrAssignId(tag);

                        if (!_tagToEntityKeys.TryGetValue(tagId, out var entityKeys))
                        {
                            entityKeys = new(_keyComparer);
                            _tagToEntityKeys[tagId] = entityKeys;
                        }

                        entityKeys.Add(key);

                        if (!_entityToTagIds.TryGetValue(key, out var tagIds))
                        {
                            tagIds = new();
                            _entityToTagIds[key] = tagIds;
                        }

                        tagIds.Add(tagId);
                    }
                }

                // 添加元数据
                if (metadata != null) _metadataStore[key] = metadata;

                return OperationResult.Success();
            }
            finally
            {
                _treeLock.ExitWriteLock();
            }
        }

        /// <summary>
        ///     获取可序列化的状态对象。
        /// </summary>
        public SerializableCategoryManagerState<T, TKey> GetSerializableState(
            Func<T, string> entitySerializer, Func<TKey, string> keySerializer,
            Func<CustomDataCollection, string> metadataSerializer)
        {
            if (keySerializer == null) throw new ArgumentNullException(nameof(keySerializer));

            var data = new SerializableCategoryManagerState<T, TKey>
            {
                Entities = new(),
                Categories = new(),
                Tags = new(),
                Metadata = new(),
                IncludeEntities = entitySerializer != null,
            };

            var entitySnapshots = new List<(TKey Key, T Entity, string CategoryName)>();
            var categoryNames = new List<string>();

            _treeLock.EnterReadLock();
            try
            {
                categoryNames.AddRange(_categoryIdToName.Values);

                foreach (var kvp in _entityKeyToNode)
                {
                    TKey key = kvp.Key;
                    CategoryNode node = kvp.Value;

                    if (!_entities.TryGetValue(key, out T entity))
                    {
                        continue;
                    }

                    string categoryName = _categoryIdToName.TryGetValue(node.TermId, out string name)
                        ? name
                        : "Default";

                    entitySnapshots.Add((key, entity, categoryName));
                }
            }
            finally
            {
                _treeLock.ExitReadLock();
            }

            foreach (string categoryName in categoryNames)
            {
                data.Categories.Add(new() { Name = categoryName });
            }

            foreach ((TKey Key, T Entity, string CategoryName) snap in entitySnapshots)
            {
                string keyJson = keySerializer(snap.Key);
                string entityJson = entitySerializer != null ? entitySerializer(snap.Entity) : null;
                data.Entities.Add(new() { KeyJson = keyJson, EntityJson = entityJson, Category = snap.CategoryName });
            }

            var tagSnapshots = new List<(string TagName, List<TKey> Keys)>();
            _tagSystemLock.EnterReadLock();
            try
            {
                foreach (var kvp in _tagToEntityKeys)
                {
                    int tagId = kvp.Key;
                    if (!_tagMapper.TryGetString(tagId, out string tagName))
                    {
                        continue;
                    }

                    var keysCopy = kvp.Value != null ? kvp.Value.ToList() : new();
                    tagSnapshots.Add((tagName, keysCopy));
                }
            }
            finally
            {
                _tagSystemLock.ExitReadLock();
            }

            foreach (var tagSnap in tagSnapshots)
            {
                var keyJsons = new List<string>(tagSnap.Keys.Count);
                foreach (TKey k in tagSnap.Keys)
                {
                    keyJsons.Add(keySerializer(k));
                }

                data.Tags.Add(new() { TagName = tagSnap.TagName, EntityKeyJsons = keyJsons });
            }

            var metadataSnapshots = new List<(TKey Key, CustomDataCollection Metadata)>();
            foreach (var kvp in _metadataStore)
            {
                metadataSnapshots.Add((kvp.Key, kvp.Value));
            }

            foreach ((TKey Key, CustomDataCollection Metadata) metaSnap in metadataSnapshots)
            {
                string keyJson = keySerializer(metaSnap.Key);
                string metadataJson = metadataSerializer != null ? metadataSerializer(metaSnap.Metadata) : null;
                data.Metadata.Add(new() { EntityKeyJson = keyJson, MetadataJson = metadataJson });
            }

            return data;
        }

        #endregion

    }
}