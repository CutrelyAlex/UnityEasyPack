using System;

namespace EasyPack.CustomData
{
    public class StringValueHandler : IValueHandler
    {
        public CustomDataType SupportedType => CustomDataType.String;

        public object GetValue(CustomDataEntry entry) => entry.StringValue;

        public void SetValue(CustomDataEntry entry, object value)
        {
            entry.StringValue = value as string ?? value?.ToString() ?? string.Empty;
            entry.Type = CustomDataType.String;
            ClearOtherValues(entry);
        }

        public bool TryDeserialize(CustomDataEntry entry, string data, Type jsonClrType = null)
        {
            entry.StringValue = data ?? string.Empty;
            entry.Type = CustomDataType.String;
            ClearOtherValues(entry);
            return true;
        }

        public string Serialize(CustomDataEntry entry) => entry.StringValue ?? string.Empty;

        public void Clear(CustomDataEntry entry) { entry.StringValue = null; }

        private static void ClearOtherValues(CustomDataEntry entry)
        {
            entry.IntValue = 0;
            entry.FloatValue = 0;
            entry.BoolValue = false;
            entry.Vector2Value = default;
            entry.Vector3Value = default;
            entry.ColorValue = default;
            entry.JsonValue = null;
            entry.JsonClrType = null;
        }
    }
}