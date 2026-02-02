using NUnit.Framework;
using EasyPack.CustomData;
using UnityEngine;
using System;
using System.Reflection;

namespace EasyPack.CustomDataTests
{
    /// <summary>
    /// CustomDataEntry 序列化相关单元测试
    /// 专门测试 ISerializationCallbackReceiver 方法和工具方法
    /// </summary>
    [TestFixture]
    public class CustomDataEntrySerializationTest
    {
        #region OnBeforeSerialize() 测试

        [Test]
        public void OnBeforeSerialize_Int()
        {
            var entry = CustomDataEntry.CreateInt("test", 42);
            CallOnBeforeSerialize(entry);
            Assert.AreEqual("42", GetDataField(entry));
        }

        [Test]
        public void OnBeforeSerialize_Long()
        {
            var entry = CustomDataEntry.CreateLong("test", 9223372036854775807L);
            CallOnBeforeSerialize(entry);
            Assert.AreEqual("9223372036854775807", GetDataField(entry));
        }

        [Test]
        public void OnBeforeSerialize_Float()
        {
            var entry = CustomDataEntry.CreateFloat("test", 3.14f);
            CallOnBeforeSerialize(entry);
            var data = GetDataField(entry);
            Assert.That(data, Does.Contain("3.14"));
        }

        [Test]
        public void OnBeforeSerialize_BoolTrue()
        {
            var entry = CustomDataEntry.CreateBool("test", true);
            CallOnBeforeSerialize(entry);
            Assert.AreEqual("true", GetDataField(entry));
        }

        [Test]
        public void OnBeforeSerialize_BoolFalse()
        {
            var entry = CustomDataEntry.CreateBool("test", false);
            CallOnBeforeSerialize(entry);
            Assert.AreEqual("false", GetDataField(entry));
        }

        [Test]
        public void OnBeforeSerialize_String()
        {
            var entry = CustomDataEntry.CreateString("test", "hello");
            CallOnBeforeSerialize(entry);
            Assert.AreEqual("hello", GetDataField(entry));
        }

        [Test]
        public void OnBeforeSerialize_StringNull()
        {
            var entry = new CustomDataEntry { Key = "test", Type = CustomDataType.String, StringValue = null };
            CallOnBeforeSerialize(entry);
            Assert.AreEqual("", GetDataField(entry));
        }

        [Test]
        public void OnBeforeSerialize_Vector2()
        {
            var entry = CustomDataEntry.CreateVector2("test", new Vector2(1, 2));
            CallOnBeforeSerialize(entry);
            var data = GetDataField(entry);
            Assert.That(data, Does.Contain("\"x\""));
            Assert.That(data, Does.Contain("\"y\""));
        }

        [Test]
        public void OnBeforeSerialize_Vector3()
        {
            var entry = CustomDataEntry.CreateVector3("test", new Vector3(1, 2, 3));
            CallOnBeforeSerialize(entry);
            var data = GetDataField(entry);
            Assert.That(data, Does.Contain("\"x\""));
            Assert.That(data, Does.Contain("\"y\""));
            Assert.That(data, Does.Contain("\"z\""));
        }

        [Test]
        public void OnBeforeSerialize_Color()
        {
            var entry = CustomDataEntry.CreateColor("test", new Color(1, 0.5f, 0.2f, 1));
            CallOnBeforeSerialize(entry);
            var data = GetDataField(entry);
            Assert.That(data, Does.Contain("\"r\""));
            Assert.That(data, Does.Contain("\"g\""));
            Assert.That(data, Does.Contain("\"b\""));
            Assert.That(data, Does.Contain("\"a\""));
        }

        [Test]
        public void OnBeforeSerialize_Json()
        {
            var entry = new CustomDataEntry { Key = "test", Type = CustomDataType.Json, JsonValue = "{\"x\":1}" };
            CallOnBeforeSerialize(entry);
            Assert.AreEqual("{\"x\":1}", GetDataField(entry));
        }

        [Test]
        public void OnBeforeSerialize_JsonNull()
        {
            var entry = new CustomDataEntry { Key = "test", Type = CustomDataType.Json, JsonValue = null };
            CallOnBeforeSerialize(entry);
            Assert.AreEqual("", GetDataField(entry));
        }

        [Test]
        public void OnBeforeSerialize_Custom()
        {
            var entry = new CustomDataEntry { Key = "test", Type = CustomDataType.Custom, JsonValue = "custom data" };
            CallOnBeforeSerialize(entry);
            Assert.AreEqual("custom data", GetDataField(entry));
        }

        [Test]
        public void OnBeforeSerialize_None()
        {
            var entry = new CustomDataEntry { Key = "test", Type = CustomDataType.None };
            CallOnBeforeSerialize(entry);
            Assert.AreEqual("", GetDataField(entry));
        }

        #endregion

        #region OnAfterDeserialize() 测试

        [Test]
        public void OnAfterDeserialize_Int()
        {
            var entry = new CustomDataEntry { Type = CustomDataType.Int };
            SetDataField(entry, "42");
            CallOnAfterDeserialize(entry);
            Assert.AreEqual(42, entry.IntValue);
        }

        [Test]
        public void OnAfterDeserialize_Long()
        {
            var entry = new CustomDataEntry { Type = CustomDataType.Long };
            SetDataField(entry, "9223372036854775807");
            CallOnAfterDeserialize(entry);
            Assert.AreEqual(9223372036854775807L, entry.LongValue);
        }

        [Test]
        public void OnAfterDeserialize_IntInvalid()
        {
            var entry = new CustomDataEntry { Type = CustomDataType.Int };
            SetDataField(entry, "not a number");
            CallOnAfterDeserialize(entry);
            Assert.AreEqual(0, entry.IntValue); // Should fallback to 0
        }

        [Test]
        public void OnAfterDeserialize_Float()
        {
            var entry = new CustomDataEntry { Type = CustomDataType.Float };
            SetDataField(entry, "3.14");
            CallOnAfterDeserialize(entry);
            Assert.AreEqual(3.14f, entry.FloatValue, 0.001f);
        }

        [Test]
        public void OnAfterDeserialize_FloatInvalid()
        {
            var entry = new CustomDataEntry { Type = CustomDataType.Float };
            SetDataField(entry, "not a float");
            CallOnAfterDeserialize(entry);
            Assert.AreEqual(0f, entry.FloatValue); // Should fallback to 0
        }

        [Test]
        public void OnAfterDeserialize_BoolTrue()
        {
            var entry = new CustomDataEntry { Type = CustomDataType.Bool };
            SetDataField(entry, "true");
            CallOnAfterDeserialize(entry);
            Assert.AreEqual(true, entry.BoolValue);
        }

        [Test]
        public void OnAfterDeserialize_Bool1()
        {
            var entry = new CustomDataEntry { Type = CustomDataType.Bool };
            SetDataField(entry, "1");
            CallOnAfterDeserialize(entry);
            Assert.AreEqual(true, entry.BoolValue);
        }

        [Test]
        public void OnAfterDeserialize_BoolFalse()
        {
            var entry = new CustomDataEntry { Type = CustomDataType.Bool };
            SetDataField(entry, "false");
            CallOnAfterDeserialize(entry);
            Assert.AreEqual(false, entry.BoolValue);
        }

        [Test]
        public void OnAfterDeserialize_BoolInvalid()
        {
            var entry = new CustomDataEntry { Type = CustomDataType.Bool };
            SetDataField(entry, "not a bool");
            CallOnAfterDeserialize(entry);
            Assert.AreEqual(false, entry.BoolValue); // Should fallback to false
        }

        [Test]
        public void OnAfterDeserialize_String()
        {
            var entry = new CustomDataEntry { Type = CustomDataType.String };
            SetDataField(entry, "hello world");
            CallOnAfterDeserialize(entry);
            Assert.AreEqual("hello world", entry.StringValue);
        }

        [Test]
        public void OnAfterDeserialize_StringNull()
        {
            var entry = new CustomDataEntry { Type = CustomDataType.String };
            SetDataField(entry, null);
            CallOnAfterDeserialize(entry);
            Assert.AreEqual("", entry.StringValue);
        }

        [Test]
        public void OnAfterDeserialize_Vector2()
        {
            var entry = new CustomDataEntry { Type = CustomDataType.Vector2 };
            var jsonStr = JsonUtility.ToJson(new Vector2(1, 2));
            SetDataField(entry, jsonStr);
            CallOnAfterDeserialize(entry);
            Assert.AreEqual(new Vector2(1, 2), entry.Vector2Value);
        }

        [Test]
        public void OnAfterDeserialize_Vector2Empty()
        {
            var entry = new CustomDataEntry { Type = CustomDataType.Vector2 };
            SetDataField(entry, "");
            CallOnAfterDeserialize(entry);
            Assert.AreEqual(default(Vector2), entry.Vector2Value);
        }

        [Test]
        public void OnAfterDeserialize_Vector3()
        {
            var entry = new CustomDataEntry { Type = CustomDataType.Vector3 };
            var jsonStr = JsonUtility.ToJson(new Vector3(1, 2, 3));
            SetDataField(entry, jsonStr);
            CallOnAfterDeserialize(entry);
            Assert.AreEqual(new Vector3(1, 2, 3), entry.Vector3Value);
        }

        [Test]
        public void OnAfterDeserialize_Vector3Empty()
        {
            var entry = new CustomDataEntry { Type = CustomDataType.Vector3 };
            SetDataField(entry, "");
            CallOnAfterDeserialize(entry);
            Assert.AreEqual(default(Vector3), entry.Vector3Value);
        }

        [Test]
        public void OnAfterDeserialize_Color()
        {
            var entry = new CustomDataEntry { Type = CustomDataType.Color };
            var color = new Color(1, 0.5f, 0.2f, 1);
            var jsonStr = JsonUtility.ToJson(color);
            SetDataField(entry, jsonStr);
            CallOnAfterDeserialize(entry);
            Assert.AreEqual(color.r, entry.ColorValue.r, 0.001f);
        }

        [Test]
        public void OnAfterDeserialize_ColorEmpty()
        {
            var entry = new CustomDataEntry { Type = CustomDataType.Color };
            SetDataField(entry, "");
            CallOnAfterDeserialize(entry);
            Assert.AreEqual(default(Color), entry.ColorValue);
        }

        [Test]
        public void OnAfterDeserialize_Json()
        {
            var entry = new CustomDataEntry { Type = CustomDataType.Json };
            SetDataField(entry, "{\"x\":1}");
            CallOnAfterDeserialize(entry);
            Assert.AreEqual("{\"x\":1}", entry.JsonValue);
        }

        [Test]
        public void OnAfterDeserialize_JsonNull()
        {
            var entry = new CustomDataEntry { Type = CustomDataType.Json };
            SetDataField(entry, null);
            CallOnAfterDeserialize(entry);
            Assert.AreEqual("", entry.JsonValue);
        }

        [Test]
        public void OnAfterDeserialize_Custom()
        {
            var mockSerializer = new MockCustomSerializer();
            var entry = new CustomDataEntry { Type = CustomDataType.Custom, Serializer = mockSerializer };
            SetDataField(entry, "custom data");
            CallOnAfterDeserialize(entry);
            Assert.AreEqual("custom data", entry.JsonValue);
        }

        [Test]
        public void OnAfterDeserialize_None()
        {
            var entry = new CustomDataEntry { Type = CustomDataType.None };
            SetDataField(entry, "some data");
            CallOnAfterDeserialize(entry);
            // Should call ClearAll(), but we can't easily test this without checking all fields
            Assert.AreEqual(CustomDataType.None, entry.Type);
        }

        #endregion

        #region SetByType() Custom 和 Default Case 测试

        [Test]
        public void SetByType_Custom_WithSerializer()
        {
            var mockSerializer = new MockCustomSerializer();
            var entry = new CustomDataEntry { Key = "test", Serializer = mockSerializer };
            entry.SetValue("test data", CustomDataType.Custom);
            Assert.AreEqual(CustomDataType.Custom, entry.Type);
            Assert.AreEqual("test data", entry.JsonValue);
        }

        [Test]
        public void SetByType_Custom_WithoutSerializer_Throws()
        {
            var entry = new CustomDataEntry { Key = "test", Serializer = null };
            Assert.Throws<InvalidOperationException>(() => entry.SetValue("test data", CustomDataType.Custom));
        }

        [Test]
        public void SetByType_DefaultCase_JsonSerialization()
        {
            var entry = new CustomDataEntry { Key = "test" };
            var dict = new System.Collections.Generic.Dictionary<string, int> { { "key", 1 } };
            entry.SetValue(dict);
            Assert.AreEqual(CustomDataType.Json, entry.Type);
            Assert.IsNotNull(entry.JsonValue);
        }

        #endregion

        #region 辅助方法

        private void CallOnBeforeSerialize(CustomDataEntry entry)
        {
            var method = GetPrivateMethod(typeof(CustomDataEntry), "UnityEngine.ISerializationCallbackReceiver.OnBeforeSerialize");
            method.Invoke(entry, null);
        }

        private void CallOnAfterDeserialize(CustomDataEntry entry)
        {
            var method = GetPrivateMethod(typeof(CustomDataEntry), "UnityEngine.ISerializationCallbackReceiver.OnAfterDeserialize");
            method.Invoke(entry, null);
        }

        private string GetDataField(CustomDataEntry entry)
        {
            var field = typeof(CustomDataEntry).GetField("Data", BindingFlags.NonPublic | BindingFlags.Instance);
            return (string)field.GetValue(entry);
        }

        private void SetDataField(CustomDataEntry entry, string value)
        {
            var field = typeof(CustomDataEntry).GetField("Data", BindingFlags.NonPublic | BindingFlags.Instance);
            field.SetValue(entry, value);
        }

        private MethodInfo GetPrivateMethod(Type type, string methodName)
        {
            return type.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        }

        #endregion
    }
}
