using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly Dictionary<int, HashSet<TKey>> _tagToEntityKeys; // tagId → entityKeys
        private readonly Dictionary<TKey, HashSet<int>> _entityToTagIds; // entityKey → tagIds
        private readonly Dictionary<int, List<T>> _tagCache;

        // 映射层
        private readonly IntegerMapper _tagMapper;

        // 锁
        private readonly ReaderWriterLockSlim _tagSystemLock;

        #region 增
        /// <summary>
        ///     为实体添加标签。
        /// </summary>
        public OperationResult AddTag(TKey key, string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
            {
                return OperationResult.Failure(ErrorCode.InvalidCategory, "标签名称不能为空");
            }

            _tagSystemLock.EnterWriteLock();
            try
            {
                if (!_entities.ContainsKey(key))
                {
                    return OperationResult.Failure(ErrorCode.NotFound, $"未找到键为 '{key}' 的实体");
                }

                AddTagInternalLocked(key, tag);
                return OperationResult.Success();
            }
            finally
            {
                _tagSystemLock.ExitWriteLock();
            }
        }


        /// <summary>
        ///     内部方法：在持有 _tagSystemLock 写锁的情况下添加标签。
        ///     用于批量操作中合并锁。
        /// </summary>
        private void AddTagInternalLocked(TKey key, string tag)
        {
            if (string.IsNullOrWhiteSpace(tag)) return;

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

            // 清除标签缓存
            _tagCache.Remove(tagId);
        }

        /// <summary>
        ///     内部方法：批量添加标签（已持有锁）。
        ///     在单个锁持有下添加多个标签，用于批量注册优化。
        /// </summary>
        private void AddTagsInternalLocked(TKey key, IEnumerable<string> tags)
        {
            if (tags == null) return;

            foreach (string tag in tags)
            {
                AddTagInternalLocked(key, tag);
            }
        }

        /// <summary>
        ///     为实体批量添加标签。
        /// </summary>
        public OperationResult AddTags(TKey key, params string[] tags)
        {
            if (tags == null || tags.Length == 0)
            {
                return OperationResult.Success();
            }

            // 单次锁持有批量添加
            _tagSystemLock.EnterWriteLock();
            try
            {
                if (!_entities.ContainsKey(key))
                {
                    return OperationResult.Failure(ErrorCode.NotFound, $"未找到键为 '{key}' 的实体");
                }

                AddTagsInternalLocked(key, tags);
            }
            finally
            {
                _tagSystemLock.ExitWriteLock();
            }

            return OperationResult.Success();
        }
        #endregion
        #region 删
        /// <summary>
        ///     从实体移除标签。
        /// </summary>
        public OperationResult RemoveTag(TKey key, string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
            {
                return OperationResult.Failure(ErrorCode.InvalidCategory, "标签名称不能为空");
            }

            _tagSystemLock.EnterWriteLock();
            try
            {
                if (!_entities.ContainsKey(key))
                {
                    return OperationResult.Failure(ErrorCode.NotFound, $"未找到键为 '{key}' 的实体");
                }

                if (!_tagMapper.TryGetId(tag, out int tagId))
                {
                    return OperationResult.Failure(ErrorCode.NotFound, $"标签 '{tag}' 不存在");
                }

                if (!_tagToEntityKeys.TryGetValue(tagId, out var entityKeys) ||
                    !entityKeys.Remove(key))
                {
                    return OperationResult.Failure(ErrorCode.NotFound, $"实体不包含标签 '{tag}'");
                }

                if (_entityToTagIds.TryGetValue(key, out var tagIds))
                {
                    tagIds.Remove(tagId);
                }

                if (entityKeys.Count == 0)
                {
                    _tagToEntityKeys.Remove(tagId);
                }

                // 清除标签缓存
                _tagCache.Remove(tagId);

                return OperationResult.Success();
            }
            finally
            {
                _tagSystemLock.ExitWriteLock();
            }
        }
        #endregion
        #region 查
        /// <summary>
        ///     检查实体是否拥有指定标签。
        /// </summary>
        public bool HasTag(TKey key, string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
            {
                return false;
            }

            if (!_entities.ContainsKey(key))
            {
                return false;
            }

            _tagSystemLock.EnterReadLock();
            try
            {
                if (!_tagMapper.TryGetId(tag, out int tagId))
                {
                    return false;
                }

                return _entityToTagIds.TryGetValue(key, out var tagIds) && tagIds.Contains(tagId);
            }
            finally
            {
                _tagSystemLock.ExitReadLock();
            }
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
            if (!_entities.ContainsKey(key))
            {
                return Array.Empty<string>();
            }

            _tagSystemLock.EnterReadLock();
            try
            {
                if (!_entityToTagIds.TryGetValue(key, out var tagIds))
                {
                    return Array.Empty<string>();
                }

                var tags = new List<string>(tagIds.Count);
                foreach (int tagId in tagIds)
                {
                    if (_tagMapper.TryGetString(tagId, out string tagName)) tags.Add(tagName);
                }

                return tags;
            }
            finally
            {
                _tagSystemLock.ExitReadLock();
            }
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

            // 先尝试读取缓存
            _tagSystemLock.EnterReadLock();
            try
            {
                if (!_tagMapper.TryGetId(tag, out int tagId))
                {
                    return new List<T>();
                }

                // 检查缓存
                if (_tagCache.TryGetValue(tagId, out var cachedResult))
                {
                    RecordCacheQuery(true);
                    return cachedResult;
                }
            }
            finally
            {
                _tagSystemLock.ExitReadLock();
            }

            // 缓存未命中，需要写入缓存
            _tagSystemLock.EnterWriteLock();
            try
            {
                if (!_tagMapper.TryGetId(tag, out int tagId))
                {
                    return new List<T>();
                }

                // 双重检查缓存（可能在等待写锁时已被其他线程填充）
                if (_tagCache.TryGetValue(tagId, out var cachedResult))
                {
                    RecordCacheQuery(true);
                    return cachedResult;
                }

                RecordCacheQuery(false);

                if (_tagToEntityKeys.TryGetValue(tagId, out var entityKeys))
                {
                    var result = new List<T>(entityKeys.Count);
                    foreach (TKey k in entityKeys)
                    {
                        if (_entities.TryGetValue(k, out T entity) && entity != null)
                        {
                            result.Add(entity);
                        }
                    }

                    _tagCache[tagId] = result;
                    return result;
                }

                return new List<T>();
            }
            finally
            {
                _tagSystemLock.ExitWriteLock();
            }
        }

        /// <summary>
        ///     按多个标签查询实体。
        /// </summary>
        public IReadOnlyList<T> GetByTags(string[] tags, bool matchAll = true)
        {
            if (tags == null || tags.Length == 0) return new List<T>();

            HashSet<TKey> resultKeys = null;

            _tagSystemLock.EnterReadLock();
            try
            {
                foreach (string tag in tags)
                {
                    if (string.IsNullOrWhiteSpace(tag)) continue;

                    if (!_tagMapper.TryGetId(tag, out int tagId)) continue;

                    if (!_tagToEntityKeys.TryGetValue(tagId, out var entityKeys)) continue;

                    if (resultKeys == null)
                    {
                        resultKeys = new(entityKeys, _keyComparer);
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
            }
            finally
            {
                _tagSystemLock.ExitReadLock();
            }

            if (resultKeys == null || resultKeys.Count == 0)
            {
                return new List<T>();
            }

            var result = new List<T>(resultKeys.Count);
            foreach (TKey k in resultKeys)
            {
                if (_entities.TryGetValue(k, out T entity) && entity != null)
                {
                    result.Add(entity);
                }
            }

            return result;
        }

        /// <summary>
        ///     在持有 <see cref="_tagSystemLock"/> 写锁的情况下，从标签系统中移除某个实体的所有关联。
        ///     用于 DeleteEntity / DeleteCategoryRecursive 等批量清理，避免循环内反复加锁。
        /// </summary>
        private void RemoveEntityFromTagSystemLocked(TKey key)
        {
            if (!_entityToTagIds.TryGetValue(key, out var tagIds)) return;

            // 复制一份，避免在遍历期间修改集合引发异常
            //（HashSet 的枚举在集合被修改时会抛异常）
            var tagIdSnapshot = tagIds.ToArray();
            foreach (int tagId in tagIdSnapshot)
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

        #region 缓存
                /// <summary>
        ///     失效特定标签的缓存。
        /// </summary>
        /// <param name="tag">标签名称。</param>
        public void InvalidateTagCache(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return;

            if (_tagMapper.TryGetId(tag, out int tagId))
            {
                _tagCache.Remove(tagId);
            }
        }

        #endregion
    }
}