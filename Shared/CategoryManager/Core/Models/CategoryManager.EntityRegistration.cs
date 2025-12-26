using System;
using System.Collections.Generic;
using EasyPack.CustomData;

namespace EasyPack.Category
{
    public partial class CategoryManager<T, TKey> where TKey : IEquatable<TKey>
    {
        #region 内部类

        /// <summary>
        ///     泛型实体注册内部类（用于 RegisterEntitySafeWithKey）。
        /// </summary>
        private class EntityRegistration : IEntityRegistration<T, TKey>
        {
            private readonly CategoryManager<T, TKey> _manager;
            private readonly T _entity;
            private readonly TKey _entityKey;
            private readonly string _category;
            private readonly List<string> _tags;
            private CustomDataCollection _metadata;

            public EntityRegistration(
                CategoryManager<T, TKey> manager,
                T entity,
                TKey key,
                string category)
            {
                _manager = manager;
                _entity = entity;
                _entityKey = key;
                _category = category;
                _tags = new();
            }

            public IEntityRegistration<T, TKey> WithTags(params string[] tags)
            {
                if (tags != null)
                {
                    _tags.AddRange(tags);
                }

                return this;
            }

            public IEntityRegistration<T, TKey> WithMetadata(CustomDataCollection metadata)
            {
                _metadata = metadata;
                return this;
            }

            public OperationResult Complete()
            {
                if (string.IsNullOrEmpty(_category))
                {
                    return OperationResult.Failure(ErrorCode.InvalidCategory, "分类不能为空");
                }

                string normalizedCategory = CategoryNameNormalizer.Normalize(_category);

                try
                {
                    // 存储实体
                    if (!_manager._entities.TryAdd(_entityKey, _entity))
                    {
                        return OperationResult.Failure(ErrorCode.DuplicateId,
                            $"键为 '{_entityKey}' 的实体已存在");
                    }

                    // 创建分类节点并关联
                    _manager._treeLock.EnterWriteLock();
                    try
                    {
                        CategoryNode node = _manager.GetOrCreateNode(normalizedCategory);
                        // 只使用 _entityKeyToNode 来跟踪实体键到节点的映射
                        _manager._entityKeyToNode[_entityKey] = node;
                    }
                    finally
                    {
                        _manager._treeLock.ExitWriteLock();
                    }

                    // 添加标签
                    foreach (string tag in _tags)
                    {
                        if (!string.IsNullOrWhiteSpace(tag)) _manager.AddTagInternal(_entityKey, tag);
                    }

                    // 存储元数据
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
                    return OperationResult.Failure(ErrorCode.ConcurrencyConflict, ex.Message);
                }
            }
        }

        #endregion
    }
}