using System;
using System.Globalization;

namespace EasyPack.CustomData
{
    /// <summary>64位整数值处理器</summary>
    public class LongValueHandler : IValueHandler
    {
        public CustomDataType SupportedType => CustomDataType.Long;

        public object GetValue(CustomDataEntry entry) => entry.LongValue;

        public void SetValue(CustomDataEntry entry, object value)
        {
            entry.LongValue = Convert.ToInt64(value, CultureInfo.InvariantCulture);
            entry.Type = CustomDataType.Long;
            ClearOtherValues(entry);
        }

        public bool TryDeserialize(CustomDataEntry entry, string data, Type jsonClrType = null)
        {
            if (!long.TryParse(data, NumberStyles.Integer, CultureInfo.InvariantCulture, out long value))
                return false;

            entry.LongValue = value;
            entry.Type = CustomDataType.Long;
            ClearOtherValues(entry);
            return true;
        }

        public string Serialize(CustomDataEntry entry) => entry.LongValue.ToString(CultureInfo.InvariantCulture);

        public void Clear(CustomDataEntry entry)
        {
            entry.LongValue = 0;
        }

        private static void ClearOtherValues(CustomDataEntry entry)
        {
            entry.IntValue = 0;
            entry.FloatValue = 0;
            entry.BoolValue = false;
            entry.StringValue = null;
            entry.Vector2Value = default;
            entry.Vector3Value = default;
            entry.ColorValue = default;
            entry.JsonValue = null;
            entry.JsonClrType = null;
        }
    }
}
