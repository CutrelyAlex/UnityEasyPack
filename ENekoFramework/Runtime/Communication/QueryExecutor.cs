using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace EasyPack.ENekoFramework
{
    /// <summary>
    /// 查询执行器
    /// 负责同步执行查询，支持查询历史跟踪
    /// </summary>
    public class QueryExecutor
    {
        private readonly List<QueryDescriptor> _queryHistory;

        /// <summary>
        /// 构造函数
        /// </summary>
        public QueryExecutor()
        {
            _queryHistory = new List<QueryDescriptor>();
        }

        /// <summary>
        /// 同步执行查询
        /// </summary>
        /// <typeparam name="TResult">查询返回类型</typeparam>
        /// <param name="query">要执行的查询</param>
        /// <returns>查询结果</returns>
        public TResult Execute<TResult>(IQuery<TResult> query)
        {
            if (query == null)
                throw new ArgumentNullException(nameof(query));

            var descriptor = new QueryDescriptor
            {
                QueryType = query.GetType(),
                ExecutionId = Guid.NewGuid(),
                StartedAt = DateTime.UtcNow,
                Status = QueryStatus.Running
            };

            _queryHistory.Add(descriptor);

            try
            {
                var stopwatch = Stopwatch.StartNew();
                var result = query.Execute();
                stopwatch.Stop();

                descriptor.CompletedAt = DateTime.UtcNow;
                descriptor.Status = QueryStatus.Succeeded;
                descriptor.Result = result;
                descriptor.ExecutionTimeMs = stopwatch.Elapsed.TotalMilliseconds;

                return result;
            }
            catch (Exception ex)
            {
                descriptor.CompletedAt = DateTime.UtcNow;
                descriptor.Status = QueryStatus.Failed;
                descriptor.Exception = ex;
                throw;
            }
        }

        /// <summary>
        /// 获取查询执行历史
        /// </summary>
        /// <returns>查询描述符列表（只读）</returns>
        public IReadOnlyList<QueryDescriptor> GetQueryHistory()
        {
            return _queryHistory.AsReadOnly();
        }

        /// <summary>
        /// 清空查询历史
        /// </summary>
        public void ClearHistory()
        {
            _queryHistory.Clear();
        }
    }

    /// <summary>
    /// 查询描述符
    /// 封装查询执行的元数据
    /// </summary>
    public class QueryDescriptor
    {
        /// <summary>查询类型</summary>
        public Type QueryType { get; set; }

        /// <summary>执行 ID</summary>
        public Guid ExecutionId { get; set; }

        /// <summary>开始时间</summary>
        public DateTime StartedAt { get; set; }

        /// <summary>完成时间</summary>
        public DateTime? CompletedAt { get; set; }

        /// <summary>执行状态</summary>
        public QueryStatus Status { get; set; }

        /// <summary>执行时长（毫秒）</summary>
        public double ExecutionTimeMs { get; set; }

        /// <summary>查询结果（成功时）</summary>
        public object Result { get; set; }

        /// <summary>异常信息（失败时）</summary>
        public Exception Exception { get; set; }
    }

    /// <summary>
    /// 查询执行状态
    /// </summary>
    public enum QueryStatus
    {
        /// <summary>运行中</summary>
        Running,
        /// <summary>成功</summary>
        Succeeded,
        /// <summary>失败</summary>
        Failed
    }
}
