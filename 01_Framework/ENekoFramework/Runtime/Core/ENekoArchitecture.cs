using System;
using System.Threading;
using System.Threading.Tasks;

namespace EasyPack.ENekoFramework
{
    /// <summary>
    /// 提供核心框架基础设施的基础架构类。
    /// 为派生架构实现泛型单例模式。
    /// 管理服务容器并提供对框架功能的访问。
    /// </summary>
    /// <typeparam name="T">派生架构类型（用于单例模式）</typeparam>
    public abstract class ENekoArchitecture<T> where T : ENekoArchitecture<T>, new()
    {
        private static T _instance;
        private static readonly object _lock = new object();
    
        /// <summary>
        /// 架构的单例实例。
        /// 首次访问时创建并初始化架构。
        /// </summary>
        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new T();
                            _instance.Init();
                        }
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// 管理所有已注册服务的服务容器。
        /// </summary>
        protected ServiceContainer Container { get; private set; }

        /// <summary>
        /// 命令调度器，负责异步执行命令。
        /// </summary>
        protected CommandDispatcher CommandDispatcher { get; private set; }

        /// <summary>
        /// 查询执行器，负责同步执行查询。
        /// </summary>
        protected QueryExecutor QueryExecutor { get; private set; }

        /// <summary>
        /// 事件总线，负责事件发布和订阅。
        /// </summary>
        protected EventBus EventBus { get; private set; }

        /// <summary>
        /// 指示架构是否已初始化。
        /// </summary>
        public bool IsInitialized { get; private set; }

        /// <summary>
        /// 受保护的构造函数以强制执行单例模式。
        /// </summary>
        protected ENekoArchitecture()
        {
            Container = new ServiceContainer();
            CommandDispatcher = new CommandDispatcher();
            QueryExecutor = new QueryExecutor();
            EventBus = new EventBus();
        }

        /// <summary>
        /// 初始化架构。
        /// 在首次访问 Instance 时自动调用。
        /// 重写此方法以注册服务和配置架构。
        /// </summary>
        protected virtual void Init()
        {
            if (IsInitialized)
            {
                return;
            }

            OnInit();
            IsInitialized = true;
        }

        /// <summary>
        /// 重写此方法以注册服务和配置架构。
        /// 在初始化期间调用一次。
        /// </summary>
        protected abstract void OnInit();

        /// <summary>
        /// 在容器中注册服务。
        /// </summary>
        /// <typeparam name="TService">服务接口类型</typeparam>
        /// <typeparam name="TImplementation">具体实现类型</typeparam>
        public void RegisterService<TService, TImplementation>()
            where TService : class, IService
            where TImplementation : class, TService, new()
        {
            Container.Register<TService, TImplementation>();
        }

        /// <summary>
        /// 从容器解析服务。
        /// </summary>
        /// <typeparam name="TService">服务接口类型</typeparam>
        /// <returns>服务实例</returns>
        public async Task<TService> GetServiceAsync<TService>() where TService : class, IService
        {
            return await Container.ResolveAsync<TService>();
        }

        /// <summary>
        /// 发送命令并异步执行
        /// </summary>
        /// <typeparam name="TResult">命令返回类型</typeparam>
        /// <param name="command">要执行的命令</param>
        /// <param name="timeoutSeconds">超时秒数（null 使用默认值）</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>命令执行结果</returns>
        public async Task<TResult> SendCommandAsync<TResult>(
            ICommand<TResult> command,
            int? timeoutSeconds = null,
            CancellationToken cancellationToken = default)
        {
            return await CommandDispatcher.ExecuteAsync(command, timeoutSeconds, cancellationToken);
        }

        /// <summary>
        /// 执行查询并同步返回结果
        /// </summary>
        /// <typeparam name="TResult">查询返回类型</typeparam>
        /// <param name="query">要执行的查询</param>
        /// <returns>查询结果</returns>
        public TResult ExecuteQuery<TResult>(IQuery<TResult> query)
        {
            return QueryExecutor.Execute(query);
        }

        /// <summary>
        /// 发布事件
        /// </summary>
        /// <typeparam name="TEvent">事件类型</typeparam>
        /// <param name="eventData">事件数据</param>
        public void PublishEvent<TEvent>(TEvent eventData) where TEvent : IEvent
        {
            EventBus.Publish(eventData);
        }

        /// <summary>
        /// 订阅事件
        /// </summary>
        /// <typeparam name="TEvent">事件类型</typeparam>
        /// <param name="handler">事件处理函数</param>
        public void SubscribeEvent<TEvent>(Action<TEvent> handler) where TEvent : IEvent
        {
            EventBus.Subscribe(handler);
        }

        /// <summary>
        /// 取消订阅事件
        /// </summary>
        /// <typeparam name="TEvent">事件类型</typeparam>
        /// <param name="handler">要取消的事件处理函数</param>
        public void UnsubscribeEvent<TEvent>(Action<TEvent> handler) where TEvent : IEvent
        {
            EventBus.Unsubscribe(handler);
        }

        /// <summary>
        /// 重置架构单例（用于测试目的）。
        /// 警告：释放所有服务并清空容器。
        /// </summary>
        public static void ResetInstance()
        {
            lock (_lock)
            {
                if (_instance != null)
                {
                    _instance.Container?.Clear();
                    _instance = null;
                }
            }
        }
    }
}
