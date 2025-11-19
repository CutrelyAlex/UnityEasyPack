using EasyPack.CategoryService.Core.Interfaces;

namespace EasyPack.CategoryService
{
    /// <summary>
    /// 缓存策略工厂
    /// </summary>
    public static class CacheStrategyFactory
    {
        /// <summary>
        /// 创建缓存策略实例
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="strategy">策略类型</param>
        /// <returns>缓存策略实例</returns>
        public static ICacheStrategy<T> Create<T>(CacheStrategy strategy)
        {
            switch (strategy)
            {
                case CacheStrategy.Loose:
                    return new LooseCacheStrategy<T>();
                case CacheStrategy.Balanced:
                    return new BalancedCacheStrategy<T>();
                case CacheStrategy.Efficient:
                    return new EfficientCacheStrategy<T>();
                case CacheStrategy.Aggressive:
                    return new AggressiveCacheStrategy<T>();
                default:
                    return new BalancedCacheStrategy<T>();
            }
        }
    }

    /// <summary>
    /// 松散缓存策略（临时实现）
    /// </summary>
    internal class LooseCacheStrategy<T> : ICacheStrategy<T>
    {
        public bool Get(string key, out System.Collections.Generic.IReadOnlyList<T> result)
        {
            result = null;
            return false;
        }

        public void Set(string key, System.Collections.Generic.IReadOnlyList<T> value) { }
        public void Invalidate(string key) { }
        public void Clear() { }
    }

    /// <summary>
    /// 平衡缓存策略（临时实现）
    /// </summary>
    internal class BalancedCacheStrategy<T> : ICacheStrategy<T>
    {
        public bool Get(string key, out System.Collections.Generic.IReadOnlyList<T> result)
        {
            result = null;
            return false;
        }

        public void Set(string key, System.Collections.Generic.IReadOnlyList<T> value) { }
        public void Invalidate(string key) { }
        public void Clear() { }
    }

    /// <summary>
    /// 高效缓存策略（临时实现）
    /// </summary>
    internal class EfficientCacheStrategy<T> : ICacheStrategy<T>
    {
        public bool Get(string key, out System.Collections.Generic.IReadOnlyList<T> result)
        {
            result = null;
            return false;
        }

        public void Set(string key, System.Collections.Generic.IReadOnlyList<T> value) { }
        public void Invalidate(string key) { }
        public void Clear() { }
    }

    /// <summary>
    /// 激进缓存策略（临时实现）
    /// </summary>
    internal class AggressiveCacheStrategy<T> : ICacheStrategy<T>
    {
        public bool Get(string key, out System.Collections.Generic.IReadOnlyList<T> result)
        {
            result = null;
            return false;
        }

        public void Set(string key, System.Collections.Generic.IReadOnlyList<T> value) { }
        public void Invalidate(string key) { }
        public void Clear() { }
    }
}
