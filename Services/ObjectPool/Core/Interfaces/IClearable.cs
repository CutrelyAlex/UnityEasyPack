namespace EasyPack.ObjectPool
{
    /// <summary>
    ///     可清空接口。
    /// </summary>
    internal interface IClearable
    {
        /// <summary>
        ///     清空池中的所有对象。
        /// </summary>
        void Clear();
    }
}
