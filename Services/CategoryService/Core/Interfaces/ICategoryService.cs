using EasyPack.ENekoFramework;
using System;

namespace EasyPack.Category
{
    /// <summary>
    /// 分类服务接口
    /// 继承自 ENekoFramework 的 IService 接口
    /// </summary>
    public interface ICategoryService : IService
    {
        /// <summary>
        /// 获取或创建指定类型的 CategoryManager
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="idExtractor">实体 ID 提取函数</param>
        /// <param name="comparisonMode">字符串比较模式</param>
        /// <param name="cacheStrategy">缓存策略</param>
        /// <returns>CategoryManager 实例</returns>
        CategoryManager<T> GetOrCreateManager<T>(
            Func<T, string> idExtractor,
            StringComparison comparisonMode = StringComparison.OrdinalIgnoreCase,
            CacheStrategy cacheStrategy = CacheStrategy.Balanced);
    }
}
