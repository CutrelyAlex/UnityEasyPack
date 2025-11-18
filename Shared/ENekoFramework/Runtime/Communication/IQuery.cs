namespace EasyPack.ENekoFramework
{
    /// <summary>
    /// 同步查询的基础接口。
    /// </summary>
    /// <typeparam name="TResult">查询结果的类型</typeparam>
    public interface IQuery<TResult>
    {
        /// <summary>
        /// 同步执行查询并返回结果。
        /// </summary>
        /// <returns>查询结果</returns>
        TResult Execute();
    }
}
