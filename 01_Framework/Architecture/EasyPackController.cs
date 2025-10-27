using EasyPack.ENekoFramework;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace EasyPack
{
    /// <summary>
    /// EasyPack Controller 基类
    /// 用于 MonoBehaviour 快速访问 EasyPack 架构
    /// </summary>
    public abstract class EasyPackController : MonoBehaviour
    {
        /// <summary>
        /// 获取 EasyPack 架构实例
        /// </summary>
        public EasyPackArchitecture GetArchitecture()
        {
            return EasyPackArchitecture.Instance;
        }
        
        /// <summary>
        /// 快速获取 EasyPack 服务
        /// </summary>
        /// <typeparam name="T">服务类型</typeparam>
        /// <returns>服务实例（异步）</returns>
        protected async Task<T> GetServiceAsync<T>() where T : class, IService
        {
            return await GetArchitecture().GetServiceAsync<T>();
        }
        
        /// <summary>
        /// 发送命令并异步执行
        /// </summary>
        /// <typeparam name="TResult">命令返回类型</typeparam>
        /// <param name="command">命令实例</param>
        /// <param name="timeoutSeconds">超时秒数</param>
        protected async Task<TResult> SendCommandAsync<TResult>(
            ICommand<TResult> command,
            int? timeoutSeconds = null,
            CancellationToken cancellationToken = default)
        {
            return await GetArchitecture().SendCommandAsync(command, timeoutSeconds, cancellationToken);
        }
        
        /// <summary>
        /// 执行查询并同步返回结果
        /// </summary>
        /// <typeparam name="TResult">查询结果类型</typeparam>
        /// <param name="query">查询实例</param>
        /// <returns>查询结果</returns>
        protected TResult ExecuteQuery<TResult>(IQuery<TResult> query)
        {
            return GetArchitecture().ExecuteQuery(query);
        }
        
        /// <summary>
        /// 订阅事件
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="onEvent">事件回调</param>
        protected void SubscribeEvent<T>(System.Action<T> onEvent) where T : IEvent
        {
            GetArchitecture().SubscribeEvent(onEvent);
        }
        
        /// <summary>
        /// 取消订阅事件
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="onEvent">事件回调</param>
        protected void UnsubscribeEvent<T>(System.Action<T> onEvent) where T : IEvent
        {
            GetArchitecture().UnsubscribeEvent(onEvent);
        }
        
        /// <summary>
        /// 发布事件
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="e">事件实例</param>
        protected void PublishEvent<T>(T e) where T : IEvent
        {
            GetArchitecture().PublishEvent(e);
        }
    }
}
