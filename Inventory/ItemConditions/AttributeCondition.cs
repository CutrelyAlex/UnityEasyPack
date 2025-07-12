namespace EasyPack
{
    public class AttributeCondition : IItemCondition
    {
        public string AttributeName { get; set; }
        public object AttributeValue { get; set; }
        public AttributeCondition(string attributeName, object requiredValue)
        {
            AttributeName = attributeName;
            AttributeValue = requiredValue;
        }
        public void SetAttribute(string attributeName, object requiredValue)
        {
            AttributeName = attributeName;
            AttributeValue = requiredValue;
        }
        public bool IsCondition(IItem item)
        {
            return item.Attributes.TryGetValue(AttributeName, out var value) && value.Equals(AttributeValue);
        }
    }
}