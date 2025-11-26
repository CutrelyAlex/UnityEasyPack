using System;
using System.Globalization;

namespace EasyPack.CustomData
{
    public class BoolValueHandler : IValueHandler
    {
        public CustomDataType SupportedType => CustomDataType.Bool;

        public object GetValue(CustomDataEntry entry) => entry.BoolValue;

        public void SetValue(CustomDataEntry entry, object value)
        {
            entry.BoolValue = Convert.ToBoolean(value, CultureInfo.InvariantCulture);
            entry.Type = CustomDataType.Bool;
            ClearOtherValues(entry);
        }

        public bool TryDeserialize(CustomDataEntry entry, string data, Type jsonClrType = null)
        {
            entry.BoolValue = string.Equals(data, "true", StringComparison.OrdinalIgnoreCase) ||
                              string.Equals(data, "1", StringComparison.OrdinalIgnoreCase);
            entry.Type = CustomDataType.Bool;
            ClearOtherValues(entry);
            return true;
        }

        public string Serialize(CustomDataEntry entry) => entry.BoolValue ? "true" : "false";

        public void Clear(CustomDataEntry entry)
        {
            entry.BoolValue = false;
        }

        private static void ClearOtherValues(CustomDataEntry entry)
        {
            entry.IntValue = 0;
            entry.FloatValue = 0;
            entry.StringValue = null;
            entry.Vector2Value = default;
            entry.Vector3Value = default;
            entry.ColorValue = default;
            entry.JsonValue = null;
            entry.JsonClrType = null;
        }
    }
}