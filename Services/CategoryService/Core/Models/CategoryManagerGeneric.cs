using System;
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
    public class CategoryManager<T, TKey> : ICategoryManager<T, TKey>
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

        // 实体存储
        private readonly Dictionary<TKey, T> _entities;

        // 分类树
        private readonly Dictionary<int, CategoryNode> _categoryNodes;
        private readonly Dictionary<string, int> _categoryNameToId;
        private readonly Dictionary<int, string> _categoryIdToName;

        // 标签系统
        private readonly Dictionary<int, HashSet<TKey>> _tagToEntityKeys; // tagId → entityKeys
        private readonly Dictionary<TKey, HashSet<int>> _entityToTagIds;  // entityKey → tagIds
        private readonly Dictionary<int, ReaderWriterLockSlim> _tagLocks;
        private readonly Dictionary<int, List<T>> _tagCache;

        // 反向索引缓存
        private readonly Dictionary<TKey, CategoryNode> _entityKeyToNode; // entityKey → 所属分类节点

        // 映射层
        private readonly IntegerMapper _tagMapper;
        private readonly IntegerMapper _categoryTermMapper;

        // 元数据存储
        private readonly Dictionary<TKey, CustomDataCollection> _metadataStore;

        // 锁
        private readonly ReaderWriterLockSlim _treeLock;
        private readonly ReaderWriterLockSlim _entitiesLock;
        private readonly ReaderWriterLockSlim _metadataLock;

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
            _entities = new Dictionary<TKey, T>(_keyComparer);
            _entityKeyToNode = new Dictionary<TKey, CategoryNode>(_keyComparer);
            _tagToEntityKeys = new Dictionary<int, HashSet<TKey>>();
            _entityToTagIds = new Dictionary<TKey, HashSet<int>>(_keyComparer);
            _metadataStore = new Dictionary<TKey, CustomDataCollection>(_keyComparer);

            _categoryNodes = new Dictionary<int, CategoryNode>();
            _categoryNameToId = new Dictionary<string, int>(StringComparer.Ordinal);
            _categoryIdToName = new Dictionary<int, string>();

            _tagLocks = new Dictionary<int, ReaderWriterLockSlim>();
            _tagCache = new Dictionary<int, List<T>>();

            _tagMapper = new IntegerMapper();
            _categoryTermMapper = new IntegerMapper();

            _treeLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
            _entitiesLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
            _metadataLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        }

        #endregion

        #region 实体注册

        /// <summary>
        ///     注册实体到指定分类（同步操作）。
        /// </summary>
        public OperationResult RegisterEntityComplete(T entity, string category) =>
            RegisterEntity(entity, category).Complete();

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
                return new EntityRegistrationGeneric(this, key, entity, category,
                    OperationResult.Failure(ErrorCode.InvalidCategory, errorMessage));

            return new EntityRegistrationGeneric(this, key, entity, category, OperationResult.Success());
        }

        /// <summary>
        ///     使用显式键注册实体到指定分类。
        /// </summary>
        public OperationResult RegisterEntity(TKey key, T entity, string category)
        {
            category = CategoryNameNormalizer.Normalize(category);
            if (!CategoryNameNormalizer.IsValid(category, out string errorMessage))
                return OperationResult.Failure(ErrorCode.InvalidCategory, errorMessage);

            _entitiesLock.EnterReadLock();
            try
            {
                if (_entities.ContainsKey(key))
                    return OperationResult.Failure(ErrorCode.DuplicateId, $"实体键 '{key}' 已存在");
            }
            finally
            {
                _entitiesLock.ExitReadLock();
            }

            _treeLock.EnterWriteLock();
            try
            {
                CategoryNode node = GetOrCreateNode(category);

                _entitiesLock.EnterWriteLock();
                try
                {
                    _entities[key] = entity;
                }
                finally
                {
                    _entitiesLock.ExitWriteLock();
                }

                _entityKeyToNode[key] = node;

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

            if (tags != null && tags.Length > 0)
            {
                foreach (string tag in tags)
                {
                    if (!string.IsNullOrWhiteSpace(tag))
                    {
                        AddTagInternal(key, tag);
                    }
                }
            }

            return OperationResult.Success();
        }

        /// <summary>
        ///     使用显式键注册实体并添加元数据。
        /// </summary>
        public OperationResult RegisterEntityWithMetadata(TKey key, T entity, string category, CustomDataCollection metadata)
        {
            OperationResult result = RegisterEntity(key, entity, category);
            if (!result.IsSuccess) return result;

            if (metadata != null)
            {
                _metadataLock.EnterWriteLock();
                try
                {
                    _metadataStore[key] = metadata;
                }
                finally
                {
                    _metadataLock.ExitWriteLock();
                }
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

            return new BatchOperationResult
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
        ///     按分类查询实体。
        /// </summary>
        public IReadOnlyList<T> GetByCategory(string pattern, bool includeChildren = false)
        {
            pattern = CategoryNameNormalizer.Normalize(pattern);

            var entityKeys = new HashSet<TKey>(_keyComparer);

            _treeLock.EnterReadLock();
            try
            {
                if (pattern.Contains("*"))
                {
                    Regex regex = ConvertWildcardToRegex(pattern);
                    foreach (var kvp in _categoryNameToId)
                    {
                        if (regex.IsMatch(kvp.Key))
                        {
                            if (_categoryNodes.TryGetValue(kvp.Value, out CategoryNode node))
                            {
                                CollectEntityKeysFromNode(node, entityKeys, includeChildren);
                            }
                        }
                    }
                }
                else
                {
                    if (_categoryTermMapper.TryGetId(pattern, out int nodeId) &&
                        _categoryNodes.TryGetValue(nodeId, out CategoryNode node))
                    {
                        CollectEntityKeysFromNode(node, entityKeys, includeChildren);
                    }
                }
            }
            finally
            {
                _treeLock.ExitReadLock();
            }

            _entitiesLock.EnterReadLock();
            try
            {
                return entityKeys.Select(k => _entities.TryGetValue(k, out T e) ? e : default)
                                 .Where(e => e != null)
                                 .ToList();
            }
            finally
            {
                _entitiesLock.ExitReadLock();
            }
        }

        private void CollectEntityKeysFromNode(CategoryNode node, HashSet<TKey> keys, bool includeChildren)
        {
            foreach (var kvp in _entityKeyToNode)
            {
                if (ReferenceEquals(kvp.Value, node))
                {
                    keys.Add(kvp.Key);
                }
            }

            if (includeChildren)
            {
                foreach (CategoryNode child in node.Children)
                {
                    CollectEntityKeysFromNode(child, keys, true);
                }
            }
        }

        /// <summary>
        ///     按键查询实体。
        /// </summary>
        public OperationResult<T> GetById(TKey key)
        {
            _entitiesLock.EnterReadLock();
            try
            {
                if (_entities.TryGetValue(key, out T entity))
                    return OperationResult<T>.Success(entity);
                return OperationResult<T>.Failure(ErrorCode.NotFound, $"未找到键为 '{key}' 的实体");
            }
            finally
            {
                _entitiesLock.ExitReadLock();
            }
        }

        /// <summary>
        ///     按正则表达式查询分类中的实体。
        /// </summary>
        public IReadOnlyList<T> GetByCategoryRegex(string pattern, bool includeChildren = false)
        {
            if (string.IsNullOrEmpty(pattern)) return new List<T>();

            var entityKeys = new HashSet<TKey>(_keyComparer);

            _treeLock.EnterReadLock();
            try
            {
                Regex regex = RegexCache.GetOrCreate(pattern);
                foreach (var kvp in _categoryNameToId)
                {
                    if (regex.IsMatch(kvp.Key))
                    {
                        if (_categoryNodes.TryGetValue(kvp.Value, out CategoryNode node))
                        {
                            CollectEntityKeysFromNode(node, entityKeys, includeChildren);
                        }
                    }
                }
            }
            finally
            {
                _treeLock.ExitReadLock();
            }

            _entitiesLock.EnterReadLock();
            try
            {
                return entityKeys.Select(k => _entities.TryGetValue(k, out T e) ? e : default)
                                 .Where(e => e != null)
                                 .ToList();
            }
            finally
            {
                _entitiesLock.ExitReadLock();
            }
        }

        /// <summary>
        ///     获取实体的分类路径。
        /// </summary>
        public string GetReadableCategoryPath(TKey key)
        {
            _treeLock.EnterReadLock();
            try
            {
                if (!_entityKeyToNode.TryGetValue(key, out CategoryNode node))
                    return string.Empty;

                return GetNodeReadablePath(node);
            }
            finally
            {
                _treeLock.ExitReadLock();
            }
        }

        /// <summary>
        ///     从路径 ID 数组获取可读路径。
        /// </summary>
        public string GetReadablePathFromIds(int[] pathIds)
        {
            if (pathIds == null || pathIds.Length == 0) return string.Empty;

            var parts = new List<string>(pathIds.Length);
            foreach (int id in pathIds)
            {
                if (_categoryIdToName.TryGetValue(id, out string name))
                {
                    string[] segments = name.Split('.');
                    if (segments.Length > 0)
                        parts.Add(segments[^1]);
                }
            }

            return string.Join(".", parts);
        }

        /// <summary>
        ///     获取节点的可读路径。
        /// </summary>
        public string GetNodeReadablePath(CategoryNode node)
        {
            if (node == null) return string.Empty;
            if (_categoryIdToName.TryGetValue(node.TermId, out string path))
                return path;
            return string.Empty;
        }

        /// <summary>
        ///     检查实体是否在指定分类中。
        /// </summary>
        public bool IsInCategory(TKey key, string category, bool includeChildren = false)
        {
            if (string.IsNullOrEmpty(category))
                return false;

            if (!_categoryTermMapper.TryGetId(category, out int targetCategoryId))
                return false;

            _treeLock.EnterReadLock();
            try
            {
                if (!_entityKeyToNode.TryGetValue(key, out var entityNode))
                    return false;

                if (!includeChildren)
                {
                    return entityNode.TermId == targetCategoryId;
                }
                else
                {
                    CategoryNode current = entityNode;
                    while (current != null)
                    {
                        if (current.TermId == targetCategoryId)
                            return true;
                        current = current.ParentNode;
                    }
                    return false;
                }
            }
            finally
            {
                _treeLock.ExitReadLock();
            }
        }

        /// <summary>
        ///     检查实体是否在指定分类中（基于实体对象）。
        /// </summary>
        public bool IsInCategory(T entity, string category, bool includeChildren = false)
        {
            if (entity == null) return false;
            TKey key = _keyExtractor(entity);
            return IsInCategory(key, category, includeChildren);
        }

        /// <summary>
        ///     获取所有分类节点。
        /// </summary>
        public IReadOnlyList<string> GetCategoriesNodes()
        {
            _treeLock.EnterReadLock();
            try
            {
                return _categoryNameToId.Keys.ToList();
            }
            finally
            {
                _treeLock.ExitReadLock();
            }
        }

        /// <summary>
        ///     获取叶子分类。
        /// </summary>
        public IReadOnlyList<string> GetLeafCategories()
        {
            _treeLock.EnterReadLock();
            try
            {
                return _categoryNodes.Values
                    .Where(n => n.Children.Count == 0)
                    .Select(n => _categoryIdToName.TryGetValue(n.TermId, out string name) ? name : null)
                    .Where(n => n != null)
                    .ToList();
            }
            finally
            {
                _treeLock.ExitReadLock();
            }
        }

        #endregion

        #region 实体删除

        /// <summary>
        ///     删除实体。
        /// </summary>
        public OperationResult DeleteEntity(TKey key)
        {
            _entitiesLock.EnterReadLock();
            try
            {
                if (!_entities.ContainsKey(key))
                    return OperationResult.Failure(ErrorCode.NotFound, $"未找到键为 '{key}' 的实体");
            }
            finally
            {
                _entitiesLock.ExitReadLock();
            }

            _treeLock.EnterWriteLock();
            try
            {
                // 从分类节点移除
                _entityKeyToNode.Remove(key);

                // 从标签系统移除
                if (_entityToTagIds.TryGetValue(key, out var tagIds))
                {
                    foreach (int tagId in tagIds)
                    {
                        if (_tagToEntityKeys.TryGetValue(tagId, out var entityKeys))
                        {
                            entityKeys.Remove(key);
                            if (entityKeys.Count == 0)
                            {
                                _tagToEntityKeys.Remove(tagId);
                            }
                        }

                        // 清除标签缓存
                        _tagCache.Remove(tagId);
                    }
                    _entityToTagIds.Remove(key);
                }

                // 从元数据存储移除
                _metadataLock.EnterWriteLock();
                try
                {
                    _metadataStore.Remove(key);
                }
                finally
                {
                    _metadataLock.ExitWriteLock();
                }

                // 从实体存储移除
                _entitiesLock.EnterWriteLock();
                try
                {
                    _entities.Remove(key);
                }
                finally
                {
                    _entitiesLock.ExitWriteLock();
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

        #endregion

        #region 层级操作

        /// <summary>
        ///     获取或创建分类节点。
        /// </summary>
        private CategoryNode GetOrCreateNode(string fullPath)
        {
            fullPath = CategoryNameNormalizer.Normalize(fullPath);

            if (_categoryNameToId.TryGetValue(fullPath, out int existingId) &&
                _categoryNodes.TryGetValue(existingId, out CategoryNode existingNode))
                return existingNode;

            string[] parts = fullPath.Split('.');
            CategoryNode node;

            if (parts.Length > 1)
            {
                string parentPath = string.Join(".", parts.Take(parts.Length - 1));
                CategoryNode parent = GetOrCreateNode(parentPath);

                int newCategoryId = _categoryTermMapper.GetOrAssignId(fullPath);
                node = parent.GetOrCreateChild(newCategoryId);

                _categoryNodes[newCategoryId] = node;
                _categoryNameToId[fullPath] = newCategoryId;
                _categoryIdToName[newCategoryId] = fullPath;
            }
            else
            {
                int newCategoryId = _categoryTermMapper.GetOrAssignId(fullPath);
                node = new CategoryNode(newCategoryId);
                _categoryNodes[newCategoryId] = node;
                _categoryNameToId[fullPath] = newCategoryId;
                _categoryIdToName[newCategoryId] = fullPath;
            }

            return node;
        }

        private static Regex ConvertWildcardToRegex(string pattern)
        {
            string regexPattern = "^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$";
            return RegexCache.GetOrCreate(regexPattern);
        }

        #endregion

        #region 标签系统

        /// <summary>
        ///     为实体添加标签。
        /// </summary>
        public OperationResult AddTag(TKey key, string tag)
        {
            _entitiesLock.EnterReadLock();
            try
            {
                if (!_entities.ContainsKey(key))
                    return OperationResult.Failure(ErrorCode.NotFound, $"未找到键为 '{key}' 的实体");
            }
            finally
            {
                _entitiesLock.ExitReadLock();
            }

            if (string.IsNullOrWhiteSpace(tag))
                return OperationResult.Failure(ErrorCode.InvalidCategory, "标签名称不能为空");

            AddTagInternal(key, tag);
            return OperationResult.Success();
        }

        /// <summary>
        ///     内部方法：添加标签（跳过实体存在检查）。
        /// </summary>
        private void AddTagInternal(TKey key, string tag)
        {
            if (string.IsNullOrWhiteSpace(tag)) return;

            int tagId = _tagMapper.GetOrAssignId(tag);

            AcquireTagWriteLock(tagId);
            try
            {
                if (!_tagToEntityKeys.TryGetValue(tagId, out var entityKeys))
                {
                    entityKeys = new HashSet<TKey>(_keyComparer);
                    _tagToEntityKeys[tagId] = entityKeys;
                }
                entityKeys.Add(key);

                if (!_entityToTagIds.TryGetValue(key, out var tagIds))
                {
                    tagIds = new HashSet<int>();
                    _entityToTagIds[key] = tagIds;
                }
                tagIds.Add(tagId);

                // 清除标签缓存
                _tagCache.Remove(tagId);
            }
            finally
            {
                ReleaseTagWriteLock(tagId);
            }
        }

        /// <summary>
        ///     为实体批量添加标签。
        /// </summary>
        public OperationResult AddTags(TKey key, params string[] tags)
        {
            _entitiesLock.EnterReadLock();
            try
            {
                if (!_entities.ContainsKey(key))
                    return OperationResult.Failure(ErrorCode.NotFound, $"未找到键为 '{key}' 的实体");
            }
            finally
            {
                _entitiesLock.ExitReadLock();
            }

            if (tags == null || tags.Length == 0)
                return OperationResult.Success();

            foreach (string tag in tags)
            {
                if (!string.IsNullOrWhiteSpace(tag))
                {
                    AddTagInternal(key, tag);
                }
            }

            return OperationResult.Success();
        }

        /// <summary>
        ///     从实体移除标签。
        /// </summary>
        public OperationResult RemoveTag(TKey key, string tag)
        {
            _entitiesLock.EnterReadLock();
            try
            {
                if (!_entities.ContainsKey(key))
                    return OperationResult.Failure(ErrorCode.NotFound, $"未找到键为 '{key}' 的实体");
            }
            finally
            {
                _entitiesLock.ExitReadLock();
            }

            if (string.IsNullOrWhiteSpace(tag))
                return OperationResult.Failure(ErrorCode.InvalidCategory, "标签名称不能为空");

            if (!_tagMapper.TryGetId(tag, out int tagId))
                return OperationResult.Failure(ErrorCode.NotFound, $"标签 '{tag}' 不存在");

            AcquireTagWriteLock(tagId);
            try
            {
                if (!_tagToEntityKeys.TryGetValue(tagId, out var entityKeys) ||
                    !entityKeys.Remove(key))
                    return OperationResult.Failure(ErrorCode.NotFound, $"实体不包含标签 '{tag}'");

                if (_entityToTagIds.TryGetValue(key, out var tagIds))
                    tagIds.Remove(tagId);

                if (entityKeys.Count == 0)
                    _tagToEntityKeys.Remove(tagId);

                // 清除标签缓存
                _tagCache.Remove(tagId);

                return OperationResult.Success();
            }
            finally
            {
                ReleaseTagWriteLock(tagId);
            }
        }

        /// <summary>
        ///     检查实体是否拥有指定标签。
        /// </summary>
        public bool HasTag(TKey key, string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
                return false;

            if (!_tagMapper.TryGetId(tag, out int tagId))
                return false;

            if (!_entityToTagIds.TryGetValue(key, out var tagIds))
                return false;

            return tagIds.Contains(tagId);
        }

        /// <summary>
        ///     检查实体是否拥有指定标签（基于实体对象）。
        /// </summary>
        public bool HasTag(T entity, string tag)
        {
            if (entity == null) return false;
            TKey key = _keyExtractor(entity);
            return HasTag(key, tag);
        }

        /// <summary>
        ///     获取实体的所有标签。
        /// </summary>
        public IReadOnlyList<string> GetEntityTags(TKey key)
        {
            if (!_entityToTagIds.TryGetValue(key, out var tagIds))
                return Array.Empty<string>();

            var tags = new List<string>(tagIds.Count);
            foreach (int tagId in tagIds)
            {
                if (_tagMapper.TryGetString(tagId, out string tagName))
                {
                    tags.Add(tagName);
                }
            }

            return tags;
        }

        /// <summary>
        ///     获取实体的所有标签（基于实体对象）。
        /// </summary>
        public IReadOnlyList<string> GetTags(T entity)
        {
            if (entity == null) return Array.Empty<string>();
            TKey key = _keyExtractor(entity);
            return GetEntityTags(key);
        }

        /// <summary>
        ///     按标签查询实体。
        /// </summary>
        public IReadOnlyList<T> GetByTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag)) return new List<T>();

            if (!_tagMapper.TryGetId(tag, out int tagId))
                return new List<T>();

            // 检查缓存
            if (_tagCache.TryGetValue(tagId, out var cachedResult))
            {
                RecordCacheQuery(true);
                return cachedResult;
            }

            RecordCacheQuery(false);

            AcquireTagReadLock(tagId);
            try
            {
                if (_tagToEntityKeys.TryGetValue(tagId, out var entityKeys))
                {
                    _entitiesLock.EnterReadLock();
                    try
                    {
                        var result = entityKeys
                            .Select(k => _entities.TryGetValue(k, out T e) ? e : default)
                            .Where(e => e != null)
                            .ToList();

                        _tagCache[tagId] = result;
                        return result;
                    }
                    finally
                    {
                        _entitiesLock.ExitReadLock();
                    }
                }

                return new List<T>();
            }
            finally
            {
                ReleaseTagReadLock(tagId);
            }
        }

        /// <summary>
        ///     按多个标签查询实体。
        /// </summary>
        public IReadOnlyList<T> GetByTags(string[] tags, bool matchAll = true)
        {
            if (tags == null || tags.Length == 0) return new List<T>();

            HashSet<TKey> resultKeys = null;

            foreach (string tag in tags)
            {
                if (string.IsNullOrWhiteSpace(tag)) continue;

                if (!_tagMapper.TryGetId(tag, out int tagId)) continue;

                if (!_tagToEntityKeys.TryGetValue(tagId, out var entityKeys)) continue;

                if (resultKeys == null)
                {
                    resultKeys = new HashSet<TKey>(entityKeys, _keyComparer);
                }
                else if (matchAll)
                {
                    resultKeys.IntersectWith(entityKeys);
                }
                else
                {
                    resultKeys.UnionWith(entityKeys);
                }
            }

            if (resultKeys == null || resultKeys.Count == 0)
                return new List<T>();

            _entitiesLock.EnterReadLock();
            try
            {
                return resultKeys
                    .Select(k => _entities.TryGetValue(k, out T e) ? e : default)
                    .Where(e => e != null)
                    .ToList();
            }
            finally
            {
                _entitiesLock.ExitReadLock();
            }
        }

        /// <summary>
        ///     根据分类和标签获取实体交集。
        /// </summary>
        public IReadOnlyList<T> GetByCategoryAndTag(string category, string tag, bool includeChildren = true)
        {
            var categoryEntities = GetByCategory(category, includeChildren);
            var tagEntities = GetByTag(tag);

            return categoryEntities.Intersect(tagEntities).ToList();
        }

        #endregion

        #region 元数据

        /// <summary>
        ///     获取实体的元数据。
        /// </summary>
        public CustomDataCollection GetMetadata(TKey key)
        {
            _entitiesLock.EnterReadLock();
            try
            {
                if (!_entities.ContainsKey(key)) return new CustomDataCollection();
            }
            finally
            {
                _entitiesLock.ExitReadLock();
            }

            _metadataLock.EnterReadLock();
            try
            {
                return _metadataStore.TryGetValue(key, out var metadata)
                    ? metadata
                    : new CustomDataCollection();
            }
            finally
            {
                _metadataLock.ExitReadLock();
            }
        }

        /// <summary>
        ///     更新实体的元数据。
        /// </summary>
        public OperationResult UpdateMetadata(TKey key, CustomDataCollection metadata)
        {
            _entitiesLock.EnterReadLock();
            try
            {
                if (!_entities.ContainsKey(key))
                    return OperationResult.Failure(ErrorCode.NotFound, $"未找到键为 '{key}' 的实体");
            }
            finally
            {
                _entitiesLock.ExitReadLock();
            }

            _metadataLock.EnterWriteLock();
            try
            {
                _metadataStore[key] = metadata;
                return OperationResult.Success();
            }
            finally
            {
                _metadataLock.ExitWriteLock();
            }
        }

        #endregion

        #region 锁

        private void AcquireTagReadLock(int tagId)
        {
            if (!_tagLocks.TryGetValue(tagId, out ReaderWriterLockSlim lockObj))
            {
                lockObj = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
                _tagLocks[tagId] = lockObj;
            }

            lockObj.EnterReadLock();
        }

        private void ReleaseTagReadLock(int tagId)
        {
            if (_tagLocks.TryGetValue(tagId, out ReaderWriterLockSlim lockObj) && lockObj.IsReadLockHeld)
                lockObj.ExitReadLock();
        }

        private void AcquireTagWriteLock(int tagId)
        {
            if (!_tagLocks.TryGetValue(tagId, out ReaderWriterLockSlim lockObj))
            {
                lockObj = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
                _tagLocks[tagId] = lockObj;
            }

            lockObj.EnterWriteLock();
        }

        private void ReleaseTagWriteLock(int tagId)
        {
            if (_tagLocks.TryGetValue(tagId, out ReaderWriterLockSlim lockObj) && lockObj.IsWriteLockHeld)
                lockObj.ExitWriteLock();
        }

        #endregion

        #region 序列化

        /// <summary>
        ///     序列化为 JSON 字符串。
        /// </summary>
        public string SerializeToJson()
        {
            return CategoryManagerSerializer.Serialize(this);
        }

        #endregion

        #region 清空与重置

        /// <summary>
        ///     清除所有缓存。
        /// </summary>
        public void ClearCache()
        {
            _tagCache.Clear();
        }

        /// <summary>
        ///     清空所有数据。
        /// </summary>
        public void Clear()
        {
            _treeLock.EnterWriteLock();
            try
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

#if UNITY_EDITOR
                _cachedStatistics = null;
#endif
            }
            finally
            {
                _treeLock.ExitWriteLock();
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
            if (_cachedStatistics != null && (now - _lastStatisticsUpdate) < StatisticsCacheTimeout)
                return _cachedStatistics;

            _cachedStatistics = new Statistics
            {
                TotalEntities = _entities.Count,
                TotalCategories = _categoryNodes.Count,
                TotalTags = _tagMapper.Count,
                CacheSize = _tagCache.Count,
                CacheHitRate = _totalCacheQueries > 0 ? (double)_cacheHits / _totalCacheQueries : 0,
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
                _cacheHits++;
            else
                _cacheMisses++;
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
            _entitiesLock?.Dispose();
            _metadataLock?.Dispose();

            foreach (ReaderWriterLockSlim lockObj in _tagLocks.Values)
            {
                lockObj?.Dispose();
            }
        }

        #endregion

        #region 内部类

        /// <summary>
        ///     泛型实体注册链式构建器。
        /// </summary>
        private class EntityRegistrationGeneric : IEntityRegistration
        {
            private readonly CategoryManager<T, TKey> _manager;
            private readonly TKey _entityKey;
            private readonly T _entity;
            private readonly string _category;
            private readonly List<string> _tags;
            private CustomDataCollection _metadata;
            private readonly OperationResult _validationResult;

            public EntityRegistrationGeneric(
                CategoryManager<T, TKey> manager,
                TKey entityKey,
                T entity,
                string category,
                OperationResult validationResult)
            {
                _manager = manager;
                _entityKey = entityKey;
                _entity = entity;
                _category = category;
                _tags = new List<string>();
                _validationResult = validationResult;
            }

            public IEntityRegistration WithTags(params string[] tags)
            {
                if (tags != null) _tags.AddRange(tags);
                return this;
            }

            public IEntityRegistration WithMetadata(CustomDataCollection metadata)
            {
                _metadata = metadata;
                return this;
            }

            public OperationResult Complete()
            {
                if (!_validationResult.IsSuccess) return _validationResult;

                string normalizedCategory = CategoryNameNormalizer.Normalize(_category);
                if (!CategoryNameNormalizer.IsValid(normalizedCategory, out string errorMessage))
                    return OperationResult.Failure(ErrorCode.InvalidCategory, errorMessage);

                // 检查键是否已存在
                _manager._entitiesLock.EnterReadLock();
                try
                {
                    if (_manager._entities.ContainsKey(_entityKey))
                        return OperationResult.Failure(ErrorCode.DuplicateId, $"实体键 '{_entityKey}' 已存在");
                }
                finally
                {
                    _manager._entitiesLock.ExitReadLock();
                }

                try
                {
                    // 存储实体
                    _manager._entitiesLock.EnterWriteLock();
                    try
                    {
                        _manager._entities[_entityKey] = _entity;
                    }
                    finally
                    {
                        _manager._entitiesLock.ExitWriteLock();
                    }

                    // 创建分类节点并关联
                    _manager._treeLock.EnterWriteLock();
                    try
                    {
                        CategoryNode node = _manager.GetOrCreateNode(normalizedCategory);
                        _manager._entityKeyToNode[_entityKey] = node;
                    }
                    finally
                    {
                        _manager._treeLock.ExitWriteLock();
                    }

                    // 添加标签
                    foreach (string tag in _tags)
                    {
                        if (!string.IsNullOrWhiteSpace(tag))
                        {
                            _manager.AddTagInternal(_entityKey, tag);
                        }
                    }

                    // 存储元数据
                    if (_metadata != null)
                    {
                        _manager._metadataLock.EnterWriteLock();
                        try
                        {
                            _manager._metadataStore[_entityKey] = _metadata;
                        }
                        finally
                        {
                            _manager._metadataLock.ExitWriteLock();
                        }
                    }

#if UNITY_EDITOR
                    _manager._cachedStatistics = null;
#endif

                    return OperationResult.Success();
                }
                catch (Exception ex)
                {
                    return OperationResult.Failure(ErrorCode.ConcurrencyConflict, ex.Message);
                }
            }
        }

        #endregion
    }
}
