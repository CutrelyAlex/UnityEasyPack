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
        // 元数据存储
        private readonly Dictionary<TKey, CustomDataCollection> _metadataStore;
        private readonly ReaderWriterLockSlim _metadataLock;
        #region 查询
        
        /// <summary>
        ///     获取实体的元数据。
        /// </summary>
        public CustomDataCollection GetMetadata(TKey key)
        {
            _entitiesLock.EnterReadLock();
            try
            {
                if (!_entities.ContainsKey(key)) return new();
            }
            finally
            {
                _entitiesLock.ExitReadLock();
            }

            _metadataLock.EnterReadLock();
            try
            {
                return _metadataStore.TryGetValue(key, out CustomDataCollection metadata)
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
        /// <param name="key">实体键。</param>
        /// <returns>包含元数据的操作结果。</returns>
        public OperationResult<CustomDataCollection> GetMetadataResult(TKey key)
        {
            _entitiesLock.EnterReadLock();
            try
            {
                if (!_entities.ContainsKey(key))
                {
                    return OperationResult<CustomDataCollection>.Failure(
                        ErrorCode.NotFound, $"未找到键为 '{key}' 的实体");
                }
            }
            finally
            {
                _entitiesLock.ExitReadLock();
            }

            _metadataLock.EnterReadLock();
            try
            {
                return OperationResult<CustomDataCollection>.Success(
                    _metadataStore.TryGetValue(key, out CustomDataCollection metadata)
                        ? metadata
                        : new());
            }
            finally
            {
                _metadataLock.ExitReadLock();
            }
        }

        #endregion
        
        #region 修改
        /// <summary>
        ///     更新实体的元数据。
        /// </summary>
        public OperationResult UpdateMetadata(TKey key, CustomDataCollection metadata)
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

        public OperationResult DeleteMetadata(TKey key)
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

            _metadataLock.EnterWriteLock();
            try
            {
                _metadataStore.Remove(key);
                return OperationResult.Success();
            }
            finally
            {
                _metadataLock.ExitWriteLock();
            }
        }
        #endregion
    }
}