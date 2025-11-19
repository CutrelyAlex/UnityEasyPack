namespace EasyPack.ENekoFramework
{
    /// <summary>
    /// 定义可批量更新的绑定对象接口
    /// </summary>
    public interface IBindable
    {
        /// <summary>
        /// 刷新所有待处理的更新
        /// 在 LateUpdate 时由 BindingBatchUpdater 调用
        /// </summary>
        void FlushUpdates();
    }
}
