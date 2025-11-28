using System;
using EasyPack.ENekoFramework;

namespace EasyPack.Category
{
    /// <summary>
    ///     分类服务接口
    ///     继承自 ENekoFramework 的 IService 接口
    ///     支持双泛型 CategoryManager&lt;T, TKey&gt;
    /// </summary>
    public interface ICategoryService : IService
    {
        /// <summary>
        ///     获取或创建指定实体类型和键类型的 CategoryManager
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <typeparam name="TKey">键类型</typeparam>
        /// <param name="keyExtractor">实体键提取函数</param>
        /// <returns>CategoryManager 实例</returns>
        ICategoryManager<T, TKey> GetOrCreateManager<T, TKey>(Func<T, TKey> keyExtractor)
            where TKey : IEquatable<TKey>;

        /// <summary>
        ///     获取指定实体类型和键类型的 CategoryManager
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <typeparam name="TKey">键类型</typeparam>
        /// <returns>CategoryManager 实例，如果不存在则返回 null</returns>
        ICategoryManager<T, TKey> GetManager<T, TKey>()
            where TKey : IEquatable<TKey>;

        /// <summary>
        ///     移除指定实体类型的 CategoryManager
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <typeparam name="TKey">键类型</typeparam>
        /// <returns>是否成功移除</returns>
        bool RemoveManager<T, TKey>()
            where TKey : IEquatable<TKey>;

        /// <summary>
        ///     序列化指定类型的 Manager
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <typeparam name="TKey">键类型</typeparam>
        /// <returns>JSON 字符串，如果 Manager 不存在则返回 null</returns>
        string SerializeManager<T, TKey>()
            where TKey : IEquatable<TKey>;

        /// <summary>
        ///     从 JSON 加载 Manager 数据
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <typeparam name="TKey">键类型</typeparam>
        /// <param name="json">JSON 字符串</param>
        /// <param name="keyExtractor">实体键提取函数</param>
        /// <returns>操作结果</returns>
        OperationResult LoadManager<T, TKey>(string json, Func<T, TKey> keyExtractor)
            where TKey : IEquatable<TKey>;
    }
}