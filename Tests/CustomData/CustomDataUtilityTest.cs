using NUnit.Framework;
using EasyPack.CustomData;
using System.Collections.Generic;
using System.Linq;

namespace EasyPack.CustomDataTests
{
    /// <summary>
    /// CustomDataUtility 单元测试
    /// 覆盖所有扩展方法和工具函数，提高分支覆盖率和CRAP评分
    /// </summary>
    [TestFixture]
    public class CustomDataUtilityTest
    {
        private CustomDataCollection _entries;

        [SetUp]
        public void Setup()
        {
            _entries = new CustomDataCollection();
        }

        #region ToEntries() 转换测试

        [Test]
        public void ToEntries_EmptyDictionary()
        {
            var dict = new Dictionary<string, object>();
            var entries = CustomDataUtility.ToEntries(dict);
            Assert.AreEqual(0, entries.Count);
        }

        [Test]
        public void ToEntries_WithNullDictionary()
        {
            var entries = CustomDataUtility.ToEntries(null);
            Assert.AreEqual(0, entries.Count);
        }

        [Test]
        public void ToEntries_WithIntValue()
        {
            var dict = new Dictionary<string, object> { { "key1", 42 } };
            var entries = CustomDataUtility.ToEntries(dict);
            Assert.AreEqual(1, entries.Count);
            Assert.AreEqual("key1", entries[0].Key);
            Assert.AreEqual(42, entries[0].GetValue());
        }

        [Test]
        public void ToEntries_WithMultipleTypes()
        {
            var dict = new Dictionary<string, object>
            {
                { "int", 42 },
                { "long", 9223372036854775807L },
                { "float", 3.14f },
                { "string", "hello" },
                { "bool", true }
            };
            var entries = CustomDataUtility.ToEntries(dict);
            Assert.AreEqual(5, entries.Count);
        }

        #endregion

        #region ToDictionary() 转换测试

        [Test]
        public void ToDictionary_EmptyEntries()
        {
            var dict = CustomDataUtility.ToDictionary(_entries);
            Assert.AreEqual(0, dict.Count);
        }

        [Test]
        public void ToDictionary_WithNullEntries()
        {
            var dict = CustomDataUtility.ToDictionary(null);
            Assert.AreEqual(0, dict.Count);
        }

        [Test]
        public void ToDictionary_WithMultipleEntries()
        {
            _entries.Add(CustomDataEntry.CreateInt("int", 42));
            _entries.Add(CustomDataEntry.CreateLong("long", 9223372036854775807L));
            _entries.Add(CustomDataEntry.CreateString("str", "hello"));
            _entries.Add(CustomDataEntry.CreateBool("bool", true));

            var dict = CustomDataUtility.ToDictionary(_entries);
            Assert.AreEqual(4, dict.Count);
            Assert.AreEqual(42, dict["int"]);
            Assert.AreEqual(9223372036854775807L, dict["long"]);
            Assert.AreEqual("hello", dict["str"]);
            Assert.AreEqual(true, dict["bool"]);
        }

        #endregion

        #region Merge() 合并测试

        [Test]
        public void Merge_WithOtherList()
        {
            _entries.Add(CustomDataEntry.CreateInt("key1", 10));

            var other = new CustomDataCollection
            {
                CustomDataEntry.CreateInt("key2", 20),
                CustomDataEntry.CreateString("key3", "hello")
            };

            CustomDataUtility.Merge(_entries, other);
            Assert.AreEqual(3, _entries.Count);
        }

        [Test]
        public void Merge_Overwrites()
        {
            _entries.Add(CustomDataEntry.CreateInt("key", 10));

            var other = new CustomDataCollection
            {
                CustomDataEntry.CreateInt("key", 99)
            };

            CustomDataUtility.Merge(_entries, other);
            Assert.AreEqual(1, _entries.Count);
            Assert.AreEqual(99, _entries[0].IntValue);
        }

        [Test]
        public void Merge_WithNullOther()
        {
            _entries.Add(CustomDataEntry.CreateInt("key", 10));
            // Should not throw
            CustomDataUtility.Merge(_entries, null);
            Assert.AreEqual(1, _entries.Count);
        }

        [Test]
        public void Merge_WithNullEntries()
        {
            var other = new CustomDataCollection { CustomDataEntry.CreateInt("key", 10) };
            // Should not throw
            CustomDataUtility.Merge(null, other);
        }

        #endregion

        #region GetDifference() 测试

        [Test]
        public void GetDifference_NoCommonKeys()
        {
            _entries.Add(CustomDataEntry.CreateInt("key1", 10));

            var other = new CustomDataCollection
            {
                CustomDataEntry.CreateInt("key2", 20),
                CustomDataEntry.CreateString("key3", "hello")
            };

            var diff = CustomDataUtility.GetDifference(_entries, other).ToList();
            Assert.AreEqual(2, diff.Count);
            Assert.Contains("key2", diff);
            Assert.Contains("key3", diff);
        }

        [Test]
        public void GetDifference_AllCommonKeys()
        {
            _entries.Add(CustomDataEntry.CreateInt("key1", 10));
            _entries.Add(CustomDataEntry.CreateInt("key2", 20));

            var other = new CustomDataCollection
            {
                CustomDataEntry.CreateInt("key1", 30),
                CustomDataEntry.CreateInt("key2", 40)
            };

            var diff = CustomDataUtility.GetDifference(_entries, other).ToList();
            Assert.AreEqual(0, diff.Count);
        }

        #endregion
    }
}
