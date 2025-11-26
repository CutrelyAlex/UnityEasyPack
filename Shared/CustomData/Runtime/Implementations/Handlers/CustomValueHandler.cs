using System;

namespace EasyPack.CustomData
{
    public class CustomValueHandler : IValueHandler
    {
        public CustomDataType SupportedType => CustomDataType.Custom;

        public object GetValue(CustomDataEntry entry)
        {
            if (entry.Serializer == null || string.IsNullOrEmpty(entry.JsonValue))
                return null;

            try
            {
                return entry.Serializer.Deserialize(entry.JsonValue);
            }
            catch
            {
                return null;
            }
        }

        public void SetValue(CustomDataEntry entry, object value)
        {
            if (entry.Serializer == null)
                throw new InvalidOperationException("需要一个有效的序列化器来设置自定义值");

            try
            {
                entry.JsonValue = entry.Serializer.Serialize(value);
                entry.Type = CustomDataType.Custom;
                ClearOtherValues(entry);
            }
            catch
            {
                throw new InvalidOperationException("自定义值序列化失败");
            }
        }

        public bool TryDeserialize(CustomDataEntry entry, string data, Type jsonClrType = null)
        {
            if (entry.Serializer == null)
                return false;

            try
            {
                entry.Serializer.Deserialize(data);
                entry.JsonValue = data;
                entry.Type = CustomDataType.Custom;
                ClearOtherValues(entry);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public string Serialize(CustomDataEntry entry) =>
            // 对于自定义类型，JsonValue已经存储了序列化形式，无需额外序列化
            entry.JsonValue ?? string.Empty;

        public void Clear(CustomDataEntry entry) { ClearOtherValues(entry); }

        private static void ClearOtherValues(CustomDataEntry entry)
        {
            entry.IntValue = 0;
            entry.FloatValue = 0;
            entry.BoolValue = false;
            entry.StringValue = null;
            entry.Vector2Value = default;
            entry.Vector3Value = default;
            entry.ColorValue = default;
            entry.JsonClrType = null;
        }
    }
}