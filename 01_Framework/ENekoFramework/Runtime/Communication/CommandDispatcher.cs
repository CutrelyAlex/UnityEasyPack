using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EasyPack.ENekoFramework
{
    /// <summary>
    /// 命令调度器
    /// 负责异步执行命令，支持超时处理和命令历史跟踪
    /// </summary>
    public class CommandDispatcher
    {
        private readonly List<CommandDescriptor> _commandHistory;
        private readonly int _defaultTimeoutSeconds;
        private readonly bool _enableHistory;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="defaultTimeoutSeconds">默认超时秒数（默认 4 秒）</param>
        /// <param name="enableHistory">是否启用命令历史记录（默认 true，禁用可提升性能）</param>
        public CommandDispatcher(int defaultTimeoutSeconds = 4, bool enableHistory = true)
        {
            _commandHistory = enableHistory ? new List<CommandDescriptor>() : null;
            _defaultTimeoutSeconds = defaultTimeoutSeconds;
            _enableHistory = enableHistory;
        }

        /// <summary>
        /// 异步执行命令
        /// </summary>
        /// <typeparam name="TResult">命令返回类型</typeparam>
        /// <param name="command">要执行的命令</param>
        /// <param name="timeoutSeconds">超时秒数（null 使用默认值）</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>命令执行结果</returns>
        public async Task<TResult> ExecuteAsync<TResult>(
            ICommand<TResult> command,
            int? timeoutSeconds = null,
            CancellationToken cancellationToken = default)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            var timeout = timeoutSeconds ?? _defaultTimeoutSeconds;
            
            // 只在启用历史记录时创建描述符
            CommandDescriptor descriptor = null;
            if (_enableHistory)
            {
                descriptor = new CommandDescriptor
                {
                    CommandType = command.GetType(),
                    ExecutionId = Guid.NewGuid(),
                    StartedAt = DateTime.UtcNow,
                    TimeoutSeconds = timeout,
                    Status = CommandStatus.Running
                };
                _commandHistory.Add(descriptor);
            }

            try
            {
                using (var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
                {
                    cts.CancelAfter(TimeSpan.FromSeconds(timeout));

                    var task = command.ExecuteAsync(cts.Token);
                    var result = await task;

                    if (descriptor != null)
                    {
                        descriptor.CompletedAt = DateTime.UtcNow;
                        descriptor.Status = CommandStatus.Succeeded;
                        descriptor.Result = result;
                    }

                    return result;
                }
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                // Timeout occurred
                if (descriptor != null)
                {
                    descriptor.CompletedAt = DateTime.UtcNow;
                    descriptor.Status = CommandStatus.TimedOut;
                }
                throw new TimeoutException($"Command {command.GetType().Name} exceeded timeout of {timeout} seconds");
            }
            catch (Exception ex)
            {
                if (descriptor != null)
                {
                    descriptor.CompletedAt = DateTime.UtcNow;
                    descriptor.Status = CommandStatus.Failed;
                    descriptor.Exception = ex;
                }
                throw;
            }
        }

        /// <summary>
        /// 获取命令执行历史
        /// </summary>
        /// <returns>命令描述符列表（只读）</returns>
        public IReadOnlyList<CommandDescriptor> GetCommandHistory()
        {
            if (!_enableHistory || _commandHistory == null)
                return new List<CommandDescriptor>().AsReadOnly();
            return _commandHistory.AsReadOnly();
        }

        /// <summary>
        /// 清空命令历史
        /// </summary>
        public void ClearHistory()
        {
            if (_enableHistory && _commandHistory != null)
            {
                _commandHistory.Clear();
            }
        }
    }

    /// <summary>
    /// 命令描述符
    /// 封装命令执行的元数据
    /// </summary>
    public class CommandDescriptor
    {
        /// <summary>命令类型</summary>
        public Type CommandType { get; set; }

        /// <summary>执行 ID</summary>
        public Guid ExecutionId { get; set; }

        /// <summary>开始时间</summary>
        public DateTime StartedAt { get; set; }

        /// <summary>完成时间</summary>
        public DateTime? CompletedAt { get; set; }

        /// <summary>执行状态</summary>
        public CommandStatus Status { get; set; }

        /// <summary>超时秒数</summary>
        public int TimeoutSeconds { get; set; }

        /// <summary>执行结果（成功时）</summary>
        public object Result { get; set; }

        /// <summary>异常信息（失败时）</summary>
        public Exception Exception { get; set; }

        /// <summary>执行时长（毫秒）</summary>
        public double ExecutionTimeMs
        {
            get
            {
                if (!CompletedAt.HasValue)
                    return 0;
                return (CompletedAt.Value - StartedAt).TotalMilliseconds;
            }
        }
    }

    /// <summary>
    /// 命令执行状态
    /// </summary>
    public enum CommandStatus
    {
        /// <summary>运行中</summary>
        Running,
        /// <summary>成功</summary>
        Succeeded,
        /// <summary>失败</summary>
        Failed,
        /// <summary>超时</summary>
        TimedOut,
        /// <summary>已取消</summary>
        Cancelled
    }
}
