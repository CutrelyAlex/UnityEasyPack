using System;
using UnityEngine;

namespace EasyPack.CustomData
{
    public class Vector2ValueHandler : IValueHandler
    {
        public CustomDataType SupportedType => CustomDataType.Vector2;
        public object GetValue(CustomDataEntry entry) => entry.Vector2Value;

        public void SetValue(CustomDataEntry entry, object value)
        {
            if (value is Vector2 v2)
            {
                entry.Vector2Value = v2;
            }
            else if (value is string strValue && !string.IsNullOrEmpty(strValue))
            {
                var parsed = JsonUtility.FromJson<Vector2>(strValue);
                entry.Vector2Value = parsed;
            }
            entry.Type = CustomDataType.Vector2;
            ClearOtherValues(entry);
        }

        public bool TryDeserialize(CustomDataEntry entry, string data, Type jsonClrType = null)
        {
            try
            {
                var value = JsonUtility.FromJson<Vector2>(data);
                entry.Vector2Value = value;
                entry.Type = CustomDataType.Vector2;
                ClearOtherValues(entry);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public string Serialize(CustomDataEntry entry) => JsonUtility.ToJson(entry.Vector2Value);

        public void Clear(CustomDataEntry entry) => entry.Vector2Value = default;

        private static void ClearOtherValues(CustomDataEntry entry)
        {
            entry.IntValue = default;
            entry.FloatValue = default;
            entry.BoolValue = default;
            entry.StringValue = default;
            entry.Vector3Value = default;
            entry.ColorValue = default;
            entry.JsonValue = default;
            entry.JsonClrType = default;
        }
    }
}
