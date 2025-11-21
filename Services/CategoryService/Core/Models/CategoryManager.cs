using EasyPack.CustomData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace EasyPack.Category
{
    /// <summary>
    /// 通用分类管理器
    /// 提供实体的分类管理、标签系统和元数据管理功能
    /// </summary>
    /// <typeparam name="T">实体类型（必须可序列化）</typeparam>
    public class CategoryManager<T> : ICategoryManager<T>
    {
        #region 属性

        /// <summary>
        /// 实体类型
        /// </summary>
        public Type EntityType => typeof(T);

        private readonly Func<T, string> _idExtractor;
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

        /// <param name="cacheStrategy">缓存策略</param>
        public CategoryManager(
            Func<T, string> idExtractor,
            CacheStrategy cacheStrategy = CacheStrategy.Balanced)
        {
            _idExtractor = idExtractor ?? throw new ArgumentNullException(nameof(idExtractor));
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

        public OperationResult RegisterEntityComplete(T entity, string category)
        {
            return RegisterEntity(entity, category).Complete();
        }
        
        /// <summary>
        /// 注册一个实体到指定分类
        /// </summary>
        /// <param name="entity">实体</param>
        /// <param name="category">类别</param>
        /// <returns></returns>
        public IEntityRegistration RegisterEntity(T entity, string category)
        {
            var id = _idExtractor(entity);
            return RegisterEntityInternal(entity, id, category);
        }
        /// <summary>
        /// 安全注册一个实体到指定分类（包含分类名称验证）
        /// </summary>
        /// <param name="entity">实体</param>
        /// <param name="category">类别</param>
        /// <returns></returns>
        public IEntityRegistration RegisterEntitySafe(T entity, string category)
        {
            var id = _idExtractor(entity);
    
            // 规范化分类名称
            category = CategoryNameNormalizer.Normalize(category);
            if (!CategoryNameNormalizer.IsValid(category, out var errorMessage))
            {
                return new EntityRegistration(this, id, entity, category,
                    OperationResult.Failure(ErrorCode.InvalidCategory, errorMessage));
            }
            return RegisterEntityInternal(entity, id, category);
        }
        
        /// <summary>
        /// 内部注册实体方法
        /// </summary>
        /// <param name="entity">实体</param>
        /// <param name="id">id</param>
        /// <param name="category">类别</param>
        /// <returns></returns>
        private IEntityRegistration RegisterEntityInternal(T entity, string id, string category)
        {
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
        /// 根据分类获取实体（支持通配符）
        /// </summary>
        /// <param name="pattern">分类模式（支持通配符 * ）</param>
        /// <param name="includeChildren">是否包含子分类</param>
        /// <returns>实体列表</returns>
        public IReadOnlyList<T> GetByCategory(string pattern, bool includeChildren = false)
        {
            pattern = CategoryNameNormalizer.Normalize(pattern);

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
                        if (!regex.IsMatch(kvp.Key)) continue;
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
            return _entities.TryGetValue(id, out var entity)
                ? OperationResult<T>.Success(entity)
                : OperationResult<T>.Failure(ErrorCode.NotFound, $"未找到 ID 为 '{id}' 的实体");
        }

        /// <summary>
        /// 获取所有分类名称（包括所有级别，包括仅作为父节点的中间分类）
        /// </summary>
        /// <remarks>返回_categoryTree中的所有分类，包括中间节点</remarks>
        /// <returns>分类名称列表</returns>
        public IReadOnlyList<string> GetCategoriesNodes()
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

        /// <summary>
        /// 获取所有叶子级分类（只返回没有子分类的分类）
        /// </summary>
        /// <remarks>返回的是实际包含实体的叶子级分类，不包括仅作为父节点的中间分类</remarks>
        /// <returns>叶子级分类名称列表</returns>
        public IReadOnlyList<string> GetLeafCategories()
        {
            _treeLock.EnterReadLock();
            try
            {
                var leafCategories = new List<string>();
                foreach (var kvp in _categoryTree)
                {
                    var node = kvp.Value;
                    // 如果没有子分类，则为叶子分类
                    if (node.Children.Count == 0)
                    {
                        leafCategories.Add(kvp.Key);
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
        /// 删除实体（内部方法，不获取锁）
        /// 调用者必须持有 _treeLock 写锁
        /// </summary>
        private void DeleteEntity_NoLock(string id)
        {
            // 从实体存储中移除
            if (!_entities.Remove(id))
            {
                return;
            }
            
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
            category = CategoryNameNormalizer.Normalize(category);

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
                node.Parent?.RemoveChild(node.Name);

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
            category = CategoryNameNormalizer.Normalize(category);

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
                node.Parent?.RemoveChild(node.Name);

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
            fullPath = CategoryNameNormalizer.Normalize(fullPath);

            if (_categoryTree.TryGetValue(fullPath, out var existingNode))
            {
                return existingNode;
            }

            // 创建新节点
            var parts = fullPath.Split('.');
            var name = parts[^1];
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
            // 严格匹配，不使用忽略大小写选项
            var options = RegexOptions.Compiled;

            return RegexCache.GetOrCreate(regexPattern, options);
        }

        #endregion

        #region Tag系统

        /// <summary>
        /// 添加标签到实体
        /// </summary>
        /// <param name="entityId">实体 ID</param>
        /// <param name="tag">标签名称</param>
        /// <returns>操作结果</returns>
        public OperationResult AddTag(string entityId, string tag)
        {
            // 验证实体存在
            if (!_entities.ContainsKey(entityId))
            {
                return OperationResult.Failure(ErrorCode.NotFound, $"未找到 ID 为 '{entityId}' 的实体");
            }

            // 验证标签名称
            if (string.IsNullOrWhiteSpace(tag))
            {
                return OperationResult.Failure(ErrorCode.InvalidCategory, "标签名称不能为空");
            }

            tag = tag.Trim();

            AcquireTagWriteLock(tag);
            try
            {
                // 创建或获取标签集合
                if (!_tagIndex.TryGetValue(tag, out var entityIds))
                {
                    entityIds = new HashSet<string>();
                    _tagIndex[tag] = entityIds;
                }

                // 添加实体 ID
                entityIds.Add(entityId);

                // 清除相关缓存
                var cacheKey = $"tag:{tag}";
                _cacheStrategy.Invalidate(cacheKey);

                return OperationResult.Success();
            }
            finally
            {
                ReleaseTagLock(tag);
            }
        }

        /// <summary>
        /// 从实体移除标签
        /// </summary>
        /// <param name="entityId">实体 ID</param>
        /// <param name="tag">标签名称</param>
        /// <returns>操作结果</returns>
        public OperationResult RemoveTag(string entityId, string tag)
        {
            // 验证实体存在
            if (!_entities.ContainsKey(entityId))
            {
                return OperationResult.Failure(ErrorCode.NotFound, $"未找到 ID 为 '{entityId}' 的实体");
            }

            if (string.IsNullOrWhiteSpace(tag))
            {
                return OperationResult.Failure(ErrorCode.InvalidCategory, "标签名称不能为空");
            }

            tag = tag.Trim();

            AcquireTagWriteLock(tag);
            try
            {
                if (_tagIndex.TryGetValue(tag, out var entityIds))
                {
                    var removed = entityIds.Remove(entityId);
                    
                    if (removed)
                    {
                        // 如果标签已经没有实体了，可以删除这个标签
                        if (entityIds.Count == 0)
                        {
                            _tagIndex.Remove(tag);
                        }

                        // 清除相关缓存
                        var cacheKey = $"tag:{tag}";
                        _cacheStrategy.Invalidate(cacheKey);

                        return OperationResult.Success();
                    }
                }

                return OperationResult.Failure(ErrorCode.NotFound, $"实体 '{entityId}' 不具有标签 '{tag}'");
            }
            finally
            {
                ReleaseTagLock(tag);
            }
        }

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
                RecordCacheQuery(true); // 缓存命中
                return cachedResult;
            }
            
            RecordCacheQuery(false); // 缓存未命中

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
        /// <returns>元数据集合，如果不存在则返回新的空集合</returns>
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
        /// 获取实体元数据的操作结果版本
        /// </summary>
        /// <param name="id">实体 ID</param>
        /// <returns>元数据操作结果</returns>
        public OperationResult<CustomDataCollection> GetMetadataResult(string id)
        {
            if (!_entities.ContainsKey(id))
            {
                return OperationResult<CustomDataCollection>.Failure(
                    ErrorCode.NotFound, $"未找到 ID 为 '{id}' 的实体");
            }

            return OperationResult<CustomDataCollection>
                            .Success(_metadataStore.TryGetValue(id, out var metadata)
                ? metadata
                : new CustomDataCollection());
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

        #region 内部访问器

        /// <summary>
        /// 获取标签索引
        /// </summary>
        /// <returns>标签到实体 ID 集合的映射</returns>
        internal IReadOnlyDictionary<string, HashSet<string>> GetTagIndex()
        {
            return _tagIndex;
        }

        /// <summary>
        /// 获取元数据存储
        /// </summary>
        /// <returns>实体 ID 到元数据集合的映射</returns>
        internal IReadOnlyDictionary<string, CustomDataCollection> GetMetadataStore()
        {
            return _metadataStore;
        }

        /// <summary>
        /// 设置标签索引
        /// </summary>
        /// <param name="tagIndex">标签索引数据</param>
        internal void SetTagIndex(Dictionary<string, HashSet<string>> tagIndex)
        {
            _tagIndex.Clear();
            foreach (var kvp in tagIndex)
            {
                _tagIndex[kvp.Key] = kvp.Value;
            }
        }

        /// <summary>
        /// 设置元数据存储
        /// </summary>
        /// <param name="metadataStore">元数据存储数据</param>
        internal void SetMetadataStore(Dictionary<string, CustomDataCollection> metadataStore)
        {
            _metadataStore.Clear();
            foreach (var kvp in metadataStore)
            {
                _metadataStore[kvp.Key] = kvp.Value;
            }
        }

        #endregion

        #region 语法糖操作

        /// <summary>
        /// 将实体移动到新分类并规范化和验证新分类名称
        /// </summary>
        /// <param name="entityId">实体 ID</param>
        /// <param name="newCategory">新分类名称</param>
        /// <returns>操作结果</returns>
        public OperationResult MoveEntityToCategorySafe(string entityId, string newCategory)
        {
            // 验证并规范化新分类名称
            newCategory = CategoryNameNormalizer.Normalize(newCategory);
            return !CategoryNameNormalizer.IsValid(newCategory, out var errorMessage)
                ? OperationResult.Failure(ErrorCode.InvalidCategory, errorMessage)
                : MoveEntityToCategory(entityId, newCategory);
        }
        
        /// <summary>
        /// 将实体移动到新分类
        /// </summary>
        /// <param name="entityId">实体 ID</param>
        /// <param name="newCategory">新分类名称</param>
        /// <returns></returns>
        public OperationResult MoveEntityToCategory(string entityId, string newCategory)
        {
            // 验证实体存在
            if (!_entities.ContainsKey(entityId))
            {
                return OperationResult.Failure(ErrorCode.NotFound, $"未找到 ID 为 '{entityId}' 的实体");
            }
            
            _treeLock.EnterWriteLock();
            try
            {
                // 从旧分类中移除
                foreach (var kvp in _categoryTree.ToList())
                {
                    if (kvp.Value.EntityIds.Remove(entityId))
                    {
                        _categoryIndex[kvp.Key]?.Remove(entityId);
                    }
                }

                // 添加到新分类（自动创建）
                var node = GetOrCreateNode(newCategory);
                node.EntityIds.Add(entityId);

                if (!_categoryIndex.ContainsKey(newCategory))
                {
                    _categoryIndex[newCategory] = new HashSet<string>();
                }
                _categoryIndex[newCategory].Add(entityId);

                // 清除缓存
                _cacheStrategy.Clear();

                return OperationResult.Success();
            }
            finally
            {
                _treeLock.ExitWriteLock();
            }
        }

        public OperationResult RenameCategory(string oldName, string newName)
        {
             _treeLock.EnterWriteLock();
            try
            {
                // 检查旧分类是否存在
                if (!_categoryTree.TryGetValue(oldName, out var oldNode))
                {
                    return OperationResult.Failure(ErrorCode.NotFound, $"未找到分类 '{oldName}'");
                }

                // 检查新分类名称是否已存在
                if (_categoryTree.ContainsKey(newName))
                {
                    return OperationResult.Failure(ErrorCode.DuplicateId, $"分类 '{newName}' 已存在");
                }

                // 重命名节点及其所有子节点
                var descendants = oldNode.GetDescendants();
                var nodesToRename = new List<CategoryNode> { oldNode };
                nodesToRename.AddRange(descendants);

                // 从后往前重命名（先处理子节点）
                for (int i = nodesToRename.Count - 1; i >= 0; i--)
                {
                    var node = nodesToRename[i];
                    var oldFullPath = node.FullPath;
                    var newFullPath = oldFullPath.Replace(oldName, newName);

                    // 更新节点数据
                    var entityIds = new HashSet<string>(node.EntityIds);
                    
                    // 从旧路径移除
                    _categoryTree.Remove(oldFullPath);
                    _categoryIndex.Remove(oldFullPath);

                    // 创建新节点
                    var newNode = new CategoryNode(
                        newFullPath.Split('.').Last(),
                        newFullPath
                    );
                    
                    foreach (var id in entityIds)
                    {
                        newNode.EntityIds.Add(id);
                    }

                    // 重建父子关系
                    if (node.Parent != null)
                    {
                        var parentNewPath = node.Parent.FullPath.Replace(oldName, newName);
                        if (_categoryTree.TryGetValue(parentNewPath, out var newParent))
                        {
                            newParent.AddChild(newNode);
                        }
                    }

                    // 复制子节点引用
                    foreach (var child in node.Children.Values)
                    {
                        var childNewPath = child.FullPath.Replace(oldName, newName);
                        if (_categoryTree.TryGetValue(childNewPath, out var newChild))
                        {
                            newNode.AddChild(newChild);
                        }
                    }

                    // 添加到新路径
                    _categoryTree[newFullPath] = newNode;
                    _categoryIndex[newFullPath] = entityIds;
                }

                // 清除缓存
                _cacheStrategy.Clear();

                return OperationResult.Success();
            }
            finally
            {
                _treeLock.ExitWriteLock();
            }
        }
        
        
        /// <summary>
        /// 规范化分类名称和验证并重命名分类
        /// </summary>
        /// <param name="oldName">旧分类名称</param>
        /// <param name="newName">新分类名称</param>

        /// <returns>操作结果</returns>
        public OperationResult RenameCategorySafe(string oldName, string newName)
        {
            // 规范化分类名称
            oldName = CategoryNameNormalizer.Normalize(oldName);
            newName = CategoryNameNormalizer.Normalize(newName);

            // 验证新分类名称
            return !CategoryNameNormalizer.IsValid(newName, out var errorMessage)
                ? OperationResult.Failure(ErrorCode.InvalidCategory, errorMessage)
                : RenameCategory(oldName, newName);
        }

        #endregion

        #region 序列化支持

        /// <summary>
        /// 序列化为 JSON 字符串
        /// 使用 EasyPack 序列化服务
        /// </summary>
        /// <returns>JSON 字符串</returns>
        public string SerializeToJson()
        {
            var serializer = new CategoryManagerJsonSerializer<T>(_idExtractor);
            return serializer.SerializeToJson(this);
        }

        /// <summary>
        /// 从 JSON 字符串反序列化并替换当前实例的数据
        /// 注意：此方法会清空当前实例并加载新数据
        /// </summary>
        /// <param name="json">JSON 字符串</param>
        /// <returns>操作结果</returns>
        public OperationResult LoadFromJson(string json)
        {
            try
            {
                var serializer = new CategoryManagerJsonSerializer<T>(_idExtractor);
                var newManager = serializer.DeserializeFromJson(json);

                // 清空当前数据
                _treeLock.EnterWriteLock();
                try
                {
                    _entities.Clear();
                    _categoryTree.Clear();
                    _categoryIndex.Clear();
                    _tagIndex.Clear();
                    _metadataStore.Clear();
                    _cacheStrategy.Clear();

                    // 从新实例复制数据
                    foreach (var kvp in newManager._entities)
                    {
                        _entities[kvp.Key] = kvp.Value;
                    }
                    foreach (var kvp in newManager._categoryTree)
                    {
                        _categoryTree[kvp.Key] = kvp.Value;
                    }
                    foreach (var kvp in newManager._categoryIndex)
                    {
                        _categoryIndex[kvp.Key] = kvp.Value;
                    }
                    foreach (var kvp in newManager._tagIndex)
                    {
                        _tagIndex[kvp.Key] = kvp.Value;
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
                return OperationResult.Failure(ErrorCode.InvalidCategory, 
                    $"反序列化失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 从 JSON 字符串创建新的 CategoryManager 实例
        /// </summary>
        /// <param name="json">JSON 字符串</param>
        /// <param name="idExtractor">实体 ID 提取函数</param>
        /// <returns>新的 CategoryManager 实例</returns>
        public static CategoryManager<T> CreateFromJson(string json, Func<T, string> idExtractor)
        {
            var serializer = new CategoryManagerJsonSerializer<T>(idExtractor);
            return serializer.DeserializeFromJson(json);
        }

        #endregion

        #region 实体注册内部类

        /// <summary>
        /// 实体注册内部实现
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
                // 如果验证失败，直接返回
                if (!_validationResult.IsSuccess)
                {
                    return _validationResult;
                }

                try
                {
                    // 存储实体
                    _manager._entities[_entityId] = _entity;

                    // 创建或获取分类节点
                    _manager._treeLock.EnterWriteLock();
                    try
                    {
                        var node = _manager.GetOrCreateNode(_category);
                        node.EntityIds.Add(_entityId);

                        if (!_manager._categoryIndex.ContainsKey(_category))
                        {
                            _manager._categoryIndex[_category] = new HashSet<string>();
                        }
                        _manager._categoryIndex[_category].Add(_entityId);
                    }
                    finally
                    {
                        _manager._treeLock.ExitWriteLock();
                    }

                    // 处理标签
                    foreach (var tag in _tags)
                    {
                        _manager.AcquireTagWriteLock(tag);
                        try
                        {
                            if (!_manager._tagIndex.ContainsKey(tag))
                            {
                                _manager._tagIndex[tag] = new HashSet<string>();
                            }
                            _manager._tagIndex[tag].Add(_entityId);
                        }
                        finally
                        {
                            _manager.ReleaseTagLock(tag);
                        }
                    }

                    // 处理元数据
                    if (_metadata != null)
                    {
                        _manager._metadataStore[_entityId] = _metadata;
                    }

                    // 清除缓存
                    _manager._cacheStrategy.Clear();

                    return OperationResult.Success();
                }
                catch (Exception ex)
                {
                    return OperationResult.Failure(ErrorCode.ConcurrencyConflict, ex.Message);
                }
            }
        }

        #endregion

        #region 统计与监控

#if UNITY_EDITOR
        private Statistics _cachedStatistics;
        private long _lastStatisticsUpdate;
        private const long StatisticsCacheTimeout = 10000000; // 1秒的Ticks

        private long _totalCacheQueries;
        private long _cacheHits;
        private long _cacheMisses;

        /// <summary>
        /// 获取运行时统计信息（仅编辑器环境可用）
        /// </summary>
        /// <returns>统计信息对象</returns>
        public Statistics GetStatistics()
        {
            var currentTicks = DateTime.UtcNow.Ticks;
            
            // 如果缓存未过期，返回缓存的统计信息
            if (_cachedStatistics != null && (currentTicks - _lastStatisticsUpdate) < StatisticsCacheTimeout)
            {
                return _cachedStatistics;
            }

            _treeLock.EnterReadLock();
            try
            {
                // 计算最大分类深度
                int maxDepth = 0;
                foreach (var category in _categoryTree.Keys)
                {
                    int depth = category.Split('.').Length;
                    if (depth > maxDepth)
                    {
                        maxDepth = depth;
                    }
                }

                // TODO: 性能监控转为实际值
                // 计算内存使用（估算）
                long memoryUsage = 0;
                
                // 实体存储内存
                memoryUsage += _entities.Count * 64; // 估算每个实体引用64个字节
                
                // 分类树内存
                memoryUsage += _categoryTree.Count * 128; // 估算每个节点128个字节
                
                // 索引内存
                foreach (var kvp in _categoryIndex)
                {
                    memoryUsage += kvp.Value.Count * 16; // 估算每个索引项16个字节
                }
                
                // 标签索引内存
                foreach (var kvp in _tagIndex)
                {
                    memoryUsage += kvp.Value.Count * 16;
                }

                // 元数据存储内存
                memoryUsage += _metadataStore.Count * 64;

                // 计算缓存命中率
                float cacheHitRate = 0f;
                if (_totalCacheQueries > 0)
                {
                    cacheHitRate = (float)_cacheHits / _totalCacheQueries;
                }

                _cachedStatistics = new Statistics
                {
                    TotalEntities = _entities.Count,
                    TotalCategories = _categoryTree.Count,
                    TotalTags = _tagIndex.Count,
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
        /// 记录缓存查询结果（仅编辑器环境）
        /// </summary>
        /// <param name="isHit">是否命中缓存</param>
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
            
            // 统计信息缓存失效
            _cachedStatistics = null;
        }
#else
        /// <summary>
        /// TODO:获取运行时统计信息
        /// </summary>
        /// <returns>空统计信息对象</returns>
        public Statistics GetStatistics()
        {
            return new Statistics();
        }

        /// <summary>
        /// TODO:记录缓存查询
        /// </summary>
        /// <param name="isHit">是否命中缓存</param>
        private void RecordCacheQuery(bool isHit)
        {
            // 生产环境不记录统计信息
        }
#endif

        #endregion

        #region 对象池优化

        /// <summary>
        /// 使用对象池构建查询结果
        /// </summary>
        /// <param name="entityIds">实体 ID 集合</param>
        /// <returns>实体列表</returns>
        private List<T> BuildResultWithPool(IEnumerable<string> entityIds)
        {
            var result = CollectionPool.GetList<T>();
            
            foreach (var id in entityIds)
            {
                if (_entities.TryGetValue(id, out var entity))
                {
                    result.Add(entity);
                }
            }
            
            return result;
        }

        /// <summary>
        /// 归还列表到对象池
        /// </summary>
        /// <param name="list">要归还的列表</param>
        private void ReturnListToPool(List<T> list)
        {
            CollectionPool.ReturnList(list);
        }

        #endregion

        #region 缓存管理

        /// <summary>
        /// 清除所有缓存
        /// </summary>
        public void ClearCache()
        {
            _cacheStrategy.Clear();
        }

        #endregion

        #region 注销

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
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
        }

        #endregion
    }
}
