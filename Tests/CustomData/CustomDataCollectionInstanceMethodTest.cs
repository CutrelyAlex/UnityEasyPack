using NUnit.Framework;
using EasyPack.CustomData;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace EasyPack.CustomDataTests
{
    /// <summary>
    /// CustomDataCollection 实例方法测试
    /// 测试直接在 CustomDataCollection 实例上调用的方法
    /// </summary>
    [TestFixture]
    public class CustomDataCollectionInstanceMethodTest
    {
        private CustomDataCollection _collection;

        [SetUp]
        public void Setup()
        {
            _collection = new CustomDataCollection();
        }

        #region GetValue 实例方法测试

        [Test]
        public void GetValue_WithExistingKey_ReturnsValue()
        {
            _collection.Set("count", 42);
            int value = _collection.Get<int>("count", 0);
            Assert.AreEqual(42, value);
        }

        [Test]
        public void GetValue_WithMissingKey_ReturnsDefault()
        {
            int value = _collection.Get<int>("missing", 99);
            Assert.AreEqual(99, value);
        }

        [Test]
        public void GetValue_WithNullKey_ReturnsDefault()
        {
            int value = _collection.Get<int>(null, 42);
            Assert.AreEqual(42, value);
        }

        [Test]
        public void GetValue_WithEmptyKey_ReturnsDefault()
        {
            int value = _collection.Get<int>("", 42);
            Assert.AreEqual(42, value);
        }

        #endregion

        #region TryGetValue 实例方法测试

        [Test]
        public void TryGetValue_WithExistingKey_ReturnsTrue()
        {
            _collection.Set("age", 25);
            bool success = _collection.TryGetValue<int>("age", out int value);
            Assert.IsTrue(success);
            Assert.AreEqual(25, value);
        }

        [Test]
        public void TryGetValue_WithMissingKey_ReturnsFalse()
        {
            bool success = _collection.TryGetValue<int>("nonexistent", out int value);
            Assert.IsFalse(success);
            Assert.AreEqual(0, value);
        }

        [Test]
        public void TryGetValue_WithNullKey_ReturnsFalse()
        {
            bool success = _collection.TryGetValue<int>(null, out int value);
            Assert.IsFalse(success);
            Assert.AreEqual(0, value);
        }

        [Test]
        public void TryGetValue_WithEmptyKey_ReturnsFalse()
        {
            bool success = _collection.TryGetValue<int>("", out int value);
            Assert.IsFalse(success);
            Assert.AreEqual(0, value);
        }

        #endregion

        #region SetValue 实例方法测试

        [Test]
        public void SetValue_AddsNewEntry()
        {
            _collection.Set("newint", 42);
            Assert.AreEqual(1, _collection.Count);
            Assert.AreEqual(42, _collection.Get<int>("newint"));
        }

        [Test]
        public void SetValue_UpdatesExistingEntry()
        {
            _collection.Set("age", 20);
            _collection.Set("age", 30);
            Assert.AreEqual(1, _collection.Count);
            Assert.AreEqual(30, _collection.Get<int>("age"));
        }

        #endregion

        #region RemoveValue 实例方法测试

        [Test]
        public void RemoveValue_WithExistingKey_ReturnsTrue()
        {
            _collection.Set("toremove", 42);
            bool removed = _collection.Remove("toremove");
            Assert.IsTrue(removed);
            Assert.AreEqual(0, _collection.Count);
        }

        [Test]
        public void RemoveValue_WithNonexistentKey_ReturnsFalse()
        {
            bool removed = _collection.Remove("nonexistent");
            Assert.IsFalse(removed);
        }

        [Test]
        public void RemoveValue_WithNullKey_ReturnsFalse()
        {
            bool removed = _collection.Remove((string)null);
            Assert.IsFalse(removed);
        }

        [Test]
        public void RemoveValue_WithEmptyKey_ReturnsFalse()
        {
            bool removed = _collection.Remove("");
            Assert.IsFalse(removed);
        }

        #endregion

        #region HasValue 实例方法测试

        [Test]
        public void HasValue_WithExistingKey_ReturnsTrue()
        {
            _collection.Set("key", 42);
            Assert.IsTrue(_collection.HasValue("key"));
        }

        [Test]
        public void HasValue_WithNonexistentKey_ReturnsFalse()
        {
            Assert.IsFalse(_collection.HasValue("other"));
        }

        [Test]
        public void HasValue_WithNullKey_ReturnsFalse()
        {
            Assert.IsFalse(_collection.HasValue(null));
        }

        [Test]
        public void HasValue_WithEmptyKey_ReturnsFalse()
        {
            Assert.IsFalse(_collection.HasValue(""));
        }

        #endregion

        #region 快捷方法测试

        [Test]
        public void SetInt_AndGetInt()
        {
            _collection.Set("score", 100);
            Assert.AreEqual(100, _collection.Get<int>("score"));
        }

        [Test]
        public void SetLong_AndGetLong()
        {
            _collection.Set("bigNumber", 9223372036854775807L);
            Assert.AreEqual(9223372036854775807L, _collection.Get<long>("bigNumber"));
        }

        [Test]
        public void SetFloat_AndGetFloat()
        {
            _collection.Set("health", 99.5f);
            Assert.AreEqual(99.5f, _collection.Get<float>("health"));
        }

        [Test]
        public void SetBool_AndGetBool()
        {
            _collection.Set("active", true);
            Assert.AreEqual(true, _collection.Get<bool>("active"));
        }

        [Test]
        public void SetString_AndGetString()
        {
            _collection.Set("name", "Player");
            Assert.AreEqual("Player", _collection.Get<string>("name"));
        }

        [Test]
        public void SetVector3_AndGetVector3()
        {
            var pos = new Vector3(1, 2, 3);
            _collection.Set("position", pos);
            Assert.AreEqual(pos, _collection.Get<Vector3>("position"));
        }

        #endregion

        #region 数值增加操作测试

        [Test]
        public void AddInt_WithExistingKey_IncrementsValue()
        {
            _collection.Set("count", 10);
            int current = _collection.Get<int>("count", 0);
            int newValue = current + 5;
            _collection.Set("count", newValue);
            Assert.AreEqual(15, newValue);
            Assert.AreEqual(15, _collection.Get<int>("count"));
        }

        [Test]
        public void AddInt_WithNonexistentKey_StartsFromZero()
        {
            int current = _collection.Get<int>("newcount", 0);
            int newValue = current + 5;
            _collection.Set("newcount", newValue);
            Assert.AreEqual(5, newValue);
        }

        [Test]
        public void AddFloat_WithExistingKey_IncrementsValue()
        {
            _collection.Set("distance", 10.5f);
            float current = _collection.Get<float>("distance", 0f);
            float newValue = current + 2.5f;
            _collection.Set("distance", newValue);
            Assert.AreEqual(13f, newValue);
        }

        #endregion

        #region 批量操作测试

        [Test]
        public void SetValues_AddsMultipleEntries()
        {
            var values = new Dictionary<string, object>
            {
                { "int", 42 },
                { "float", 3.14f },
                { "string", "hello" }
            };
            _collection.SetValues(values);
            Assert.AreEqual(3, _collection.Count);
            Assert.AreEqual(42, _collection.Get<int>("int"));
            Assert.AreEqual("hello", _collection.Get<string>("string"));
        }

        [Test]
        public void GetValues_ReturnsMultipleValues()
        {
            _collection.Set("a", 1);
            _collection.Set("b", 2);
            _collection.Set("c", 3);
            
            var ids = new[] { "a", "b", "c" };
            var values = _collection.GetValues<int>(ids, 0);
            
            Assert.AreEqual(3, values.Count);
            Assert.AreEqual(1, values["a"]);
            Assert.AreEqual(2, values["b"]);
            Assert.AreEqual(3, values["c"]);
        }

        [Test]
        public void GetValues_WithMissingKeys_UsesDefault()
        {
            _collection.Set("a", 1);
            
            var ids = new[] { "a", "missing" };
            var values = _collection.GetValues<int>(ids, 99);
            
            Assert.AreEqual(1, values["a"]);
            Assert.AreEqual(99, values["missing"]);
        }

        [Test]
        public void RemoveValues_RemovesMultipleEntries()
        {
            _collection.Set("a", 1);
            _collection.Set("b", 2);
            _collection.Set("c", 3);
            
            int removed = _collection.RemoveValues(new[] { "a", "c" });
            
            Assert.AreEqual(2, removed);
            Assert.AreEqual(1, _collection.Count);
            Assert.IsTrue(_collection.HasValue("b"));
        }

        #endregion

        #region 查询操作测试

        [Test]
        public void GetKeys_ReturnsAllKeys()
        {
            _collection.Set("a", 1);
            _collection.Set("b", 2);
            _collection.Set("c", 3);
            
            var keys = _collection.GetKeys().ToList();
            
            Assert.AreEqual(3, keys.Count);
            Assert.Contains("a", keys);
            Assert.Contains("b", keys);
            Assert.Contains("c", keys);
        }

        [Test]
        public void GetKeysByType_ReturnsKeysOfType()
        {
            _collection.Set("score", 100);
            _collection.Set("health", 99.5f);
            _collection.Set("level", 5);
            
            var intKeys = _collection.GetKeysByType(CustomDataType.Int).ToList();
            
            Assert.AreEqual(2, intKeys.Count);
            Assert.Contains("score", intKeys);
            Assert.Contains("level", intKeys);
        }

        [Test]
        public void GetEntriesWhere_FiltersEntries()
        {
            _collection.Set("a", 10);
            _collection.Set("b", 25);
            _collection.Set("c", 30);
            
            var filtered = _collection.GetEntriesWhere(e => e.IntValue >= 25).ToList();
            
            Assert.AreEqual(2, filtered.Count);
        }

        [Test]
        public void GetFirstValue_ReturnsFirstMatching()
        {
            _collection.Set("a", 10);
            _collection.Set("b", 25);
            _collection.Set("c", 30);
            
            int value = _collection.GetFirstValue<int>((k, v) => v >= 25, 0);
            
            Assert.GreaterOrEqual(value, 25);
        }

        #endregion

        #region 克隆和合并测试

        [Test]
        public void Clone_CreatesDeepcopy()
        {
            _collection.Set("a", 1);
            _collection.Set("b", "test");
            
            var cloned = _collection.Clone();
            
            Assert.AreEqual(_collection.Count, cloned.Count);
            Assert.AreEqual(1, cloned.Get<int>("a"));
            Assert.AreEqual("test", cloned.Get<string>("b"));
        }

        [Test]
        public void Clone_CopiedDataIsIndependent()
        {
            _collection.Set("a", 1);
            var cloned = _collection.Clone();
            
            cloned.Set("a", 999);
            
            Assert.AreEqual(1, _collection.Get<int>("a"));
            Assert.AreEqual(999, cloned.Get<int>("a"));
        }

        [Test]
        public void Merge_CombinesCollections()
        {
            _collection.Set("a", 1);
            _collection.Set("b", 2);
            
            var other = new CustomDataCollection();
            other.Set("c", 3);
            other.Set("d", 4);
            
            _collection.Merge(other);
            
            Assert.AreEqual(4, _collection.Count);
            Assert.AreEqual(3, _collection.Get<int>("c"));
        }

        [Test]
        public void Merge_OverwritesExistingKeys()
        {
            _collection.Set("a", 1);
            
            var other = new CustomDataCollection();
            other.Set("a", 999);
            
            _collection.Merge(other);
            
            Assert.AreEqual(999, _collection.Get<int>("a"));
        }

        [Test]
        public void GetDifference_ReturnsKeysOnlyInOther()
        {
            _collection.Set("a", 1);
            _collection.Set("b", 2);
            
            var other = new CustomDataCollection();
            other.Set("b", 2);
            other.Set("c", 3);
            other.Set("d", 4);
            
            var diff = _collection.GetDifference(other).ToList();
            
            Assert.AreEqual(2, diff.Count);
            Assert.Contains("c", diff);
            Assert.Contains("d", diff);
        }

        #endregion

        #region 条件操作测试

        [Test]
        public void IfHasValue_ExecutesActionIfExists()
        {
            _collection.Set("name", "Alice");
            
            bool executed = false;
            _collection.IfHasValue<string>("name", name =>
            {
                executed = true;
                Assert.AreEqual("Alice", name);
            });
            
            Assert.IsTrue(executed);
        }

        [Test]
        public void IfHasValue_DoesNotExecuteIfNotExists()
        {
            bool executed = false;
            _collection.IfHasValue<string>("missing", name =>
            {
                executed = true;
            });
            
            Assert.IsFalse(executed);
        }

        [Test]
        public void IfElse_ExecutesCorrectBranch()
        {
            _collection.Set("count", 10);
            
            int result = 0;
            _collection.IfElse<int>(
                "count",
                value => result = value,
                () => result = -1
            );
            
            Assert.AreEqual(10, result);
        }

        [Test]
        public void IfElse_ExecutesElseBranch()
        {
            int result = 0;
            _collection.IfElse<int>(
                "missing",
                value => result = value,
                () => result = -1
            );
            
            Assert.AreEqual(-1, result);
        }

        #endregion

        #region 属性测试

        [Test]
        public void IsEmpty_ReturnsTrueForEmpty()
        {
            Assert.IsTrue(_collection.IsEmpty);
        }

        [Test]
        public void IsEmpty_ReturnsFalseForNonEmpty()
        {
            _collection.Set("a", 1);
            Assert.IsFalse(_collection.IsEmpty);
        }

        #endregion
    }
}
