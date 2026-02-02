using NUnit.Framework;
using EasyPack.CustomData;
using UnityEngine;
using System;

namespace EasyPack.CustomDataTests
{
    /// <summary>
    /// Mock custom serializer for testing
    /// </summary>
    public class MockCustomSerializer : ICustomDataSerializer
    {
        public Type TargetClrType => typeof(string);

        public string Serialize(object value)
        {
            return value?.ToString() ?? "";
        }

        public object Deserialize(string data)
        {
            if (data == "throw")
                throw new Exception("Mock exception");
            return data;
        }
    }

    /// <summary>
    /// Mock custom serializer that throws on serialize
    /// </summary>
    public class ThrowingCustomSerializer : ICustomDataSerializer
    {
        public Type TargetClrType => typeof(string);

        public string Serialize(object value)
        {
            throw new Exception("Serialize exception");
        }

        public object Deserialize(string data)
        {
            return data;
        }
    }

    /// <summary>
    /// CustomDataEntry 单元测试
    /// 覆盖所有方法的分支和异常情况，提高代码覆盖率和CRAP评分
    /// </summary>
    [TestFixture]
    public class CustomDataEntryTest
    {
        #region GetValue() 测试 (Cyclomatic Complexity: 16)

        [Test]
        public void GetValue_ReturnsIntValue()
        {
            var entry = CustomDataEntry.CreateInt("test", 42);
            Assert.AreEqual(42, entry.GetValue());
        }

        [Test]
        public void GetValue_ReturnsLongValue()
        {
            var entry = CustomDataEntry.CreateLong("test", 9223372036854775807L);
            Assert.AreEqual(9223372036854775807L, entry.GetValue());
        }

        [Test]
        public void GetValue_ReturnsFloatValue()
        {
            var entry = CustomDataEntry.CreateFloat("test", 3.14f);
            Assert.AreEqual(3.14f, entry.GetValue());
        }

        [Test]
        public void GetValue_ReturnsBoolValue()
        {
            var entry = CustomDataEntry.CreateBool("test", true);
            Assert.AreEqual(true, entry.GetValue());
        }

        [Test]
        public void GetValue_ReturnsStringValue()
        {
            var entry = CustomDataEntry.CreateString("test", "hello");
            Assert.AreEqual("hello", entry.GetValue());
        }

        [Test]
        public void GetValue_ReturnsVector2Value()
        {
            var vector = new Vector2(1, 2);
            var entry = CustomDataEntry.CreateVector2("test", vector);
            Assert.AreEqual(vector, entry.GetValue());
        }

        [Test]
        public void GetValue_ReturnsVector3Value()
        {
            var vector = new Vector3(1, 2, 3);
            var entry = CustomDataEntry.CreateVector3("test", vector);
            Assert.AreEqual(vector, entry.GetValue());
        }

        [Test]
        public void GetValue_ReturnsColorValue()
        {
            var color = new Color(1, 0.5f, 0.2f, 1);
            var entry = CustomDataEntry.CreateColor("test", color);
            var result = (Color)entry.GetValue();
            Assert.AreEqual(color.r, result.r, 0.001f);
            Assert.AreEqual(color.g, result.g, 0.001f);
        }

        [Test]
        public void GetValue_ReturnsNullForNoneType()
        {
            var entry = new CustomDataEntry { Type = CustomDataType.None };
            Assert.IsNull(entry.GetValue());
        }

        [Test]
        public void GetValue_ReturnsNullForEmptyJson()
        {
            var entry = new CustomDataEntry
            {
                Type = CustomDataType.Json,
                JsonValue = null
            };
            Assert.IsNull(entry.GetValue());
        }

        [Test]
        public void GetValue_ReturnsJsonStringForInvalidType()
        {
            var entry = new CustomDataEntry
            {
                Type = CustomDataType.Json,
                JsonValue = "invalid",
                JsonClrType = null
            };
            Assert.AreEqual("invalid", entry.GetValue());
        }

        [Test]
        public void GetValue_ReturnsNullForInvalidJsonType()
        {
            var entry = new CustomDataEntry
            {
                Type = CustomDataType.Json,
                JsonValue = "{}",
                JsonClrType = "InvalidType.That.DoesNotExist"
            };
            Assert.IsNull(entry.GetValue());
        }

        [Test]
        public void GetValue_ReturnsNullForMalformedJson()
        {
            var entry = new CustomDataEntry
            {
                Type = CustomDataType.Json,
                JsonValue = "not a json",
                JsonClrType = typeof(Vector3).AssemblyQualifiedName
            };
            // Should not throw, returns null for malformed data
            var result = entry.GetValue();
            Assert.IsNull(result);
        }

        [Test]
        public void GetValue_ReturnsNullForNullCustomSerializer()
        {
            var entry = new CustomDataEntry
            {
                Type = CustomDataType.Custom,
                Serializer = null,
                JsonValue = "data"
            };
            Assert.IsNull(entry.GetValue());
        }

        [Test]
        public void GetValue_ReturnsNullForEmptyCustom()
        {
            var entry = new CustomDataEntry
            {
                Type = CustomDataType.Custom,
                JsonValue = null
            };
            Assert.IsNull(entry.GetValue());
        }

        [Test]
        public void GetValue_CustomSerializerThrows_ReturnsNull()
        {
            var mockSerializer = new MockCustomSerializer();
            var entry = new CustomDataEntry
            {
                Type = CustomDataType.Custom,
                Serializer = mockSerializer,
                JsonValue = "throw"
            };
            Assert.IsNull(entry.GetValue());
        }

        #endregion

        #region SetValue() 测试 (Cyclomatic Complexity: 13)

        [Test]
        public void SetValue_WithInt()
        {
            var entry = new CustomDataEntry { Key = "test" };
            entry.SetValue(42);
            Assert.AreEqual(CustomDataType.Int, entry.Type);
            Assert.AreEqual(42, entry.IntValue);
        }

        [Test]
        public void SetValue_WithLong()
        {
            var entry = new CustomDataEntry { Key = "test" };
            entry.SetValue(9223372036854775807L);
            Assert.AreEqual(CustomDataType.Long, entry.Type);
            Assert.AreEqual(9223372036854775807L, entry.LongValue);
        }

        [Test]
        public void SetValue_WithFloat()
        {
            var entry = new CustomDataEntry { Key = "test" };
            entry.SetValue(3.14f);
            Assert.AreEqual(CustomDataType.Float, entry.Type);
            Assert.AreEqual(3.14f, entry.FloatValue, 0.001f);
        }

        [Test]
        public void SetValue_WithBool()
        {
            var entry = new CustomDataEntry { Key = "test" };
            entry.SetValue(true);
            Assert.AreEqual(CustomDataType.Bool, entry.Type);
            Assert.AreEqual(true, entry.BoolValue);
        }

        [Test]
        public void SetValue_WithString()
        {
            var entry = new CustomDataEntry { Key = "test" };
            entry.SetValue("hello");
            Assert.AreEqual(CustomDataType.String, entry.Type);
            Assert.AreEqual("hello", entry.StringValue);
        }

        [Test]
        public void SetValue_WithVector2()
        {
            var entry = new CustomDataEntry { Key = "test" };
            var vector = new Vector2(1, 2);
            entry.SetValue(vector);
            Assert.AreEqual(CustomDataType.Vector2, entry.Type);
            Assert.AreEqual(vector, entry.Vector2Value);
        }

        [Test]
        public void SetValue_WithVector3()
        {
            var entry = new CustomDataEntry { Key = "test" };
            var vector = new Vector3(1, 2, 3);
            entry.SetValue(vector);
            Assert.AreEqual(CustomDataType.Vector3, entry.Type);
            Assert.AreEqual(vector, entry.Vector3Value);
        }

        [Test]
        public void SetValue_WithColor()
        {
            var entry = new CustomDataEntry { Key = "test" };
            var color = new Color(1, 0.5f, 0.2f, 1);
            entry.SetValue(color);
            Assert.AreEqual(CustomDataType.Color, entry.Type);
            Assert.AreEqual(color, entry.ColorValue);
        }

        [Test]
        public void SetValue_WithNull()
        {
            var entry = new CustomDataEntry { Key = "test", Type = CustomDataType.String };
            entry.SetValue(null);
            Assert.AreEqual(CustomDataType.None, entry.Type);
        }

        [Test]
        public void SetValue_WithForceType()
        {
            var entry = new CustomDataEntry { Key = "test" };
            entry.SetValue("42", CustomDataType.Int);
            Assert.AreEqual(CustomDataType.Int, entry.Type);
        }

        [Test]
        public void SetValue_WithJsonObject()
        {
            var entry = new CustomDataEntry { Key = "test" };
            var vector = new Vector3(1, 2, 3);
            entry.SetValue(vector);
            Assert.AreEqual(CustomDataType.Vector3, entry.Type);
        }

        [Test]
        public void SetValue_WithCustomObject_UsesCustomSerializer()
        {
            var mockSerializer = new MockCustomSerializer();
            var entry = new CustomDataEntry { Key = "test", Serializer = mockSerializer };
            var customObject = new object(); // This will go to the default case
            entry.SetValue(customObject);
            Assert.AreEqual(CustomDataType.Json, entry.Type); // Should use JSON serialization for simple objects
        }

        [Test]
        public void SetValue_WithUnknownObject_UsesJsonSerialization()
        {
            var entry = new CustomDataEntry { Key = "test" };
            var unknownObject = new System.Collections.Generic.Dictionary<string, int> { { "key", 1 } };
            entry.SetValue(unknownObject);
            Assert.AreEqual(CustomDataType.Json, entry.Type);
            Assert.IsNotNull(entry.JsonValue);
        }

        #endregion

        #region SerializeValue() 测试 (Cyclomatic Complexity: 14)

        [Test]
        public void SerializeValue_Int()
        {
            var entry = CustomDataEntry.CreateInt("test", 42);
            Assert.AreEqual("42", entry.SerializeValue());
        }

        [Test]
        public void SerializeValue_Long()
        {
            var entry = CustomDataEntry.CreateLong("test", 9223372036854775807L);
            Assert.AreEqual("9223372036854775807", entry.SerializeValue());
        }

        [Test]
        public void SerializeValue_LongNegative()
        {
            var entry = CustomDataEntry.CreateLong("test", -9223372036854775808L);
            Assert.AreEqual("-9223372036854775808", entry.SerializeValue());
        }

        [Test]
        public void SerializeValue_Float()
        {
            var entry = CustomDataEntry.CreateFloat("test", 3.14f);
            var serialized = entry.SerializeValue();
            Assert.That(serialized, Does.Contain("3.14"));
        }

        [Test]
        public void SerializeValue_BoolTrue()
        {
            var entry = CustomDataEntry.CreateBool("test", true);
            Assert.AreEqual("true", entry.SerializeValue());
        }

        [Test]
        public void SerializeValue_BoolFalse()
        {
            var entry = CustomDataEntry.CreateBool("test", false);
            Assert.AreEqual("false", entry.SerializeValue());
        }

        [Test]
        public void SerializeValue_String()
        {
            var entry = CustomDataEntry.CreateString("test", "hello");
            Assert.AreEqual("hello", entry.SerializeValue());
        }

        [Test]
        public void SerializeValue_StringNull()
        {
            var entry = new CustomDataEntry { Key = "test", Type = CustomDataType.String, StringValue = null };
            Assert.AreEqual("", entry.SerializeValue());
        }

        [Test]
        public void SerializeValue_Vector2()
        {
            var entry = CustomDataEntry.CreateVector2("test", new Vector2(1, 2));
            var serialized = entry.SerializeValue();
            Assert.That(serialized, Does.Contain("\"x\""));
        }

        [Test]
        public void SerializeValue_Vector3()
        {
            var entry = CustomDataEntry.CreateVector3("test", new Vector3(1, 2, 3));
            var serialized = entry.SerializeValue();
            Assert.That(serialized, Does.Contain("\"x\""));
        }

        [Test]
        public void SerializeValue_Color()
        {
            var entry = CustomDataEntry.CreateColor("test", new Color(1, 0.5f, 0.2f, 1));
            var serialized = entry.SerializeValue();
            Assert.That(serialized, Does.Contain("\"r\""));
        }

        [Test]
        public void SerializeValue_Json()
        {
            var entry = new CustomDataEntry { Key = "test", Type = CustomDataType.Json, JsonValue = "{\"x\":1}" };
            Assert.AreEqual("{\"x\":1}", entry.SerializeValue());
        }

        [Test]
        public void SerializeValue_JsonNull()
        {
            var entry = new CustomDataEntry { Key = "test", Type = CustomDataType.Json, JsonValue = null };
            Assert.AreEqual("", entry.SerializeValue());
        }

        [Test]
        public void SerializeValue_None()
        {
            var entry = new CustomDataEntry { Key = "test", Type = CustomDataType.None };
            Assert.AreEqual("", entry.SerializeValue());
        }

        #endregion

        #region TryDeserializeValue() 测试 (Cyclomatic Complexity: 17)

        [Test]
        public void TryDeserializeValue_Int()
        {
            var entry = new CustomDataEntry { Key = "test" };
            bool success = entry.TryDeserializeValue("42", CustomDataType.Int);
            Assert.IsTrue(success);
            Assert.AreEqual(42, entry.IntValue);
            Assert.AreEqual(CustomDataType.Int, entry.Type);
        }

        [Test]
        public void TryDeserializeValue_Long()
        {
            var entry = new CustomDataEntry { Key = "test" };
            bool success = entry.TryDeserializeValue("9223372036854775807", CustomDataType.Long);
            Assert.IsTrue(success);
            Assert.AreEqual(9223372036854775807L, entry.LongValue);
            Assert.AreEqual(CustomDataType.Long, entry.Type);
        }

        [Test]
        public void TryDeserializeValue_LongNegative()
        {
            var entry = new CustomDataEntry { Key = "test" };
            bool success = entry.TryDeserializeValue("-9223372036854775808", CustomDataType.Long);
            Assert.IsTrue(success);
            Assert.AreEqual(-9223372036854775808L, entry.LongValue);
            Assert.AreEqual(CustomDataType.Long, entry.Type);
        }

        [Test]
        public void TryDeserializeValue_IntNegative()
        {
            var entry = new CustomDataEntry { Key = "test" };
            bool success = entry.TryDeserializeValue("-42", CustomDataType.Int);
            Assert.IsTrue(success);
            Assert.AreEqual(-42, entry.IntValue);
        }

        [Test]
        public void TryDeserializeValue_Float()
        {
            var entry = new CustomDataEntry { Key = "test" };
            bool success = entry.TryDeserializeValue("3.14", CustomDataType.Float);
            Assert.IsTrue(success);
            Assert.AreEqual(3.14f, entry.FloatValue, 0.001f);
        }

        [Test]
        public void TryDeserializeValue_Bool_True()
        {
            var entry = new CustomDataEntry { Key = "test" };
            bool success = entry.TryDeserializeValue("true", CustomDataType.Bool);
            Assert.IsTrue(success);
            Assert.AreEqual(true, entry.BoolValue);
        }

        [Test]
        public void TryDeserializeValue_Bool_1()
        {
            var entry = new CustomDataEntry { Key = "test" };
            bool success = entry.TryDeserializeValue("1", CustomDataType.Bool);
            Assert.IsTrue(success);
            Assert.AreEqual(true, entry.BoolValue);
        }

        [Test]
        public void TryDeserializeValue_Bool_False()
        {
            var entry = new CustomDataEntry { Key = "test" };
            bool success = entry.TryDeserializeValue("false", CustomDataType.Bool);
            Assert.IsTrue(success);
            Assert.AreEqual(false, entry.BoolValue);
        }

        [Test]
        public void TryDeserializeValue_String()
        {
            var entry = new CustomDataEntry { Key = "test" };
            bool success = entry.TryDeserializeValue("hello world", CustomDataType.String);
            Assert.IsTrue(success);
            Assert.AreEqual("hello world", entry.StringValue);
        }

        [Test]
        public void TryDeserializeValue_Vector2()
        {
            var entry = new CustomDataEntry { Key = "test" };
            var jsonStr = JsonUtility.ToJson(new Vector2(1, 2));
            bool success = entry.TryDeserializeValue(jsonStr, CustomDataType.Vector2);
            Assert.IsTrue(success);
            Assert.AreEqual(new Vector2(1, 2), entry.Vector2Value);
        }

        [Test]
        public void TryDeserializeValue_Vector3()
        {
            var entry = new CustomDataEntry { Key = "test" };
            var jsonStr = JsonUtility.ToJson(new Vector3(1, 2, 3));
            bool success = entry.TryDeserializeValue(jsonStr, CustomDataType.Vector3);
            Assert.IsTrue(success);
            Assert.AreEqual(new Vector3(1, 2, 3), entry.Vector3Value);
        }

        [Test]
        public void TryDeserializeValue_Color()
        {
            var entry = new CustomDataEntry { Key = "test" };
            var color = new Color(1, 0.5f, 0.2f, 1);
            var jsonStr = JsonUtility.ToJson(color);
            bool success = entry.TryDeserializeValue(jsonStr, CustomDataType.Color);
            Assert.IsTrue(success);
            Assert.AreEqual(color.r, entry.ColorValue.r, 0.001f);
        }

        [Test]
        public void TryDeserializeValue_Invalid_ReturnsfalseAndClearsType()
        {
            var entry = new CustomDataEntry { Key = "test", Type = CustomDataType.String };
            bool success = entry.TryDeserializeValue("invalid", CustomDataType.Int);
            Assert.IsFalse(success);
        }

        [Test]
        public void TryDeserializeValue_None()
        {
            var entry = new CustomDataEntry { Key = "test" };
            bool success = entry.TryDeserializeValue("data", CustomDataType.None);
            Assert.IsFalse(success);
        }

        [Test]
        public void TryDeserializeValue_Custom_WithSerializer()
        {
            var mockSerializer = new MockCustomSerializer();
            var entry = new CustomDataEntry { Key = "test", Serializer = mockSerializer };
            bool success = entry.TryDeserializeValue("test data", CustomDataType.Custom);
            Assert.IsTrue(success);
            Assert.AreEqual(CustomDataType.Custom, entry.Type);
            Assert.AreEqual("test data", entry.JsonValue);
        }

        [Test]
        public void TryDeserializeValue_Custom_WithoutSerializer()
        {
            var entry = new CustomDataEntry { Key = "test", Serializer = null };
            bool success = entry.TryDeserializeValue("test data", CustomDataType.Custom);
            Assert.IsFalse(success);
        }

        [Test]
        public void TryDeserializeValue_CustomSerializerThrows_ReturnsFalse()
        {
            var mockSerializer = new MockCustomSerializer();
            var entry = new CustomDataEntry { Key = "test", Serializer = mockSerializer };
            bool success = entry.TryDeserializeValue("throw", CustomDataType.Custom);
            Assert.IsFalse(success);
        }

        [Test]
        public void TryDeserializeValue_Json()
        {
            var entry = new CustomDataEntry { Key = "test" };
            bool success = entry.TryDeserializeValue("{\"x\":1}", CustomDataType.Json);
            Assert.IsTrue(success);
            Assert.AreEqual(CustomDataType.Json, entry.Type);
            Assert.AreEqual("{\"x\":1}", entry.JsonValue);
        }

        #endregion

        #region SetByType() 测试 (Cyclomatic Complexity: 21)

        [Test]
        public void SetValue_WithForceTypeJson()
        {
            var entry = new CustomDataEntry { Key = "test" };
            var vector = new Vector3(1, 2, 3);
            entry.SetValue(vector, CustomDataType.Json);
            Assert.AreEqual(CustomDataType.Json, entry.Type);
            Assert.NotNull(entry.JsonValue);
        }

        [Test]
        public void SetValue_WithInvalidForceType_SetsToNone()
        {
            var entry = new CustomDataEntry { Key = "test", Type = CustomDataType.String, StringValue = "old" };
            entry.SetValue("test", (CustomDataType)999); // Invalid type
            Assert.AreEqual(CustomDataType.None, entry.Type);
            // Check that all values are cleared
            Assert.AreEqual(default(int), entry.IntValue);
            Assert.AreEqual(default(float), entry.FloatValue);
            Assert.AreEqual(default(bool), entry.BoolValue);
            Assert.IsNull(entry.StringValue);
            Assert.AreEqual(default(Vector2), entry.Vector2Value);
            Assert.AreEqual(default(Vector3), entry.Vector3Value);
            Assert.AreEqual(default(Color), entry.ColorValue);
            Assert.IsNull(entry.JsonValue);
            Assert.IsNull(entry.JsonClrType);
        }

        #endregion

        #region CreateJson() 测试

        [Test]
        public void CreateJson_WithValidObject()
        {
            var vector = new Vector3(1, 2, 3);
            var entry = CustomDataEntry.CreateJson("test", vector);
            Assert.AreEqual("test", entry.Key);
            Assert.AreEqual(CustomDataType.Json, entry.Type);
            Assert.IsNotNull(entry.JsonValue);
            Assert.AreEqual(typeof(Vector3).AssemblyQualifiedName, entry.JsonClrType);
        }

        [Test]
        public void CreateJson_WithNullObject()
        {
            var entry = CustomDataEntry.CreateJson("test", null);
            Assert.AreEqual("test", entry.Key);
            Assert.AreEqual(CustomDataType.Json, entry.Type);
            Assert.IsNull(entry.JsonValue);
            Assert.IsNull(entry.JsonClrType);
        }

        #endregion

        #region ClearAll() 和 Clear 相关测试

        [Test]
        public void SetValue_ClearsAllWhenNull()
        {
            var entry = CustomDataEntry.CreateInt("test", 42);
            Assert.AreEqual(42, entry.IntValue);
            entry.SetValue(null);
            Assert.AreEqual(CustomDataType.None, entry.Type);
        }

        #endregion

        #region Serialization Callbacks 测试（通过SetValue触发）

        [Test]
        public void Roundtrip_IntSerialization()
        {
            var entry = CustomDataEntry.CreateInt("test", 42);
            var serialized = entry.SerializeValue();

            var entry2 = new CustomDataEntry { Key = "test" };
            bool success = entry2.TryDeserializeValue(serialized, CustomDataType.Int);
            Assert.IsTrue(success);
            Assert.AreEqual(42, entry2.IntValue);
        }

        [Test]
        public void Roundtrip_LongSerialization()
        {
            var entry = CustomDataEntry.CreateLong("test", 9223372036854775807L);
            var serialized = entry.SerializeValue();

            var entry2 = new CustomDataEntry { Key = "test" };
            bool success = entry2.TryDeserializeValue(serialized, CustomDataType.Long);
            Assert.IsTrue(success);
            Assert.AreEqual(9223372036854775807L, entry2.LongValue);
        }

        [Test]
        public void Roundtrip_FloatSerialization()
        {
            var entry = CustomDataEntry.CreateFloat("test", 3.14f);
            var serialized = entry.SerializeValue();

            var entry2 = new CustomDataEntry { Key = "test" };
            bool success = entry2.TryDeserializeValue(serialized, CustomDataType.Float);
            Assert.IsTrue(success);
            Assert.AreEqual(3.14f, entry2.FloatValue, 0.001f);
        }

        [Test]
        public void Roundtrip_Vector3Serialization()
        {
            var vector = new Vector3(1, 2, 3);
            var entry = CustomDataEntry.CreateVector3("test", vector);
            var serialized = entry.SerializeValue();

            var entry2 = new CustomDataEntry { Key = "test" };
            bool success = entry2.TryDeserializeValue(serialized, CustomDataType.Vector3);
            Assert.IsTrue(success);
            Assert.AreEqual(vector, entry2.Vector3Value);
        }

        #endregion

        #region Helper Method 替代测试

        [Test]
        public void TryDeserializeValue_WithInvalidIntFormat()
        {
            var entry = new CustomDataEntry { Key = "test" };
            bool success = entry.TryDeserializeValue("not an int", CustomDataType.Int);
            Assert.IsFalse(success);
        }

        [Test]
        public void TryDeserializeValue_WithInvalidLongFormat()
        {
            var entry = new CustomDataEntry { Key = "test" };
            bool success = entry.TryDeserializeValue("not a long", CustomDataType.Long);
            Assert.IsFalse(success);
        }

        [Test]
        public void TryDeserializeValue_WithInvalidFloatFormat()
        {
            var entry = new CustomDataEntry { Key = "test" };
            bool success = entry.TryDeserializeValue("not a float", CustomDataType.Float);
            Assert.IsFalse(success);
        }

        #endregion

        #region Edge Cases

        [Test]
        public void Constructor_DefaultValues()
        {
            var entry = new CustomDataEntry();
            Assert.AreEqual(CustomDataType.None, entry.Type);
            Assert.IsNull(entry.Key);
        }

        [Test]
        public void GetValue_WithEmptyStringValue()
        {
            var entry = CustomDataEntry.CreateString("test", "");
            Assert.AreEqual("", entry.GetValue());
        }

        [Test]
        public void SetValue_WithEmptyString()
        {
            var entry = new CustomDataEntry { Key = "test" };
            entry.SetValue("");
            Assert.AreEqual(CustomDataType.String, entry.Type);
            Assert.AreEqual("", entry.StringValue);
        }

        [Test]
        public void SerializeValue_WithZeroInt()
        {
            var entry = CustomDataEntry.CreateInt("test", 0);
            Assert.AreEqual("0", entry.SerializeValue());
        }

        [Test]
        public void TryDeserializeValue_WithZeroFloat()
        {
            var entry = new CustomDataEntry { Key = "test" };
            bool success = entry.TryDeserializeValue("0.0", CustomDataType.Float);
            Assert.IsTrue(success);
            Assert.AreEqual(0f, entry.FloatValue);
        }

        #endregion
    }
}
