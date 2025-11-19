using System.Collections.Generic;

namespace EasyPack.CategoryService
{
    /// <summary>
    /// 缓存策略接口
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    public interface ICacheStrategy<T>
    {
        /// <summary>
        /// 从缓存获取结果
        /// </summary>
        /// <param name="key">缓存键</param>
        /// <param name="result">缓存结果</param>
        /// <returns>是否命中缓存</returns>
        bool Get(string key, out IReadOnlyList<T> result);

        /// <summary>
        /// 设置缓存
        /// </summary>
        /// <param name="key">缓存键</param>
        /// <param name="value">缓存值</param>
        void Set(string key, IReadOnlyList<T> value);

        /// <summary>
        /// 使指定缓存失效
        /// </summary>
        /// <param name="key">缓存键</param>
        void Invalidate(string key);

        /// <summary>
        /// 清空所有缓存
        /// </summary>
        void Clear();
    }
}
