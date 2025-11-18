namespace EasyPack
{
    /// <summary>
    /// 可序列化 DTO 的标记接口
    /// 所有可序列化的数据传输对象（DTO）必须实现此接口
    /// 用于泛型约束，确保只有标记为可序列化的类型才能用于序列化器
    /// </summary>
    public interface ISerializable
    {
    }
}
