using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
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
        #region 属性

        /// <summary>
        ///     获取实体类型。
        /// </summary>
        public Type EntityType => typeof(T);

        /// <summary>
        ///     获取键类型。
        /// </summary>
        public Type KeyType => typeof(TKey);

        #endregion

        #region 字段

        // 键提取器
        private readonly Func<T, TKey> _keyExtractor;

        // 实体存储（并发集合：减少显式锁的需求）
        private readonly ConcurrentDictionary<TKey, T> _entities;



        // 键比较器
        private readonly IEqualityComparer<TKey> _keyComparer;

        #endregion

        #region 构造

        /// <summary>
        ///     使用键提取器初始化 CategoryManager。
        /// </summary>
        /// <param name="keyExtractor">从实体提取键的函数。</param>
        /// <param name="keyComparer">可选的键比较器，默认使用 EqualityComparer&lt;TKey&gt;.Default。</param>
        public CategoryManager(Func<T, TKey> keyExtractor, IEqualityComparer<TKey> keyComparer = null)
        {
            _keyExtractor = keyExtractor ?? throw new ArgumentNullException(nameof(keyExtractor));
            _keyComparer = keyComparer ?? EqualityComparer<TKey>.Default;

            // 使用比较器初始化字典
            _entities = new(_keyComparer);
            _entityKeyToNode = new(_keyComparer);
            _nodeToEntityKeys = new();
            _tagToEntityKeys = new();
            _entityToTagIds = new(_keyComparer);
            _metadataStore = new(_keyComparer);

            _categoryNodes = new();
            _categoryNameToId = new(StringComparer.Ordinal);
            _categoryIdToName = new();
            _tagCache = new();

            _tagMapper = new();
            _categoryTermMapper = new();

            _treeLock = new(LockRecursionPolicy.NoRecursion);
            _tagSystemLock = new(LockRecursionPolicy.NoRecursion);
        }

        #endregion

        #region 全量实体访问

        /// <summary>
        ///     获取当前 Manager 内的所有实体快照。
        ///     <para>注意：返回的是快照列表，不是实时引用</para>
        /// </summary>
        public IReadOnlyList<T> GetAllEntities()
        {
            // ConcurrentDictionary 的枚举是线程安全的（快照语义），这里直接拷贝为 List 即可。
            return _entities.Values.ToList();
        }

        #endregion

        #region 实体注册

        /// <summary>
        ///     注册实体到指定分类（同步操作）。
        /// </summary>
        public OperationResult RegisterEntityComplete(T entity, string category) =>
            RegisterEntity(entity, category).Complete();

        /// <summary>
        ///     使用指定键注册实体（安全模式）。
        /// </summary>
        public IEntityRegistration<T, TKey> RegisterEntitySafeWithKey(T entity, TKey key, string category) =>
            new EntityRegistration(this, entity, key, category);

        /// <summary>
        ///     开始注册实体，支持链式调用添加标签和元数据。
        /// </summary>
        public IEntityRegistration RegisterEntity(T entity, string category)
        {
            TKey key = _keyExtractor(entity);
            return new EntityRegistrationGeneric(this, key, entity, category, OperationResult.Success());
        }

        /// <summary>
        ///     注册实体，自动验证分类名称格式。
        /// </summary>
        public IEntityRegistration RegisterEntitySafe(T entity, string category)
        {
            TKey key = _keyExtractor(entity);
            category = CategoryNameNormalizer.Normalize(category);

            if (!CategoryNameNormalizer.IsValid(category, out string errorMessage))
            {
                return new EntityRegistrationGeneric(this, key, entity, category,
                    OperationResult.Failure(ErrorCode.InvalidCategory, errorMessage));
            }

            return new EntityRegistrationGeneric(this, key, entity, category, OperationResult.Success());
        }

        /// <summary>
        ///     使用显式键注册实体到指定分类。
        /// </summary>
        public OperationResult RegisterEntity(TKey key, T entity, string category)
        {
            category = CategoryNameNormalizer.Normalize(category);
            if (!CategoryNameNormalizer.IsValid(category, out string errorMessage))
            {
                return OperationResult.Failure(ErrorCode.InvalidCategory, errorMessage);
            }

            _treeLock.EnterWriteLock();
            try
            {
                // 先写入实体存储，避免出现分类映射存在但实体不存在
                if (!_entities.TryAdd(key, entity))
                {
#if UNITY_EDITOR || DEBUG
                    UnityEngine.Debug.LogWarning($"[CategoryManager] RegisterEntity 失败: Key '{key}' 已经存在. Entity type: {typeof(T).Name}, Category: {category}");
#endif
                    return OperationResult.Failure(ErrorCode.DuplicateId, $"实体键 '{key}' 已存在");
                }

                CategoryNode node = GetOrCreateNode(category);
                _entityKeyToNode[key] = node;

                // 维护反向索引
                if (!_nodeToEntityKeys.TryGetValue(node.TermId, out var nodeEntityKeys))
                {
                    nodeEntityKeys = new(_keyComparer);
                    _nodeToEntityKeys[node.TermId] = nodeEntityKeys;
                }

                nodeEntityKeys.Add(key);

#if UNITY_EDITOR
                _cachedStatistics = null;
#endif

                return OperationResult.Success();
            }
            finally
            {
                _treeLock.ExitWriteLock();
            }
        }

        /// <summary>
        ///     使用显式键注册实体并添加标签。
        /// </summary>
        public OperationResult RegisterEntityWithTags(TKey key, T entity, string category, params string[] tags)
        {
            OperationResult result = RegisterEntity(key, entity, category);
            if (!result.IsSuccess) return result;

            return (tags == null || tags.Length == 0)
                ? OperationResult.Success()
                : AddTags(key, tags);
        }

        /// <summary>
        ///     使用显式键注册实体并添加元数据。
        /// </summary>
        public OperationResult RegisterEntityWithMetadata(TKey key, T entity, string category,
                                                          CustomDataCollection metadata)
        {
            OperationResult result = RegisterEntity(key, entity, category);
            if (!result.IsSuccess) return result;

            if (metadata != null)
            {
                _metadataStore[key] = metadata;
            }

            return OperationResult.Success();
        }

        /// <summary>
        ///     批量注册多个实体到同一分类。
        /// </summary>
        public BatchOperationResult RegisterBatch(List<T> entities, string category)
        {
            var results = new List<(string EntityId, bool Success, ErrorCode ErrorCode, string ErrorMessage)>();
            int successCount = 0;

            foreach (T entity in entities)
            {
                TKey key = _keyExtractor(entity);
                OperationResult result = RegisterEntity(key, entity, category);
                results.Add((key?.ToString() ?? "null", result.IsSuccess, result.ErrorCode, result.ErrorMessage));
                if (result.IsSuccess) successCount++;
            }

            return new()
            {
                TotalCount = entities.Count,
                SuccessCount = successCount,
                FailureCount = entities.Count - successCount,
                Details = results,
            };
        }

        #endregion

        #region 实体查询



        /// <summary>
        ///     按键查询实体。
        /// </summary>
        public OperationResult<T> GetById(TKey key)
        {
            return _entities.TryGetValue(key, out T entity)
                ? OperationResult<T>.Success(entity)
                : OperationResult<T>.Failure(ErrorCode.NotFound, $"未找到键为 '{key}' 的实体");
        }
        #endregion

        #region 实体删除

        /// <summary>
        ///     删除实体。
        /// </summary>
        public OperationResult DeleteEntity(TKey key)
        {
#if UNITY_EDITOR || DEBUG
            // UnityEngine.Debug.Log($"[CategoryManager] DeleteEntity key '{key}'");
#endif
            // 使用确定的锁顺序：_treeLock -> _tagSystemLock
            _treeLock.EnterWriteLock();
            try
            {
                // 并发情况下，实体/映射可能已经被其他线程部分清理；这里允许“补偿式清理”。
                if (!_entities.ContainsKey(key) && !_entityKeyToNode.ContainsKey(key))
                {
                    return OperationResult.Failure(ErrorCode.NotFound, $"未找到键为 '{key}' 的实体");
                }

                // 从反向索引移除
                if (_entityKeyToNode.TryGetValue(key, out CategoryNode node))
                {
                    if (_nodeToEntityKeys.TryGetValue(node.TermId, out var nodeEntityKeys))
                    {
                        nodeEntityKeys.Remove(key);
                        if (nodeEntityKeys.Count == 0) _nodeToEntityKeys.Remove(node.TermId);
                    }
                }

                // 从分类节点移除
                _entityKeyToNode.Remove(key);

                // 从标签系统移除
                _tagSystemLock.EnterWriteLock();
                try
                {
                    try
                    {
                        RemoveEntityFromTagSystemLocked(key);
                    }
                    catch (Exception ex)
                    {
#if UNITY_EDITOR || DEBUG
                        UnityEngine.Debug.LogError($"[CategoryManager] RemoveEntityFromTagSystemLocked 失败 '{key}': {ex.Message}");
#endif
                    }

                    _metadataStore.TryRemove(key, out _);
                    _entities.TryRemove(key, out _);
                }
                finally
                {
                    _tagSystemLock.ExitWriteLock();
                }

#if UNITY_EDITOR
                _cachedStatistics = null;
#endif

                return OperationResult.Success();
            }
            finally
            {
                _treeLock.ExitWriteLock();
            }
        }

        /// <summary>
        ///     更新实体的引用。
        ///     如果键不存在，则返回失败。
        ///     此操作不会改变实体的分类、标签或元数据。
        /// </summary>
        public OperationResult UpdateEntityReference(TKey key, T entity)
        {
            if (entity == null) return OperationResult.Failure(ErrorCode.InvalidParameter, "实体不能为空");

            TKey extractedKey = _keyExtractor(entity);
            if (!_keyComparer.Equals(extractedKey, key))
            {
                return OperationResult.Failure(ErrorCode.InvalidParameter,
                    "实体的键与提供的键不匹配");
            }

            if (!_entities.ContainsKey(key))
            {
                return OperationResult.Failure(ErrorCode.NotFound, $"未找到键为 '{key}' 的实体");
            }

            // 更新实体引用（并发集合写入线程安全）
            _entities[key] = entity;

            // 检查该实体是否有关联的标签，如果有，需要清理标签缓存
            _tagSystemLock.EnterWriteLock();
            try
            {
                if (_entityToTagIds.TryGetValue(key, out var tagIds))
                {
                    foreach (int tagId in tagIds)
                    {
                        _tagCache.Remove(tagId);
                    }
                }
            }
            finally
            {
                _tagSystemLock.ExitWriteLock();
            }

            return OperationResult.Success();
        }

        #endregion

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
        ///     获取元数据存储。
        /// </summary>
        public IReadOnlyDictionary<TKey, CustomDataCollection> GetMetadataStore()
        {
            // 并发集合：直接快照拷贝即可
            return new Dictionary<TKey, CustomDataCollection>(_metadataStore, _keyComparer);
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

        #region 缓存
        /// <summary>
        ///     获取缓存条目数量。
        /// </summary>
        /// <returns>缓存项数。</returns>
        public int GetCacheSize() => _tagCache.Count;

        /// <summary>
        ///     清除所有缓存。
        /// </summary>
        public void ClearCache()
        {
            _tagCache.Clear();
        }

        #endregion

        #region 清空与重置

        /// <summary>
        ///     清空所有数据。
        /// </summary>
        public void Clear()
        {
            _treeLock.EnterWriteLock();
            try
            {
                Clear_NoLock();
            }
            finally
            {
                _treeLock.ExitWriteLock();
            }
        }

        /// <summary>
        ///     清空所有数据（无锁版本，假设已持有写锁）。
        /// </summary>
        private void Clear_NoLock()
        {
            _entities.Clear();
            _entityKeyToNode.Clear();
            _categoryNodes.Clear();
            _categoryNameToId.Clear();
            _categoryIdToName.Clear();
            _tagToEntityKeys.Clear();
            _entityToTagIds.Clear();
            _tagCache.Clear();
            _metadataStore.Clear();
            _tagMapper.Clear();
            _categoryTermMapper.Clear();

#if UNITY_EDITOR
            _cachedStatistics = null;
#endif
        }

        /// <summary>
        ///     确保分类存在（无锁版本，假设已持有写锁）。
        /// </summary>
        private void EnsureCategoryExists_NoLock(string category)
        {
            if (_categoryNameToId.ContainsKey(category)) return;

            // 使用 GetOrCreateNode 的逻辑
            string fullPath = CategoryNameNormalizer.Normalize(category);
            string[] parts = fullPath.Split('.');
            CategoryNode node;

            if (parts.Length > 1)
            {
                string parentPath = string.Join(".", parts.Take(parts.Length - 1));
                EnsureCategoryExists_NoLock(parentPath);

                if (!_categoryNameToId.TryGetValue(parentPath, out int parentId) ||
                    !_categoryNodes.TryGetValue(parentId, out CategoryNode parent))
                {
                    return; // 父节点创建失败
                }

                int newCategoryId = _categoryTermMapper.GetOrAssignId(fullPath);
                node = parent.GetOrCreateChild(newCategoryId);
                _categoryNodes[newCategoryId] = node;
                _categoryNameToId[fullPath] = newCategoryId;
                _categoryIdToName[newCategoryId] = fullPath;
            }
            else
            {
                int newCategoryId = _categoryTermMapper.GetOrAssignId(fullPath);
                node = new(newCategoryId);
                _categoryNodes[newCategoryId] = node;
                _categoryNameToId[fullPath] = newCategoryId;
                _categoryIdToName[newCategoryId] = fullPath;
            }
        }

        #endregion

        #region 统计

#if UNITY_EDITOR
        private Statistics _cachedStatistics;
        private long _lastStatisticsUpdate;
        private const long StatisticsCacheTimeout = 10000000;

        private long _totalCacheQueries;
        private long _cacheHits;
        private long _cacheMisses;
#endif

        /// <summary>
        ///     获取统计信息。
        /// </summary>
        public Statistics GetStatistics()
        {
#if UNITY_EDITOR
            long now = DateTime.UtcNow.Ticks;
            if (_cachedStatistics != null && now - _lastStatisticsUpdate < StatisticsCacheTimeout)
            {
                return _cachedStatistics;
            }

            // 计算最大分类深度
            int maxDepth = 0;
            foreach (var kvp in _categoryIdToName)
            {
                int depth = kvp.Value.Split('.').Length;
                if (depth > maxDepth) maxDepth = depth;
            }

            // 估算内存使用
            long memoryUsage = _entities.Count * 64 + _categoryNodes.Count * 128;
            foreach (var kvp in _tagToEntityKeys)
            {
                memoryUsage += kvp.Value.Count * 16;
            }

            memoryUsage += _metadataStore.Count * 64;

            _cachedStatistics = new()
            {
                TotalEntities = _entities.Count,
                TotalCategories = _categoryNodes.Count,
                TotalTags = _tagMapper.Count,
                CacheHitRate = (float)(_totalCacheQueries > 0 ? (double)_cacheHits / _totalCacheQueries : 0),
                MaxCategoryDepth = maxDepth,
                MemoryUsageBytes = memoryUsage,
                TotalCacheQueries = _totalCacheQueries,
                CacheHits = _cacheHits,
                CacheMisses = _cacheMisses,
            };
            _lastStatisticsUpdate = now;

            return _cachedStatistics;
#else
            return new Statistics();
#endif
        }

        private void RecordCacheQuery(bool isHit)
        {
#if UNITY_EDITOR
            _totalCacheQueries++;
            if (isHit)
            {
                _cacheHits++;
            }
            else
            {
                _cacheMisses++;
            }
#endif
        }

        #endregion

        #region 释放资源

        /// <summary>
        ///     释放所有资源。
        /// </summary>
        public void Dispose()
        {
            _treeLock?.Dispose();

            _tagSystemLock?.Dispose();
        }

        #endregion
    }
}