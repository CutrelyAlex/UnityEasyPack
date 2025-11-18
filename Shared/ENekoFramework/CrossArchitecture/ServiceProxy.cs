using System;
using System.Threading.Tasks;
using UnityEngine;

namespace EasyPack.ENekoFramework
{
    /// <summary>
    /// 跨架构服务代理
    /// 提供对其他架构中服务的缓存访问
    /// </summary>
    /// <typeparam name="TService">服务类型</typeparam>
    public class ServiceProxy<TService> : IDisposable where TService : class
    {
        private readonly ServiceContainer _sourceContainer;
        private TService _cachedService;
        private bool _isResolved = false;
        private bool _disposed = false;
        private readonly object _lock = new object();
        
        /// <summary>
        /// 创建服务代理
        /// </summary>
        /// <param name="sourceContainer">源服务容器</param>
        public ServiceProxy(ServiceContainer sourceContainer)
        {
            _sourceContainer = sourceContainer ?? throw new ArgumentNullException(nameof(sourceContainer));
        }
        
        /// <summary>
        /// 获取服务实例（使用缓存）
        /// </summary>
        /// <returns>服务实例</returns>
        public Task<TService> GetServiceAsync()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(ServiceProxy<TService>));
            }

            // 已解析
            if (_isResolved)
            {
                return Task.FromResult(_cachedService);
            }
            
            // 首次解析
            lock (_lock)
            {
                if (_isResolved)
                {
                    return Task.FromResult(_cachedService);
                }
            }
            
            // 在锁外解析服务
            var service = _sourceContainer.Resolve<TService>();
            
            lock (_lock)
            {
                if (!_isResolved)
                {
                    _cachedService = service;
                    _isResolved = true;
                }
            }
            
            return Task.FromResult(_cachedService);
        }
        
        /// <summary>
        /// 同步获取服务实例
        /// </summary>
        /// <returns>服务实例</returns>
        public TService GetService()
        {
            var task = GetServiceAsync();
            task.Wait();
            return task.Result;
        }
        
        /// <summary>
        /// 释放代理
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            
            lock (_lock)
            {
                _cachedService = null;
                _isResolved = false;
                _disposed = true;
            }
        }
    }
}
