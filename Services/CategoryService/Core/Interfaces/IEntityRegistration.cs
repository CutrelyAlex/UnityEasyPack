using EasyPack.CustomData;
using System;

namespace EasyPack.Category
{
    /// <summary>
    ///     实体注册接口
    ///     支持链式调用来配置实体的标签和元数据
    /// </summary>
    public interface IEntityRegistration
    {
        /// <summary>
        ///     添加标签
        /// </summary>
        /// <param name="tags">标签数组</param>
        /// <returns>当前注册实例</returns>
        IEntityRegistration WithTags(params string[] tags);

        /// <summary>
        ///     添加元数据
        /// </summary>
        /// <param name="metadata">元数据条目列表</param>
        /// <returns>当前注册实例</returns>
        IEntityRegistration WithMetadata(CustomDataCollection metadata);

        /// <summary>
        ///     完成注册
        /// </summary>
        /// <returns>操作结果</returns>
        OperationResult Complete();
    }

    /// <summary>
    ///     泛型实体注册接口
    ///     支持链式调用来配置实体的标签和元数据
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    /// <typeparam name="TKey">键类型</typeparam>
    public interface IEntityRegistration<T, TKey>
        where TKey : IEquatable<TKey>
    {
        /// <summary>
        ///     添加标签
        /// </summary>
        /// <param name="tags">标签数组</param>
        /// <returns>当前注册实例</returns>
        IEntityRegistration<T, TKey> WithTags(params string[] tags);

        /// <summary>
        ///     添加元数据
        /// </summary>
        /// <param name="metadata">元数据条目列表</param>
        /// <returns>当前注册实例</returns>
        IEntityRegistration<T, TKey> WithMetadata(CustomDataCollection metadata);

        /// <summary>
        ///     完成注册
        /// </summary>
        /// <returns>操作结果</returns>
        OperationResult Complete();
    }
}