using System;
using System.Globalization;
using UnityEngine;

namespace EasyPack
{
    public interface ICustomDataSerializer
    {
        Type TargetClrType { get; }
        string Serialize(object value);
        object Deserialize(string data);
    }

    [Serializable]
    public class CustomDataEntry : ISerializationCallbackReceiver
    {
        public string Id;
        public CustomDataType Type = CustomDataType.None;

        [NonSerialized] public int IntValue;
        [NonSerialized] public float FloatValue;
        [NonSerialized] public bool BoolValue;
        [NonSerialized] public string StringValue;
        [NonSerialized] public Vector2 Vector2Value;
        [NonSerialized] public Vector3 Vector3Value;
        [NonSerialized] public Color ColorValue;

        // 复杂类型值（仅运行期保留）
        [NonSerialized] public string JsonValue;

        public string JsonClrType;

        [NonSerialized] public ICustomDataSerializer Serializer;

        [SerializeField] private string Data;

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
                    return JsonValue;
                case CustomDataType.Custom:
                    if (Serializer == null || string.IsNullOrEmpty(JsonValue)) return null;
                    try { return Serializer.Deserialize(JsonValue); }
                    catch { return null; }
                default:
                    return null;
            }
        }

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
                    try
                    {
                        JsonValue = JsonUtility.ToJson(value);
                        JsonClrType = value.GetType().AssemblyQualifiedName;
                        Type = CustomDataType.Json;
                        ClearNonJson();
                    }
                    catch
                    {
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
            Data = default;
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

        // 仅序列化所需字段
        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            switch (Type)
            {
                case CustomDataType.Int:
                    Data = IntValue.ToString(CultureInfo.InvariantCulture);
                    break;
                case CustomDataType.Float:
                    Data = FloatValue.ToString("R", CultureInfo.InvariantCulture);
                    break;
                case CustomDataType.Bool:
                    Data = BoolValue ? "true" : "false";
                    break;
                case CustomDataType.String:
                    Data = StringValue ?? "";
                    break;
                case CustomDataType.Vector2:
                    Data = JsonUtility.ToJson(Vector2Value);
                    break;
                case CustomDataType.Vector3:
                    Data = JsonUtility.ToJson(Vector3Value);
                    break;
                case CustomDataType.Color:
                    Data = JsonUtility.ToJson(ColorValue);
                    break;
                case CustomDataType.Json:
                case CustomDataType.Custom:
                    Data = JsonValue ?? "";
                    break;
                default:
                    Data = "";
                    break;
            }
        }

        // 反序列化时若缺失则回退为0/默认
        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            // Data 可能为 null/空，按需求回退为默认
            switch (Type)
            {
                case CustomDataType.Int:
                    IntValue = TryParseInt(Data);
                    break;
                case CustomDataType.Float:
                    FloatValue = TryParseFloat(Data);
                    break;
                case CustomDataType.Bool:
                    BoolValue = string.Equals(Data, "true", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(Data, "1", StringComparison.OrdinalIgnoreCase);
                    break;
                case CustomDataType.String:
                    StringValue = Data ?? "";
                    break;
                case CustomDataType.Vector2:
                    Vector2Value = !string.IsNullOrEmpty(Data) ? JsonUtility.FromJson<Vector2>(Data) : default;
                    break;
                case CustomDataType.Vector3:
                    Vector3Value = !string.IsNullOrEmpty(Data) ? JsonUtility.FromJson<Vector3>(Data) : default;
                    break;
                case CustomDataType.Color:
                    ColorValue = !string.IsNullOrEmpty(Data) ? JsonUtility.FromJson<Color>(Data) : default;
                    break;
                case CustomDataType.Json:
                    JsonValue = Data ?? "";
                    break;
                case CustomDataType.Custom:
                    JsonValue = Data ?? "";
                    break;
                default:
                    ClearAll();
                    break;
            }
        }

        private static int TryParseInt(string s)
            => int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : 0;

        private static float TryParseFloat(string s)
            => float.TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var v) ? v : 0f;
    }
}