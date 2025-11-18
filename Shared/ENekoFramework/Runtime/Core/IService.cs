using System.Threading.Tasks;

namespace EasyPack.ENekoFramework
{
    /// <summary>
    /// ENekoFramework 中所有服务的基础接口。
    /// 服务必须实现生命周期方法并暴露其当前状态。
    /// </summary>
    public interface IService
    {
        /// <summary>
        /// 服务的当前生命周期状态。
        /// </summary>
        ServiceLifecycleState State { get; }
    
        /// <summary>
        /// 异步初始化服务。
        /// 当服务首次被访问时，框架会自动调用此方法。
        /// </summary>
        /// <returns>初始化完成时完成的任务</returns>
        Task InitializeAsync();
    
        /// <summary>
        /// 暂时暂停服务，阻止新操作。
        /// 现有操作可能会继续完成。
        /// </summary>
        void Pause();
    
        /// <summary>
        /// 恢复已暂停的服务，允许新操作。
        /// </summary>
        void Resume();
    
        /// <summary>
        /// 释放服务并释放所有资源。
        /// 服务释放后无法再使用。
        /// </summary>
        void Dispose();
    }
}
