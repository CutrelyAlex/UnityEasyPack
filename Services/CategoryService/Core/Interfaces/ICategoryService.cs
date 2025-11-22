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
        /// 获取或创建指定实体类型的 CategoryManager
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="idExtractor">实体 ID 提取函数</param>
        /// <returns>CategoryManager 实例</returns>
        CategoryManager<T> GetOrCreateManager<T>(
            Func<T, string> idExtractor);
    }
}
