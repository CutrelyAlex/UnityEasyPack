using System;
using UnityEngine;

namespace EasyPack.CustomData
{
    public class JsonValueHandler : IValueHandler
    {
        public CustomDataType SupportedType => CustomDataType.Json;

        public object GetValue(CustomDataEntry entry)
        {
            if (string.IsNullOrEmpty(entry.JsonValue))
            {
                return null;
            }

            if (string.IsNullOrEmpty(entry.JsonClrType))
            {
                return entry.JsonValue;
            }

            try
            {
                var type = Type.GetType(entry.JsonClrType);
                if (type != null)
                {
                    return JsonUtility.FromJson(entry.JsonValue, type);
                }
            }
            catch
            {
                Debug.LogWarning("从CustomDataEntry获取Value失败");
            }

            return null;
        }

        public void SetValue(CustomDataEntry entry, object value)
        {
            if (value == null)
            {
                entry.JsonValue = null;
                entry.JsonClrType = null;
            }
            else
            {
                entry.JsonValue = JsonUtility.ToJson(value);
                entry.JsonClrType = value.GetType().AssemblyQualifiedName;
            }

            entry.Type = CustomDataType.Json;
            ClearOtherValues(entry);
        }

        public bool TryDeserialize(CustomDataEntry entry, string data, Type jsonClrType = null)
        {
            entry.JsonValue = data;
            entry.Type = CustomDataType.Json;

            if (jsonClrType != null) entry.JsonClrType = jsonClrType.AssemblyQualifiedName;

            ClearOtherValues(entry);
            return true;
        }

        public string Serialize(CustomDataEntry entry) => entry.JsonValue ?? string.Empty;

        public void Clear(CustomDataEntry entry)
        {
            entry.JsonValue = null;
            entry.JsonClrType = null;
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
        }
    }
}