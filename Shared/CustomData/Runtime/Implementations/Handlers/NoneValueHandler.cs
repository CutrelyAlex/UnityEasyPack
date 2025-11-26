using System;

namespace EasyPack.CustomData
{
    public class NoneValueHandler : IValueHandler
    {
        public CustomDataType SupportedType => CustomDataType.None;

        public object GetValue(CustomDataEntry entry) => null;

        public void SetValue(CustomDataEntry entry, object value)
        {
            entry.Type = CustomDataType.None;
            ClearAllValues(entry);
        }

        public bool TryDeserialize(CustomDataEntry entry, string data, Type jsonClrType = null)
        {
            entry.Type = CustomDataType.None;
            ClearAllValues(entry);
            return false;
        }

        public string Serialize(CustomDataEntry entry) => string.Empty;

        public void Clear(CustomDataEntry entry) { ClearAllValues(entry); }

        private static void ClearAllValues(CustomDataEntry entry)
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