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
    public partial class CategoryManager<T, TKey> : ICategoryManager<T, TKey>
        where TKey : IEquatable<TKey>
    {
         // 分类树
        private readonly Dictionary<int, CategoryNode> _categoryNodes;
        private readonly Dictionary<string, int> _categoryNameToId;
        private readonly Dictionary<int, string> _categoryIdToName;

        // 反向索引缓存
        private readonly Dictionary<TKey, CategoryNode> _entityKeyToNode; // entityKey → 所属分类节点
        private readonly Dictionary<int, HashSet<TKey>> _nodeToEntityKeys; // nodeId → entityKeys

        // 映射层
        private readonly IntegerMapper _categoryTermMapper;

        // 锁
        private readonly ReaderWriterLockSlim _treeLock;
        private readonly ReaderWriterLockSlim _entitiesLock;

        #region 增

        #endregion

        #region 删
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
                {
                    return OperationResult.Failure(ErrorCode.NotFound, $"未找到分类 '{category}'");
                }

                // 使用反向索引获取该分类下直接的实体键 (O(1))
                var entityKeysToRemove = new List<TKey>();
                if (_nodeToEntityKeys.TryGetValue(nodeId, out var nodeEntityKeys))
                {
                    entityKeysToRemove.AddRange(nodeEntityKeys);
                }

                // 从分类节点和反向索引移除这些实体的关联
                foreach (TKey key in entityKeysToRemove)
                {
                    _entityKeyToNode.Remove(key);
                }

                _nodeToEntityKeys.Remove(nodeId);

                // 清除分类映射
                _categoryNodes.Remove(nodeId);
                _categoryNameToId.Remove(category);
                _categoryIdToName.Remove(nodeId);

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
                {
                    return OperationResult.Failure(ErrorCode.NotFound, $"未找到分类 '{category}'");
                }

                // 收集所有要删除的节点（包括自身及所有后代）
                var nodesToDelete = new List<(int Id, CategoryNode Node)> { (nodeId, node) };
                var pathSet = new HashSet<string> { category };
                CollectDescendants(node, nodesToDelete, pathSet);

                // 收集所有相关实体的键
                var entityKeysToDelete = new List<TKey>();
                foreach ((int id, CategoryNode _) in nodesToDelete)
                {
                    if (_nodeToEntityKeys.TryGetValue(id, out var nodeEntityKeys))
                    {
                        entityKeysToDelete.AddRange(nodeEntityKeys);
                        _nodeToEntityKeys.Remove(id);
                    }
                }

                // 从 _entityKeyToNode 移除
                foreach (TKey key in entityKeysToDelete)
                {
                    _entityKeyToNode.Remove(key);
                }

                // 删除所有相关实体（从实体存储、标签系统、元数据存储中移除）
                foreach (TKey key in entityKeysToDelete)
                {
                    // 从标签系统移除
                    _tagSystemLock.EnterWriteLock();
                    try
                    {
                        if (_entityToTagIds.TryGetValue(key, out var tagIds))
                        {
                            foreach (int tagId in tagIds)
                            {
                                if (_tagToEntityKeys.TryGetValue(tagId, out var entityKeys))
                                {
                                    entityKeys.Remove(key);
                                    if (entityKeys.Count == 0) _tagToEntityKeys.Remove(tagId);
                                }

                                // 清除标签缓存
                                _tagCache.Remove(tagId);
                            }

                            _entityToTagIds.Remove(key);
                        }
                    }
                    finally
                    {
                        _tagSystemLock.ExitWriteLock();
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
                }

                // 删除所有收集的节点
                foreach ((int id, CategoryNode _) in nodesToDelete)
                {
                    _categoryNodes.Remove(id);
                    if (_categoryIdToName.TryGetValue(id, out string path))
                    {
                        _categoryNameToId.Remove(path);
                        _categoryIdToName.Remove(id);
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
        ///     递归收集所有后代节点。
        /// </summary>
        private void CollectDescendants(CategoryNode node, List<(int Id, CategoryNode Node)> result,
                                        HashSet<string> pathSet)
        {
            foreach (CategoryNode child in node.Children)
            {
                if (_categoryIdToName.TryGetValue(child.TermId, out string path) && pathSet.Add(path))
                {
                    result.Add((child.TermId, child));
                    CollectDescendants(child, result, pathSet);
                }
            }
        }
        #endregion

        #region 查
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
                return entityKeys.Select(k => _entities.GetValueOrDefault(k))
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
            int targetNodeId = node.TermId;

            if (_nodeToEntityKeys.TryGetValue(targetNodeId, out var nodeEntityKeys))
            {
                foreach (TKey key in nodeEntityKeys)
                {
                    keys.Add(key);
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
                return entityKeys.Select(k => _entities.GetValueOrDefault(k))
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
                return !_entityKeyToNode.TryGetValue(key, out CategoryNode node)
                    ? string.Empty
                    : GetNodeReadablePath(node);
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
                    {
                        parts.Add(segments[^1]);
                    }
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
            return _categoryIdToName.TryGetValue(node.TermId, out string path) ? path : string.Empty;
        }

        /// <summary>
        ///     检查实体是否在指定分类中。
        /// </summary>
        public bool IsInCategory(TKey key, string category, bool includeChildren = false)
        {
            if (string.IsNullOrEmpty(category))
            {
                return false;
            }

            if (!_categoryTermMapper.TryGetId(category, out int targetCategoryId))
            {
                return false;
            }

            _treeLock.EnterReadLock();
            try
            {
                if (!_entityKeyToNode.TryGetValue(key, out CategoryNode entityNode))
                {
                    return false;
                }

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
                        {
                            return true;
                        }

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
                    .Select(n => _categoryIdToName.GetValueOrDefault(n.TermId))
                    .Where(n => n != null)
                    .ToList();
            }
            finally
            {
                _treeLock.ExitReadLock();
            }
        }
        #endregion

        #region 跨分类移动

        /// <summary>
        ///     将实体移动到新分类，自动验证分类名称。
        /// </summary>
        /// <param name="key">实体键。</param>
        /// <param name="newCategory">新的分类名称。</param>
        /// <returns>操作结果。</returns>
        public OperationResult MoveEntityToCategorySafe(TKey key, string newCategory)
        {
            newCategory = CategoryNameNormalizer.Normalize(newCategory);
            return !CategoryNameNormalizer.IsValid(newCategory, out string errorMessage)
                ? OperationResult.Failure(ErrorCode.InvalidCategory, errorMessage)
                : MoveEntityToCategory(key, newCategory);
        }

        /// <summary>
        ///     将实体从当前分类移动到新的分类。
        /// </summary>
        /// <param name="key">实体键。</param>
        /// <param name="newCategory">新的分类名称。</param>
        /// <returns>操作结果。</returns>
        public OperationResult MoveEntityToCategory(TKey key, string newCategory)
        {
            _entitiesLock.EnterReadLock();
            try
            {
                if (!_entities.ContainsKey(key))
                {
                    return OperationResult.Failure(ErrorCode.NotFound, $"未找到键为 '{key}' 的实体");
                }
            }
            finally
            {
                _entitiesLock.ExitReadLock();
            }

            _treeLock.EnterWriteLock();
            try
            {
                // 从旧分类的反向索引移除
                if (_entityKeyToNode.TryGetValue(key, out CategoryNode oldNode))
                {
                    if (_nodeToEntityKeys.TryGetValue(oldNode.TermId, out var oldNodeEntityKeys))
                    {
                        oldNodeEntityKeys.Remove(key);
                        if (oldNodeEntityKeys.Count == 0) _nodeToEntityKeys.Remove(oldNode.TermId);
                    }
                }

                // 从旧分类移除
                _entityKeyToNode.Remove(key);

                // 添加到新分类
                CategoryNode node = GetOrCreateNode(newCategory);
                _entityKeyToNode[key] = node;

                // 维护新分类的反向索引
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
                foreach ((int id, _, string oldPath, string newPath) in nodesToRename)
                {
                    _categoryNameToId.Remove(oldPath);
                    _categoryIdToName[id] = newPath;
                    _categoryNameToId[newPath] = id;

                    // 更新 TermMapper 映射（从旧路径到新路径）
                    _categoryTermMapper.RemapTerm(oldPath, newPath);
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
        ///     递归收集所有需要重命名的节点。
        /// </summary>
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


        #region 节点操作

        /// <summary>
        ///     获取或创建分类节点。
        /// </summary>
        private CategoryNode GetOrCreateNode(string fullPath)
        {
            fullPath = CategoryNameNormalizer.Normalize(fullPath);

            if (_categoryNameToId.TryGetValue(fullPath, out int existingId) &&
                _categoryNodes.TryGetValue(existingId, out CategoryNode existingNode))
            {
                return existingNode;
            }

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
                node = new(newCategoryId);
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

        /// <summary>
        ///     重建所有子树索引。
        /// </summary>
        public OperationResult RebuildSubtreeIndices()
        {
            try
            {
                _treeLock.EnterWriteLock();
                try
                {
                    // 获取所有根节点（没有父节点的节点）
                    var rootNodes = _categoryNodes.Values.Where(n => n.ParentNode == null).ToList();

                    foreach (CategoryNode root in rootNodes)
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
        ///     递归重建节点及子树。
        /// </summary>
        private static void RebuildSubtreeIndices_Recursive(CategoryNode node)
        {
            foreach (CategoryNode child in node.Children)
            {
                RebuildSubtreeIndices_Recursive(child);
            }
        }

        #endregion
    }
}