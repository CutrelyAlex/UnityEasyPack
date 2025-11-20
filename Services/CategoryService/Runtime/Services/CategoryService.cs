using EasyPack.CustomData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace EasyPack.CategoryService
{
    /// <summary>
    /// 通用分类管理服务
    /// 提供实体的分类管理、标签系统和元数据管理功能
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    public class CategoryService<T> : CategoryServiceBase
    {
        #region 属性

        private readonly Func<T, string> _idExtractor;
        private readonly StringComparison _comparisonMode;
        private readonly ICacheStrategy<T> _cacheStrategy;

        // 核心存储
        private readonly Dictionary<string, T> _entities;
        private readonly Dictionary<string, CategoryNode> _categoryTree;
        private readonly Dictionary<string, HashSet<string>> _categoryIndex;

        // 标签系统
        private readonly Dictionary<string, HashSet<string>> _tagIndex;
        private readonly Dictionary<string, ReaderWriterLockSlim> _tagLocks;

        // 元数据存储
        private readonly Dictionary<string, CustomDataCollection> _metadataStore;

        // 根节点锁
        private readonly ReaderWriterLockSlim _treeLock;

        #endregion

        #region 构造

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="idExtractor">实体 ID 提取函数</param>
        /// <param name="comparisonMode">字符串比较模式</param>
        /// <param name="cacheStrategy">缓存策略</param>
        public CategoryService(
            Func<T, string> idExtractor,
            StringComparison comparisonMode = StringComparison.OrdinalIgnoreCase,
            CacheStrategy cacheStrategy = CacheStrategy.Balanced)
        {
            _idExtractor = idExtractor ?? throw new ArgumentNullException(nameof(idExtractor));
            _comparisonMode = comparisonMode;
            _cacheStrategy = CacheStrategyFactory.Create<T>(cacheStrategy);

            _entities = new Dictionary<string, T>();
            _categoryTree = new Dictionary<string, CategoryNode>();
            _categoryIndex = new Dictionary<string, HashSet<string>>();
            _tagIndex = new Dictionary<string, HashSet<string>>();
            _tagLocks = new Dictionary<string, ReaderWriterLockSlim>();
            _metadataStore = new Dictionary<string, CustomDataCollection>();
            _treeLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        }

        #endregion

        #region 实体注册

        /// <summary>
        /// 注册实体到指定分类
        /// </summary>
        /// <param name="entity">实体对象</param>
        /// <param name="category">分类名称</param>
        /// <returns>实体注册流畅接口</returns>
        public IEntityRegistration RegisterEntity(T entity, string category)
        {
            var id = _idExtractor(entity);

            // 验证分类名称
            category = CategoryNameNormalizer.Normalize(category, _comparisonMode);
            if (!CategoryNameNormalizer.IsValid(category, out var errorMessage))
            {
                return new EntityRegistration(this, id, entity, category,
                    OperationResult.Failure(ErrorCode.InvalidCategory, errorMessage));
            }

            // 检查重复 ID
            if (_entities.ContainsKey(id))
            {
                return new EntityRegistration(this, id, entity, category,
                    OperationResult.Failure(ErrorCode.DuplicateId, $"实体 ID '{id}' 已存在"));
            }

            return new EntityRegistration(this, id, entity, category, OperationResult.Success());
        }

        /// <summary>
        /// 批量注册实体
        /// </summary>
        /// <param name="entities">实体列表</param>
        /// <param name="category">分类名称</param>
        /// <returns>批量操作结果</returns>
        public BatchOperationResult RegisterBatch(List<T> entities, string category)
        {
            var results = new List<(string EntityId, bool Success, ErrorCode ErrorCode, string ErrorMessage)>();
            int successCount = 0;

            foreach (var entity in entities)
            {
                var id = _idExtractor(entity);
                var registration = RegisterEntity(entity, category);
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
        /// 根据分类获取实体（支持通配符）
        /// </summary>
        /// <param name="pattern">分类模式（支持通配符 * ）</param>
        /// <param name="includeChildren">是否包含子分类</param>
        /// <returns>实体列表</returns>
        public IReadOnlyList<T> GetByCategory(string pattern, bool includeChildren = false)
        {
            pattern = CategoryNameNormalizer.Normalize(pattern, _comparisonMode);

            // 检查缓存
            var cacheKey = $"cat:{pattern}:{includeChildren}";
            if (_cacheStrategy.Get(cacheKey, out var cachedResult))
            {
                return cachedResult;
            }

            var entityIds = new HashSet<string>();

            // 处理通配符
            if (pattern.Contains("*"))
            {
                var regex = ConvertWildcardToRegex(pattern);

                _treeLock.EnterReadLock();
                try
                {
                    foreach (var kvp in _categoryTree)
                    {
                        if (regex.IsMatch(kvp.Key))
                        {
                            if (includeChildren)
                            {
                                // 获取所有后代节点
                                var descendants = kvp.Value.GetDescendants();
                                foreach (var node in descendants)
                                {
                                    foreach (var id in node.EntityIds)
                                    {
                                        entityIds.Add(id);
                                    }
                                }
                            }
                            else
                            {
                                foreach (var id in kvp.Value.EntityIds)
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
            }
            else
            {
                // 精确匹配
                if (_categoryTree.TryGetValue(pattern, out var node))
                {
                    // 总是包含当前节点的实体
                    foreach (var id in node.EntityIds)
                    {
                        entityIds.Add(id);
                    }

                    // 如果需要包含子节点
                    if (includeChildren)
                    {
                        var descendants = node.GetDescendants();
                        foreach (var desc in descendants)
                        {
                            foreach (var id in desc.EntityIds)
                            {
                                entityIds.Add(id);
                            }
                        }
                    }
                }
            }

            // 构建结果
            var result = entityIds.Select(id => _entities[id]).ToList();
            _cacheStrategy.Set(cacheKey, result);
            return result;
        }

        /// <summary>
        /// 根据 ID 获取实体
        /// </summary>
        /// <param name="id">实体 ID</param>
        /// <returns>操作结果</returns>
        public OperationResult<T> GetById(string id)
        {
            if (_entities.TryGetValue(id, out var entity))
            {
                return OperationResult<T>.Success(entity);
            }
            return OperationResult<T>.Failure(ErrorCode.NotFound, $"未找到 ID 为 '{id}' 的实体");
        }

        /// <summary>
        /// 获取所有分类名称
        /// </summary>
        /// <returns>分类名称列表</returns>
        public IReadOnlyList<string> GetAllCategories()
        {
            _treeLock.EnterReadLock();
            try
            {
                return _categoryTree.Keys.ToList();
            }
            finally
            {
                _treeLock.ExitReadLock();
            }
        }

        #endregion

        #region 实体删除

        /// <summary>
        /// 删除实体（内部方法，不获取锁）
        /// 调用者必须持有 _treeLock 写锁
        /// </summary>
        private void DeleteEntity_NoLock(string id)
        {
            if (!_entities.ContainsKey(id))
            {
                return;
            }

            // 从实体存储中移除
            _entities.Remove(id);

            // 从分类索引中移除（假定已持有写锁）
            foreach (var kvp in _categoryTree)
            {
                kvp.Value.EntityIds.Remove(id);
            }

            foreach (var kvp in _categoryIndex)
            {
                kvp.Value.Remove(id);
            }

            // 从标签索引中移除
            foreach (var kvp in _tagIndex.ToList())
            {
                if (kvp.Value.Contains(id))
                {
                    AcquireTagWriteLock(kvp.Key);
                    try
                    {
                        kvp.Value.Remove(id);
                    }
                    finally
                    {
                        ReleaseTagLock(kvp.Key);
                    }
                }
            }

            // 从元数据存储中移除
            _metadataStore.Remove(id);
        }

        /// <summary>
        /// 删除实体
        /// </summary>
        /// <param name="id">实体 ID</param>
        /// <returns>操作结果</returns>
        public OperationResult DeleteEntity(string id)
        {
            if (!_entities.ContainsKey(id))
            {
                return OperationResult.Failure(ErrorCode.NotFound, $"未找到 ID 为 '{id}' 的实体");
            }

            _treeLock.EnterWriteLock();
            try
            {
                DeleteEntity_NoLock(id);
            }
            finally
            {
                _treeLock.ExitWriteLock();
            }

            // 清除缓存
            _cacheStrategy.Clear();

            return OperationResult.Success();
        }

        /// <summary>
        /// 删除分类（不包含子分类）
        /// </summary>
        /// <param name="category">分类名称</param>
        /// <returns>操作结果</returns>
        public OperationResult DeleteCategory(string category)
        {
            category = CategoryNameNormalizer.Normalize(category, _comparisonMode);

            _treeLock.EnterWriteLock();
            try
            {
                if (!_categoryTree.TryGetValue(category, out var node))
                {
                    return OperationResult.Failure(ErrorCode.NotFound, $"未找到分类 '{category}'");
                }

                // 删除实体
                foreach (var id in node.EntityIds.ToList())
                {
                    DeleteEntity_NoLock(id);
                }

                // 从树中移除节点
                _categoryTree.Remove(category);
                _categoryIndex.Remove(category);

                // 从父节点移除引用
                if (node.Parent != null)
                {
                    node.Parent.RemoveChild(node.Name);
                }

                _cacheStrategy.Clear();
                return OperationResult.Success();
            }
            finally
            {
                _treeLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// 递归删除分类及其所有子分类
        /// </summary>
        /// <param name="category">分类名称</param>
        /// <returns>操作结果</returns>
        public OperationResult DeleteCategoryRecursive(string category)
        {
            category = CategoryNameNormalizer.Normalize(category, _comparisonMode);

            _treeLock.EnterWriteLock();
            try
            {
                if (!_categoryTree.TryGetValue(category, out var node))
                {
                    return OperationResult.Failure(ErrorCode.NotFound, $"未找到分类 '{category}'");
                }

                // 获取所有后代节点
                var descendants = node.GetDescendants();

                // 从叶子节点开始删除
                for (int i = descendants.Count - 1; i >= 0; i--)
                {
                    var desc = descendants[i];

                    // 删除实体
                    foreach (var id in desc.EntityIds.ToList())
                    {
                        DeleteEntity_NoLock(id);
                    }

                    // 从树中移除
                    _categoryTree.Remove(desc.FullPath);
                    _categoryIndex.Remove(desc.FullPath);
                }

                // 删除根节点的实体
                foreach (var id in node.EntityIds.ToList())
                {
                    DeleteEntity_NoLock(id);
                }

                // 从树中移除根节点
                _categoryTree.Remove(category);
                _categoryIndex.Remove(category);

                // 从父节点移除引用
                if (node.Parent != null)
                {
                    node.Parent.RemoveChild(node.Name);
                }

                _cacheStrategy.Clear();
                return OperationResult.Success();
            }
            finally
            {
                _treeLock.ExitWriteLock();
            }
        }

        #endregion

        #region 分类层级

        /// <summary>
        /// 获取或创建分类节点
        /// </summary>
        private CategoryNode GetOrCreateNode(string fullPath)
        {
            fullPath = CategoryNameNormalizer.Normalize(fullPath, _comparisonMode);

            if (_categoryTree.TryGetValue(fullPath, out var existingNode))
            {
                return existingNode;
            }

            // 创建新节点
            var parts = fullPath.Split('.');
            var name = parts[parts.Length - 1];
            var node = new CategoryNode(name, fullPath);

            _categoryTree[fullPath] = node;

            if (!_categoryIndex.ContainsKey(fullPath))
            {
                _categoryIndex[fullPath] = new HashSet<string>();
            }

            // 处理父节点关系
            if (parts.Length > 1)
            {
                var parentPath = string.Join(".", parts.Take(parts.Length - 1));
                var parent = GetOrCreateNode(parentPath);
                parent.AddChild(node);
            }

            return node;
        }

        /// <summary>
        /// 将通配符模式转换为正则表达式
        /// </summary>
        private Regex ConvertWildcardToRegex(string pattern)
        {
            var regexPattern = "^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$";
            var options = _comparisonMode == StringComparison.OrdinalIgnoreCase
                ? RegexOptions.IgnoreCase | RegexOptions.Compiled
                : RegexOptions.Compiled;

            return RegexCache.GetOrCreate(regexPattern, options);
        }

        #endregion

        #region Tag系统

        /// <summary>
        /// 根据标签获取实体
        /// </summary>
        /// <param name="tag">标签名称</param>
        /// <returns>实体列表</returns>
        public IReadOnlyList<T> GetByTag(string tag)
        {
            var cacheKey = $"tag:{tag}";
            if (_cacheStrategy.Get(cacheKey, out var cachedResult))
            {
                return cachedResult;
            }

            AcquireTagReadLock(tag);
            try
            {
                if (_tagIndex.TryGetValue(tag, out var ids))
                {
                    var result = ids.Select(id => _entities[id]).ToList();
                    _cacheStrategy.Set(cacheKey, result);
                    return result;
                }
                return new List<T>();
            }
            finally
            {
                ReleaseTagLock(tag);
            }
        }

        /// <summary>
        /// 根据分类和标签获取实体（交集查询）
        /// </summary>
        /// <param name="category">分类名称</param>
        /// <param name="tag">标签名称</param>
        /// <returns>实体列表</returns>
        public IReadOnlyList<T> GetByCategoryAndTag(string category, string tag)
        {
            var categoryEntities = GetByCategory(category);
            var tagEntities = GetByTag(tag);

            return categoryEntities.Intersect(tagEntities).ToList();
        }

        private void AcquireTagReadLock(string tag)
        {
            if (!_tagLocks.TryGetValue(tag, out var lockObj))
            {
                lockObj = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
                _tagLocks[tag] = lockObj;
            }
            lockObj.EnterReadLock();
        }

        private void AcquireTagWriteLock(string tag)
        {
            if (!_tagLocks.TryGetValue(tag, out var lockObj))
            {
                lockObj = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
                _tagLocks[tag] = lockObj;
            }
            lockObj.EnterWriteLock();
        }

        private void ReleaseTagLock(string tag)
        {
            if (_tagLocks.TryGetValue(tag, out var lockObj))
            {
                if (lockObj.IsReadLockHeld)
                    lockObj.ExitReadLock();
                if (lockObj.IsWriteLockHeld)
                    lockObj.ExitWriteLock();
            }
        }

        #endregion

        #region 元数据

        /// <summary>
        /// 获取实体元数据
        /// </summary>
        /// <param name="id">实体 ID</param>
        /// <returns>元数据操作结果</returns>
        public OperationResult<CustomDataCollection> GetMetadata(string id)
        {
            if (!_entities.ContainsKey(id))
            {
                return OperationResult<CustomDataCollection>.Failure(
                    ErrorCode.NotFound, $"未找到 ID 为 '{id}' 的实体");
            }

            if (_metadataStore.TryGetValue(id, out var metadata))
            {
                return OperationResult<CustomDataCollection>.Success(metadata);
            }

            return OperationResult<CustomDataCollection>.Success(new CustomDataCollection());
        }

        /// <summary>
        /// 更新实体元数据
        /// </summary>
        /// <param name="id">实体 ID</param>
        /// <param name="metadata">元数据列表</param>
        /// <returns>操作结果</returns>
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

        #region 实体注册内部类

        /// <summary>
        /// 实体注册内部实现
        /// </summary>
        private class EntityRegistration : IEntityRegistration
        {
            private readonly CategoryService<T> _service;
            private readonly string _entityId;
            private readonly T _entity;
            private readonly string _category;
            private readonly List<string> _tags;
            private CustomDataCollection _metadata;
            private readonly OperationResult _validationResult;

            public EntityRegistration(
                CategoryService<T> service,
                string entityId,
                T entity,
                string category,
                OperationResult validationResult)
            {
                _service = service;
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
                // 如果验证失败，直接返回
                if (!_validationResult.IsSuccess)
                {
                    return _validationResult;
                }

                try
                {
                    // 存储实体
                    _service._entities[_entityId] = _entity;

                    // 创建或获取分类节点
                    _service._treeLock.EnterWriteLock();
                    try
                    {
                        var node = _service.GetOrCreateNode(_category);
                        node.EntityIds.Add(_entityId);

                        if (!_service._categoryIndex.ContainsKey(_category))
                        {
                            _service._categoryIndex[_category] = new HashSet<string>();
                        }
                        _service._categoryIndex[_category].Add(_entityId);
                    }
                    finally
                    {
                        _service._treeLock.ExitWriteLock();
                    }

                    // 处理标签
                    foreach (var tag in _tags)
                    {
                        _service.AcquireTagWriteLock(tag);
                        try
                        {
                            if (!_service._tagIndex.ContainsKey(tag))
                            {
                                _service._tagIndex[tag] = new HashSet<string>();
                            }
                            _service._tagIndex[tag].Add(_entityId);
                        }
                        finally
                        {
                            _service.ReleaseTagLock(tag);
                        }
                    }

                    // 处理元数据
                    if (_metadata != null)
                    {
                        _service._metadataStore[_entityId] = _metadata;
                    }

                    // 清除缓存
                    _service._cacheStrategy.Clear();

                    return OperationResult.Success();
                }
                catch (Exception ex)
                {
                    return OperationResult.Failure(ErrorCode.ConcurrencyConflict, ex.Message);
                }
            }
        }

        #endregion

        #region 注销

        public override void Dispose()
        {
            _treeLock?.Dispose();

            foreach (var lockObj in _tagLocks.Values)
            {
                lockObj?.Dispose();
            }

            foreach (var node in _categoryTree.Values)
            {
                node?.Dispose();
            }

            base.Dispose();
        }

        #endregion
    }
}
