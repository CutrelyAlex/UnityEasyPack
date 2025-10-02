using System;

namespace EasyPack
{
    /// <summary>
    /// Attribute条件序列化器
    /// </summary>
    public class AttributeConditionSerializer : IConditionSerializer
    {
        public string Kind => "Attr";

        public bool CanHandle(IItemCondition condition)
        {
            return condition is AttributeCondition;
        }

        public SerializedCondition Serialize(IItemCondition condition)
        {
            if (!(condition is AttributeCondition attrCondition))
                throw new ArgumentException("条件类型不匹配");

            var dto = new SerializedCondition { Kind = Kind };

            var name = new CustomDataEntry { Id = "Name" };
            name.SetValue(attrCondition.AttributeName, CustomDataType.String);

            var cmp = new CustomDataEntry { Id = "Cmp" };
            cmp.SetValue((int)attrCondition.ComparisonType, CustomDataType.Int);

            var val = new CustomDataEntry { Id = "Value" };
            val.SetValue(attrCondition.AttributeValue);

            dto.Params.Add(name);
            dto.Params.Add(cmp);
            dto.Params.Add(val);
            return dto;
        }

        public IItemCondition Deserialize(SerializedCondition dto)
        {
            if (dto == null || dto.Params == null)
                return new AttributeCondition("", null);

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

            return new AttributeCondition(name ?? "", value, (AttributeComparisonType)cmp);
        }
    }
}
