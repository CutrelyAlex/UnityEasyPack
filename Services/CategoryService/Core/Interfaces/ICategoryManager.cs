using System;
using System.Collections.Generic;
using EasyPack.CustomData;

namespace EasyPack.Category
{
    /// <summary>
    ///     分类管理器接口
    ///     定义分类管理器的核心功能契约
    /// </summary>
    public interface ICategoryManager : IDisposable
    {
        /// <summary>
        ///     获取实体类型
        /// </summary>
        Type EntityType { get; }

        /// <summary>
        ///     获取所有已注册的分类名称
        /// </summary>
        IReadOnlyList<string> GetCategoriesNodes();

        /// <summary>
        ///     清除所有缓存
        /// </summary>
        void ClearCache();

        /// <summary>
        ///     获取统计信息
        /// </summary>
        Statistics GetStatistics();

        /// <summary>
        ///     将Manager数据序列化为JSON字符串
        /// </summary>
        string SerializeToJson();
    }

    /// <summary>
    ///     泛型分类管理器接口
    ///     提供类型安全的分类管理功能
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    public interface ICategoryManager<T> : ICategoryManager
    {
        #region 实体注册

        /// <summary>
        ///     注册实体到指定分类
        /// </summary>
        IEntityRegistration RegisterEntity(T entity, string category);

        /// <summary>
        ///     注册实体到指定分类
        /// </summary>
        IEntityRegistration RegisterEntitySafe(T entity, string category);

        /// <summary>
        ///     使用整数ID注册实体到指定分类（独立整数ID缓存体系）
        /// </summary>
        OperationResult RegisterEntityInt(int entityId, T entity, string category);

        /// <summary>
        ///     使用整数ID注册实体并添加标签（独立整数ID缓存体系）
        /// </summary>
        OperationResult RegisterEntityIntWithTags(int entityId, T entity, string category, params string[] tags);

        /// <summary>
        ///     使用整数ID注册实体并添加元数据（独立整数ID缓存体系）
        /// </summary>
        OperationResult RegisterEntityIntWithMetadata(int entityId, T entity, string category,
                                                      CustomDataCollection metadata);

        /// <summary>
        ///     批量注册实体
        /// </summary>
        BatchOperationResult RegisterBatch(List<T> entities, string category);

        /// <summary>
        ///     删除实体
        /// </summary>
        OperationResult DeleteEntity(string entityId);

        /// <summary>
        ///     删除实体（整数ID，使用独立整数ID缓存体系）
        /// </summary>
        OperationResult DeleteEntity(int entityId);

        #endregion

        #region 分类查询

        /// <summary>
        ///     根据分类获取实体（支持通配符）
        /// </summary>
        IReadOnlyList<T> GetByCategory(string pattern, bool includeChildren = false);

        /// <summary>
        ///     根据 ID 获取实体
        /// </summary>
        OperationResult<T> GetById(string id);

        /// <summary>
        ///     根据整数 ID 获取实体（使用独立整数ID缓存体系）
        /// </summary>
        OperationResult<T> GetById(int id);

        /// <summary>
        ///     根据正则表达式匹配分类名称并获取实体
        /// </summary>
        IReadOnlyList<T> GetByCategoryRegex(string pattern, bool includeChildren = false);

        /// <summary>
        ///     根据实体 ID 获取其所在分类的可读路径字符串
        /// </summary>
        string GetReadableCategoryPath(string entityId);

        /// <summary>
        ///     根据实体整数 ID 获取其所在分类的可读路径字符串（使用独立整数ID缓存体系）
        /// </summary>
        string GetReadableCategoryPath(int entityId);

        /// <summary>
        ///     根据路径 ID 数组转换为可读的分类路径字符串
        /// </summary>
        string GetReadablePathFromIds(int[] pathIds);

        /// <summary>
        ///     检查实体是否在指定分类中
        /// </summary>
        bool IsInCategory(string entityId, string category, bool includeChildren = false);

        /// <summary>
        ///     检查实体是否在指定分类中（整数ID重载）
        /// </summary>
        bool IsInCategory(int entityId, string category, bool includeChildren = false);

        #endregion

        #region 标签管理

        /// <summary>
        ///     为实体添加标签
        /// </summary>
        OperationResult AddTag(string entityId, string tag);

        /// <summary>
        ///     为实体添加标签（整数ID重载）
        /// </summary>
        OperationResult AddTag(int entityId, string tag);

        /// <summary>
        ///     为实体批量添加标签
        /// </summary>
        OperationResult AddTags(string entityId, params string[] tags);

        /// <summary>
        ///     为整数ID实体批量添加标签
        /// </summary>
        OperationResult AddTags(int entityId, params string[] tags);

        /// <summary>
        ///     从实体移除标签
        /// </summary>
        OperationResult RemoveTag(string entityId, string tag);

        /// <summary>
        ///     从实体移除标签（整数ID重载）
        /// </summary>
        OperationResult RemoveTag(int entityId, string tag);

        /// <summary>
        ///     检查实体是否拥有指定标签
        /// </summary>
        bool HasTag(string entityId, string tag);

        /// <summary>
        ///     检查实体是否拥有指定标签（整数ID重载）
        /// </summary>
        bool HasTag(int entityId, string tag);

        /// <summary>
        ///     检查实体是否拥有指定标签
        /// </summary>
        bool HasTag(T entity, string tag);

        /// <summary>
        ///     获取实体的所有标签
        /// </summary>
        IReadOnlyList<string> GetEntityTags(string entityId);

        /// <summary>
        ///     获取实体的所有标签（整数ID重载）
        /// </summary>
        IReadOnlyList<string> GetEntityTags(int entityId);

        /// <summary>
        ///     获取实体的所有标签
        /// </summary>
        IReadOnlyList<string> GetTags(T entity);

        /// <summary>
        ///     检查实体是否在指定分类中
        /// </summary>
        bool IsInCategory(T entity, string category, bool includeChildren = false);

        /// <summary>
        ///     根据标签获取实体
        /// </summary>
        IReadOnlyList<T> GetByTag(string tag);

        /// <summary>
        ///     根据多个标签获取实体
        /// </summary>
        /// <param name="tags">标签数组</param>
        /// <param name="matchAll">true为AND（交集），false为OR（并集）</param>
        IReadOnlyList<T> GetByTags(string[] tags, bool matchAll = true);

        /// <summary>
        ///     根据分类和标签获取实体交集
        /// </summary>
        IReadOnlyList<T> GetByCategoryAndTag(string category, string tag, bool includeChildren = true);

        #endregion

        #region 元数据

        /// <summary>
        ///     获取元数据
        /// </summary>
        CustomDataCollection GetMetadata(string entityId);

        /// <summary>
        ///     获取元数据（整数ID重载）
        /// </summary>
        CustomDataCollection GetMetadata(int entityId);

        /// <summary>
        ///     更新元数据
        /// </summary>
        OperationResult UpdateMetadata(string entityId, CustomDataCollection metadata);

        /// <summary>
        ///     更新元数据（整数ID重载）
        /// </summary>
        OperationResult UpdateMetadata(int entityId, CustomDataCollection metadata);

        #endregion
    }
}