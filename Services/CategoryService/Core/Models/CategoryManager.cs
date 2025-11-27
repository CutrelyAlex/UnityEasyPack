using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using EasyPack.CustomData;

namespace EasyPack.Category
{
    /// <summary>
    ///     通用分类管理系统，支持层级分类、标签、元数据 查询。
    /// </summary>
    [Obsolete("请使用 CategoryManager<T, TKey> 替代，它支持任意键类型。CategoryManager<T> 将在未来版本中移除。", false)]
    public class CategoryManager<T> : ICategoryManager<T>
    {
        #region 属性实例基本属性

        /// <summary>
        ///     获取实体类型。
        /// </summary>
        public Type EntityType => typeof(T);

        #endregion

        #region 字段

        // 基础字段
        private readonly Func<T, string> _idExtractor;
        private readonly Func<T, int> _idExtractorInt;
        private readonly bool _useIntId;

        // 实体存储 (字符串ID)
        private Dictionary<string, T> _entities; // 实体ID->实体对象

        // 实体存储 (整数ID) - 
        private Dictionary<int, T> _entitiesInt; // 整数实体ID->实体对象

        // 分类树
        private Dictionary<int, CategoryNode> _categoryNodes;
        private Dictionary<string, int> _categoryNameToId; // 分类名称 → 整数ID
        private Dictionary<int, string> _categoryIdToName; // 整数ID → 分类名称

        // 标签系统 (字符串ID)
        private Dictionary<int, HashSet<string>> _tagToEntityIds; // tagId → entityIds (string)
        private Dictionary<string, HashSet<int>> _entityToTagIds; // entityId (string) → tagIds
        private Dictionary<int, ReaderWriterLockSlim> _tagLocks;
        private Dictionary<int, List<T>> _tagCache; // 整数键缓存

        // 标签系统 (整数ID) - 
        private Dictionary<int, HashSet<int>> _tagToEntityIdsInt; // tagId → entityIds (int)
        private Dictionary<int, HashSet<int>> _entityToTagIdsInt; // entityId (int) → tagIds

        // 反向索引缓存 (字符串ID)
        private Dictionary<string, CategoryNode> _entityIdToNode; // entityId (string) → 所属分类节点

        // 反向索引缓存 (整数ID) - 
        private Dictionary<int, CategoryNode> _entityIdToNodeInt; // entityId (int) → 所属分类节点

        // 映射层
        private IntegerMapper _tagMapper;
        private IntegerMapper _categoryTermMapper;

        // 元数据存储 (字符串ID)
        private Dictionary<string, CustomDataCollection> _metadataStore;

        // 元数据存储 (整数ID) - 
        private Dictionary<int, CustomDataCollection> _metadataStoreInt;

        // 锁
        private ReaderWriterLockSlim _treeLock;
        private ReaderWriterLockSlim _entitiesLock; // 保护 _entities 和 _entitiesInt 字典
        private ReaderWriterLockSlim _metadataLock; // 保护 _metadataStore 和 _metadataStoreInt 字典

        #endregion

        #region 构造

        /// <summary>
        ///     使用字符串 ID 提取器初始化 CategoryManager。
        /// </summary>
        /// <param name="idExtractor">从实体提取字符串 ID 的函数。</param>
        public CategoryManager(Func<T, string> idExtractor)
        {
            _idExtractor = idExtractor ?? throw new ArgumentNullException(nameof(idExtractor));
            _idExtractorInt = null;
            _useIntId = false;

            InitializeCollections();
        }

        /// <summary>
        ///     使用整数 ID 提取器初始化 CategoryManager。
        ///     这是更高效的方式，避免了字符串转换开销。
        /// </summary>
        /// <param name="idExtractorInt">从实体提取整数 ID 的函数。</param>
        public CategoryManager(Func<T, int> idExtractorInt)
        {
            _idExtractorInt = idExtractorInt ?? throw new ArgumentNullException(nameof(idExtractorInt));
            _idExtractor = null;
            _useIntId = true;

            InitializeCollections();
        }

        /// <summary>
        ///     初始化所有内部集合。
        /// </summary>
        private void InitializeCollections()
        {
            // 字符串ID存储
            _entities = new();

            // 整数ID存储
            _entitiesInt = new();

            _categoryNodes = new();
            _categoryNameToId = new();
            _categoryIdToName = new();

            // 字符串ID标签系统
            _tagToEntityIds = new();
            _entityToTagIds = new();
            _tagLocks = new();
            _tagCache = new();

            // 整数ID标签系统
            _tagToEntityIdsInt = new();
            _entityToTagIdsInt = new();

            // 字符串ID反向索引
            _entityIdToNode = new(StringComparer.Ordinal);

            // 整数ID反向索引
            _entityIdToNodeInt = new();

            _tagMapper = new();
            _categoryTermMapper = new();

            // 字符串ID元数据
            _metadataStore = new();

            // 整数ID元数据
            _metadataStoreInt = new();

            _treeLock = new(LockRecursionPolicy.NoRecursion);
            _entitiesLock = new(LockRecursionPolicy.NoRecursion);
            _metadataLock = new(LockRecursionPolicy.NoRecursion);
        }

        /// <summary>
        ///     获取是否使用整数 ID 模式。
        /// </summary>
        public bool UseIntId => _useIntId;

        #endregion

        #region 实体注册

        /// <summary>
        ///     注册实体到指定分类（同步操作）。
        /// </summary>
        /// <param name="entity">要注册的实体。</param>
        /// <param name="category">目标分类名称。</param>
        /// <returns>操作结果。</returns>
        public OperationResult RegisterEntityComplete(T entity, string category) =>
            RegisterEntity(entity, category).Complete();

        /// <summary>
        ///     开始注册实体，支持链式调用添加标签和元数据。
        ///     如果使用整数 ID 模式构造，将自动使用整数 ID 注册。
        /// </summary>
        /// <param name="entity">要注册的实体。</param>
        /// <param name="category">目标分类名称。</param>
        /// <returns>链式注册对象。</returns>
        public IEntityRegistration RegisterEntity(T entity, string category)
        {
            if (_useIntId)
            {
                // 使用整数 ID 模式
                int intId = _idExtractorInt(entity);
                return new EntityRegistrationInt(this, intId, entity, category, OperationResult.Success());
            }
            else
            {
                // 使用字符串 ID 模式
                string id = _idExtractor(entity);
                return RegisterEntityInternal(entity, id, category);
            }
        }

        /// <summary>
        ///     注册实体，自动验证分类名称格式。
        /// </summary>
        /// <param name="entity">要注册的实体。</param>
        /// <param name="category">目标分类名称（将自动规范化）。</param>
        /// <returns>链式注册对象；若分类名称无效，返回失败结果。</returns>
        public IEntityRegistration RegisterEntitySafe(T entity, string category)
        {
            category = CategoryNameNormalizer.Normalize(category);
            if (!CategoryNameNormalizer.IsValid(category, out string errorMessage))
            {
                if (_useIntId)
                {
                    int intId = _idExtractorInt(entity);
                    return new EntityRegistrationInt(this, intId, entity, category,
                        OperationResult.Failure(ErrorCode.InvalidCategory, errorMessage));
                }
                else
                {
                    string id = _idExtractor(entity);
                    return new EntityRegistration(this, id, entity, category,
                        OperationResult.Failure(ErrorCode.InvalidCategory, errorMessage));
                }
            }

            return RegisterEntity(entity, category);
        }

        /// <summary>
        ///     注册实体，指定ID和分类名称，自动验证分类格式。
        /// </summary>
        /// <param name="entity">要注册的实体。</param>
        /// <param name="id">实体ID。</param>
        /// <param name="category">目标分类名称（将自动规范化）。</param>
        /// <returns>链式注册对象；若分类名称无效，返回失败结果。</returns>
        public IEntityRegistration RegisterEntitySafeWithId(T entity, string id, string category)
        {
            category = CategoryNameNormalizer.Normalize(category);
            if (!CategoryNameNormalizer.IsValid(category, out string errorMessage))
                return new EntityRegistration(this, id, entity, category,
                    OperationResult.Failure(ErrorCode.InvalidCategory, errorMessage));

            return RegisterEntityInternal(entity, id, category);
        }

        /// <summary>
        ///     内部注册逻辑，验证实体ID唯一性。
        /// </summary>
        /// <param name="entity">要注册的实体。</param>
        /// <param name="id">实体ID。</param>
        /// <param name="category">目标分类。</param>
        /// <returns>链式注册对象；若ID已存在，返回失败结果。</returns>
        private IEntityRegistration RegisterEntityInternal(T entity, string id, string category)
        {
            if (string.IsNullOrEmpty(id))
                return new EntityRegistration(this, id, entity, category,
                    OperationResult.Failure(ErrorCode.InvalidCategory, $"实体 ID 不能为空"));

            _entitiesLock.EnterReadLock();
            try
            {
                if (_entities.ContainsKey(id))
                    return new EntityRegistration(this, id, entity, category,
                        OperationResult.Failure(ErrorCode.DuplicateId, $"实体 ID '{id}' 已存在"));
            }
            finally
            {
                _entitiesLock.ExitReadLock();
            }

            return new EntityRegistration(this, id, entity, category, OperationResult.Success());
        }

        /// <summary>
        ///     仅注册分类关联
        ///     适用于Entity无法序列化的场景，此时只保留ID、分类、标签和元数据的关联。
        /// </summary>
        /// <param name="id">实体ID。</param>
        /// <param name="category">分类名称。</param>
        /// <param name="tags">标签列表（可选）。</param>
        /// <param name="metadata">元数据（可选）。</param>
        /// <returns>OperationResult</returns>
        public OperationResult RegisterCategoryAssociationOnly(
            string id,
            string category,
            List<string> tags = null,
            CustomDataCollection metadata = null)
        {
            if (string.IsNullOrEmpty(id))
                return OperationResult.Failure(ErrorCode.InvalidCategory, "实体 ID 不能为空");

            category = CategoryNameNormalizer.Normalize(category);
            if (!CategoryNameNormalizer.IsValid(category, out string errorMessage))
                return OperationResult.Failure(ErrorCode.InvalidCategory, errorMessage);

            _treeLock.EnterWriteLock();
            try
            {
                // 获取或创建分类节点
                CategoryNode node = GetOrCreateNode(category);
                node.AddEntity(id);
                _entityIdToNode[id] = node; // 缓存 entityId -> node 映射

                // 添加标签
                if (tags is { Count: > 0 })
                    foreach (string tag in tags)
                    {
                        if (string.IsNullOrWhiteSpace(tag)) continue;

                        int tagId = _tagMapper.GetOrAssignId(tag);
                        if (!_tagToEntityIds.TryGetValue(tagId, out var entityIds))
                        {
                            entityIds = new();
                            _tagToEntityIds[tagId] = entityIds;
                        }

                        entityIds.Add(id);

                        if (!_entityToTagIds.TryGetValue(id, out var tagIds))
                        {
                            tagIds = new();
                            _entityToTagIds[id] = tagIds;
                        }

                        tagIds.Add(tagId);
                    }

                // 添加元数据
                if (metadata != null)
                {
                    _metadataLock.EnterWriteLock();
                    try
                    {
                        _metadataStore[id] = metadata;
                    }
                    finally
                    {
                        _metadataLock.ExitWriteLock();
                    }
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
        ///     批量注册多个实体到同一分类。
        /// </summary>
        /// <param name="entities">实体列表。</param>
        /// <param name="category">目标分类名称。</param>
        /// <returns>批量操作结果，含成功数、失败数和详细信息。</returns>
        public BatchOperationResult RegisterBatch(List<T> entities, string category)
        {
            var results = new List<(string EntityId, bool Success, ErrorCode ErrorCode, string ErrorMessage)>();
            int successCount = 0;

            foreach (T entity in entities)
            {
                string id = _idExtractor(entity);
                IEntityRegistration registration = RegisterEntitySafe(entity, category);
                OperationResult result = registration.Complete();

                results.Add((id, result.IsSuccess, result.ErrorCode, result.ErrorMessage));
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

        /// <summary>
        ///     使用整数ID注册实体到指定分类（整数ID）。
        /// </summary>
        /// <param name="entityId">实体整数ID。</param>
        /// <param name="entity">要注册的实体。</param>
        /// <param name="category">目标分类名称。</param>
        /// <returns>操作结果。</returns>
        public OperationResult RegisterEntityInt(int entityId, T entity, string category)
        {
            category = CategoryNameNormalizer.Normalize(category);
            if (!CategoryNameNormalizer.IsValid(category, out string errorMessage))
                return OperationResult.Failure(ErrorCode.InvalidCategory, errorMessage);

            _entitiesLock.EnterReadLock();
            try
            {
                if (_entitiesInt.ContainsKey(entityId))
                    return OperationResult.Failure(ErrorCode.DuplicateId, $"整数实体 ID '{entityId}' 已存在");
            }
            finally
            {
                _entitiesLock.ExitReadLock();
            }

            _treeLock.EnterWriteLock();
            try
            {
                // 获取或创建分类节点
                CategoryNode node = GetOrCreateNode(category);

                // 注册到整数ID存储
                _entitiesLock.EnterWriteLock();
                try
                {
                    _entitiesInt[entityId] = entity;
                }
                finally
                {
                    _entitiesLock.ExitWriteLock();
                }

                // 添加到分类节点（使用整数ID的字符串表示用于节点内部）
                // 但整数ID缓存使用的映射
                _entityIdToNodeInt[entityId] = node;

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
        ///     使用整数ID注册实体并添加标签（整数ID）。
        /// </summary>
        /// <param name="entityId">实体整数ID。</param>
        /// <param name="entity">要注册的实体。</param>
        /// <param name="category">目标分类名称。</param>
        /// <param name="tags">标签列表。</param>
        /// <returns>操作结果。</returns>
        public OperationResult RegisterEntityIntWithTags(int entityId, T entity, string category, params string[] tags)
        {
            OperationResult result = RegisterEntityInt(entityId, entity, category);
            if (!result.IsSuccess) return result;

            if (tags != null && tags.Length > 0)
            {
                foreach (string tag in tags)
                {
                    if (!string.IsNullOrWhiteSpace(tag))
                    {
                        AddTagInt(entityId, tag);
                    }
                }
            }

            return OperationResult.Success();
        }

        /// <summary>
        ///     使用整数ID注册实体并添加元数据（整数ID）。
        /// </summary>
        /// <param name="entityId">实体整数ID。</param>
        /// <param name="entity">要注册的实体。</param>
        /// <param name="category">目标分类名称。</param>
        /// <param name="metadata">元数据。</param>
        /// <returns>操作结果。</returns>
        public OperationResult RegisterEntityIntWithMetadata(int entityId, T entity, string category, CustomDataCollection metadata)
        {
            OperationResult result = RegisterEntityInt(entityId, entity, category);
            if (!result.IsSuccess) return result;

            if (metadata != null)
            {
                _metadataLock.EnterWriteLock();
                try
                {
                    _metadataStoreInt[entityId] = metadata;
                }
                finally
                {
                    _metadataLock.ExitWriteLock();
                }
            }

            return OperationResult.Success();
        }

        #endregion

        #region 实体查询

        /// <summary>
        ///     按分类查询实体，支持通配符匹配和子分类包含。
        /// </summary>
        /// <param name="pattern">分类名称或通配符模式（如 "Equipment.*"）。</param>
        /// <param name="includeChildren">是否包含子分类中的实体。</param>
        /// <returns>匹配的实体列表。</returns>
        public IReadOnlyList<T> GetByCategory(string pattern, bool includeChildren = false)
        {
            pattern = CategoryNameNormalizer.Normalize(pattern);

            var entityIds = new HashSet<string>();

            _treeLock.EnterReadLock();
            try
            {
                if (pattern.Contains("*"))
                {
                    // 通配符查询编译正则表达式，遍历所有节点
                    Regex regex = ConvertWildcardToRegex(pattern);

                    foreach ((int key, CategoryNode node) in _categoryNodes)
                    {
                        if (!_categoryIdToName.TryGetValue(key, out string nodePath)) continue;

                        if (!regex.IsMatch(nodePath)) continue;

                        if (includeChildren)
                            foreach (string id in node.GetSubtreeEntityIds())
                            {
                                entityIds.Add(id);
                            }
                        else
                            foreach (string id in node.EntityIds)
                            {
                                entityIds.Add(id);
                            }
                    }
                }
                else
                {
                    if (_categoryTermMapper.TryGetId(pattern, out int nodeId) &&
                        _categoryNodes.TryGetValue(nodeId, out CategoryNode node))
                    {
                        if (includeChildren)
                            foreach (string id in node.GetSubtreeEntityIds())
                            {
                                entityIds.Add(id);
                            }
                        else
                            foreach (string id in node.EntityIds)
                            {
                                entityIds.Add(id);
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
                var result = entityIds.Select(id => _entities[id]).ToList();
                return result;
            }
            finally
            {
                _entitiesLock.ExitReadLock();
            }
        }

        /// <summary>
        ///     按 ID 查询实体。
        /// </summary>
        /// <param name="id">实体 ID。</param>
        /// <returns>包含实体的操作结果；若没有找到，返回失败。</returns>
        public OperationResult<T> GetById(string id)
        {
            _entitiesLock.EnterReadLock();
            try
            {
                return _entities.TryGetValue(id, out T entity)
                    ? OperationResult<T>.Success(entity)
                    : OperationResult<T>.Failure(ErrorCode.NotFound, $"未找到 ID 为 '{id}' 的实体");
            }
            finally
            {
                _entitiesLock.ExitReadLock();
            }
        }

        /// <summary>
        ///     按整数 ID 查询实体（使用整数ID）。
        /// </summary>
        /// <param name="id">实体整数 ID。</param>
        /// <returns>包含实体的操作结果；若没有找到，返回失败。</returns>
        public OperationResult<T> GetById(int id)
        {
            _entitiesLock.EnterReadLock();
            try
            {
                return _entitiesInt.TryGetValue(id, out T entity)
                    ? OperationResult<T>.Success(entity)
                    : OperationResult<T>.Failure(ErrorCode.NotFound, $"未找到整数 ID 为 '{id}' 的实体");
            }
            finally
            {
                _entitiesLock.ExitReadLock();
            }
        }

        /// <summary>
        ///     按 ID 查询实体并获取其分类路径。
        /// </summary>
        /// <param name="id">实体 ID。</param>
        /// <returns>
        ///     包含 (T entity, string categoryPath, bool success)
        /// </returns>
        public (T entity, string categoryPath, bool success) GetByIdWithCategory(string id)
        {
            _entitiesLock.EnterReadLock();
            try
            {
                if (!_entities.TryGetValue(id, out T entity)) return (default, string.Empty, false);

                string categoryPath = GetReadableCategoryPath(id);
                return (entity, categoryPath, true);
            }
            finally
            {
                _entitiesLock.ExitReadLock();
            }
        }

        /// <summary>
        ///     获取所有分类节点（包括中间节点）。
        /// </summary>
        /// <returns>分类名称列表。</returns>
        public IReadOnlyList<string> GetCategoriesNodes()
        {
            _treeLock.EnterReadLock();
            try
            {
                return _categoryIdToName.Values.ToList();
            }
            finally
            {
                _treeLock.ExitReadLock();
            }
        }

        /// <summary>
        ///     获取叶子分类（没有子分类的分类）。
        /// </summary>
        /// <returns>叶子分类名称列表。</returns>
        public IReadOnlyList<string> GetLeafCategories()
        {
            _treeLock.EnterReadLock();
            try
            {
                var leafCategories = new List<string>();
                foreach ((int key, CategoryNode node) in _categoryNodes)
                {
                    if (node.Children.Count == 0)
                        leafCategories.Add(_categoryIdToName[key]);
                }

                return leafCategories;
            }
            finally
            {
                _treeLock.ExitReadLock();
            }
        }

        #endregion

        #region 实体删除

        /// <summary>
        ///     从所有分类和标签中删除实体。
        /// </summary>
        /// <param name="id">实体ID。</param>
        /// <returns>操作结果。</returns>
        public OperationResult DeleteEntity(string id)
        {
            _entitiesLock.EnterReadLock();
            try
            {
                if (!_entities.ContainsKey(id))
                    return OperationResult.Failure(ErrorCode.NotFound, $"未找到 ID 为 '{id}' 的实体");
            }
            finally
            {
                _entitiesLock.ExitReadLock();
            }

            _treeLock.EnterWriteLock();
            try
            {
                //var affectedNodePaths = new HashSet<string>();

                // 从分类中移除
                foreach (var kvp in _categoryNodes.ToList())
                {
                    if (!kvp.Value.EntityIds.Contains(id)) continue;

                    kvp.Value.RemoveEntity(id);
                    _entityIdToNode.Remove(id); // 移除缓存
                    //affectedNodePaths.Add(_categoryIdToName[kvp.Key]);

                    CategoryNode parent = kvp.Value.ParentNode;
                    while (parent != null)
                        //affectedNodePaths.Add(_categoryIdToName[parent.TermId]);
                    {
                        parent = parent.ParentNode;
                    }
                }

                // 从实体存储移除
                _entitiesLock.EnterWriteLock();
                try
                {
                    _entities.Remove(id);
                }
                finally
                {
                    _entitiesLock.ExitWriteLock();
                }

                // 从标签中移除
                if (_entityToTagIds.TryGetValue(id, out var tagIds))
                {
                    foreach (int tagId in tagIds.ToList())
                    {
                        AcquireTagWriteLock(tagId);
                        try
                        {
                            if (_tagToEntityIds.TryGetValue(tagId, out var entityIds))
                            {
                                entityIds.Remove(id);
                                if (entityIds.Count == 0) _tagToEntityIds.Remove(tagId);
                            }
                        }
                        finally
                        {
                            ReleaseTagLock(tagId);
                        }
                    }

                    _entityToTagIds.Remove(id);
                }

                // 从元数据移除
                _metadataLock.EnterWriteLock();
                try
                {
                    _metadataStore.Remove(id);
                }
                finally
                {
                    _metadataLock.ExitWriteLock();
                }

                return OperationResult.Success();
            }
            finally
            {
                _treeLock.ExitWriteLock();
            }
        }

        /// <summary>
        ///     从所有分类和标签中删除实体（使用整数ID）。
        /// </summary>
        /// <param name="id">实体整数ID。</param>
        /// <returns>操作结果。</returns>
        public OperationResult DeleteEntity(int id)
        {
            _entitiesLock.EnterReadLock();
            try
            {
                if (!_entitiesInt.ContainsKey(id))
                    return OperationResult.Failure(ErrorCode.NotFound, $"未找到整数 ID 为 '{id}' 的实体");
            }
            finally
            {
                _entitiesLock.ExitReadLock();
            }

            _treeLock.EnterWriteLock();
            try
            {
                // 从分类节点缓存中移除
                _entityIdToNodeInt.Remove(id);

                // 从实体存储移除
                _entitiesLock.EnterWriteLock();
                try
                {
                    _entitiesInt.Remove(id);
                }
                finally
                {
                    _entitiesLock.ExitWriteLock();
                }

                // 从标签中移除
                if (_entityToTagIdsInt.TryGetValue(id, out var tagIds))
                {
                    foreach (int tagId in tagIds.ToList())
                    {
                        AcquireTagWriteLock(tagId);
                        try
                        {
                            if (_tagToEntityIdsInt.TryGetValue(tagId, out var entityIds))
                            {
                                entityIds.Remove(id);
                                if (entityIds.Count == 0) _tagToEntityIdsInt.Remove(tagId);
                            }
                        }
                        finally
                        {
                            ReleaseTagLock(tagId);
                        }
                    }

                    _entityToTagIdsInt.Remove(id);
                }

                // 从元数据移除
                _metadataLock.EnterWriteLock();
                try
                {
                    _metadataStoreInt.Remove(id);
                }
                finally
                {
                    _metadataLock.ExitWriteLock();
                }

                return OperationResult.Success();
            }
            finally
            {
                _treeLock.ExitWriteLock();
            }
        }

        /// <summary>
        ///     删除单个分类及一级实体，不影响子分类。
        /// </summary>
        /// <param name="category">分类名称。</param>
        /// <returns>操作结果。</returns>
        public OperationResult DeleteCategory(string category)
        {
            category = CategoryNameNormalizer.Normalize(category);

            _treeLock.EnterWriteLock();
            try
            {
                if (!_categoryTermMapper.TryGetId(category, out int nodeId) ||
                    !_categoryNodes.TryGetValue(nodeId, out CategoryNode node))
                    return OperationResult.Failure(ErrorCode.NotFound, $"未找到分类 '{category}'");

                //var affectedNodePaths = new HashSet<string> { category };

                // 删除该节点的所有实体
                foreach (string id in node.EntityIds.ToList())
                {
                    _entities.Remove(id);
                    _metadataStore.Remove(id);
                }

                // 从父节点移除此节点
                node.ParentNode?.RemoveChild(node.TermId);

                // 收集祖先节点
                CategoryNode parent = node.ParentNode;
                while (parent != null)
                    //affectedNodePaths.Add(_categoryIdToName[parent.TermId]);
                {
                    parent = parent.ParentNode;
                }

                // 从字典移除
                _categoryNodes.Remove(node.TermId);
                _categoryNameToId.Remove(category);
                _categoryIdToName.Remove(node.TermId);

                return OperationResult.Success();
            }
            finally
            {
                _treeLock.ExitWriteLock();
            }
        }

        /// <summary>
        ///     递归删除分类及所有子分类。
        /// </summary>
        /// <param name="category">分类名称。</param>
        /// <returns>操作结果。</returns>
        public OperationResult DeleteCategoryRecursive(string category)
        {
            category = CategoryNameNormalizer.Normalize(category);

            _treeLock.EnterWriteLock();
            try
            {
                if (!_categoryTermMapper.TryGetId(category, out int nodeId) ||
                    !_categoryNodes.TryGetValue(nodeId, out CategoryNode node))
                    return OperationResult.Failure(ErrorCode.NotFound, $"未找到分类 '{category}'");

                var affectedNodePaths = new HashSet<string>();
                var nodesToDelete = new List<(int Id, CategoryNode Node)>();

                // 收集所有后代节点
                CollectDescendants(node, nodesToDelete, affectedNodePaths);
                nodesToDelete.Add((node.TermId, node));
                affectedNodePaths.Add(category);

                // 删除所有后代节点（从子向上）
                for (int i = nodesToDelete.Count - 1; i >= 0; i--)
                {
                    (int key, CategoryNode n) = nodesToDelete[i];
                    foreach (string id in n.EntityIds.ToList())
                    {
                        _entities.Remove(id);
                        _metadataStore.Remove(id);
                    }

                    _categoryNodes.Remove(key);
                    _categoryIdToName.TryGetValue(key, out string nodeName);

                    if (nodeName != null)
                    {
                        _categoryNameToId.Remove(nodeName);
                        _categoryIdToName.Remove(key);
                    }
                }

                // 从父节点移除根节点
                if (node.ParentNode != null)
                {
                    node.ParentNode.RemoveChild(node.TermId);
                    CategoryNode parent = node.ParentNode;
                    while (parent != null)
                    {
                        affectedNodePaths.Add(_categoryIdToName[parent.TermId]);
                        parent = parent.ParentNode;
                    }
                }

                return OperationResult.Success();
            }
            finally
            {
                _treeLock.ExitWriteLock();
            }
        }

        #endregion

        #region 层级操作和节点管理

        /// <summary>
        ///     获取或创建分类节点（自动创建父节点）。
        /// </summary>
        /// <param name="fullPath">完整分类路径（如 Equipment.Weapon.Sword）。</param>
        /// <returns>分类节点。</returns>
        private CategoryNode GetOrCreateNode(string fullPath)
        {
            fullPath = CategoryNameNormalizer.Normalize(fullPath);

            // 检查缓存
            if (_categoryNameToId.TryGetValue(fullPath, out int existingId) &&
                _categoryNodes.TryGetValue(existingId, out CategoryNode existingNode))
                return existingNode;

            // 处理父节点关系
            string[] parts = fullPath.Split('.');
            CategoryNode node;

            if (parts.Length > 1)
            {
                string parentPath = string.Join(".", parts.Take(parts.Length - 1));
                CategoryNode parent = GetOrCreateNode(parentPath);

                // 创建新节点ID
                int newCategoryId = _categoryTermMapper.GetOrAssignId(fullPath);
                // 从父节点获取或创建子节点（这样parent引用就会被正确设置）
                node = parent.GetOrCreateChild(newCategoryId);

                // 更新缓存
                _categoryNodes[newCategoryId] = node;
                _categoryNameToId[fullPath] = newCategoryId;
                _categoryIdToName[newCategoryId] = fullPath;
            }
            else
            {
                // 根节点
                int newCategoryId = _categoryTermMapper.GetOrAssignId(fullPath);
                node = new(newCategoryId);
                _categoryNodes[newCategoryId] = node;
                _categoryNameToId[fullPath] = newCategoryId;
                _categoryIdToName[newCategoryId] = fullPath;
            }

            return node;
        }

        /// <summary>
        ///     将通配符模式转换为正则表达式。
        /// </summary>
        /// <param name="pattern">包含 * 通配符的模式。</param>
        /// <returns>编译的正则表达式。</returns>
        private static Regex ConvertWildcardToRegex(string pattern)
        {
            string regexPattern = "^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$";
            return RegexCache.GetOrCreate(regexPattern);
        }

        /// <summary>
        ///     递归收集所有后代节点（DeleteCategoryRecursive的辅助方法）。
        /// </summary>
        /// <param name="node">起始节点。</param>
        /// <param name="result">结果节点列表。</param>
        /// <param name="pathSet">已收集的路径集合。</param>
        private void CollectDescendants(CategoryNode node, List<(int Id, CategoryNode Node)> result,
                                        HashSet<string> pathSet)
        {
            foreach (CategoryNode child in node.Children)
            {
                pathSet.Add(_categoryIdToName[child.TermId]);
                result.Add((child.TermId, child));
                CollectDescendants(child, result, pathSet);
            }
        }

        #endregion

        #region 标签系统

        /// <summary>
        ///     为实体添加标签。
        /// </summary>
        /// <param name="entityId">实体ID。</param>
        /// <param name="tag">标签名称。</param>
        /// <returns>操作结果。</returns>
        public OperationResult AddTag(string entityId, string tag)
        {
            _entitiesLock.EnterReadLock();
            try
            {
                if (!_entities.ContainsKey(entityId))
                    return OperationResult.Failure(ErrorCode.NotFound, $"未找到 ID 为 '{entityId}' 的实体");
            }
            finally
            {
                _entitiesLock.ExitReadLock();
            }

            if (string.IsNullOrWhiteSpace(tag)) return OperationResult.Failure(ErrorCode.InvalidCategory, "标签名称不能为空");

            // 字符串→整数映射
            int tagId = _tagMapper.GetOrAssignId(tag);

            AcquireTagWriteLock(tagId);
            try
            {
                // 添加到标签→实体映射
                if (!_tagToEntityIds.TryGetValue(tagId, out var entityIds))
                {
                    entityIds = new();
                    _tagToEntityIds[tagId] = entityIds;
                }

                entityIds.Add(entityId);

                // 添加到实体→标签映射
                if (!_entityToTagIds.TryGetValue(entityId, out var tagIds))
                {
                    tagIds = new();
                    _entityToTagIds[entityId] = tagIds;
                }

                tagIds.Add(tagId);

                // 失效缓存
                _tagCache.Remove(tagId);

                return OperationResult.Success();
            }
            finally
            {
                ReleaseTagLock(tagId);
            }
        }

        /// <summary>
        ///     为实体添加标签（使用整数ID）。
        /// </summary>
        /// <param name="entityId">实体整数ID。</param>
        /// <param name="tag">标签名称。</param>
        /// <returns>操作结果。</returns>
        public OperationResult AddTag(int entityId, string tag)
        {
            _entitiesLock.EnterReadLock();
            try
            {
                if (!_entitiesInt.ContainsKey(entityId))
                    return OperationResult.Failure(ErrorCode.NotFound, $"未找到整数 ID 为 '{entityId}' 的实体");
            }
            finally
            {
                _entitiesLock.ExitReadLock();
            }

            if (string.IsNullOrWhiteSpace(tag)) return OperationResult.Failure(ErrorCode.InvalidCategory, "标签名称不能为空");

            // 字符串→整数映射
            int tagId = _tagMapper.GetOrAssignId(tag);

            AcquireTagWriteLock(tagId);
            try
            {
                // 添加到标签→实体映射（整数ID版本）
                if (!_tagToEntityIdsInt.TryGetValue(tagId, out var entityIds))
                {
                    entityIds = new();
                    _tagToEntityIdsInt[tagId] = entityIds;
                }

                entityIds.Add(entityId);

                // 添加到实体→标签映射（整数ID版本）
                if (!_entityToTagIdsInt.TryGetValue(entityId, out var tagIds))
                {
                    tagIds = new();
                    _entityToTagIdsInt[entityId] = tagIds;
                }

                tagIds.Add(tagId);

                // 失效缓存
                _tagCache.Remove(tagId);

                return OperationResult.Success();
            }
            finally
            {
                ReleaseTagLock(tagId);
            }
        }

        /// <summary>
        ///     内部方法：为整数ID实体添加标签（跳过实体存在检查，用于注册时）。
        /// </summary>
        private void AddTagInt(int entityId, string tag)
        {
            if (string.IsNullOrWhiteSpace(tag)) return;

            int tagId = _tagMapper.GetOrAssignId(tag);

            AcquireTagWriteLock(tagId);
            try
            {
                if (!_tagToEntityIdsInt.TryGetValue(tagId, out var entityIds))
                {
                    entityIds = new();
                    _tagToEntityIdsInt[tagId] = entityIds;
                }
                entityIds.Add(entityId);

                if (!_entityToTagIdsInt.TryGetValue(entityId, out var tagIds))
                {
                    tagIds = new();
                    _entityToTagIdsInt[entityId] = tagIds;
                }
                tagIds.Add(tagId);

                _tagCache.Remove(tagId);
            }
            finally
            {
                ReleaseTagLock(tagId);
            }
        }

        /// <summary>
        ///     为实体批量添加标签。
        /// </summary>
        /// <param name="entityId">实体ID。</param>
        /// <param name="tags">标签名称数组。</param>
        /// <returns>操作结果。</returns>
        public OperationResult AddTags(string entityId, params string[] tags)
        {
            _entitiesLock.EnterReadLock();
            try
            {
                if (!_entities.TryGetValue(entityId, out _))
                    return OperationResult.Failure(ErrorCode.NotFound, $"未找到 ID 为 '{entityId}' 的实体");
            }
            finally
            {
                _entitiesLock.ExitReadLock();
            }

            if (tags == null || tags.Length == 0)
                return OperationResult.Success();

            int successCount = 0;
            foreach (string tag in tags)
            {
                if (!string.IsNullOrWhiteSpace(tag))
                {
                    OperationResult result = AddTag(entityId, tag);
                    if (result.IsSuccess) successCount++;
                }
            }

            return successCount > 0 
                ? OperationResult.Success() 
                : OperationResult.Failure(ErrorCode.InvalidCategory, "未成功添加任何标签");
        }

        /// <summary>
        ///     为整数ID实体批量添加标签（使用独立整数ID缓存体系）。
        /// </summary>
        /// <param name="entityId">实体整数ID。</param>
        /// <param name="tags">标签名称数组。</param>
        /// <returns>操作结果。</returns>
        public OperationResult AddTags(int entityId, params string[] tags)
        {
            _entitiesLock.EnterReadLock();
            try
            {
                if (!_entitiesInt.TryGetValue(entityId, out _))
                    return OperationResult.Failure(ErrorCode.NotFound, $"未找到整数 ID 为 '{entityId}' 的实体");
            }
            finally
            {
                _entitiesLock.ExitReadLock();
            }

            if (tags == null || tags.Length == 0)
                return OperationResult.Success();

            int successCount = 0;
            foreach (string tag in tags)
            {
                if (!string.IsNullOrWhiteSpace(tag))
                {
                    OperationResult result = AddTag(entityId, tag);
                    if (result.IsSuccess) successCount++;
                }
            }

            return successCount > 0 
                ? OperationResult.Success() 
                : OperationResult.Failure(ErrorCode.InvalidCategory, "未成功添加任何标签");
        }

        /// <summary>
        ///     从实体移除标签。
        /// </summary>
        /// <param name="entityId">实体ID。</param>
        /// <param name="tag">标签名称。</param>
        /// <returns>操作结果。</returns>
        public OperationResult RemoveTag(string entityId, string tag)
        {
            _entitiesLock.EnterReadLock();
            try
            {
                if (!_entities.ContainsKey(entityId))
                    return OperationResult.Failure(ErrorCode.NotFound, $"未找到 ID 为 '{entityId}' 的实体");
            }
            finally
            {
                _entitiesLock.ExitReadLock();
            }

            if (string.IsNullOrWhiteSpace(tag)) return OperationResult.Failure(ErrorCode.InvalidCategory, "标签名称不能为空");

            if (!_tagMapper.TryGetId(tag, out int tagId))
                return OperationResult.Failure(ErrorCode.NotFound, $"标签 '{tag}' 不存在");

            AcquireTagWriteLock(tagId);
            try
            {
                if (!_tagToEntityIds.TryGetValue(tagId, out var entityIds) ||
                    !entityIds.Remove(entityId))
                    return OperationResult.Failure(ErrorCode.NotFound,
                        $"实体 '{entityId}' 不具有标签 '{tag}'");

                // 从实体标签集合移除
                if (_entityToTagIds.TryGetValue(entityId, out var tagIds)) tagIds.Remove(tagId);

                // 如果标签无实体，删除标签
                if (entityIds.Count == 0) _tagToEntityIds.Remove(tagId);

                // 失效缓存
                _tagCache.Remove(tagId);

                return OperationResult.Success();
            }
            finally
            {
                ReleaseTagLock(tagId);
            }
        }

        /// <summary>
        ///     从实体移除标签（使用整数ID）。
        /// </summary>
        /// <param name="entityId">实体整数ID。</param>
        /// <param name="tag">标签名称。</param>
        /// <returns>操作结果。</returns>
        public OperationResult RemoveTag(int entityId, string tag)
        {
            _entitiesLock.EnterReadLock();
            try
            {
                if (!_entitiesInt.ContainsKey(entityId))
                    return OperationResult.Failure(ErrorCode.NotFound, $"未找到整数 ID 为 '{entityId}' 的实体");
            }
            finally
            {
                _entitiesLock.ExitReadLock();
            }

            if (string.IsNullOrWhiteSpace(tag)) return OperationResult.Failure(ErrorCode.InvalidCategory, "标签名称不能为空");

            if (!_tagMapper.TryGetId(tag, out int tagId))
                return OperationResult.Failure(ErrorCode.NotFound, $"标签 '{tag}' 不存在");

            AcquireTagWriteLock(tagId);
            try
            {
                if (!_tagToEntityIdsInt.TryGetValue(tagId, out var entityIds) ||
                    !entityIds.Remove(entityId))
                    return OperationResult.Failure(ErrorCode.NotFound,
                        $"整数实体 '{entityId}' 不具有标签 '{tag}'");

                // 从实体标签集合移除
                if (_entityToTagIdsInt.TryGetValue(entityId, out var tagIds)) tagIds.Remove(tagId);

                // 如果标签无实体，删除标签
                if (entityIds.Count == 0) _tagToEntityIdsInt.Remove(tagId);

                // 失效缓存
                _tagCache.Remove(tagId);

                return OperationResult.Success();
            }
            finally
            {
                ReleaseTagLock(tagId);
            }
        }

        /// <summary>
        ///     按标签查询实体。
        /// </summary>
        /// <param name="tag">标签名称。</param>
        /// <returns>匹配的实体列表。</returns>
        public IReadOnlyList<T> GetByTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag)) return new List<T>();

            // 查询时不应创建新 ID，使用 TryGetId
            if (!_tagMapper.TryGetId(tag, out int tagId))
                return new List<T>();

            // 检查缓存快速路径
            if (_tagCache.TryGetValue(tagId, out var cachedResult))
            {
                RecordCacheQuery(true);
                return cachedResult;
            }

            RecordCacheQuery(false);

            // 获取读锁查询
            AcquireTagReadLock(tagId);
            try
            {
                if (_tagToEntityIds.TryGetValue(tagId, out var entityIds))
                {
                    _entitiesLock.EnterReadLock();
                    try
                    {
                        var result = entityIds.Select(id => _entities[id]).ToList();
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
                ReleaseTagLock(tagId);
            }
        }

        /// <summary>
        ///     同时按分类和标签查询实体。
        /// </summary>
        /// <param name="category">分类名称</param>
        /// <param name="tag">标签名称</param>
        /// <param name="includeChildren">是否要包含子类</param>
        /// <returns>同时满足两个条件的实体列表。</returns>
        public IReadOnlyList<T> GetByCategoryAndTag(string category, string tag, bool includeChildren = true)
        {
            var categoryEntities = GetByCategory(category, includeChildren);
            var tagEntities = GetByTag(tag);

            return categoryEntities.Intersect(tagEntities).ToList();
        }

        /// <summary>
        ///     <para>根据正则表达式匹配分类名称并获取实体。</para>
        ///     注意：点分割符(.)需要用\来转义，例如 "Equipment\\.Weapon\\..*" 匹配 Equipment.Weapon 下的所有分类。
        /// </summary>
        /// <param name="pattern">正则表达式模式，其中点分割符需转义为\.</param>
        /// <param name="includeChildren">是否包含子分类中的实体</param>
        /// <returns>匹配分类中的实体列表</returns>
        public IReadOnlyList<T> GetByCategoryRegex(string pattern, bool includeChildren = false)
        {
            if (string.IsNullOrEmpty(pattern)) return new List<T>();

            var entityIds = new HashSet<string>();

            _treeLock.EnterReadLock();
            try
            {
                Regex regex = RegexCache.GetOrCreate("^" + pattern + "$");

                foreach ((int key, CategoryNode node) in _categoryNodes)
                {
                    if (!_categoryIdToName.TryGetValue(key, out string nodePath)) continue;

                    if (!regex.IsMatch(nodePath)) continue;

                    if (includeChildren)
                        foreach (string id in node.GetSubtreeEntityIds())
                        {
                            entityIds.Add(id);
                        }
                    else
                        foreach (string id in node.EntityIds)
                        {
                            entityIds.Add(id);
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
                var result = entityIds.Select(id => _entities[id]).ToList();
                return result;
            }
            finally
            {
                _entitiesLock.ExitReadLock();
            }
        }

        /// <summary>
        ///     按多个标签查询实体，支持AND/OR逻辑。
        /// </summary>
        /// <param name="tags">标签名称数组。</param>
        /// <param name="matchAll">是true为AND不相交），false为OR（並集）。</param>
        /// <returns>查询结果。</returns>
        public IReadOnlyList<T> GetByTags(string[] tags, bool matchAll = true)
        {
            if (tags == null || tags.Length == 0) return new List<T>();

            HashSet<string> resultIds = null;

            foreach (string tag in tags)
            {
                if (string.IsNullOrWhiteSpace(tag)) continue;

                int tagId = _tagMapper.GetOrAssignId(tag);

                AcquireTagReadLock(tagId);
                try
                {
                    if (_tagToEntityIds.TryGetValue(tagId, out var tagIds))
                    {
                        if (resultIds == null)
                        {
                            resultIds = new(tagIds);
                        }
                        else
                        {
                            if (matchAll)
                                resultIds.IntersectWith(tagIds);
                            else
                                resultIds.UnionWith(tagIds);
                        }
                    }
                    else if (matchAll)
                    {
                        // AND模式下，任一的标签不存在则结果为空
                        resultIds = new();
                        break;
                    }
                }
                finally
                {
                    ReleaseTagLock(tagId);
                }
            }

            _entitiesLock.EnterReadLock();
            try
            {
                var result = resultIds != null
                    ? resultIds.Select(id => _entities[id]).ToList()
                    : new();

                return result;
            }
            finally
            {
                _entitiesLock.ExitReadLock();
            }
        }

        /// <summary>
        ///     检查实体是否拥有指定标签。
        /// </summary>
        /// <param name="entityId">实体ID。</param>
        /// <param name="tag">标签名称。</param>
        /// <returns>如果实体拥有该标签返回 true。</returns>
        public bool HasTag(string entityId, string tag)
        {
            if (string.IsNullOrEmpty(entityId) || string.IsNullOrWhiteSpace(tag))
                return false;

            if (!_tagMapper.TryGetId(tag, out int tagId))
                return false;

            if (!_entityToTagIds.TryGetValue(entityId, out var tagIds))
                return false;

            return tagIds.Contains(tagId);
        }

        /// <summary>
        ///     检查实体是否拥有指定标签（使用整数ID）。
        /// </summary>
        /// <param name="entityId">实体整数ID。</param>
        /// <param name="tag">标签名称。</param>
        /// <returns>如果实体拥有该标签返回 true。</returns>
        public bool HasTag(int entityId, string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
                return false;

            if (!_tagMapper.TryGetId(tag, out int tagId))
                return false;

            if (!_entityToTagIdsInt.TryGetValue(entityId, out var tagIds))
                return false;

            return tagIds.Contains(tagId);
        }

        /// <summary>
        ///     获取实体的所有标签。
        /// </summary>
        /// <param name="entityId">实体ID。</param>
        /// <returns>标签列表；若实体不存在则返回空列表。</returns>
        public IReadOnlyList<string> GetEntityTags(string entityId)
        {
            if (string.IsNullOrEmpty(entityId))
                return Array.Empty<string>();

            if (!_entityToTagIds.TryGetValue(entityId, out var tagIds))
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
        ///     获取实体的所有标签（使用整数ID）。
        /// </summary>
        /// <param name="entityId">实体整数ID。</param>
        /// <returns>标签列表；若实体不存在则返回空列表。</returns>
        public IReadOnlyList<string> GetEntityTags(int entityId)
        {
            if (!_entityToTagIdsInt.TryGetValue(entityId, out var tagIds))
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
        ///     检查实体是否拥有指定标签（基于实体对象）。
        /// </summary>
        /// <param name="entity">实体对象。</param>
        /// <param name="tag">标签名称。</param>
        /// <returns>如果实体拥有该标签返回 true。</returns>
        public bool HasTag(T entity, string tag)
        {
            if (entity == null) return false;
            string entityId = _idExtractor(entity);
            return HasTag(entityId, tag);
        }

        /// <summary>
        ///     获取实体的所有标签（基于实体对象）。
        /// </summary>
        /// <param name="entity">实体对象。</param>
        /// <returns>标签列表；若实体不存在则返回空列表。</returns>
        public IReadOnlyList<string> GetTags(T entity)
        {
            if (entity == null) return Array.Empty<string>();
            string entityId = _idExtractor(entity);
            return GetEntityTags(entityId);
        }

        /// <summary>
        ///     检查实体是否在指定分类中（基于实体对象）。
        /// </summary>
        /// <param name="entity">实体对象。</param>
        /// <param name="category">分类名称。</param>
        /// <param name="includeChildren">是否检查子分类。</param>
        /// <returns>如果实体在该分类中返回 true。</returns>
        public bool IsInCategory(T entity, string category, bool includeChildren = false)
        {
            if (entity == null) return false;
            string entityId = _idExtractor(entity);
            return IsInCategory(entityId, category, includeChildren);
        }

        /// <summary>
        ///     检查实体是否在指定分类中。
        /// </summary>
        /// <param name="entityId">实体ID。</param>
        /// <param name="category">分类名称。</param>
        /// <param name="includeChildren">是否检查子分类（即实体是否在该分类或其子分类中）。</param>
        /// <returns>如果实体在该分类中返回 true。</returns>
        public bool IsInCategory(string entityId, string category, bool includeChildren = false)
        {
            if (string.IsNullOrEmpty(entityId) || string.IsNullOrEmpty(category))
                return false;

            // 获取目标分类的整数ID
            if (!_categoryTermMapper.TryGetId(category, out int targetCategoryId))
                return false;

            _treeLock.EnterReadLock();
            try
            {
                // 获取实体所在的节点
                if (!_entityIdToNode.TryGetValue(entityId, out var entityNode))
                    return false;

                if (!includeChildren)
                {
                    // 实体节点的ID必须等于目标分类ID
                    return entityNode.TermId == targetCategoryId;
                }
                else
                {
                    // 检查实体节点是否是目标分类或其子分类
                    // 通过向上遍历父节点链来检查
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
        ///     检查实体是否在指定分类中（使用整数ID）。
        /// </summary>
        /// <param name="entityId">实体整数ID。</param>
        /// <param name="category">分类名称。</param>
        /// <param name="includeChildren">是否检查子分类（即实体是否在该分类或其子分类中）。</param>
        /// <returns>如果实体在该分类中返回 true。</returns>
        public bool IsInCategory(int entityId, string category, bool includeChildren = false)
        {
            if (string.IsNullOrEmpty(category))
                return false;

            // 获取目标分类的整数ID
            if (!_categoryTermMapper.TryGetId(category, out int targetCategoryId))
                return false;

            _treeLock.EnterReadLock();
            try
            {
                // 获取实体所在的节点（使用整数ID缓存）
                if (!_entityIdToNodeInt.TryGetValue(entityId, out var entityNode))
                    return false;

                if (!includeChildren)
                {
                    // 实体节点的ID必须等于目标分类ID
                    return entityNode.TermId == targetCategoryId;
                }
                else
                {
                    // 检查实体节点是否是目标分类或其子分类
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

        #endregion

        #region 元数据数据管理

        /// <summary>
        ///     获取实体的元数据。
        /// </summary>
        /// <param name="id">实体ID。</param>
        /// <returns>元数据集合，不存在则返回空集。</returns>
        public CustomDataCollection GetMetadata(string id)
        {
            _entitiesLock.EnterReadLock();
            try
            {
                if (!_entities.ContainsKey(id)) return new();
            }
            finally
            {
                _entitiesLock.ExitReadLock();
            }

            _metadataLock.EnterReadLock();
            try
            {
                return _metadataStore.TryGetValue(id, out CustomDataCollection metadata)
                    ? metadata
                    : new();
            }
            finally
            {
                _metadataLock.ExitReadLock();
            }
        }

        /// <summary>
        ///     获取实体的元数据（使用整数ID）。
        /// </summary>
        /// <param name="id">实体整数ID。</param>
        /// <returns>元数据集合，不存在则返回空集。</returns>
        public CustomDataCollection GetMetadata(int id)
        {
            _entitiesLock.EnterReadLock();
            try
            {
                if (!_entitiesInt.ContainsKey(id)) return new();
            }
            finally
            {
                _entitiesLock.ExitReadLock();
            }

            _metadataLock.EnterReadLock();
            try
            {
                return _metadataStoreInt.TryGetValue(id, out CustomDataCollection metadata)
                    ? metadata
                    : new();
            }
            finally
            {
                _metadataLock.ExitReadLock();
            }
        }

        /// <summary>
        ///     获取实体的元数据。
        /// </summary>
        /// <param name="id">实体ID。</param>
        /// <returns>包含元数据的操作结果。</returns>
        public OperationResult<CustomDataCollection> GetMetadataResult(string id)
        {
            _entitiesLock.EnterReadLock();
            try
            {
                if (!_entities.ContainsKey(id))
                    return OperationResult<CustomDataCollection>.Failure(
                        ErrorCode.NotFound, $"未找到 ID 为 '{id}' 的实体");
            }
            finally
            {
                _entitiesLock.ExitReadLock();
            }

            _metadataLock.EnterReadLock();
            try
            {
                return OperationResult<CustomDataCollection>.Success(
                    _metadataStore.TryGetValue(id, out CustomDataCollection metadata)
                        ? metadata
                        : new());
            }
            finally
            {
                _metadataLock.ExitReadLock();
            }
        }

        /// <summary>
        ///     更新实体的元数据。
        /// </summary>
        /// <param name="id">实体ID。</param>
        /// <param name="metadata">新的元数据集合。</param>
        /// <returns>操作结果。</returns>
        public OperationResult UpdateMetadata(string id, CustomDataCollection metadata)
        {
            _entitiesLock.EnterReadLock();
            try
            {
                if (!_entities.ContainsKey(id))
                    return OperationResult.Failure(ErrorCode.NotFound, $"未找到 ID 为 '{id}' 的实体");
            }
            finally
            {
                _entitiesLock.ExitReadLock();
            }

            _metadataLock.EnterWriteLock();
            try
            {
                _metadataStore[id] = metadata;
                return OperationResult.Success();
            }
            finally
            {
                _metadataLock.ExitWriteLock();
            }
        }

        /// <summary>
        ///     更新实体的元数据（使用整数ID）。
        /// </summary>
        /// <param name="id">实体整数ID。</param>
        /// <param name="metadata">新的元数据集合。</param>
        /// <returns>操作结果。</returns>
        public OperationResult UpdateMetadata(int id, CustomDataCollection metadata)
        {
            _entitiesLock.EnterReadLock();
            try
            {
                if (!_entitiesInt.ContainsKey(id))
                    return OperationResult.Failure(ErrorCode.NotFound, $"未找到整数 ID 为 '{id}' 的实体");
            }
            finally
            {
                _entitiesLock.ExitReadLock();
            }

            _metadataLock.EnterWriteLock();
            try
            {
                _metadataStoreInt[id] = metadata;
                return OperationResult.Success();
            }
            finally
            {
                _metadataLock.ExitWriteLock();
            }
        }

        #endregion

        #region 锁

        /// <summary>
        ///     为标签获取读锁。
        /// </summary>
        /// <param name="tagId">标签整数ID。</param>
        private void AcquireTagReadLock(int tagId)
        {
            if (!_tagLocks.TryGetValue(tagId, out ReaderWriterLockSlim lockObj))
            {
                lockObj = new(LockRecursionPolicy.NoRecursion);
                _tagLocks[tagId] = lockObj;
            }

            lockObj.EnterReadLock();
        }

        /// <summary>
        ///     为标签获取写锁。
        /// </summary>
        /// <param name="tagId">标签整数ID。</param>
        private void AcquireTagWriteLock(int tagId)
        {
            if (!_tagLocks.TryGetValue(tagId, out ReaderWriterLockSlim lockObj))
            {
                lockObj = new(LockRecursionPolicy.NoRecursion);
                _tagLocks[tagId] = lockObj;
            }

            lockObj.EnterWriteLock();
        }

        /// <summary>
        ///     释放标签的锁。
        /// </summary>
        /// <param name="tagId">标签整数ID。</param>
        private void ReleaseTagLock(int tagId)
        {
            if (!_tagLocks.TryGetValue(tagId, out ReaderWriterLockSlim lockObj)) return;

            if (lockObj.IsReadLockHeld) lockObj.ExitReadLock();

            if (lockObj.IsWriteLockHeld) lockObj.ExitWriteLock();
        }

        #endregion

        #region 跨分类移动

        /// <summary>
        ///     将实体移动到新分类，自动验证分类名称。
        /// </summary>
        /// <param name="entityId">实体ID。</param>
        /// <param name="newCategory">新的分类名称。</param>
        /// <returns>操作结果。</returns>
        public OperationResult MoveEntityToCategorySafe(string entityId, string newCategory)
        {
            newCategory = CategoryNameNormalizer.Normalize(newCategory);
            return !CategoryNameNormalizer.IsValid(newCategory, out string errorMessage)
                ? OperationResult.Failure(ErrorCode.InvalidCategory, errorMessage)
                : MoveEntityToCategory(entityId, newCategory);
        }

        /// <summary>
        ///     将实体从当前分类移动到新的分类。
        /// </summary>
        /// <param name="entityId">实体ID。</param>
        /// <param name="newCategory">新的分类名称。</param>
        /// <returns>操作结果。</returns>
        public OperationResult MoveEntityToCategory(string entityId, string newCategory)
        {
            if (!_entities.ContainsKey(entityId))
                return OperationResult.Failure(ErrorCode.NotFound, $"未找到 ID 为 '{entityId}' 的实体");

            _treeLock.EnterWriteLock();
            try
            {
                //var affectedNodePaths = new HashSet<string>();

                // 从旧分类移除
                foreach (var kvp in _categoryNodes.ToList())
                {
                    if (!kvp.Value.RemoveEntity(entityId)) continue;

                    _entityIdToNode.Remove(entityId); // 清除旧缓存
                    //affectedNodePaths.Add(_categoryIdToName[kvp.Key]);

                    CategoryNode parent = kvp.Value.ParentNode;
                    while (parent != null)
                        //affectedNodePaths.Add(_categoryIdToName[parent.TermId]);
                    {
                        parent = parent.ParentNode;
                    }
                }

                // 添加到新分类
                CategoryNode node = GetOrCreateNode(newCategory);
                node.AddEntity(entityId);
                _entityIdToNode[entityId] = node; // 缓存新映射

                // affectedNodePaths.Add(newCategory);
                CategoryNode newParent = node.ParentNode;
                while (newParent != null)
                    // affectedNodePaths.Add(_categoryIdToName[newParent.TermId]);
                {
                    newParent = newParent.ParentNode;
                }

                return OperationResult.Success();
            }
            finally
            {
                _treeLock.ExitWriteLock();
            }
        }

        #endregion

        #region 重命名分类

        /// <summary>
        ///     验证后重命名分类。
        /// </summary>
        /// <param name="oldName">旧分类名称。</param>
        /// <param name="newName">新分类名称。</param>
        /// <returns>操作结果。</returns>
        public OperationResult RenameCategorySafe(string oldName, string newName)
        {
            oldName = CategoryNameNormalizer.Normalize(oldName);
            newName = CategoryNameNormalizer.Normalize(newName);

            return !CategoryNameNormalizer.IsValid(newName, out string errorMessage)
                ? OperationResult.Failure(ErrorCode.InvalidCategory, errorMessage)
                : RenameCategory(oldName, newName);
        }

        /// <summary>
        ///     重命名分类及其所有子分类。
        /// </summary>
        /// <param name="oldName">旧分类名称。</param>
        /// <param name="newName">新分类名称。</param>
        /// <returns>操作结果。</returns>
        public OperationResult RenameCategory(string oldName, string newName)
        {
            _treeLock.EnterWriteLock();
            try
            {
                // 检查旧分类
                if (!_categoryNameToId.TryGetValue(oldName, out int oldId) ||
                    !_categoryNodes.TryGetValue(oldId, out CategoryNode oldNode))
                    return OperationResult.Failure(ErrorCode.NotFound, $"未找到分类 '{oldName}'");

                // 检查新名称冲突
                if (_categoryNameToId.ContainsKey(newName))
                    return OperationResult.Failure(ErrorCode.DuplicateId, $"分类 '{newName}' 已存在");

                // 收集所有要重命名的节点
                var nodesToRename = new List<(int Id, CategoryNode Node, string OldPath, string NewPath)>();
                CollectNodesToRename(oldNode, oldName, newName, nodesToRename);

                // 执行重命名
                foreach ((int id, _, string oldPath, string newPath) in nodesToRename)
                {
                    _categoryNameToId.Remove(oldPath);
                    _categoryIdToName[id] = newPath;
                    _categoryNameToId[newPath] = id;

                    // 更新 TermMapper 映射（从旧路径到新路径）
                    _categoryTermMapper.RemapTerm(oldPath, newPath);
                }

                return OperationResult.Success();
            }
            finally
            {
                _treeLock.ExitWriteLock();
            }
        }

        /// <summary>
        ///     递归收集所有需要重命名的节点（RenameCategory 的辅助方法）。
        /// </summary>
        /// <param name="node">起始节点。</param>
        /// <param name="oldPrefix">旧前缀。</param>
        /// <param name="newPrefix">新前缀。</param>
        /// <param name="result">结果列表。</param>
        private void CollectNodesToRename(
            CategoryNode node,
            string oldPrefix,
            string newPrefix,
            List<(int Id, CategoryNode Node, string OldPath, string NewPath)> result)
        {
            // 获取当前节点的旧路径
            string oldPath = _categoryIdToName[node.TermId];
            string newPath = oldPath.Replace(oldPrefix, newPrefix);

            result.Add((node.TermId, node, oldPath, newPath));

            // 递归子节点
            foreach (CategoryNode child in node.Children)
            {
                CollectNodesToRename(child, oldPrefix, newPrefix, result);
            }
        }

        #endregion

        #region 序列化辅助方法

        /// <summary>
        ///     获取标签索引（仅供序列化器使用）。
        ///     映射标签名称到包含的实体ID列表。
        /// </summary>
        /// <returns>标签名称到实体ID集合的字典。</returns>
        internal Dictionary<string, IEnumerable<string>> GetTagIndex()
        {
            var tagIndex = new Dictionary<string, IEnumerable<string>>();

            foreach ((int tagId, var entityIds) in _tagToEntityIds)
                // 从 _tagMapper 获取标签名称
            {
                if (_tagMapper.TryGetString(tagId, out string tagName))
                    tagIndex[tagName] = entityIds.ToList();
            }

            return tagIndex;
        }

        /// <summary>
        ///     根据实体 ID 获取其所在分类的可读路径字符串。
        ///     示例：如果实体在 "Equipment.Weapon.Sword" 分类中，返回该字符串。
        /// </summary>
        /// <param name="entityId">实体 ID</param>
        /// <returns>可读的分类路径字符串；若实体不存在，返回空字符串</returns>
        public string GetReadableCategoryPath(string entityId)
        {
            if (string.IsNullOrEmpty(entityId)) return string.Empty;

            _treeLock.EnterReadLock();
            try
            {
                if (_entityIdToNode.TryGetValue(entityId, out CategoryNode node)) return GetNodeReadablePath(node);

                // 实体不存在于任何分类
                return string.Empty;
            }
            finally
            {
                _treeLock.ExitReadLock();
            }
        }

        /// <summary>
        ///     根据实体整数 ID 获取其所在分类的可读路径字符串（使用整数ID）。
        /// </summary>
        /// <param name="entityId">实体整数 ID</param>
        /// <returns>可读的分类路径字符串；若实体不存在，返回空字符串</returns>
        public string GetReadableCategoryPath(int entityId)
        {
            _treeLock.EnterReadLock();
            try
            {
                if (_entityIdToNodeInt.TryGetValue(entityId, out CategoryNode node)) 
                    return GetNodeReadablePath(node);

                // 实体不存在于任何分类
                return string.Empty;
            }
            finally
            {
                _treeLock.ExitReadLock();
            }
        }

        public string GetNodeReadablePath(CategoryNode node)
        {
            if (node == null) return string.Empty;

            int[] pathIds = node.GetPathAsIds();
            return GetReadablePathFromIds(pathIds);
        }


        /// <summary>
        ///     从路径 ID 数组转换为可读的分类路径字符串
        /// </summary>
        /// <param name="pathIds">路径 ID 数组（从 CategoryNode.GetPathAsIds() 获取）</param>
        /// <returns>可读的分类路径字符串，如 "Equipment.Weapon.Sword"</returns>
        public string GetReadablePathFromIds(int[] pathIds)
        {
            if (pathIds == null || pathIds.Length == 0) return string.Empty;

            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < pathIds.Length; i++)
            {
                if (i > 0) sb.Append(CategoryConstants.CATEGORY_SEPARATOR);

                // 从 _categoryTermMapper 获取可读的分类名称
                if (_categoryIdToName.TryGetValue(pathIds[i], out string categoryName))
                    sb.Append(categoryName);
                else
                    // 备选方案：如果映射不存在，使用 ID 作为后备
                    sb.Append(pathIds[i]);
            }

            return sb.ToString();
        }

        /// <summary>
        ///     获取元数据存储（仅供序列化器使用）。
        ///     映射实体ID到自定义数据集合。
        /// </summary>
        /// <returns>实体ID到CustomDataCollection的字典。</returns>
        internal Dictionary<string, CustomDataCollection> GetMetadataStore() => new(_metadataStore);

        /// <summary>
        ///     获取序列化索引
        /// </summary>
        /// <param name="entityTagIndex">输出参数：EntityId -> TagNames 字典</param>
        /// <param name="entityMetadataIndex">输出参数：EntityId -> CustomDataCollection 字典</param>
        internal void GetOptimizedSerializationIndices(
            out Dictionary<string, List<string>> entityTagIndex,
            out Dictionary<string, CustomDataCollection> entityMetadataIndex)
        {
            entityTagIndex = new(StringComparer.Ordinal);
            entityMetadataIndex = new(_metadataStore);

            foreach (var kvp in _entityToTagIds)
            {
                string entityId = kvp.Key;
                var tagIds = kvp.Value;

                var tagNames = new List<string>(tagIds.Count);
                foreach (int tagId in tagIds)
                {
                    if (_tagMapper.TryGetString(tagId, out string tagName))
                        tagNames.Add(tagName);
                }

                if (tagNames.Count > 0) entityTagIndex[entityId] = tagNames;
            }
        }

        #endregion

        #region 序列化

        /// <summary>
        ///     将管理器内容序列化为 JSON 字符串。
        /// </summary>
        /// <returns>JSON 字符串。</returns>
        public string SerializeToJson()
        {
            var serializer = new CategoryManagerJsonSerializer<T>(_idExtractor);
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
                var serializer = new CategoryManagerJsonSerializer<T>(_idExtractor);
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

                    foreach (var kvp in newManager._tagToEntityIds)
                    {
                        _tagToEntityIds[kvp.Key] = new(kvp.Value);
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
        /// <param name="idExtractor">ID 提取函数。</param>
        /// <returns>新的 CategoryManager 实例。</returns>
        public static CategoryManager<T> CreateFromJson(string json, Func<T, string> idExtractor)
        {
            var serializer = new CategoryManagerJsonSerializer<T>(idExtractor);
            return serializer.DeserializeFromJson(json);
        }

        #endregion

        #region 清空数据清空与重置

        /// <summary>
        ///     清空所有数据（无锁版本，假设已持有写锁）。
        /// </summary>
        private void Clear_NoLock()
        {
            // 清空字符串ID存储
            _entities.Clear();
            _tagToEntityIds.Clear();
            _entityToTagIds.Clear();
            _entityIdToNode.Clear();
            _metadataStore.Clear();

            // 清空整数ID存储
            _entitiesInt.Clear();
            _tagToEntityIdsInt.Clear();
            _entityToTagIdsInt.Clear();
            _entityIdToNodeInt.Clear();
            _metadataStoreInt.Clear();

            // 清空共享数据
            _categoryNodes.Clear();
            _categoryNameToId.Clear();
            _categoryIdToName.Clear();
            _categoryTermMapper.Clear();
            _tagCache.Clear();

#if UNITY_EDITOR
            _cachedStatistics = null;
            _lastStatisticsUpdate = 0;
            _totalCacheQueries = 0;
            _cacheHits = 0;
            _cacheMisses = 0;
#endif

            foreach (ReaderWriterLockSlim lockObj in _tagLocks.Values)
            {
                lockObj?.Dispose();
            }

            _tagLocks.Clear();
        }

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
        ///     清除缓存清空所有缓存（不清除实体数据）
        /// </summary>
        public void ClearCache()
        {
            _tagCache.Clear();
        }

        #endregion

        #region 统计运行时统计信息

#if UNITY_EDITOR
        private Statistics _cachedStatistics;
        private long _lastStatisticsUpdate;
        private const long StatisticsCacheTimeout = 10000000;

        private long _totalCacheQueries;
        private long _cacheHits;
        private long _cacheMisses;

        /// <summary>
        ///     统计获取运行时统计信息
        /// </summary>
        public Statistics GetStatistics()
        {
            long currentTicks = DateTime.UtcNow.Ticks;

            _treeLock.EnterReadLock();
            try
            {
                // 检查缓存 
                Statistics cachedStats = Volatile.Read(ref _cachedStatistics);
                if (cachedStats != null && currentTicks - _lastStatisticsUpdate < StatisticsCacheTimeout)
                    return cachedStats;

                int maxDepth = 0;
                foreach (var kvp in _categoryIdToName)
                {
                    int depth = kvp.Value.Split('.').Length;
                    if (depth > maxDepth) maxDepth = depth;
                }

                long memoryUsage = _entities.Count * 64 + _categoryNodes.Count * 128;

                foreach (var kvp in _tagToEntityIds)
                {
                    memoryUsage += kvp.Value.Count * 16;
                }

                memoryUsage += _metadataStore.Count * 64;

                float cacheHitRate = _totalCacheQueries > 0
                    ? (float)_cacheHits / _totalCacheQueries
                    : 0f;

                _cachedStatistics = new()
                {
                    TotalEntities = _entities.Count,
                    TotalCategories = _categoryNodes.Count,
                    TotalTags = _tagToEntityIds.Count,
                    CacheHitRate = cacheHitRate,
                    MemoryUsageBytes = memoryUsage,
                    MaxCategoryDepth = maxDepth,
                    TotalCacheQueries = _totalCacheQueries,
                    CacheHits = _cacheHits,
                    CacheMisses = _cacheMisses,
                };

                _lastStatisticsUpdate = currentTicks;
                return _cachedStatistics;
            }
            finally
            {
                _treeLock.ExitReadLock();
            }
        }

        /// <summary>
        ///     记录缓存记录缓存查询结果
        /// </summary>
        private void RecordCacheQuery(bool isHit)
        {
            Interlocked.Increment(ref _totalCacheQueries);
            if (isHit)
                Interlocked.Increment(ref _cacheHits);
            else
                Interlocked.Increment(ref _cacheMisses);

            _cachedStatistics = null;
        }
#else
        /// <summary>
        /// 预业业场准。
        /// </summary>
        public Statistics GetStatistics()
        {
            return new Statistics();
        }

        private void RecordCacheQuery(bool isHit) { }
#endif

        #endregion

        #region 缓存

        /// <summary>
        ///     失效特定标签的缓存。
        /// </summary>
        /// <param name="tag">标签名称。</param>
        public void InvalidateTagCache(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return;

            if (_tagMapper.TryGetId(tag, out int tagId)) _tagCache.Remove(tagId);
        }

        /// <summary>
        ///     获取缓存条目数量。
        /// </summary>
        /// <returns>缓存项数。</returns>
        public int GetCacheSize() => _tagCache.Count;

        #endregion

        #region 重建子树

        /// <summary>
        ///     重建子树重建所有子树索引
        /// </summary>
        public OperationResult RebuildSubtreeIndices()
        {
            try
            {
                _treeLock.EnterWriteLock();
                try
                {
                    // 获取所有根节点
                    var roots = _categoryNodes.Values
                        .Where(n => n.ParentNode == null)
                        .ToList();

                    foreach (CategoryNode root in roots)
                    {
                        RebuildSubtreeIndices_Recursive(root);
                    }

                    return OperationResult.Success();
                }
                finally
                {
                    _treeLock.ExitWriteLock();
                }
            }
            catch (Exception ex)
            {
                return OperationResult.Failure(ErrorCode.ConcurrencyConflict,
                    $"重建子树索引失败: {ex.Message}");
            }
        }

        /// <summary>
        ///     递归重建递归重建节点及子树
        /// </summary>
        private static void RebuildSubtreeIndices_Recursive(CategoryNode node)
        {
            foreach (CategoryNode child in node.Children)
            {
                RebuildSubtreeIndices_Recursive(child);
            }
        }

        #endregion

        #region 释放资源释放

        /// <summary>
        ///     释放释放所有资源
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

        #region 实体注册内部类

        /// <summary>
        ///     内部类实体注册链式构建器
        ///     支持: WithTags() - 添加标签, WithMetadata() - 添加元数据, Complete() - 完成注册
        /// </summary>
        private class EntityRegistration : IEntityRegistration
        {
            private readonly CategoryManager<T> _manager;
            private readonly string _entityId;
            private readonly T _entity;
            private readonly string _category;
            private readonly List<string> _tags;
            private CustomDataCollection _metadata;
            private readonly OperationResult _validationResult;

            public EntityRegistration(
                CategoryManager<T> manager,
                string entityId,
                T entity,
                string category,
                OperationResult validationResult)
            {
                _manager = manager;
                _entityId = entityId;
                _entity = entity;
                _category = category;
                _tags = new();
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

                try
                {
                    // 存储实体
                    _manager._entitiesLock.EnterWriteLock();
                    try
                    {
                        _manager._entities[_entityId] = _entity;
                    }
                    finally
                    {
                        _manager._entitiesLock.ExitWriteLock();
                    }

                    // 创建分类节点
                    _manager._treeLock.EnterWriteLock();
                    try
                    {
                        CategoryNode node = _manager.GetOrCreateNode(_category);
                        node.AddEntity(_entityId);
                    }
                    finally
                    {
                        _manager._treeLock.ExitWriteLock();
                    }

                    // 添加标签
                    foreach (string tag in _tags)
                    {
                        _manager.AddTag(_entityId, tag);
                    }

                    // 存储元数据
                    if (_metadata != null)
                    {
                        _manager._metadataLock.EnterWriteLock();
                        try
                        {
                            _manager._metadataStore[_entityId] = _metadata;
                        }
                        finally
                        {
                            _manager._metadataLock.ExitWriteLock();
                        }
                    }

                    // 清除统计缓存（数据已改变）
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

        /// <summary>
        ///     内部类：整数 ID 实体注册链式构建器
        ///     支持: WithTags() - 添加标签, WithMetadata() - 添加元数据, Complete() - 完成注册
        /// </summary>
        private class EntityRegistrationInt : IEntityRegistration
        {
            private readonly CategoryManager<T> _manager;
            private readonly int _entityId;
            private readonly T _entity;
            private readonly string _category;
            private readonly List<string> _tags;
            private CustomDataCollection _metadata;
            private readonly OperationResult _validationResult;

            public EntityRegistrationInt(
                CategoryManager<T> manager,
                int entityId,
                T entity,
                string category,
                OperationResult validationResult)
            {
                _manager = manager;
                _entityId = entityId;
                _entity = entity;
                _category = category;
                _tags = new();
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

                // 检查 ID 是否已存在
                _manager._entitiesLock.EnterReadLock();
                try
                {
                    if (_manager._entitiesInt.ContainsKey(_entityId))
                        return OperationResult.Failure(ErrorCode.DuplicateId, $"整数实体 ID '{_entityId}' 已存在");
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
                        _manager._entitiesInt[_entityId] = _entity;
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
                        _manager._entityIdToNodeInt[_entityId] = node;
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
                            _manager.AddTagInt(_entityId, tag);
                        }
                    }

                    // 存储元数据
                    if (_metadata != null)
                    {
                        _manager._metadataLock.EnterWriteLock();
                        try
                        {
                            _manager._metadataStoreInt[_entityId] = _metadata;
                        }
                        finally
                        {
                            _manager._metadataLock.ExitWriteLock();
                        }
                    }

                    // 清除统计缓存（数据已改变）
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