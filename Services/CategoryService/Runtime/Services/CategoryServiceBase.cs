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
        public ServiceLifecycleState State => throw new System.NotImplementedException();

        public virtual Task InitAsync()
        {
            return Task.CompletedTask;
        }

        public virtual void Dispose()
        {
        }

        public Task InitializeAsync()
        {
            throw new System.NotImplementedException();
        }

        public void Pause()
        {
            throw new System.NotImplementedException();
        }

        public void Resume()
        {
            throw new System.NotImplementedException();
        }
    }
}
