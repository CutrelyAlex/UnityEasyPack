using System;
using System.Collections.Concurrent;
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
        // 元数据存储的并发集合
        private readonly ConcurrentDictionary<TKey, CustomDataCollection> _metadataStore;

        #region 查询

        /// <summary>
        ///     检查实体是否拥有元数据。
        /// </summary>
        public bool HasMetadata(TKey key)
        {
            return _entities.ContainsKey(key) && _metadataStore.ContainsKey(key);
        }

        /// <summary>
        ///     获取实体的元数据。
        /// </summary>
        public CustomDataCollection GetMetadata(TKey key)
        {
            // 如果实体不存在，返回一个新的空集合
            if (!_entities.ContainsKey(key)) return new CustomDataCollection();

            return _metadataStore.TryGetValue(key, out CustomDataCollection metadata)
                ? metadata
                : new CustomDataCollection();
        }

        /// <summary>
        ///     获取或创建实体的元数据。
        /// </summary>
        public CustomDataCollection GetOrAddMetadata(TKey key)
        {
            if (!_entities.ContainsKey(key))
            {
                UnityEngine.Debug.LogWarning($"[CategoryManager] 尝试为不存在的实体获取/创建元数据: {key}");
                return null;
            }

            return _metadataStore.GetOrAdd(key, _ => new CustomDataCollection());
        }

        /// <summary>
        ///     获取实体的元数据。
        /// </summary>
        /// <param name="key">实体键。</param>
        /// <returns>包含元数据的操作结果。</returns>
        public OperationResult<CustomDataCollection> GetMetadataResult(TKey key)
        {
            if (!_entities.ContainsKey(key))
            {
                return OperationResult<CustomDataCollection>.Failure(
                    ErrorCode.NotFound, $"未找到键为 '{key}' 的实体");
            }

            return OperationResult<CustomDataCollection>.Success(
                _metadataStore.TryGetValue(key, out CustomDataCollection metadata)
                    ? metadata
                    : new CustomDataCollection());
        }
        
        #endregion

        #region 修改
        /// <summary>
        ///     更新实体的元数据。
        /// </summary>
        public OperationResult UpdateMetadata(TKey key, CustomDataCollection metadata)
        {
            if (!_entities.ContainsKey(key))
            {
                return OperationResult.Failure(ErrorCode.NotFound, $"未找到键为 '{key}' 的实体");
            }

            _metadataStore[key] = metadata;
            return OperationResult.Success();
        }

        public OperationResult DeleteMetadata(TKey key)
        {
            if (!_entities.ContainsKey(key))
            {
                return OperationResult.Failure(ErrorCode.NotFound, $"未找到键为 '{key}' 的实体");
            }

            _metadataStore.TryRemove(key, out _);
            return OperationResult.Success();
        }
        #endregion
    }
}