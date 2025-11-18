using System.Threading.Tasks;

namespace EasyPack.ENekoFramework
{
    /// <summary>
    /// 服务的抽象基类，提供生命周期管理的默认实现。
    /// 继承此类以创建自定义服务。
    /// </summary>
    public abstract class BaseService : IService
    {
        private ServiceLifecycleState _state = ServiceLifecycleState.Uninitialized;

        /// <summary>
        /// 服务的当前生命周期状态。
        /// </summary>
        public ServiceLifecycleState State
        {
            get => _state;
            protected set => _state = value;
        }

        /// <summary>
        /// 异步初始化服务。
        /// </summary>
        public async Task InitializeAsync()
        {
            if (_state >= ServiceLifecycleState.Ready)
            {
                return; // 已初始化
            }

            _state = ServiceLifecycleState.Initializing;
            await OnInitializeAsync();
            _state = ServiceLifecycleState.Ready;
        }

        /// <summary>
        /// 暂停服务。
        /// </summary>
        public void Pause()
        {
            if (_state != ServiceLifecycleState.Ready)
            {
                return;
            }

            _state = ServiceLifecycleState.Paused;
            OnPause();
        }

        /// <summary>
        /// 恢复已暂停的服务。
        /// </summary>
        public void Resume()
        {
            if (_state != ServiceLifecycleState.Paused)
            {
                return;
            }

            _state = ServiceLifecycleState.Ready;
            OnResume();
        }

        /// <summary>
        /// 释放服务并清理资源。
        /// </summary>
        public void Dispose()
        {
            if (_state == ServiceLifecycleState.Disposed)
            {
                return; // 已释放
            }

            OnDisposeAsync().Wait();
            _state = ServiceLifecycleState.Disposed;
        }

        /// <summary>
        /// 服务初始化时调用的钩子方法。
        /// 派生类应重写此方法以实现自定义初始化逻辑。
        /// </summary>
        protected virtual Task OnInitializeAsync()
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// 服务暂停时调用的钩子方法。
        /// 派生类可重写此方法以实现自定义暂停逻辑。
        /// </summary>
        protected virtual void OnPause()
        {
        }

        /// <summary>
        /// 服务恢复时调用的钩子方法。
        /// 派生类可重写此方法以实现自定义恢复逻辑。
        /// </summary>
        protected virtual void OnResume()
        {
        }

        /// <summary>
        /// 服务释放时调用的钩子方法。
        /// 派生类应重写此方法以清理资源。
        /// </summary>
        protected virtual Task OnDisposeAsync()
        {
            return Task.CompletedTask;
        }
    }
}
