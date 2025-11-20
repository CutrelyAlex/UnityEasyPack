using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace EasyPack.CategoryService
{
    /// <summary>
    /// 正则表达式缓存
    /// 用于缓存编译后的正则表达式以提高性能
    /// </summary>
    public static class RegexCache
    {
        private static readonly ConcurrentDictionary<string, Regex> _cache =
            new ConcurrentDictionary<string, Regex>();

        /// <summary>
        /// 获取或创建正则表达式
        /// </summary>
        /// <param name="pattern">正则表达式模式</param>
        /// <param name="options">正则表达式选项</param>
        /// <returns>编译后的正则表达式</returns>
        public static Regex GetOrCreate(string pattern, RegexOptions options = RegexOptions.Compiled)
        {
            var cacheKey = $"{pattern}_{options}";
            return _cache.GetOrAdd(cacheKey, _ => new Regex(pattern, options));
        }

        /// <summary>
        /// 清空缓存
        /// </summary>
        public static void Clear()
        {
            _cache.Clear();
        }

        /// <summary>
        /// 获取缓存大小
        /// </summary>
        public static int Count => _cache.Count;
    }
}
