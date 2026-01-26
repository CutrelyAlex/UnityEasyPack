namespace EasyPack.ObjectPool
{
    /// <summary>
    ///     可池化对象接口。
    /// </summary>
    public interface IPoolable
    {
        /// <summary>
        ///     对象是否已被回收。用于防止重复回收。
        /// </summary>
        bool IsRecycled { get; set; }

        /// <summary>
        ///     对象从池中分配时调用。用于初始化或重置状态。
        /// </summary>
        void OnAllocate();

        /// <summary>
        ///     对象回收到池中时调用。用于清理状态。
        /// </summary>
        void OnRecycle();
    }
}
