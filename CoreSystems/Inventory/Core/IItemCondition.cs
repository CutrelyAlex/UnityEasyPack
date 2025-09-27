namespace EasyPack
{
    public interface IItemCondition
    {
        bool CheckCondition(IItem item);
    }
    public interface ISerializableCondition : IItemCondition
    {
        string Kind { get; }
        SerializableCondition ToDto();
    }
}