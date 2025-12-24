using System;
using System.Collections.Generic;
using EasyPack.CustomData;
using EasyPack.Category;

namespace EasyPack.Category
{
    /// <summary>
    ///     双泛型分类管理器接口
    ///     <para>T: 实体类型</para>
    ///     <para>TKey: 键类型（如 int, string, Guid 等）</para>
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    /// <typeparam name="TKey">键类型，用于唯一标识实体</typeparam>
    public interface ICategoryManager<T, TKey> : ICategoryManager
        where TKey : IEquatable<TKey>
    {
        #region 实体注册

        /// <summary>
        ///     注册实体到指定分类，支持链式调用。
        /// </summary>
        /// <param name="entity">要注册的实体。</param>
        /// <param name="category">目标分类名称。</param>
        /// <returns>链式注册对象。</returns>
        IEntityRegistration RegisterEntity(T entity, string category);

        /// <summary>
        ///     注册实体到指定分类，自动验证分类名称格式。
        /// </summary>
        /// <param name="entity">要注册的实体。</param>
        /// <param name="category">目标分类名称（将自动规范化）。</param>
        /// <returns>链式注册对象。</returns>
        IEntityRegistration RegisterEntitySafe(T entity, string category);

        /// <summary>
        ///     使用显式键注册实体到指定分类。
        /// </summary>
        /// <param name="key">实体键。</param>
        /// <param name="entity">要注册的实体。</param>
        /// <param name="category">目标分类名称。</param>
        /// <returns>操作结果。</returns>
        OperationResult RegisterEntity(TKey key, T entity, string category);

        /// <summary>
        ///     使用显式键注册实体并添加标签。
        /// </summary>
        /// <param name="key">实体键。</param>
        /// <param name="entity">要注册的实体。</param>
        /// <param name="category">目标分类名称。</param>
        /// <param name="tags">标签列表。</param>
        /// <returns>操作结果。</returns>
        OperationResult RegisterEntityWithTags(TKey key, T entity, string category, params string[] tags);

        /// <summary>
        ///     使用显式键注册实体并添加元数据。
        /// </summary>
        /// <param name="key">实体键。</param>
        /// <param name="entity">要注册的实体。</param>
        /// <param name="category">目标分类名称。</param>
        /// <param name="metadata">元数据。</param>
        /// <returns>操作结果。</returns>
        OperationResult RegisterEntityWithMetadata(TKey key, T entity, string category, CustomDataCollection metadata);

        /// <summary>
        ///     更新实体的引用。
        ///     如果键不存在，则返回失败。
        ///     此操作不会改变实体的分类、标签或元数据。
        /// </summary>
        /// <param name="key">实体键。</param>
        /// <param name="entity">新的实体对象。</param>
        /// <returns>操作结果。</returns>
        OperationResult UpdateEntityReference(TKey key, T entity);

        /// <summary>
        ///     批量注册多个实体到同一分类。
        /// </summary>
        /// <param name="entities">实体列表。</param>
        /// <param name="category">目标分类名称。</param>
        /// <returns>批量操作结果。</returns>
        // TODO: 双泛型系统暂未完全支持批量操作的错误回滚机制，可能无法保证批量操作的原子性
        BatchOperationResult RegisterBatch(List<T> entities, string category);

        /// <summary>
        ///     删除实体。
        /// </summary>
        /// <param name="key">实体键。</param>
        /// <returns>操作结果。</returns>
        OperationResult DeleteEntity(TKey key);

        #endregion

        #region 分类查询

        /// <summary>
        ///     获取当前 Manager 内的所有实体快照。
        ///     <para>注意：返回的是快照列表，不是实时引用。</para>
        /// </summary>
        /// <returns>所有实体列表（快照）。</returns>
        IReadOnlyList<T> GetAllEntities();

        /// <summary>
        ///     根据分类获取实体（支持通配符）。
        /// </summary>
        /// <param name="pattern">分类名称或通配符模式。</param>
        /// <param name="includeChildren">是否包含子分类中的实体。</param>
        /// <returns>匹配的实体列表。</returns>
        IReadOnlyList<T> GetByCategory(string pattern, bool includeChildren = false);

        /// <summary>
        ///     根据键获取实体。
        /// </summary>
        /// <param name="key">实体键。</param>
        /// <returns>包含实体的操作结果。</returns>
        OperationResult<T> GetById(TKey key);

        /// <summary>
        ///     根据正则表达式匹配分类名称并获取实体。
        /// </summary>
        /// <param name="pattern">正则表达式模式。</param>
        /// <param name="includeChildren">是否包含子分类中的实体。</param>
        /// <returns>匹配分类中的实体列表。</returns>
        IReadOnlyList<T> GetByCategoryRegex(string pattern, bool includeChildren = false);

        /// <summary>
        ///     根据实体键获取其所在分类的可读路径字符串。
        /// </summary>
        /// <param name="key">实体键。</param>
        /// <returns>分类路径字符串。</returns>
        string GetReadableCategoryPath(TKey key);

        /// <summary>
        ///     根据路径 ID 数组转换为可读的分类路径字符串。
        /// </summary>
        /// <param name="pathIds">路径 ID 数组。</param>
        /// <returns>可读的分类路径字符串。</returns>
        string GetReadablePathFromIds(int[] pathIds);

        /// <summary>
        ///     检查实体是否在指定分类中。
        /// </summary>
        /// <param name="key">实体键。</param>
        /// <param name="category">分类名称。</param>
        /// <param name="includeChildren">是否检查子分类。</param>
        /// <returns>如果实体在该分类中返回 true。</returns>
        bool IsInCategory(TKey key, string category, bool includeChildren = false);

        /// <summary>
        ///     检查实体是否在指定分类中（基于实体对象）。
        /// </summary>
        /// <param name="entity">实体对象。</param>
        /// <param name="category">分类名称。</param>
        /// <param name="includeChildren">是否检查子分类。</param>
        /// <returns>如果实体在该分类中返回 true。</returns>
        bool IsInCategory(T entity, string category, bool includeChildren = false);

        #endregion

        #region 标签管理

        /// <summary>
        ///     为实体添加标签。
        /// </summary>
        /// <param name="key">实体键。</param>
        /// <param name="tag">标签名称。</param>
        /// <returns>操作结果。</returns>
        OperationResult AddTag(TKey key, string tag);

        /// <summary>
        ///     为实体批量添加标签。
        /// </summary>
        /// <param name="key">实体键。</param>
        /// <param name="tags">标签名称数组。</param>
        /// <returns>操作结果。</returns>
        OperationResult AddTags(TKey key, params string[] tags);

        /// <summary>
        ///     从实体移除标签。
        /// </summary>
        /// <param name="key">实体键。</param>
        /// <param name="tag">标签名称。</param>
        /// <returns>操作结果。</returns>
        OperationResult RemoveTag(TKey key, string tag);

        /// <summary>
        ///     检查实体是否拥有指定标签。
        /// </summary>
        /// <param name="key">实体键。</param>
        /// <param name="tag">标签名称。</param>
        /// <returns>如果实体拥有该标签返回 true。</returns>
        bool HasTag(TKey key, string tag);

        /// <summary>
        ///     检查实体是否拥有指定标签（基于实体对象）。
        /// </summary>
        /// <param name="entity">实体对象。</param>
        /// <param name="tag">标签名称。</param>
        /// <returns>如果实体拥有该标签返回 true。</returns>
        bool HasTag(T entity, string tag);

        /// <summary>
        ///     获取实体的所有标签。
        /// </summary>
        /// <param name="key">实体键。</param>
        /// <returns>标签列表。</returns>
        IReadOnlyList<string> GetEntityTags(TKey key);

        /// <summary>
        ///     获取实体的所有标签（基于实体对象）。
        /// </summary>
        /// <param name="entity">实体对象。</param>
        /// <returns>标签列表。</returns>
        IReadOnlyList<string> GetTags(T entity);

        /// <summary>
        ///     根据标签获取实体。
        /// </summary>
        /// <param name="tag">标签名称。</param>
        /// <returns>匹配的实体列表。</returns>
        IReadOnlyList<T> GetByTag(string tag);

        /// <summary>
        ///     根据多个标签获取实体。
        /// </summary>
        /// <param name="tags">标签数组。</param>
        /// <param name="matchAll">true 为 AND（交集），false 为 OR（并集）。</param>
        /// <returns>匹配的实体列表。</returns>
        IReadOnlyList<T> GetByTags(string[] tags, bool matchAll = true);

        /// <summary>
        ///     根据分类和标签获取实体交集。
        /// </summary>
        /// <param name="category">分类名称。</param>
        /// <param name="tag">标签名称。</param>
        /// <param name="includeChildren">是否包含子分类。</param>
        /// <returns>同时满足条件的实体列表。</returns>
        IReadOnlyList<T> GetByCategoryAndTag(string category, string tag, bool includeChildren = true);

        #endregion

        #region 元数据

        /// <summary>
        ///     获取实体的元数据。
        /// </summary>
        /// <param name="key">实体键。</param>
        /// <returns>元数据集合。</returns>
        CustomDataCollection GetMetadata(TKey key);

        /// <summary>
        ///     更新实体的元数据。
        /// </summary>
        /// <param name="key">实体键。</param>
        /// <param name="metadata">新的元数据集合。</param>
        /// <returns>操作结果。</returns>
        OperationResult UpdateMetadata(TKey key, CustomDataCollection metadata);

        #endregion

        #region 序列化支持

        /// <summary>
        ///     获取可序列化的状态对象。
        /// </summary>
        /// <param name="entitySerializer">实体序列化函数。</param>
        /// <param name="keySerializer">键序列化函数。</param>
        /// <param name="metadataSerializer">元数据序列化函数。</param>
        /// <returns>可序列化的状态对象。</returns>
        SerializableCategoryManagerState<T, TKey> GetSerializableState(Func<T, string> entitySerializer, Func<TKey, string> keySerializer, Func<CustomDataCollection, string> metadataSerializer);

        #endregion
    }
}