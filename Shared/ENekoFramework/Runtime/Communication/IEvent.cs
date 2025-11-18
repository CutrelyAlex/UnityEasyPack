using System;

namespace EasyPack.ENekoFramework
{
    /// <summary>
    /// 框架中事件的基础接口。
    /// 事件使用至多一次传递语义与 WeakReference 订阅。
    /// </summary>
    public interface IEvent
    {
        /// <summary>
        /// 事件创建时的时间戳。
        /// </summary>
        DateTime Timestamp { get; }
    }
}
