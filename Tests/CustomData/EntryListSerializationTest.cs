using EasyPack.CustomData;
using NUnit.Framework;

namespace EasyPack.CustomDataTests
{
    /// <summary>
    /// 测试 CustomDataCollection 和 CustomDataList 的序列化和反序列化
    /// </summary>
    [TestFixture]
    public class EntryListSerializationTests
    {
        #region CustomDataCollection 序列化测试

        [Test]
        public void SerializeCustomDataCollection_PreservesType()
        {
            // 创建 CustomDataCollection
            var collection = new CustomDataCollection();
            collection.Set("name", "test");
            collection.Set("value", 42);

            var entry = CustomDataEntry.CreateEntryList("data", collection);

            // 序列化
            string serialized = entry.SerializeValue();

            // 反序列化
            var deserializedEntry = new CustomDataEntry { Key = "data" };
            bool success = deserializedEntry.TryDeserializeValue(serialized, CustomDataType.EntryList);

            // 验证
            Assert.IsTrue(success);
            Assert.IsInstanceOf<CustomDataCollection>(deserializedEntry.EntryListValue);
            
            var deserializedCollection = (CustomDataCollection)deserializedEntry.EntryListValue;
            Assert.AreEqual("test", deserializedCollection.Get<string>("name"));
            Assert.AreEqual(42, deserializedCollection.Get<int>("value"));
        }

        #endregion

        #region CustomDataList 序列化测试

        [Test]
        public void SerializeCustomDataList_PreservesType()
        {
            // 创建 CustomDataList
            var list = CustomDataList.FromValues("hello", 123, 3.14f);

            var entry = CustomDataEntry.CreateEntryList("data", list);

            // 序列化
            string serialized = entry.SerializeValue();

            // 反序列化
            var deserializedEntry = new CustomDataEntry { Key = "data" };
            bool success = deserializedEntry.TryDeserializeValue(serialized, CustomDataType.EntryList);

            // 验证
            Assert.IsTrue(success);
            Assert.IsInstanceOf<CustomDataList>(deserializedEntry.EntryListValue);
            
            var deserializedList = (CustomDataList)deserializedEntry.EntryListValue;
            Assert.AreEqual(3, deserializedList.Count);
            Assert.AreEqual("hello", deserializedList.Get<string>(0));
            Assert.AreEqual(123, deserializedList.Get<int>(1));
            Assert.AreEqual(3.14f, deserializedList.Get<float>(2));
        }

        #endregion

        #region 空集合序列化测试

        [Test]
        public void SerializeEmptyCustomDataCollection()
        {
            var entry = CustomDataEntry.CreateEntryList("empty", new CustomDataCollection());

            string serialized = entry.SerializeValue();
            
            var deserializedEntry = new CustomDataEntry { Key = "empty" };
            bool success = deserializedEntry.TryDeserializeValue(serialized, CustomDataType.EntryList);

            Assert.IsTrue(success);
            Assert.IsInstanceOf<CustomDataCollection>(deserializedEntry.EntryListValue);
            Assert.AreEqual(0, deserializedEntry.EntryListValue.Count);
        }

        [Test]
        public void SerializeEmptyCustomDataList()
        {
            var entry = CustomDataEntry.CreateEntryList("empty", new CustomDataList());

            string serialized = entry.SerializeValue();
            
            var deserializedEntry = new CustomDataEntry { Key = "empty" };
            bool success = deserializedEntry.TryDeserializeValue(serialized, CustomDataType.EntryList);

            Assert.IsTrue(success);
            Assert.IsInstanceOf<CustomDataList>(deserializedEntry.EntryListValue);
            Assert.AreEqual(0, deserializedEntry.EntryListValue.Count);
        }

        #endregion

        #region 嵌套序列化测试

        [Test]
        public void SerializeNestedCollections()
        {
            // 创建嵌套结构：CustomDataCollection 包含 CustomDataList
            var innerList = CustomDataList.FromValues(1, 2, 3);
            var outerCollection = new CustomDataCollection();
            outerCollection.Set("numbers", CustomDataEntry.CreateEntryList("numbers", innerList));

            var entry = CustomDataEntry.CreateEntryList("outer", outerCollection);

            // 序列化和反序列化
            string serialized = entry.SerializeValue();
            var deserializedEntry = new CustomDataEntry { Key = "outer" };
            bool success = deserializedEntry.TryDeserializeValue(serialized, CustomDataType.EntryList);

            // 验证外层
            Assert.IsTrue(success);
            Assert.IsInstanceOf<CustomDataCollection>(deserializedEntry.EntryListValue);
            
            var deserializedOuter = (CustomDataCollection)deserializedEntry.EntryListValue;
            Assert.IsTrue(deserializedOuter.HasValue("numbers"));

            // 验证内层
            var numbersEntry = deserializedOuter.Get<CustomDataEntry>("numbers");
            Assert.IsInstanceOf<CustomDataList>(numbersEntry.EntryListValue);
            
            var deserializedInner = (CustomDataList)numbersEntry.EntryListValue;
            Assert.AreEqual(3, deserializedInner.Count);
            Assert.AreEqual(1, deserializedInner.Get<int>(0));
            Assert.AreEqual(2, deserializedInner.Get<int>(1));
            Assert.AreEqual(3, deserializedInner.Get<int>(2));
        }

        #endregion

        #region 类型转换测试

        [Test]
        public void SetValue_CustomDataList_PreservesType()
        {
            var entry = new CustomDataEntry { Key = "test" };
            var list = CustomDataList.FromValues("a", "b", "c");

            entry.SetValue(list);

            Assert.AreEqual(CustomDataType.EntryList, entry.Type);
            Assert.IsInstanceOf<CustomDataList>(entry.EntryListValue);
            Assert.AreEqual(3, entry.EntryListValue.Count);
        }

        [Test]
        public void SetValue_CustomDataCollection_PreservesType()
        {
            var entry = new CustomDataEntry { Key = "test" };
            var collection = new CustomDataCollection();
            collection.Set("key", "value");

            entry.SetValue(collection);

            Assert.AreEqual(CustomDataType.EntryList, entry.Type);
            Assert.IsInstanceOf<CustomDataCollection>(entry.EntryListValue);
            Assert.AreEqual(1, entry.EntryListValue.Count);
        }

        #endregion
    }
}