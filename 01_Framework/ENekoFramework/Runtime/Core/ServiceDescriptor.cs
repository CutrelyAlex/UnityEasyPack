using System;

namespace EasyPack.ENekoFramework
{
    /// <summary>
    /// 包含已注册服务的元数据和实例引用的描述符。
    /// ServiceContainer 内部使用它来跟踪服务注册和生命周期。
    /// 支持工厂函数和单例实例。
    /// </summary>
    public class ServiceDescriptor
    {
        /// <summary>服务接口的类型</summary>
        public Type ServiceType { get; }
    
        /// <summary>具体实现的类型</summary>
        public Type ImplementationType { get; }
        
        /// <summary>服务工厂函数（用于延迟创建）</summary>
        public Func<ServiceContainer, object> Factory { get; }
    
        /// <summary>服务的单例实例（如果尚未创建则为 null）</summary>
        public object Instance { get; set; }
    
        /// <summary>服务的当前生命周期状态</summary>
        public ServiceLifecycleState State 
        {
            get
            {
                if (Instance is IService service)
                {
                    return service.State;
                }
                return Instance != null ? ServiceLifecycleState.Ready : ServiceLifecycleState.Uninitialized;
            }
        }
    
        /// <summary>服务注册时的时间戳</summary>
        public DateTime RegisteredAt { get; }
    
        /// <summary>服务最后访问时的时间戳（如果从未访问则为 null）</summary>
        public DateTime? LastAccessedAt { get; set; }

        /// <summary>
        /// 创建一个新的服务描述符（类型注册方式）。
        /// </summary>
        /// <param name="serviceType">服务的接口类型</param>
        /// <param name="implementationType">具体实现类型</param>
        public ServiceDescriptor(Type serviceType, Type implementationType)
        {
            ServiceType = serviceType ?? throw new ArgumentNullException(nameof(serviceType));
            ImplementationType = implementationType ?? throw new ArgumentNullException(nameof(implementationType));
            RegisteredAt = DateTime.UtcNow;
        }
        
        /// <summary>
        /// 创建一个新的服务描述符（工厂函数方式）。
        /// </summary>
        /// <param name="serviceType">服务的接口类型</param>
        /// <param name="factory">服务工厂函数</param>
        public ServiceDescriptor(Type serviceType, Func<ServiceContainer, object> factory)
        {
            ServiceType = serviceType ?? throw new ArgumentNullException(nameof(serviceType));
            Factory = factory ?? throw new ArgumentNullException(nameof(factory));
            ImplementationType = null; // 工厂模式下类型未知
            RegisteredAt = DateTime.UtcNow;
        }
        
        /// <summary>
        /// 创建一个新的服务描述符（单例实例方式）。
        /// </summary>
        /// <param name="serviceType">服务的接口类型</param>
        /// <param name="instance">服务实例</param>
        public ServiceDescriptor(Type serviceType, object instance)
        {
            ServiceType = serviceType ?? throw new ArgumentNullException(nameof(serviceType));
            Instance = instance ?? throw new ArgumentNullException(nameof(instance));
            ImplementationType = instance.GetType();
            RegisteredAt = DateTime.UtcNow;
        }
    }
}
