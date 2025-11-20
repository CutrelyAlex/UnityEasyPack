using System;
using UnityEngine;

namespace EasyPack.CustomData
{
    public class Vector3ValueHandler : IValueHandler
    {
        public CustomDataType SupportedType => CustomDataType.Vector3;
        public object GetValue(CustomDataEntry entry) => entry.Vector3Value;

        public void SetValue(CustomDataEntry entry, object value)
        {
            if (value is Vector3 v3)
            {
                entry.Vector3Value = v3;
            }
            else if (value is string strValue && !string.IsNullOrEmpty(strValue))
            {
                var parsed = JsonUtility.FromJson<Vector3>(strValue);
                entry.Vector3Value = parsed;
            }
            entry.Type = CustomDataType.Vector3;
            ClearOtherValues(entry);
        }

        public bool TryDeserialize(CustomDataEntry entry, string data, Type jsonClrType = null)
        {
            try
            {
                var value = JsonUtility.FromJson<Vector3>(data);
                entry.Vector3Value = value;
                entry.Type = CustomDataType.Vector3;
                ClearOtherValues(entry);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public string Serialize(CustomDataEntry entry) => JsonUtility.ToJson(entry.Vector3Value);

        public void Clear(CustomDataEntry entry) => entry.Vector3Value = default;

        private static void ClearOtherValues(CustomDataEntry entry)
        {
            entry.IntValue = 0;
            entry.FloatValue = 0;
            entry.BoolValue = false;
            entry.StringValue = null;
            entry.Vector2Value = default;
            entry.ColorValue = default;
            entry.JsonValue = null;
            entry.JsonClrType = null;
        }
    }
}
