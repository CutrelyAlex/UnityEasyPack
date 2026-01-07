using System;
using System.Collections.Generic;
using UnityEngine;

namespace EasyPack.CustomData
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
        #region 字段

        public string Key;
        public CustomDataType Type = CustomDataType.None;

        [NonSerialized] public int IntValue;
        [NonSerialized] public long LongValue;
        [NonSerialized] public float FloatValue;
        [NonSerialized] public bool BoolValue;
        [NonSerialized] public string StringValue;
        [NonSerialized] public Vector2 Vector2Value;
        [NonSerialized] public Vector3 Vector3Value;
        [NonSerialized] public Color ColorValue;

        [NonSerialized] public string JsonValue;

        [NonSerialized] public CustomDataCollection CustomDataListValue;

        public string JsonClrType;

        [NonSerialized] public ICustomDataSerializer Serializer;

        [SerializeField] private string Data;

        #endregion

        #region 静态工厂方法

        public static CustomDataEntry CreateString(string key, string value) => new()
        {
            Key = key, Type = CustomDataType.String, StringValue = value ?? "",
        };

        public static CustomDataEntry CreateInt(string key, int value) =>
            new() { Key = key, Type = CustomDataType.Int, IntValue = value };

        public static CustomDataEntry CreateLong(string key, long value) =>
            new() { Key = key, Type = CustomDataType.Long, LongValue = value };

        public static CustomDataEntry CreateFloat(string key, float value) =>
            new() { Key = key, Type = CustomDataType.Float, FloatValue = value };

        public static CustomDataEntry CreateBool(string key, bool value) =>
            new() { Key = key, Type = CustomDataType.Bool, BoolValue = value };

        public static CustomDataEntry CreateVector2(string key, Vector2 value) => new()
        {
            Key = key, Type = CustomDataType.Vector2, Vector2Value = value,
        };

        public static CustomDataEntry CreateVector3(string key, Vector3 value) => new()
        {
            Key = key, Type = CustomDataType.Vector3, Vector3Value = value,
        };

        public static CustomDataEntry CreateColor(string key, Color value) =>
            new() { Key = key, Type = CustomDataType.Color, ColorValue = value };

        public static CustomDataEntry CreateJson(string key, object jsonObject)
        {
            var entry = new CustomDataEntry { Key = key, Type = CustomDataType.Json };
            if (jsonObject != null)
            {
                entry.JsonValue = JsonUtility.ToJson(jsonObject);
                entry.JsonClrType = jsonObject.GetType().AssemblyQualifiedName;
            }

            return entry;
        }

        public static CustomDataEntry CreateCustomDataList(string key, CustomDataCollection list) => new()
        {
            Key = key, Type = CustomDataType.CustomDataList, CustomDataListValue = list ?? new(),
        };

        #endregion

        #region 值获取与设置

        public object GetValue()
        {
            IValueHandler handler = ValueHandlerRegistry.GetHandler(Type);
            return handler.GetValue(this);
        }

        public override bool Equals(object obj)
        {
            if (obj is not CustomDataEntry other) return false;
            if (Key != other.Key || Type != other.Type) return false;

            return Type switch
            {
                CustomDataType.String => StringValue == other.StringValue,
                CustomDataType.Int => IntValue == other.IntValue,
                CustomDataType.Long => LongValue == other.LongValue,
                CustomDataType.Float => Mathf.Approximately(FloatValue, other.FloatValue),
                CustomDataType.Bool => BoolValue == other.BoolValue,
                CustomDataType.Vector2 => Vector2Value == other.Vector2Value,
                CustomDataType.Vector3 => Vector3Value == other.Vector3Value,
                CustomDataType.Color => ColorValue == other.ColorValue,
                CustomDataType.Json => JsonValue == other.JsonValue && JsonClrType == other.JsonClrType,
                CustomDataType.CustomDataList => (CustomDataListValue == null && other.CustomDataListValue == null) || 
                                                 (CustomDataListValue != null && CustomDataListValue.Equals(other.CustomDataListValue)),
                _ => true
            };
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Key, Type);
        }

        public void SetValue(object value, CustomDataType? forceType = null)
        {
            if (forceType.HasValue)
            {
                IValueHandler handler = ValueHandlerRegistry.GetHandler(forceType.Value);
                handler.SetValue(this, value);
                return;
            }

            if (value == null)
            {
                IValueHandler noneHandler = ValueHandlerRegistry.GetHandler(CustomDataType.None);
                noneHandler.SetValue(this, null);
                return;
            }

            IValueHandler valueHandler = ValueHandlerRegistry.GetHandlerForValue(value);
            valueHandler.SetValue(this, value);
        }

        public string SerializeValue()
        {
            IValueHandler handler = ValueHandlerRegistry.GetHandler(Type);
            return handler.Serialize(this);
        }

        public bool TryDeserializeValue(string data, CustomDataType type, Type jsonClrType = null)
        {
            try
            {
                IValueHandler handler = ValueHandlerRegistry.GetHandler(type);
                return handler.TryDeserialize(this, data, jsonClrType);
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region 序列化回调

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            IValueHandler handler = ValueHandlerRegistry.GetHandler(Type);
            Data = handler.Serialize(this);
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            IValueHandler handler = ValueHandlerRegistry.GetHandler(Type);
            handler.TryDeserialize(this, Data ?? "");
        }

        #endregion
    }
}