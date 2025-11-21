using EasyPack.CustomData;
using System;
using System.Collections.Generic;

namespace EasyPack.Category
{
    /// <summary>
    /// 分类管理器接口
    /// 定义分类管理器的核心功能契约
    /// </summary>
    public interface ICategoryManager : IDisposable
    {
        /// <summary>
        /// 获取实体类型
        /// </summary>
        Type EntityType { get; }

        /// <summary>
        /// 获取所有已注册的分类名称
        /// </summary>
        IReadOnlyList<string> GetCategoriesNodes();

        /// <summary>
        /// 清除所有缓存
        /// </summary>
        void ClearCache();

        /// <summary>
        /// 获取统计信息
        /// </summary>
        Statistics GetStatistics();

        /// <summary>
        /// 将Manager数据序列化为JSON字符串
        /// </summary>
        string SerializeToJson();
    }

    /// <summary>
    /// 泛型分类管理器接口
    /// 提供类型安全的分类管理功能
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    public interface ICategoryManager<T> : ICategoryManager
    {
        /// <summary>
        /// 注册实体到指定分类
        /// </summary>
        IEntityRegistration RegisterEntitySafe(T entity, string category);

        /// <summary>
        /// 批量注册实体
        /// </summary>
        BatchOperationResult RegisterBatch(List<T> entities, string category);

        /// <summary>
        /// 根据分类获取实体（支持通配符）
        /// </summary>
        IReadOnlyList<T> GetByCategory(string pattern, bool includeChildren = false);

        /// <summary>
        /// 根据 ID 获取实体
        /// </summary>
        OperationResult<T> GetById(string id);

        /// <summary>
        /// 根据标签获取实体
        /// </summary>
        IReadOnlyList<T> GetByTag(string tag);

        /// <summary>
        /// 根据分类和标签获取实体交集
        /// </summary>
        IReadOnlyList<T> GetByCategoryAndTag(string category, string tag);

        /// <summary>
        /// 获取元数据
        /// </summary>
        CustomDataCollection GetMetadata(string entityId);
    }
}

