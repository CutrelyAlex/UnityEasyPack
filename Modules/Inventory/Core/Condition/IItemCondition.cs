namespace EasyPack.InventorySystem
{
    public interface IItemCondition
    {
        bool CheckCondition(IItem item);
    }
    public interface ISerializableCondition : IItemCondition
    {
        string Kind { get; }
        SerializedCondition ToDto();
        ISerializableCondition FromDto(SerializedCondition dto);
    }
}
