namespace EasyPack
{
    public interface IItemCondition
    {
        bool IsCondition(IItem item);
    }
    public interface ISerializableCondition : IItemCondition
    {
        string Kind { get; }
        ConditionDTO ToDto();
    }
}