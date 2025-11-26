using System;
using System.Globalization;

namespace EasyPack.CustomData
{
    /// <summary>整数值处理器</summary>
    public class IntValueHandler : IValueHandler
    {
        public CustomDataType SupportedType => CustomDataType.Int;

        public object GetValue(CustomDataEntry entry) => entry.IntValue;

        public void SetValue(CustomDataEntry entry, object value)
        {
            entry.IntValue = Convert.ToInt32(value, CultureInfo.InvariantCulture);
            entry.Type = CustomDataType.Int;
            ClearOtherValues(entry);
        }

        public bool TryDeserialize(CustomDataEntry entry, string data, Type jsonClrType = null)
        {
            if (!int.TryParse(data, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
                return false;
            entry.IntValue = value;
            entry.Type = CustomDataType.Int;
            ClearOtherValues(entry);
            return true;
        }

        public string Serialize(CustomDataEntry entry) => entry.IntValue.ToString(CultureInfo.InvariantCulture);

        public void Clear(CustomDataEntry entry) { entry.IntValue = 0; }

        private static void ClearOtherValues(CustomDataEntry entry)
        {
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