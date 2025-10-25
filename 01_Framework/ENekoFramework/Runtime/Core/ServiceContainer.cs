using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EasyPack.ENekoFramework
{
    /// <summary>
    /// 中央服务容器，管理服务注册、解析和生命周期。
    /// 支持依赖注入与单例模式。
    /// 注册和解析操作线程安全。
    /// </summary>
    public class ServiceContainer
    {
        private readonly Dictionary<Type, ServiceDescriptor> _services = new Dictionary<Type, ServiceDescriptor>();
        private readonly object _lock = new object();
    
        /// <summary>
        /// 可以注册的最大服务数量。
        /// 默认：500
        /// </summary>
        public int MaxServiceCapacity { get; set; } = 500;
    
        /// <summary>
        /// 当前已注册的服务数量。
        /// </summary>
        public int RegisteredServiceCount => _services.Count;

        /// <summary>
        /// 注册服务及其实现类型。
        /// </summary>
        /// <typeparam name="TService">服务接口类型</typeparam>
        /// <typeparam name="TImplementation">具体实现类型</typeparam>
        /// <exception cref="InvalidOperationException">如果服务已注册或超出容量限制则抛出</exception>
        public void Register<TService, TImplementation>()
            where TService : class, IService
            where TImplementation : class, TService, new()
        {
            lock (_lock)
            {
                var serviceType = typeof(TService);
            
                if (_services.ContainsKey(serviceType))
                {
                    throw new InvalidOperationException($"服务 {serviceType.Name} 已经被注册。");
                }
            
                if (_services.Count >= MaxServiceCapacity)
                {
                    throw new InvalidOperationException($"服务容量已超限。最大容量：{MaxServiceCapacity}");
                }
            
                var descriptor = new ServiceDescriptor(serviceType, typeof(TImplementation));
                _services[serviceType] = descriptor;
            }
        }

        /// <summary>
        /// 解析服务实例，必要时创建并初始化它。
        /// </summary>
        /// <typeparam name="TService">服务接口类型</typeparam>
        /// <returns>服务实例</returns>
        /// <exception cref="InvalidOperationException">如果服务未注册则抛出</exception>
        public async Task<TService> ResolveAsync<TService>() where TService : class, IService
        {
            ServiceDescriptor descriptor;
        
            lock (_lock)
            {
                var serviceType = typeof(TService);
            
                if (!_services.TryGetValue(serviceType, out descriptor))
                {
                    throw new InvalidOperationException($"服务 {serviceType.Name} 未注册。");
                }
            
                descriptor.LastAccessedAt = DateTime.UtcNow;
            }
        
            // 如果实例不存在则创建（在锁外进行异步初始化）
            if (descriptor.Instance == null)
            {
                lock (_lock)
                {
                    // 双重检查模式
                    if (descriptor.Instance == null)
                    {
                        descriptor.Instance = (IService)Activator.CreateInstance(descriptor.ImplementationType);
                    }
                }
            
                // 异步初始化
                await descriptor.Instance.InitializeAsync();
            }
        
            return descriptor.Instance as TService;
        }

        /// <summary>
        /// 检查服务是否已注册。
        /// </summary>
        /// <typeparam name="TService">服务接口类型</typeparam>
        /// <returns>如果已注册返回 true，否则返回 false</returns>
        public bool IsRegistered<TService>() where TService : class, IService
        {
            lock (_lock)
            {
                return _services.ContainsKey(typeof(TService));
            }
        }

        /// <summary>
        /// 释放所有服务并清空容器。
        /// 服务按注册的逆序释放。
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                var descriptors = new List<ServiceDescriptor>(_services.Values);
                descriptors.Reverse(); // 按逆序释放
            
                foreach (var descriptor in descriptors)
                {
                    descriptor.Instance?.Dispose();
                }
            
                _services.Clear();
            }
        }
    }
}
