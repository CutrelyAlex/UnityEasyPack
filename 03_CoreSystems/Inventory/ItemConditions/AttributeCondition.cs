using System;

namespace EasyPack.InventorySystem
{
    public enum AttributeComparisonType
    {
        Equal,
        NotEqual,
        GreaterThan,
        LessThan,
        GreaterThanOrEqual,
        LessThanOrEqual,
        Contains,
        NotContains,
        Exists
    }
    public class AttributeCondition : IItemCondition, ISerializableCondition
    {
        public string AttributeName { get; set; }
        public object AttributeValue { get; set; }
        public AttributeComparisonType ComparisonType { get; set; }

        public AttributeCondition() : this(string.Empty, null)
        {
        }

        public AttributeCondition(string attributeName, object requiredValue)
        {
            AttributeName = attributeName;
            AttributeValue = requiredValue;
            ComparisonType = AttributeComparisonType.Equal;
        }

        public AttributeCondition(string attributeName, object requiredValue, AttributeComparisonType comparisonType)
        {
            AttributeName = attributeName;
            AttributeValue = requiredValue;
            ComparisonType = comparisonType;
        }

        public void SetAttribute(string attributeName, object requiredValue)
        {
            AttributeName = attributeName;
            AttributeValue = requiredValue;
        }

        public void SetAttribute(string attributeName, object requiredValue, AttributeComparisonType comparisonType)
        {
            AttributeName = attributeName;
            AttributeValue = requiredValue;
            ComparisonType = comparisonType;
        }

        public bool CheckCondition(IItem item)
        {
            if (item == null || item.Attributes == null)
                return false;

            if (ComparisonType == AttributeComparisonType.Exists)
                return item.Attributes.ContainsKey(AttributeName);

            if (!item.Attributes.TryGetValue(AttributeName, out var actualValue))
                return false;

            if (actualValue == null)
                return AttributeValue == null;

            return ComparisonType switch
            {
                AttributeComparisonType.Equal => actualValue.Equals(AttributeValue),
                AttributeComparisonType.NotEqual => !actualValue.Equals(AttributeValue),
                AttributeComparisonType.GreaterThan => CompareNumeric(actualValue, AttributeValue) > 0,
                AttributeComparisonType.LessThan => CompareNumeric(actualValue, AttributeValue) < 0,
                AttributeComparisonType.GreaterThanOrEqual => CompareNumeric(actualValue, AttributeValue) >= 0,
                AttributeComparisonType.LessThanOrEqual => CompareNumeric(actualValue, AttributeValue) <= 0,
                AttributeComparisonType.Contains => CompareContains(actualValue, AttributeValue),
                AttributeComparisonType.NotContains => !CompareContains(actualValue, AttributeValue),
                _ => false,
            };
        }

        private int CompareNumeric(object value1, object value2)
        {
            if (value1 == null || value2 == null)
                return 0;

            if (value1 is IComparable comparable1 && value2 is IComparable comparable2)
            {
                try
                {
                    if (value1.GetType() == value2.GetType())
                        return comparable1.CompareTo(comparable2);

                    if (decimal.TryParse(value1.ToString(), out decimal num1) &&
                        decimal.TryParse(value2.ToString(), out decimal num2))
                        return num1.CompareTo(num2);

                    if (double.TryParse(value1.ToString(), out double d1) &&
                        double.TryParse(value2.ToString(), out double d2))
                        return d1.CompareTo(d2);
                }
                catch
                {
                    return 0;
                }
            }
            return 0;
        }

        private bool CompareContains(object container, object value)
        {
            if (container == null || value == null)
                return false;

            if (container is string containerStr && value is string valueStr)
                return containerStr.Contains(valueStr);

            if (container is System.Collections.IEnumerable enumerable && container is not string)
            {
                foreach (var item in enumerable)
                {
                    if (item != null && item.Equals(value))
                        return true;
                }
            }
            return false;
        }

        // 自序列化支持
        public string Kind => "Attr";

        public SerializedCondition ToDto()
        {
            var dto = new SerializedCondition { Kind = Kind };

            var name = new CustomDataEntry { Id = "Name" };
            name.SetValue(AttributeName, CustomDataType.String);

            var cmp = new CustomDataEntry { Id = "Cmp" };
            cmp.SetValue((int)ComparisonType, CustomDataType.Int);

            var val = new CustomDataEntry { Id = "Value" };

            val.SetValue(AttributeValue);

            dto.Params.Add(name);
            dto.Params.Add(cmp);
            dto.Params.Add(val);
            return dto;
        }

        public ISerializableCondition FromDto(SerializedCondition dto)
        {
            if (dto == null || dto.Params == null) return this;

            string name = null;
            object value = null;
            int cmp = (int)AttributeComparisonType.Equal;

            foreach (var p in dto.Params)
            {
                if (p == null) continue;
                switch (p.Id)
                {
                    case "Name": name = p.StringValue ?? p.GetValue() as string; break;
                    case "Cmp": cmp = p.IntValue; break;
                    case "Value": value = p.GetValue(); break;
                }
            }

            if (!string.IsNullOrEmpty(name))
            {
                AttributeName = name;
                AttributeValue = value;
                ComparisonType = (AttributeComparisonType)cmp;
            }
            return this;
        }
    }
}
