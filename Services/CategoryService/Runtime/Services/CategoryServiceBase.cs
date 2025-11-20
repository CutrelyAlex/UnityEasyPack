using System;
using System.Threading.Tasks;
using EasyPack.ENekoFramework;

namespace EasyPack.CategoryService
{
    /// <summary>
    /// 分类服务基类
    /// 实现 IService 接口以集成到 ENekoFramework
    /// </summary>
    public abstract class CategoryServiceBase : ICategoryService
    {
        public ServiceLifecycleState State => throw new NotImplementedException();

        public virtual Task InitAsync()
        {
            return Task.CompletedTask;
        }

        public virtual void Dispose()
        {
        }

        public Task InitializeAsync()
        {
            throw new NotImplementedException();
        }

        public void Pause()
        {
            throw new NotImplementedException();
        }

        public void Resume()
        {
            throw new NotImplementedException();
        }

        public abstract CategoryManager<T> GetOrCreateManager<T>(
            Func<T, string> idExtractor,
            StringComparison comparisonMode = StringComparison.OrdinalIgnoreCase,
            CacheStrategy cacheStrategy = CacheStrategy.Balanced);
    }
}
