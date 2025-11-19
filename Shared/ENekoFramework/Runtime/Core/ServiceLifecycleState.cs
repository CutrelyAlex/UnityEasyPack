namespace EasyPack.ENekoFramework
{
    /// <summary>
    /// 表示框架中服务的生命周期状态。
    /// 服务状态转换流程：未初始化 → 初始化中 → 就绪 ↔ 暂停 → 已释放
    /// </summary>
    public enum ServiceLifecycleState
    {
        /// <summary>服务已注册但尚未初始化</summary>
        Uninitialized = 0,
    
        /// <summary>服务正在初始化中（异步初始化进行中）</summary>
        Initializing = 1,
    
        /// <summary>服务已完全初始化并准备就绪可用</summary>
        Ready = 2,
    
        /// <summary>服务已暂时暂停（不接受新操作）</summary>
        Paused = 3,
    
        /// <summary>服务已被释放，无法再使用</summary>
        Disposed = 4
    }
}
