using System.Threading;
using System.Threading.Tasks;

namespace EasyPack.ENekoFramework
{
    /// <summary>
    /// 异步命令的基础接口。
    /// </summary>
    public interface ICommand
    {
        /// <summary>
        /// 异步执行命令。
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>命令执行完成时完成的任务</returns>
        Task ExecuteAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// 带返回值的异步命令的基础接口。
    /// </summary>
    /// <typeparam name="TResult">命令结果的类型</typeparam>
    public interface ICommand<TResult>
    {
        /// <summary>
        /// 异步执行命令并返回结果。
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>包含命令结果的任务</returns>
        Task<TResult> ExecuteAsync(CancellationToken cancellationToken = default);
    }
}
