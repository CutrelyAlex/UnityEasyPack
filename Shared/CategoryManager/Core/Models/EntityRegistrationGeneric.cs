using System;
using System.Collections.Generic;
using EasyPack.CustomData;

namespace EasyPack.Category
{
    public partial class CategoryManager<T, TKey> where TKey : IEquatable<TKey>
    {
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
                {
                    return OperationResult.Failure(ErrorCode.InvalidCategory, errorMessage);
                }

                try
                {
                    // 存储实体（并发集合：TryAdd 用于避免覆盖）
                    if (!_manager._entities.TryAdd(_entityKey, _entity))
                    {
                        return OperationResult.Failure(ErrorCode.DuplicateId, $"实体键 '{_entityKey}' 已存在");
                    }

                    // 创建分类节点并关联
                    _manager._treeLock.EnterWriteLock();
                    try
                    {
                        CategoryNode node = _manager.GetOrCreateNode(normalizedCategory);
                        _manager._entityKeyToNode[_entityKey] = node;

                        // 维护反向索引
                        if (!_manager._nodeToEntityKeys.TryGetValue(node.TermId, out var nodeEntityKeys))
                        {
                            nodeEntityKeys = new(_manager._keyComparer);
                            _manager._nodeToEntityKeys[node.TermId] = nodeEntityKeys;
                        }

                        nodeEntityKeys.Add(_entityKey);
                    }
                    finally
                    {
                        _manager._treeLock.ExitWriteLock();
                    }

                    // 单次锁持有批量添加所有标签
                    if (_tags.Count > 0)
                    {
                        _manager._tagSystemLock.EnterWriteLock();
                        try
                        {
                            _manager.AddTagsInternalLocked(_entityKey, _tags);
                        }
                        finally
                        {
                            _manager._tagSystemLock.ExitWriteLock();
                        }
                    }

                    // 添加元数据
                    if (_metadata != null)
                    {
                        _manager._metadataStore[_entityKey] = _metadata;
                    }

#if UNITY_EDITOR
                    _manager._cachedStatistics = null;
#endif

                    return OperationResult.Success();
                }
                catch (Exception ex)
                {
                    return OperationResult.Failure(ErrorCode.NotFound, ex.Message);
                }
            }
        }

        #endregion
    }
}