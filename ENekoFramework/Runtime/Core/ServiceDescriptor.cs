using System;

namespace EasyPack.ENekoFramework
{
    /// <summary>
    /// 包含已注册服务的元数据和实例引用的描述符。
    /// ServiceContainer 内部使用它来跟踪服务注册和生命周期。
    /// </summary>
    public class ServiceDescriptor
    {
        /// <summary>服务接口的类型</summary>
        public Type ServiceType { get; }
        
        /// <summary>具体实现的类型</summary>
        public Type ImplementationType { get; }
        
        /// <summary>服务的单例实例（如果尚未创建则为 null）</summary>
        public IService Instance { get; set; }
        
        /// <summary>服务的当前生命周期状态</summary>
        public ServiceLifecycleState State => Instance?.State ?? ServiceLifecycleState.Uninitialized;
        
        /// <summary>服务注册时的时间戳</summary>
        public DateTime RegisteredAt { get; }
        
        /// <summary>服务最后访问时的时间戳（如果从未访问则为 null）</summary>
        public DateTime? LastAccessedAt { get; set; }

        /// <summary>
        /// 创建一个新的服务描述符。
        /// </summary>
        /// <param name="serviceType">服务的接口类型</param>
        /// <param name="implementationType">具体实现类型</param>
        public ServiceDescriptor(Type serviceType, Type implementationType)
        {
            ServiceType = serviceType ?? throw new ArgumentNullException(nameof(serviceType));
            ImplementationType = implementationType ?? throw new ArgumentNullException(nameof(implementationType));
            RegisteredAt = DateTime.UtcNow;
        }
    }
}
