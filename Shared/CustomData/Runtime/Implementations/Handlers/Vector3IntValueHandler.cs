using System;
using UnityEngine;

namespace EasyPack.CustomData
{
    public class Vector3IntValueHandler : IValueHandler
    {
        public CustomDataType SupportedType => CustomDataType.Vector3Int;

        public object GetValue(CustomDataEntry entry) => entry.Vector3IntValue;

        public void SetValue(CustomDataEntry entry, object value)
        {
            if (value is Vector3Int v3)
            {
                entry.Vector3IntValue = v3;
            }
            else if (value is string strValue && !string.IsNullOrEmpty(strValue))
            {
                var parsed = JsonUtility.FromJson<Vector3Int>(strValue);
                entry.Vector3IntValue = parsed;
            }

            entry.Type = CustomDataType.Vector3Int;
            ClearOtherValues(entry);
        }

        public bool TryDeserialize(CustomDataEntry entry, string data, Type jsonClrType = null)
        {
            try
            {
                var value = JsonUtility.FromJson<Vector3Int>(data);
                entry.Vector3IntValue = value;
                entry.Type = CustomDataType.Vector3Int;
                ClearOtherValues(entry);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public string Serialize(CustomDataEntry entry) => JsonUtility.ToJson(entry.Vector3IntValue);

        public void Clear(CustomDataEntry entry)
        {
            entry.Vector3IntValue = default;
        }

        private static void ClearOtherValues(CustomDataEntry entry)
        {
            entry.IntValue = 0;
            entry.LongValue = 0;
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