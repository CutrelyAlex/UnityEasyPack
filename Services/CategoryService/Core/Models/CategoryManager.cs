using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using EasyPack.CustomData;
using UnityEngine;

namespace EasyPack.Category
{
    /// <summary>
    /// 通用分类管理系统，支持层级分类、标签、元数据 查询。
    /// </summary>
    public class CategoryManager<T> : ICategoryManager<T>
    {
        #region 属性实例基本属性

        /// <summary>
        /// 获取实体类型。
        /// </summary>
        public Type EntityType => typeof(T);

        #endregion

        #region 字段

        // 基础字段
        private readonly Func<T, string> _idExtractor;

        // 实体存储
        private readonly Dictionary<string, T> _entities;

        // 分类树
        private readonly Dictionary<int, CategoryNode> _categoryNodes;
        private readonly Dictionary<string, int> _categoryNameToId;  // 分类名称 → 整数ID
        private readonly Dictionary<int, string> _categoryIdToName;  // 整数ID → 分类名称

        // 标签系统
        private readonly Dictionary<int, HashSet<string>> _tagToEntityIds;        // tagId → entityIds
        private readonly Dictionary<string, HashSet<int>> _entityToTagIds;        // entityId → tagIds
        private readonly Dictionary<int, ReaderWriterLockSlim> _tagLocks;         
        private readonly Dictionary<int, List<T>> _tagCache;                      // 整数键缓存

        // 映射层
        private readonly IntegerMapper<Tag> _tagMapper;
        private readonly IntegerMapper<CategoryTerm> _categoryTermMapper;

        // 元数据与根锁
        private readonly Dictionary<string, CustomDataCollection> _metadataStore;
        private readonly ReaderWriterLockSlim _treeLock;

        #endregion

        #region 构造

       
        public CategoryManager(
            Func<T, string> idExtractor)
        {
            _idExtractor = idExtractor ?? throw new ArgumentNullException(nameof(idExtractor));

            _entities = new Dictionary<string, T>();

            _categoryNodes = new Dictionary<int, CategoryNode>();
            _categoryNameToId = new Dictionary<string, int>();
            _categoryIdToName = new Dictionary<int, string>();

            _tagToEntityIds = new Dictionary<int, HashSet<string>>();
            _entityToTagIds = new Dictionary<string, HashSet<int>>();
            _tagLocks = new Dictionary<int, ReaderWriterLockSlim>();
            _tagCache = new Dictionary<int, List<T>>();

            _tagMapper = new IntegerMapper<Tag>(CategoryConstants.DEFAULT_MAPPER_CAPACITY);
            _categoryTermMapper = new IntegerMapper<CategoryTerm>(CategoryConstants.DEFAULT_MAPPER_CAPACITY);

            _metadataStore = new Dictionary<string, CustomDataCollection>();
            _treeLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        }

        #endregion

        #region 实体注册

        /// <summary>
        /// 注册实体到指定分类（同步操作）。
        /// </summary>
        /// <param name="entity">要注册的实体。</param>
        /// <param name="category">目标分类名称。</param>
        /// <returns>操作结果。</returns>
        public OperationResult RegisterEntityComplete(T entity, string category)
        {
            return RegisterEntity(entity, category).Complete();
        }

        /// <summary>
        /// 开始注册实体，支持链式调用添加标签和元数据。
        /// </summary>
        /// <param name="entity">要注册的实体。</param>
        /// <param name="category">目标分类名称。</param>
        /// <returns>链式注册对象。</returns>
        public IEntityRegistration RegisterEntity(T entity, string category)
        {
            var id = _idExtractor(entity);
            return RegisterEntityInternal(entity, id, category);
        }

        /// <summary>
        /// 注册实体，自动验证分类名称格式。
        /// </summary>
        /// <param name="entity">要注册的实体。</param>
        /// <param name="category">目标分类名称（将自动规范化）。</param>
        /// <returns>链式注册对象；若分类名称无效，返回失败结果。</returns>
        public IEntityRegistration RegisterEntitySafe(T entity, string category)
        {
            var id = _idExtractor(entity);

            category = CategoryNameNormalizer.Normalize(category);
            if (!CategoryNameNormalizer.IsValid(category, out var errorMessage))
            {
                return new EntityRegistration(this, id, entity, category,
                    OperationResult.Failure(ErrorCode.InvalidCategory, errorMessage));
            }
            return RegisterEntityInternal(entity, id, category);
        }

        /// <summary>
        /// 内部注册逻辑，验证实体ID唯一性。
        /// </summary>
        /// <param name="entity">要注册的实体。</param>
        /// <param name="id">实体ID。</param>
        /// <param name="category">目标分类。</param>
        /// <returns>链式注册对象；若ID已存在，返回失败结果。</returns>
        private IEntityRegistration RegisterEntityInternal(T entity, string id, string category)
        {
            if (_entities.ContainsKey(id))
            {
                return new EntityRegistration(this, id, entity, category,
                    OperationResult.Failure(ErrorCode.DuplicateId, $"实体 ID '{id}' 已存在"));
            }
            return new EntityRegistration(this, id, entity, category, OperationResult.Success());
        }

        /// <summary>
        /// 批量注册多个实体到同一分类。
        /// </summary>
        /// <param name="entities">实体列表。</param>
        /// <param name="category">目标分类名称。</param>
        /// <returns>批量操作结果，含成功数、失败数和详细信息。</returns>
        public BatchOperationResult RegisterBatch(List<T> entities, string category)
        {
            var results = new List<(string EntityId, bool Success, ErrorCode ErrorCode, string ErrorMessage)>();
            int successCount = 0;

            foreach (var entity in entities)
            {
                var id = _idExtractor(entity);
                var registration = RegisterEntitySafe(entity, category);
                var result = registration.Complete();

                results.Add((id, result.IsSuccess, result.ErrorCode, result.ErrorMessage));
                if (result.IsSuccess)
                {
                    successCount++;
                }
            }

            return new BatchOperationResult
            {
                TotalCount = entities.Count,
                SuccessCount = successCount,
                FailureCount = entities.Count - successCount,
                Details = results
            };
        }

        #endregion

        #region 实体查询

        /// <summary>
        /// 按分类查询实体，支持通配符匹配和子分类包含。
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
                    var regex = ConvertWildcardToRegex(pattern);

                    foreach (var kvp in _categoryNodes)
                    {
                        if (!_categoryIdToName.TryGetValue(kvp.Key, out var nodePath))
                            continue;

                        if (!regex.IsMatch(nodePath))
                            continue;

                        var node = kvp.Value;
                        if (includeChildren)
                        {
                            foreach (var id in node.GetSubtreeEntityIds())
                            {
                                entityIds.Add(id);
                            }
                        }
                        else
                        {
                            foreach (var id in node.EntityIds)
                            {
                                entityIds.Add(id);
                            }
                        }
                    }
                }
                else
                {
                    // 精确查询一次字典查询
                    if (_categoryTermMapper.Contains(pattern) && 
                        _categoryNodes.TryGetValue(_categoryTermMapper.GetOrAssignId(pattern), out var node))
                    {
                        if (includeChildren)
                        {
                            foreach (var id in node.GetSubtreeEntityIds())
                            {
                                entityIds.Add(id);
                            }
                        }
                        else
                        {
                            foreach (var id in node.EntityIds)
                            {
                                entityIds.Add(id);
                            }
                        }
                    }
                }
            }
            finally
            {
                _treeLock.ExitReadLock();
            }

            var result = entityIds.Select(id => _entities[id]).ToList();
            return result;
        }

        /// <summary>
        /// 按ID查询实体。
        /// </summary>
        /// <param name="id">实体ID。</param>
        /// <returns>包含实体的操作结果；若没有找到，返回失败。</returns>
        public OperationResult<T> GetById(string id)
        {
            return _entities.TryGetValue(id, out var entity)
                ? OperationResult<T>.Success(entity)
                : OperationResult<T>.Failure(ErrorCode.NotFound, $"未找到 ID 为 '{id}' 的实体");
        }

        /// <summary>
        /// 获取所有分类节点（包括中间节点）。
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
        /// 获取叶子分类（没有子分类的分类）。
        /// </summary>
        /// <returns>叶子分类名称列表。</returns>
        public IReadOnlyList<string> GetLeafCategories()
        {
            _treeLock.EnterReadLock();
            try
            {
                var leafCategories = new List<string>();
                foreach ((int key, var node) in _categoryNodes)
                {
                    if (node.Children.Count == 0)
                    {
                        leafCategories.Add(_categoryIdToName[key]);
                    }
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
        /// 从所有分类和标签中删除实体。
        /// </summary>
        /// <param name="id">实体ID。</param>
        /// <returns>操作结果。</returns>
        public OperationResult DeleteEntity(string id)
        {
            if (!_entities.ContainsKey(id))
            {
                return OperationResult.Failure(ErrorCode.NotFound, $"未找到 ID 为 '{id}' 的实体");
            }

            _treeLock.EnterWriteLock();
            try
            {
                var affectedNodePaths = new HashSet<string>();

                // 从分类中移除
                foreach (var kvp in _categoryNodes.ToList())
                {
                    if (kvp.Value.EntityIds.Contains(id))
                    {
                        kvp.Value.RemoveEntity(id);
                        affectedNodePaths.Add(_categoryIdToName[kvp.Key]);

                        var parent = kvp.Value.ParentNode;
                        while (parent != null)
                        {
                            affectedNodePaths.Add(_categoryIdToName[parent.TermId]);
                            parent = parent.ParentNode;
                        }
                    }
                }

                // 从实体存储移除
                _entities.Remove(id);

                // 从标签中移除
                if (_entityToTagIds.TryGetValue(id, out var tagIds))
                {
                    foreach (var tagId in tagIds.ToList())
                    {
                        AcquireTagWriteLock(tagId);
                        try
                        {
                            if (_tagToEntityIds.TryGetValue(tagId, out var entityIds))
                            {
                                entityIds.Remove(id);
                                if (entityIds.Count == 0)
                                {
                                    _tagToEntityIds.Remove(tagId);
                                }
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
                _metadataStore.Remove(id);

                return OperationResult.Success();
            }
            finally
            {
                _treeLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// 删除单个分类及一级实体，不影响子分类。
        /// </summary>
        /// <param name="category">分类名称。</param>
        /// <returns>操作结果。</returns>
        public OperationResult DeleteCategory(string category)
        {
            category = CategoryNameNormalizer.Normalize(category);

            _treeLock.EnterWriteLock();
            try
            {
                if (!_categoryTermMapper.Contains(category) ||
                    !_categoryNodes.TryGetValue(_categoryTermMapper.GetOrAssignId(category), out var node))
                {
                    return OperationResult.Failure(ErrorCode.NotFound, $"未找到分类 '{category}'");
                }

                var affectedNodePaths = new HashSet<string> { category };

                // 删除该节点的所有实体
                foreach (var id in node.EntityIds.ToList())
                {
                    _entities.Remove(id);
                    _metadataStore.Remove(id);
                }

                // 从父节点移除此节点
                node.ParentNode?.RemoveChild(node.TermId);

                // 收集祖先节点
                var parent = node.ParentNode;
                while (parent != null)
                {
                    affectedNodePaths.Add(_categoryIdToName[parent.TermId]);
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
        /// 递归删除分类及所有子分类。
        /// </summary>
        /// <param name="category">分类名称。</param>
        /// <returns>操作结果。</returns>
        public OperationResult DeleteCategoryRecursive(string category)
        {
            category = CategoryNameNormalizer.Normalize(category);

            _treeLock.EnterWriteLock();
            try
            {
                if (!_categoryTermMapper.Contains(category) ||
                    !_categoryNodes.TryGetValue(_categoryTermMapper.GetOrAssignId(category), out var node))
                {
                    return OperationResult.Failure(ErrorCode.NotFound, $"未找到分类 '{category}'");
                }

                var affectedNodePaths = new HashSet<string>();
                var nodesToDelete = new List<(int Id, CategoryNode Node)>();

                // 收集所有后代节点
                CollectDescendants(node, nodesToDelete, affectedNodePaths);
                nodesToDelete.Add((node.TermId, node));
                affectedNodePaths.Add(category);

                // 删除所有后代节点（从子向上）
                for (int i = nodesToDelete.Count - 1; i >= 0; i--)
                {
                    var (nodeId, n) = nodesToDelete[i];
                    foreach (var id in n.EntityIds.ToList())
                    {
                        _entities.Remove(id);
                        _metadataStore.Remove(id);
                    }

                    _categoryNodes.Remove(nodeId);
                    _categoryIdToName.TryGetValue(nodeId, out var nodeName);
                    if (nodeName != null)
                    {
                        _categoryNameToId.Remove(nodeName);
                        _categoryIdToName.Remove(nodeId);
                    }
                }

                // 从父节点移除根节点
                if (node.ParentNode != null)
                {
                    node.ParentNode.RemoveChild(node.TermId);
                    var parent = node.ParentNode;
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
        /// 获取或创建分类节点（自动创建父节点）。
        /// </summary>
        /// <param name="fullPath">完整分类路径（如 Equipment.Weapon.Sword）。</param>
        /// <returns>分类节点。</returns>
        private CategoryNode GetOrCreateNode(string fullPath)
        {
            fullPath = CategoryNameNormalizer.Normalize(fullPath);

            // 检查缓存
            if (_categoryNameToId.TryGetValue(fullPath, out int existingId) &&
                _categoryNodes.TryGetValue(existingId, out var existingNode))
            {
                return existingNode;
            }

            // 处理父节点关系
            var parts = fullPath.Split('.');
            CategoryNode node;
            
            if (parts.Length > 1)
            {
                var parentPath = string.Join(".", parts.Take(parts.Length - 1));
                var parent = GetOrCreateNode(parentPath);
                
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
                node = new CategoryNode(newCategoryId);
                _categoryNodes[newCategoryId] = node;
                _categoryNameToId[fullPath] = newCategoryId;
                _categoryIdToName[newCategoryId] = fullPath;
            }

            return node;
        }

        /// <summary>
        /// 将通配符模式转换为正则表达式。
        /// </summary>
        /// <param name="pattern">包含 * 通配符的模式。</param>
        /// <returns>编译的正则表达式。</returns>
        private static Regex ConvertWildcardToRegex(string pattern)
        {
            var regexPattern = "^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$";
            const RegexOptions options = RegexOptions.Compiled;
            return RegexCache.GetOrCreate(regexPattern, options);
        }

        /// <summary>
        /// 递归收集所有后代节点（DeleteCategoryRecursive的辅助方法）。
        /// </summary>
        /// <param name="node">起始节点。</param>
        /// <param name="result">结果节点列表。</param>
        /// <param name="pathSet">已收集的路径集合。</param>
        private void CollectDescendants(CategoryNode node, List<(int Id, CategoryNode Node)> result, HashSet<string> pathSet)
        {
            foreach (var child in node.Children)
            {
                pathSet.Add(_categoryIdToName[child.TermId]);
                result.Add((child.TermId, child));
                CollectDescendants(child, result, pathSet);
            }
        }

        #endregion

        #region 标签系统

        /// <summary>
        /// 为实体添加标签。
        /// </summary>
        /// <param name="entityId">实体ID。</param>
        /// <param name="tag">标签名称。</param>
        /// <returns>操作结果。</returns>
        public OperationResult AddTag(string entityId, string tag)
        {
            if (!_entities.ContainsKey(entityId))
            {
                return OperationResult.Failure(ErrorCode.NotFound, $"未找到 ID 为 '{entityId}' 的实体");
            }

            if (string.IsNullOrWhiteSpace(tag))
            {
                return OperationResult.Failure(ErrorCode.InvalidCategory, "标签名称不能为空");
            }

            // 字符串→整数映射
            int tagId = _tagMapper.GetOrAssignId(tag);

            AcquireTagWriteLock(tagId);
            try
            {
                // 添加到标签→实体映射
                if (!_tagToEntityIds.TryGetValue(tagId, out var entityIds))
                {
                    entityIds = new HashSet<string>();
                    _tagToEntityIds[tagId] = entityIds;
                }
                entityIds.Add(entityId);

                // 添加到实体→标签映射
                if (!_entityToTagIds.TryGetValue(entityId, out var tagIds))
                {
                    tagIds = new HashSet<int>();
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
        /// 从实体移除标签。
        /// </summary>
        /// <param name="entityId">实体ID。</param>
        /// <param name="tag">标签名称。</param>
        /// <returns>操作结果。</returns>
        public OperationResult RemoveTag(string entityId, string tag)
        {
            if (!_entities.ContainsKey(entityId))
            {
                return OperationResult.Failure(ErrorCode.NotFound, $"未找到 ID 为 '{entityId}' 的实体");
            }

            if (string.IsNullOrWhiteSpace(tag))
            {
                return OperationResult.Failure(ErrorCode.InvalidCategory, "标签名称不能为空");
            }

            // 映射到整数
            if (!_tagMapper.Contains(tag))
            {
                return OperationResult.Failure(ErrorCode.NotFound, $"标签 '{tag}' 不存在");
            }
            int tagId = _tagMapper.GetOrAssignId(tag);

            AcquireTagWriteLock(tagId);
            try
            {
                if (!_tagToEntityIds.TryGetValue(tagId, out var entityIds) ||
                    !entityIds.Remove(entityId))
                {
                    return OperationResult.Failure(ErrorCode.NotFound,
                        $"实体 '{entityId}' 不具有标签 '{tag}'");
                }

                // 从实体标签集合移除
                if (_entityToTagIds.TryGetValue(entityId, out var tagIds))
                {
                    tagIds.Remove(tagId);
                }

                // 如果标签无实体，删除标签
                if (entityIds.Count == 0)
                {
                    _tagToEntityIds.Remove(tagId);
                }

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
        /// 按标签查询实体。
        /// </summary>
        /// <param name="tag">标签名称。</param>
        /// <returns>匹配的实体列表。</returns>
        public IReadOnlyList<T> GetByTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
            {
                return new List<T>();
            }
            

            // 获取标签整数ID
            int tagId = _tagMapper.GetOrAssignId(tag);

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
                    var result = entityIds.Select(id => _entities[id]).ToList();
                    _tagCache[tagId] = result;
                    return result;
                }
                return new List<T>();
            }
            finally
            {
                ReleaseTagLock(tagId);
            }
        }

        /// <summary>
        /// 同时按分类和标签查询实体。
        /// </summary>
        /// <param name="category">分类名称。</param>
        /// <param name="tag">标签名称。</param>
        /// <returns>同时满足两个条件的实体列表。</returns>
        public IReadOnlyList<T> GetByCategoryAndTag(string category, string tag)
        {
            var categoryEntities = GetByCategory(category);
            var tagEntities = GetByTag(tag);

            return categoryEntities.Intersect(tagEntities).ToList();
        }

        /// <summary>
        /// 按多个标签查询实体，支持AND/OR逻辑。
        /// </summary>
        /// <param name="tags">标签名称数组。</param>
        /// <param name="matchAll">是true为AND不相交），false为OR（並集）。</param>
        /// <returns>查询结果。</returns>
        public IReadOnlyList<T> GetByTags(string[] tags, bool matchAll = true)
        {
            if (tags == null || tags.Length == 0)
            {
                return new List<T>();
            }

            HashSet<string> resultIds = null;

            foreach (var tag in tags)
            {
                if (string.IsNullOrWhiteSpace(tag))
                    continue;

                int tagId = _tagMapper.GetOrAssignId(tag);

                AcquireTagReadLock(tagId);
                try
                {
                    if (_tagToEntityIds.TryGetValue(tagId, out var tagIds))
                    {
                        if (resultIds == null)
                        {
                            resultIds = new HashSet<string>(tagIds);
                        }
                        else
                        {
                            if (matchAll)
                            {
                                resultIds.IntersectWith(tagIds);
                            }
                            else
                            {
                                resultIds.UnionWith(tagIds);
                            }
                        }
                    }
                    else if (matchAll)
                    {
                        // AND模式下，任一标签不存在则结果为空
                        resultIds = new HashSet<string>();
                        break;
                    }
                }
                finally
                {
                    ReleaseTagLock(tagId);
                }
            }

            var result = resultIds != null
                ? resultIds.Select(id => _entities[id]).ToList()
                : new List<T>();

            return result;
        }

        #endregion

        #region 元数据数据管理

        /// <summary>
        /// 获取实体的元数据。
        /// </summary>
        /// <param name="id">实体ID。</param>
        /// <returns>元数据集合，不存在则返回空集。</returns>
        public CustomDataCollection GetMetadata(string id)
        {
            if (!_entities.ContainsKey(id))
            {
                return new CustomDataCollection();
            }

            return _metadataStore.TryGetValue(id, out var metadata)
                ? metadata
                : new CustomDataCollection();
        }

        /// <summary>
        /// 获取实体的元数据。
        /// </summary>
        /// <param name="id">实体ID。</param>
        /// <returns>包含元数据的操作结果。</returns>
        public OperationResult<CustomDataCollection> GetMetadataResult(string id)
        {
            if (!_entities.ContainsKey(id))
            {
                return OperationResult<CustomDataCollection>.Failure(
                    ErrorCode.NotFound, $"未找到 ID 为 '{id}' 的实体");
            }

            return OperationResult<CustomDataCollection>.Success(
                _metadataStore.TryGetValue(id, out var metadata)
                    ? metadata
                    : new CustomDataCollection());
        }

        /// <summary>
        /// 更新实体的元数据。
        /// </summary>
        /// <param name="id">实体ID。</param>
        /// <param name="metadata">新的元数据集合。</param>
        /// <returns>操作结果。</returns>
        public OperationResult UpdateMetadata(string id, CustomDataCollection metadata)
        {
            if (!_entities.ContainsKey(id))
            {
                return OperationResult.Failure(ErrorCode.NotFound, $"未找到 ID 为 '{id}' 的实体");
            }

            _metadataStore[id] = metadata;
            return OperationResult.Success();
        }

        #endregion

        #region 锁

        /// <summary>
        /// 为标签获取读锁。
        /// </summary>
        /// <param name="tagId">标签整数ID。</param>
        private void AcquireTagReadLock(int tagId)
        {
            if (!_tagLocks.TryGetValue(tagId, out var lockObj))
            {
                lockObj = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
                _tagLocks[tagId] = lockObj;
            }
            lockObj.EnterReadLock();
        }

        /// <summary>
        /// 为标签获取写锁。
        /// </summary>
        /// <param name="tagId">标签整数ID。</param>
        private void AcquireTagWriteLock(int tagId)
        {
            if (!_tagLocks.TryGetValue(tagId, out var lockObj))
            {
                lockObj = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
                _tagLocks[tagId] = lockObj;
            }
            lockObj.EnterWriteLock();
        }

        /// <summary>
        /// 释放标签的锁。
        /// </summary>
        /// <param name="tagId">标签整数ID。</param>
        private void ReleaseTagLock(int tagId)
        {
            if (!_tagLocks.TryGetValue(tagId, out var lockObj)) return;
            if (lockObj.IsReadLockHeld)
                lockObj.ExitReadLock();
            if (lockObj.IsWriteLockHeld)
                lockObj.ExitWriteLock();
        }

        #endregion

        #region 跨分类移动

        /// <summary>
        /// 将实体移动到新分类，自动验证分类名称。
        /// </summary>
        /// <param name="entityId">实体ID。</param>
        /// <param name="newCategory">新的分类名称。</param>
        /// <returns>操作结果。</returns>
        public OperationResult MoveEntityToCategorySafe(string entityId, string newCategory)
        {
            newCategory = CategoryNameNormalizer.Normalize(newCategory);
            return !CategoryNameNormalizer.IsValid(newCategory, out var errorMessage)
                ? OperationResult.Failure(ErrorCode.InvalidCategory, errorMessage)
                : MoveEntityToCategory(entityId, newCategory);
        }

        /// <summary>
        /// 将实体从当前分类移动到新的分类。
        /// </summary>
        /// <param name="entityId">实体ID。</param>
        /// <param name="newCategory">新的分类名称。</param>
        /// <returns>操作结果。</returns>
        public OperationResult MoveEntityToCategory(string entityId, string newCategory)
        {
            if (!_entities.ContainsKey(entityId))
            {
                return OperationResult.Failure(ErrorCode.NotFound, $"未找到 ID 为 '{entityId}' 的实体");
            }

            _treeLock.EnterWriteLock();
            try
            {
                var affectedNodePaths = new HashSet<string>();

                // 从旧分类移除
                foreach (var kvp in _categoryNodes.ToList())
                {
                    if (kvp.Value.RemoveEntity(entityId))
                    {
                        affectedNodePaths.Add(_categoryIdToName[kvp.Key]);

                        var parent = kvp.Value.ParentNode;
                        while (parent != null)
                        {
                            affectedNodePaths.Add(_categoryIdToName[parent.TermId]);
                            parent = parent.ParentNode;
                        }
                    }
                }

                // 添加到新分类
                var node = GetOrCreateNode(newCategory);
                node.AddEntity(entityId);

                affectedNodePaths.Add(newCategory);
                var newParent = node.ParentNode;
                while (newParent != null)
                {
                    affectedNodePaths.Add(_categoryIdToName[newParent.TermId]);
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
        /// 验证后重命名分类。
        /// </summary>
        /// <param name="oldName">旧分类名称。</param>
        /// <param name="newName">新分类名称。</param>
        /// <returns>操作结果。</returns>
        public OperationResult RenameCategorySafe(string oldName, string newName)
        {
            oldName = CategoryNameNormalizer.Normalize(oldName);
            newName = CategoryNameNormalizer.Normalize(newName);

            return !CategoryNameNormalizer.IsValid(newName, out var errorMessage)
                ? OperationResult.Failure(ErrorCode.InvalidCategory, errorMessage)
                : RenameCategory(oldName, newName);
        }

        /// <summary>
        /// 重命名分类及其所有子分类。
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
                    !_categoryNodes.TryGetValue(oldId, out var oldNode))
                {
                    return OperationResult.Failure(ErrorCode.NotFound, $"未找到分类 '{oldName}'");
                }

                // 检查新名称冲突
                if (_categoryNameToId.ContainsKey(newName))
                {
                    return OperationResult.Failure(ErrorCode.DuplicateId, $"分类 '{newName}' 已存在");
                }

                // 收集所有要重命名的节点
                var nodesToRename = new List<(int Id, CategoryNode Node, string OldPath, string NewPath)>();
                CollectNodesToRename(oldNode, oldName, newName, nodesToRename);

                // 执行重命名
                foreach ((int id, var node, string oldPath, string newPath) in nodesToRename)
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
        /// 递归收集所有需要重命名的节点（RenameCategory 的辅助方法）。
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
            var oldPath = _categoryIdToName[node.TermId];
            var newPath = oldPath.Replace(oldPrefix, newPrefix);

            result.Add((node.TermId, node, oldPath, newPath));

            // 递归子节点
            foreach (var child in node.Children)
            {
                CollectNodesToRename(child, oldPrefix, newPrefix, result);
            }
        }

        #endregion

        #region 序列化辅助方法

        /// <summary>
        /// 获取标签索引（仅供序列化器使用）。
        /// 映射标签名称到包含的实体ID列表。
        /// </summary>
        /// <returns>标签名称到实体ID集合的字典。</returns>
        internal Dictionary<string, IEnumerable<string>> GetTagIndex()
        {
            var tagIndex = new Dictionary<string, IEnumerable<string>>();
            
            foreach (var kvp in _tagToEntityIds)
            {
                var tagId = kvp.Key;
                var entityIds = kvp.Value;
                
                // 从 _tagMapper 获取标签名称
                if (_tagMapper.TryGetString(tagId, out var tagName))
                {
                    tagIndex[tagName] = entityIds.ToList();
                }
            }
            
            return tagIndex;
        }

        /// <summary>
        /// 获取元数据存储（仅供序列化器使用）。
        /// 映射实体ID到自定义数据集合。
        /// </summary>
        /// <returns>实体ID到CustomDataCollection的字典。</returns>
        internal Dictionary<string, CustomDataCollection> GetMetadataStore()
        {
            return new Dictionary<string, CustomDataCollection>(_metadataStore);
        }

        #endregion

        #region 序列化

        /// <summary>
        /// 将管理器内容序列化为 JSON 字符串。
        /// </summary>
        /// <returns>JSON 字符串。</returns>
        public string SerializeToJson()
        {
            var serializer = new CategoryManagerJsonSerializer<T>(_idExtractor);
            return serializer.SerializeToJson(this);
        }

        /// <summary>
        /// 从 JSON 字符串加载数据。
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
                    foreach (var kvp in newManager._metadataStore)
                    {
                        _metadataStore[kvp.Key] = kvp.Value;
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
        /// 从 JSON 字符串创建新的 CategoryManager 实例。
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
        /// 清空所有数据（无锁版本，假设已持有写锁）。
        /// </summary>
        private void Clear_NoLock()
        {
            _entities.Clear();
            _categoryNodes.Clear();
            _categoryNameToId.Clear();
            _categoryIdToName.Clear();
            _categoryTermMapper.Clear();
            _tagToEntityIds.Clear();
            _entityToTagIds.Clear();
            _tagCache.Clear();
            _metadataStore.Clear();

#if UNITY_EDITOR
            _cachedStatistics = null;
            _lastStatisticsUpdate = 0;
            _totalCacheQueries = 0;
            _cacheHits = 0;
            _cacheMisses = 0;
#endif
            
            foreach (var lockObj in _tagLocks.Values)
            {
                lockObj?.Dispose();
            }
            _tagLocks.Clear();
        }

        /// <summary>
        /// 清空所有数据。
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
        /// 清除缓存清空所有缓存（不清除实体数据）
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
        /// 统计获取运行时统计信息
        /// </summary>
        public Statistics GetStatistics()
        {
            var currentTicks = DateTime.UtcNow.Ticks;

            _treeLock.EnterReadLock();
            try
            {
                // 检查缓存 
                var cachedStats = Volatile.Read(ref _cachedStatistics);
                if (cachedStats != null && currentTicks - _lastStatisticsUpdate < StatisticsCacheTimeout)
                {
                    return cachedStats;
                }

                int maxDepth = 0;
                foreach (var kvp in _categoryIdToName)
                {
                    int depth = kvp.Value.Split('.').Length;
                    if (depth > maxDepth)
                    {
                        maxDepth = depth;
                    }
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

                _cachedStatistics = new Statistics
                {
                    TotalEntities = _entities.Count,
                    TotalCategories = _categoryNodes.Count,
                    TotalTags = _tagToEntityIds.Count,
                    CacheHitRate = cacheHitRate,
                    MemoryUsageBytes = memoryUsage,
                    MaxCategoryDepth = maxDepth,
                    TotalCacheQueries = _totalCacheQueries,
                    CacheHits = _cacheHits,
                    CacheMisses = _cacheMisses
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
        /// 记录缓存记录缓存查询结果
        /// </summary>
        private void RecordCacheQuery(bool isHit)
        {
            Interlocked.Increment(ref _totalCacheQueries);
            if (isHit)
            {
                Interlocked.Increment(ref _cacheHits);
            }
            else
            {
                Interlocked.Increment(ref _cacheMisses);
            }
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

        private void RecordCacheQuery(bool isHit)
        {
        }
#endif

        #endregion

        #region 缓存

        /// <summary>
        /// 失效特定标签的缓存。
        /// </summary>
        /// <param name="tag">标签名称。</param>
        public void InvalidateTagCache(string tag)
        {
            if (string.IsNullOrEmpty(tag))
            {
                return;
            }

            if (_tagMapper.Contains(tag))
            {
                int tagId = _tagMapper.GetOrAssignId(tag);
                _tagCache.Remove(tagId);
            }
        }

        /// <summary>
        /// 获取缓存条目数量。
        /// </summary>
        /// <returns>缓存项数。</returns>
        public int GetCacheSize()
        {
            return _tagCache.Count;
        }

        #endregion

        #region 重建子树

        /// <summary>
        /// 重建子树重建所有子树索引
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

                    foreach (var root in roots)
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
        /// 递归重建递归重建节点及子树
        /// </summary>
        private static void RebuildSubtreeIndices_Recursive(CategoryNode node)
        {
            foreach (var child in node.Children)
            {
                RebuildSubtreeIndices_Recursive(child);
            }
        }

        #endregion

        #region 释放资源释放

        /// <summary>
        /// 释放释放所有资源
        /// </summary>
        public void Dispose()
        {
            _treeLock?.Dispose();

            foreach (var lockObj in _tagLocks.Values)
            {
                lockObj?.Dispose();
            }
        }

        #endregion

        #region 实体注册内部类

        /// <summary>
        /// 内部类实体注册链式构建器
        /// 支持: WithTags() - 添加标签, WithMetadata() - 添加元数据, Complete() - 完成注册
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
                _tags = new List<string>();
                _validationResult = validationResult;
            }

            public IEntityRegistration WithTags(params string[] tags)
            {
                if (tags != null)
                {
                    _tags.AddRange(tags);
                }
                return this;
            }

            public IEntityRegistration WithMetadata(CustomDataCollection metadata)
            {
                _metadata = metadata;
                return this;
            }

            public OperationResult Complete()
            {
                if (!_validationResult.IsSuccess)
                {
                    return _validationResult;
                }

                try
                {
                    // 存储实体
                    _manager._entities[_entityId] = _entity;

                    // 创建分类节点
                    _manager._treeLock.EnterWriteLock();
                    try
                    {
                        var node = _manager.GetOrCreateNode(_category);
                        node.AddEntity(_entityId);
                    }
                    finally
                    {
                        _manager._treeLock.ExitWriteLock();
                    }

                    // 添加标签
                    foreach (var tag in _tags)
                    {
                        _manager.AddTag(_entityId, tag);
                    }

                    // 存储元数据
                    if (_metadata != null)
                    {
                        _manager._metadataStore[_entityId] = _metadata;
                    }

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
