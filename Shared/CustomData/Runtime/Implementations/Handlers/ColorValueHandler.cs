using System;
using UnityEngine;

namespace EasyPack.CustomData
{
    public class ColorValueHandler : IValueHandler
    {
        public CustomDataType SupportedType => CustomDataType.Color;
        public object GetValue(CustomDataEntry entry) => entry.ColorValue;

        public void SetValue(CustomDataEntry entry, object value)
        {
            if (value is Color color)
            {
                entry.ColorValue = color;
            }
            else if (value is string strValue && !string.IsNullOrEmpty(strValue))
            {
                var parsed = JsonUtility.FromJson<Color>(strValue);
                entry.ColorValue = parsed;
            }
            entry.Type = CustomDataType.Color;
            ClearOtherValues(entry);
        }

        public bool TryDeserialize(CustomDataEntry entry, string data, Type jsonClrType = null)
        {
            try
            {
                var value = JsonUtility.FromJson<Color>(data);
                entry.ColorValue = value;
                entry.Type = CustomDataType.Color;
                ClearOtherValues(entry);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public string Serialize(CustomDataEntry entry) => JsonUtility.ToJson(entry.ColorValue);

        public void Clear(CustomDataEntry entry) => entry.ColorValue = default;

        private static void ClearOtherValues(CustomDataEntry entry)
        {
            entry.IntValue = 0;
            entry.FloatValue = 0;
            entry.BoolValue = false;
            entry.StringValue = null;
            entry.Vector2Value = default;
            entry.Vector3Value = default;
            entry.JsonValue = null;
            entry.JsonClrType = null;
        }
    }
}
