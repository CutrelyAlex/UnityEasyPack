using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EasyPack.ENekoFramework
{
    /// <summary>
    /// 中央服务容器，管理服务注册、解析和生命周期。
    /// 支持依赖注入与单例模式。
    /// 注册和解析操作线程安全。
    /// 支持延迟解析和循环依赖检测。
    /// </summary>
    public class ServiceContainer : IDisposable
    {
        private readonly Dictionary<Type, ServiceDescriptor> _services = new Dictionary<Type, ServiceDescriptor>();
        private readonly object _lock = new object();
        
        // 循环依赖检测：使用 ThreadLocal 支持多线程解析
        [ThreadStatic]
        private static Stack<Type> _resolutionStack;
    
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
            
                // 异步初始化（仅IService类型）
                if (descriptor.Instance is IService service)
                {
                    await service.InitializeAsync();
                }
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
        /// 获取所有已注册的服务描述符
        /// </summary>
        /// <returns>所有服务描述符的枚举</returns>
        public IEnumerable<ServiceDescriptor> GetAllServices()
        {
            lock (_lock)
            {
                return new List<ServiceDescriptor>(_services.Values);
            }
        }

        /// <summary>
        /// 获取指定服务的描述符
        /// </summary>
        /// <typeparam name="TService">服务接口类型</typeparam>
        /// <returns>服务描述符，如果未注册则返回 null</returns>
        public ServiceDescriptor GetServiceDescriptor<TService>() where TService : class, IService
        {
            lock (_lock)
            {
                var serviceType = typeof(TService);
                return _services.TryGetValue(serviceType, out var descriptor) ? descriptor : null;
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
                    if (descriptor.Instance is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
            
                _services.Clear();
            }
        }
        
        /// <summary>
        /// 注册延迟创建的服务
        /// </summary>
        /// <typeparam name="TService">服务类型</typeparam>
        /// <param name="factory">服务工厂函数</param>
        public void RegisterLazy<TService>(Func<ServiceContainer, TService> factory) where TService : class
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
                
                var descriptor = new ServiceDescriptor(serviceType, factory);
                _services[serviceType] = descriptor;
            }
        }
        
        /// <summary>
        /// 注册单例服务实例
        /// </summary>
        /// <typeparam name="TService">服务类型</typeparam>
        /// <param name="instance">服务实例</param>
        public void RegisterSingleton<TService>(TService instance) where TService : class
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
                
                var descriptor = new ServiceDescriptor(serviceType, instance);
                _services[serviceType] = descriptor;
            }
        }
        
        /// <summary>
        /// 同步解析服务（用于工厂函数中）
        /// </summary>
        /// <typeparam name="TService">服务类型</typeparam>
        /// <returns>服务实例</returns>
        public TService Resolve<TService>() where TService : class
        {
            return ResolveInternal<TService>();
        }
        
        /// <summary>
        /// 内部解析方法，支持循环依赖检测
        /// </summary>
        private TService ResolveInternal<TService>() where TService : class
        {
            var serviceType = typeof(TService);
            
            // 初始化解析栈
            if (_resolutionStack == null)
            {
                _resolutionStack = new Stack<Type>();
            }
            
            // 检测循环依赖
            if (_resolutionStack.Contains(serviceType))
            {
                var path = BuildDependencyPath(serviceType);
                throw new CircularDependencyException(path);
            }
            
            _resolutionStack.Push(serviceType);
            
            try
            {
                ServiceDescriptor descriptor;
                
                lock (_lock)
                {
                    if (!_services.TryGetValue(serviceType, out descriptor))
                    {
                        throw new InvalidOperationException($"服务 {serviceType.Name} 未注册。");
                    }
                    
                    descriptor.LastAccessedAt = DateTime.UtcNow;
                }
                
                // 如果实例已存在，直接返回
                if (descriptor.Instance != null)
                {
                    return descriptor.Instance as TService;
                }
                
                // 使用工厂创建实例
                lock (_lock)
                {
                    // 双重检查
                    if (descriptor.Instance == null && descriptor.Factory != null)
                    {
                        descriptor.Instance = descriptor.Factory(this);
                    }
                }
                
                return descriptor.Instance as TService;
            }
            finally
            {
                _resolutionStack.Pop();
            }
        }
        
        /// <summary>
        /// 构建循环依赖路径字符串
        /// </summary>
        private string BuildDependencyPath(Type circularType)
        {
            var path = new List<string>();
            
            foreach (var type in _resolutionStack)
            {
                path.Add(type.Name);
            }
            
            path.Reverse(); // 倒序以显示正确的依赖链
            path.Add(circularType.Name); // 添加导致循环的类型
            
            return string.Join(" → ", path);
        }
        
        /// <summary>
        /// 实现 IDisposable 接口
        /// </summary>
        public void Dispose()
        {
            Clear();
        }
    }
}
