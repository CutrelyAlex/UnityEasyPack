using System;
using System.Collections.Generic;
using EasyPack.CustomData;
using NUnit.Framework;

namespace EasyPack.CustomDataTests
{
    /// <summary>
    /// CustomDataCollection 功能测试
    /// 测试 CustomData 支持存储 List&lt;CustomData&gt; 的新功能
    /// </summary>
    [TestFixture]
    public class CustomDataCollectionTest
    {
        private CustomDataCollection _mainCollection;

        [SetUp]
        public void Setup()
        {
            _mainCollection = new CustomDataCollection();
        }

        /// <summary>
        /// 测试创建空的 CustomDataCollection
        /// </summary>
        [Test]
        public void CreateEmptyCustomDataCollection()
        {
            var entry = CustomDataEntry.CreateEntryList("nested_list", new CustomDataCollection());
            
            Assert.AreEqual("nested_list", entry.Key);
            Assert.AreEqual(CustomDataType.EntryList, entry.Type);
            Assert.IsNotNull(entry.EntryListValue);
            Assert.AreEqual(0, entry.EntryListValue.Count);
        }

        /// <summary>
        /// 测试创建包含数据的 CustomDataCollection
        /// </summary>
        [Test]
        public void CreateCustomDataCollectionWithData()
        {
            var nestedList = new CustomDataCollection();
            nestedList.Add(CustomDataEntry.CreateInt("id", 1));
            nestedList.Add(CustomDataEntry.CreateString("name", "Item1"));
            nestedList.Add(CustomDataEntry.CreateFloat("value", 3.14f));

            var entry = CustomDataEntry.CreateEntryList("items", nestedList);
            
            Assert.AreEqual(3, entry.EntryListValue.Count);
            Assert.AreEqual(1, entry.EntryListValue[0].GetValue());
            Assert.AreEqual("Item1", entry.EntryListValue[1].GetValue());
            Assert.AreEqual(3.14f, entry.EntryListValue[2].GetValue());
        }

        /// <summary>
        /// 测试通过 SetValue 设置 CustomDataCollection
        /// </summary>
        [Test]
        public void SetValueWithCustomDataCollection()
        {
            var entry = new CustomDataEntry { Key = "list_data" };
            var collection = new CustomDataCollection();
            collection.Add(CustomDataEntry.CreateBool("enabled", true));
            
            entry.SetValue(collection);
            
            Assert.AreEqual(CustomDataType.EntryList, entry.Type);
            Assert.AreEqual(1, entry.EntryListValue.Count);
        }

        /// <summary>
        /// 测试 CustomDataCollection 的序列化
        /// </summary>
        [Test]
        public void SerializeCustomDataCollection()
        {
            var nestedList = new CustomDataCollection
            {
                CustomDataEntry.CreateInt("x", 10),
                CustomDataEntry.CreateInt("y", 20)
            };
            
            var entry = CustomDataEntry.CreateEntryList("position", nestedList);
            string serialized = entry.SerializeValue();
            
            Assert.IsFalse(string.IsNullOrEmpty(serialized));
            Assert.IsTrue(serialized.Contains("items"));
        }

        /// <summary>
        /// 测试 CustomDataCollection 的反序列化
        /// </summary>
        [Test]
        public void DeserializeCustomDataCollection()
        {
            // 先创建并序列化
            var original = new CustomDataCollection
            {
                CustomDataEntry.CreateInt("id", 42),
                CustomDataEntry.CreateString("text", "test")
            };
            
            var entry = CustomDataEntry.CreateEntryList("data", original);
            string serialized = entry.SerializeValue();
            
            // 再反序列化
            var deserializedEntry = new CustomDataEntry { Key = "data" };
            bool success = deserializedEntry.TryDeserializeValue(serialized, CustomDataType.EntryList);
            
            Assert.IsTrue(success);
            Assert.AreEqual(2, deserializedEntry.EntryListValue.Count);
        }

        /// <summary>
        /// 测试在主 CustomDataCollection 中存储 CustomDataCollection
        /// </summary>
        [Test]
        public void StoreCustomDataCollectionInMainCollection()
        {
            var nestedList = new CustomDataCollection
            {
                CustomDataEntry.CreateString("config_name", "Setting1"),
                CustomDataEntry.CreateBool("is_active", true)
            };
            
            var entry = CustomDataEntry.CreateEntryList("configs", nestedList);
            _mainCollection.Add(entry);
            
            Assert.AreEqual(1, _mainCollection.Count);
            var retrievedEntry = _mainCollection["configs"];
            Assert.AreEqual(CustomDataType.EntryList, retrievedEntry.Type);
            Assert.AreEqual(2, retrievedEntry.EntryListValue.Count);
        }

        /// <summary>
        /// 测试多层嵌套的 CustomDataCollection
        /// </summary>
        [Test]
        public void NestedCustomDataCollection()
        {
            // 创建最深层的列表
            var deepList = new CustomDataCollection
            {
                CustomDataEntry.CreateInt("value", 100)
            };
            
            // 创建中间层列表
            var middleList = new CustomDataCollection
            {
                CustomDataEntry.CreateEntryList("deep", deepList)
            };
            
            // 创建顶层列表
            var topList = new CustomDataCollection
            {
                CustomDataEntry.CreateEntryList("middle", middleList)
            };
            
            // 添加到主集合
            var entry = CustomDataEntry.CreateEntryList("top", topList);
            _mainCollection.Add(entry);
            
            // 验证多层嵌套
            var retrieved = _mainCollection[0];
            Assert.AreEqual(CustomDataType.EntryList, retrieved.Type);
            Assert.AreEqual(1, retrieved.EntryListValue.Count);
            
            var middleEntry = retrieved.EntryListValue[0];
            Assert.AreEqual(CustomDataType.EntryList, middleEntry.Type);
        }

        /// <summary>
        /// 测试清空 CustomDataCollection
        /// </summary>
        [Test]
        public void ClearCustomDataCollection()
        {
            var list = new CustomDataCollection
            {
                CustomDataEntry.CreateInt("a", 1),
                CustomDataEntry.CreateInt("b", 2)
            };
            
            var entry = CustomDataEntry.CreateEntryList("data", list);
            Assert.AreEqual(2, entry.EntryListValue.Count);
            
            entry.EntryListValue.Clear();
            Assert.AreEqual(0, entry.EntryListValue.Count);
        }
    }
}
