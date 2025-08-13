using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace EasyPack
{
    public interface ICustomDataSerializer
    {
        // 目标 CLR 类型（可选）
        Type TargetClrType { get; }

        // 将对象序列化为字符串
        string Serialize(object value);

        // 从字符串反序列化为对象
        object Deserialize(string data);
    }

    [Serializable]
    public class CustomDataEntry
    {
        // 数据唯一键
        public string Id;

        // 存储类型
        public CustomDataType Type = CustomDataType.None;

        // 直存类型
        public int IntValue;
        public float FloatValue;
        public bool BoolValue;
        public string StringValue;
        public Vector2 Vector2Value;
        public Vector3 Vector3Value;
        public Color ColorValue;

        // 复杂类型值
        public string JsonValue;

        // 当 Type=Json/Custom 时可记录目标类型信息
        public string JsonClrType;

        [NonSerialized] public ICustomDataSerializer Serializer;

        /// <summary>
        /// 获取已存储的实际值
        /// </summary>
        public object GetValue()
        {
            switch (Type)
            {
                case CustomDataType.Int: return IntValue;
                case CustomDataType.Float: return FloatValue;
                case CustomDataType.Bool: return BoolValue;
                case CustomDataType.String: return StringValue;
                case CustomDataType.Vector2: return Vector2Value;
                case CustomDataType.Vector3: return Vector3Value;
                case CustomDataType.Color: return ColorValue;
                case CustomDataType.Json:
                    if (string.IsNullOrEmpty(JsonValue)) return null;
                    if (!string.IsNullOrEmpty(JsonClrType))
                    {
                        var clrType = System.Type.GetType(JsonClrType);
                        if (clrType != null)
                        {
                            try { return JsonUtility.FromJson(JsonValue, clrType); }
                            catch { }
                        }
                    }
                    // 类型不明时返回原始 JSON 字符串
                    return JsonValue;
                case CustomDataType.Custom:
                    if (Serializer == null || string.IsNullOrEmpty(JsonValue)) return null;
                    try { return Serializer.Deserialize(JsonValue); }
                    catch { return null; }
                default:
                    return null;
            }
        }

        /// <summary>
        /// 设置值。可强制类型；复杂类型默认用 JsonUtility 序列化，失败时尝试外部 Serializer。
        /// </summary>
        public void SetValue(object value, CustomDataType? forceType = null, Type jsonClrType = null)
        {
            if (forceType.HasValue)
            {
                SetByType(forceType.Value, value, jsonClrType);
                return;
            }

            if (value == null)
            {
                Type = CustomDataType.None;
                ClearAll();
                return;
            }

            switch (value)
            {
                case int v: SetByType(CustomDataType.Int, v); break;
                case float v: SetByType(CustomDataType.Float, v); break;
                case bool v: SetByType(CustomDataType.Bool, v); break;
                case string v: SetByType(CustomDataType.String, v); break;
                case Vector2 v: SetByType(CustomDataType.Vector2, v); break;
                case Vector3 v: SetByType(CustomDataType.Vector3, v); break;
                case Color v: SetByType(CustomDataType.Color, v); break;
                default:
                    // 优先尝试 Unity JsonUtility
                    try
                    {
                        JsonValue = JsonUtility.ToJson(value);
                        JsonClrType = value.GetType().AssemblyQualifiedName;
                        Type = CustomDataType.Json;
                        ClearNonJson();
                    }
                    catch
                    {
                        // 失败则尝试外部自定义序列化器
                        if (Serializer != null)
                        {
                            try
                            {
                                JsonValue = Serializer.Serialize(value);
                                JsonClrType = Serializer.TargetClrType != null ? Serializer.TargetClrType.AssemblyQualifiedName : null;
                                Type = CustomDataType.Custom;
                                ClearNonJson();
                            }
                            catch
                            {
                                Type = CustomDataType.None;
                                ClearAll();
                            }
                        }
                        else
                        {
                            Type = CustomDataType.None;
                            ClearAll();
                        }
                    }
                    break;
            }
        }

        /// <summary>
        /// 将当前值序列化为字符串
        /// </summary>
        public string SerializeValue()
        {
            switch (Type)
            {
                case CustomDataType.Int: return IntValue.ToString(CultureInfo.InvariantCulture);
                case CustomDataType.Float: return FloatValue.ToString("R", CultureInfo.InvariantCulture);
                case CustomDataType.Bool: return BoolValue ? "true" : "false";
                case CustomDataType.String: return StringValue ?? "";
                case CustomDataType.Vector2: return JsonUtility.ToJson(Vector2Value);
                case CustomDataType.Vector3: return JsonUtility.ToJson(Vector3Value);
                case CustomDataType.Color: return JsonUtility.ToJson(ColorValue);
                case CustomDataType.Json:
                case CustomDataType.Custom:
                    return JsonValue ?? "";
                default:
                    return "";
            }
        }

        /// <summary>
        /// 从字符串反序列化到当前条目（需要指定类型）
        /// </summary>
        public bool TryDeserializeValue(string data, CustomDataType type, Type jsonClrType = null)
        {
            try
            {
                switch (type)
                {
                    case CustomDataType.Int:
                        IntValue = int.Parse(data, CultureInfo.InvariantCulture);
                        Type = CustomDataType.Int;
                        ClearExcept(CustomDataType.Int);
                        return true;
                    case CustomDataType.Float:
                        FloatValue = float.Parse(data, CultureInfo.InvariantCulture);
                        Type = CustomDataType.Float;
                        ClearExcept(CustomDataType.Float);
                        return true;
                    case CustomDataType.Bool:
                        BoolValue = string.Equals(data, "true", StringComparison.OrdinalIgnoreCase) ||
                                    string.Equals(data, "1", StringComparison.OrdinalIgnoreCase);
                        Type = CustomDataType.Bool;
                        ClearExcept(CustomDataType.Bool);
                        return true;
                    case CustomDataType.String:
                        StringValue = data;
                        Type = CustomDataType.String;
                        ClearExcept(CustomDataType.String);
                        return true;
                    case CustomDataType.Vector2:
                        Vector2Value = JsonUtility.FromJson<Vector2>(data);
                        Type = CustomDataType.Vector2;
                        ClearExcept(CustomDataType.Vector2);
                        return true;
                    case CustomDataType.Vector3:
                        Vector3Value = JsonUtility.FromJson<Vector3>(data);
                        Type = CustomDataType.Vector3;
                        ClearExcept(CustomDataType.Vector3);
                        return true;
                    case CustomDataType.Color:
                        ColorValue = JsonUtility.FromJson<Color>(data);
                        Type = CustomDataType.Color;
                        ClearExcept(CustomDataType.Color);
                        return true;
                    case CustomDataType.Json:
                        JsonValue = data;
                        JsonClrType = jsonClrType != null ? jsonClrType.AssemblyQualifiedName : JsonClrType;
                        Type = CustomDataType.Json;
                        ClearNonJson();
                        return true;
                    case CustomDataType.Custom:
                        if (Serializer == null) return false;
                        JsonValue = data;
                        JsonClrType = Serializer.TargetClrType != null ? Serializer.TargetClrType.AssemblyQualifiedName : null;
                        Type = CustomDataType.Custom;
                        ClearNonJson();
                        return true;
                    default:
                        return false;
                }
            }
            catch
            {
                return false;
            }
        }

        private void SetByType(CustomDataType type, object value, Type jsonClrType = null)
        {
            switch (type)
            {
                case CustomDataType.Int:
                    IntValue = Convert.ToInt32(value, CultureInfo.InvariantCulture);
                    Type = CustomDataType.Int;
                    ClearExcept(CustomDataType.Int);
                    break;
                case CustomDataType.Float:
                    FloatValue = Convert.ToSingle(value, CultureInfo.InvariantCulture);
                    Type = CustomDataType.Float;
                    ClearExcept(CustomDataType.Float);
                    break;
                case CustomDataType.Bool:
                    BoolValue = Convert.ToBoolean(value, CultureInfo.InvariantCulture);
                    Type = CustomDataType.Bool;
                    ClearExcept(CustomDataType.Bool);
                    break;
                case CustomDataType.String:
                    StringValue = value as string ?? value?.ToString();
                    Type = CustomDataType.String;
                    ClearExcept(CustomDataType.String);
                    break;
                case CustomDataType.Vector2:
                    Vector2Value = (Vector2)value;
                    Type = CustomDataType.Vector2;
                    ClearExcept(CustomDataType.Vector2);
                    break;
                case CustomDataType.Vector3:
                    Vector3Value = (Vector3)value;
                    Type = CustomDataType.Vector3;
                    ClearExcept(CustomDataType.Vector3);
                    break;
                case CustomDataType.Color:
                    ColorValue = (Color)value;
                    Type = CustomDataType.Color;
                    ClearExcept(CustomDataType.Color);
                    break;
                case CustomDataType.Json:
                    JsonValue = value is string s ? s : JsonUtility.ToJson(value);
                    JsonClrType = jsonClrType != null ? jsonClrType.AssemblyQualifiedName : (value?.GetType().AssemblyQualifiedName);
                    Type = CustomDataType.Json;
                    ClearNonJson();
                    break;
                case CustomDataType.Custom:
                    if (Serializer == null) throw new InvalidOperationException("CustomDataEntry.Serializer 未设置。");
                    JsonValue = Serializer.Serialize(value);
                    JsonClrType = Serializer.TargetClrType != null ? Serializer.TargetClrType.AssemblyQualifiedName : null;
                    Type = CustomDataType.Custom;
                    ClearNonJson();
                    break;
                default:
                    Type = CustomDataType.None;
                    ClearAll();
                    break;
            }
        }

        private void ClearAll()
        {
            IntValue = default;
            FloatValue = default;
            BoolValue = default;
            StringValue = default;
            Vector2Value = default;
            Vector3Value = default;
            ColorValue = default;
            JsonValue = default;
            JsonClrType = default;
        }

        private void ClearExcept(CustomDataType keep)
        {
            if (keep != CustomDataType.Int) IntValue = default;
            if (keep != CustomDataType.Float) FloatValue = default;
            if (keep != CustomDataType.Bool) BoolValue = default;
            if (keep != CustomDataType.String) StringValue = default;
            if (keep != CustomDataType.Vector2) Vector2Value = default;
            if (keep != CustomDataType.Vector3) Vector3Value = default;
            if (keep != CustomDataType.Color) ColorValue = default;
            if (keep != CustomDataType.Json && keep != CustomDataType.Custom)
            {
                JsonValue = default;
                JsonClrType = default;
            }
        }

        private void ClearNonJson()
        {
            IntValue = default;
            FloatValue = default;
            BoolValue = default;
            StringValue = default;
            Vector2Value = default;
            Vector3Value = default;
            ColorValue = default;
        }
    }

    /// <summary>
    /// 与 Dictionary<string, object> 互转
    /// </summary>
    public static class CustomDataUtility
    {
        // Dictionary -> List<CustomDataEntry>
        public static List<CustomDataEntry> ToEntries(Dictionary<string, object> dict, ICustomDataSerializer fallbackSerializer = null)
        {
            var list = new List<CustomDataEntry>();
            if (dict == null) return list;

            foreach (var kv in dict)
            {
                var entry = new CustomDataEntry { Id = kv.Key, Serializer = fallbackSerializer };
                entry.SetValue(kv.Value);
                list.Add(entry);
            }

            return list;
        }

        // List<CustomDataEntry> -> Dictionary
        public static Dictionary<string, object> ToDictionary(IEnumerable<CustomDataEntry> entries)
        {
            var dict = new Dictionary<string, object>();
            if (entries == null) return dict;

            foreach (var e in entries)
            {
                dict[e.Id] = e.GetValue();
            }
            return dict;
        }

        // 快速按键名获取强类型值
        public static bool TryGetValue<T>(IEnumerable<CustomDataEntry> entries, string id, out T value)
        {
            value = default;
            if (entries == null) return false;

            foreach (var e in entries)
            {
                if (e.Id != id) continue;

                var obj = e.GetValue();
                if (obj is T t)
                {
                    value = t;
                    return true;
                }

                // 若为 JSON 字符串尝试反序列化
                try
                {
                    if (obj is string json)
                    {
                        value = JsonUtility.FromJson<T>(json);
                        return true;
                    }
                }
                catch { }

                return false;
            }

            return false;
        }
    }
}
