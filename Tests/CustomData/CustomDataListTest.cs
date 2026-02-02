using System.Collections.Generic;
using EasyPack.CustomData;
using NUnit.Framework;
using UnityEngine;

namespace EasyPack.CustomDataTests
{
    /// <summary>
    /// CustomDataList 单元测试
    /// 测试纯列表模式的 CustomDataList 功能
    /// </summary>
    [TestFixture]
    public class CustomDataListTests
    {
        #region 构造函数测试

        [Test]
        public void Constructor_Default_CreatesEmptyList()
        {
            var list = new CustomDataList();
            
            Assert.AreEqual(0, list.Count);
            Assert.IsNotNull(list);
        }

        [Test]
        public void Constructor_WithCapacity_CreatesEmptyListWithCapacity()
        {
            var list = new CustomDataList(10);
            
            Assert.AreEqual(0, list.Count);
        }

        [Test]
        public void Constructor_WithCollection_CopiesEntries()
        {
            var entries = new List<CustomDataEntry>
            {
                CustomDataEntry.CreateValue(1),
                CustomDataEntry.CreateValue("test"),
                CustomDataEntry.CreateValue(3.14f)
            };
            
            var list = new CustomDataList(entries);
            
            Assert.AreEqual(3, list.Count);
            Assert.AreEqual(1, list[0].GetValue());
            Assert.AreEqual("test", list[1].GetValue());
            Assert.AreEqual(3.14f, list[2].GetValue());
        }

        #endregion

        #region 工厂方法测试

        [Test]
        public void FromValues_CreatesListWithValues()
        {
            var list = CustomDataList.FromValues(1, "hello", 3.14f, true);
            
            Assert.AreEqual(4, list.Count);
            Assert.AreEqual(1, list.Get<int>(0));
            Assert.AreEqual("hello", list.Get<string>(1));
            Assert.AreEqual(3.14f, list.Get<float>(2));
            Assert.AreEqual(true, list.Get<bool>(3));
        }

        [Test]
        public void FromValues_EmptyArray_CreatesEmptyList()
        {
            var list = CustomDataList.FromValues();
            
            Assert.AreEqual(0, list.Count);
        }

        [Test]
        public void FromEntries_CreatesListWithEntries()
        {
            var list = CustomDataList.FromEntries(
                CustomDataEntry.CreateValue(100),
                CustomDataEntry.CreateValue("entry")
            );
            
            Assert.AreEqual(2, list.Count);
            Assert.AreEqual(100, list[0].GetValue());
            Assert.AreEqual("entry", list[1].GetValue());
        }

        #endregion

        #region 值访问方法测试

        [Test]
        public void GetValue_ValidIndex_ReturnsValue()
        {
            var list = CustomDataList.FromValues(42, "test", 1.5f);
            
            Assert.AreEqual(42, list.Get<int>(0));
            Assert.AreEqual("test", list.Get<string>(1));
            Assert.AreEqual(1.5f, list.Get<float>(2));
        }

        [Test]
        public void GetValue_InvalidIndex_ReturnsDefault()
        {
            var list = CustomDataList.FromValues(1, 2, 3);
            
            Assert.AreEqual(0, list.Get<int>(-1));
            Assert.AreEqual(0, list.Get<int>(100));
            Assert.AreEqual("default", list.Get<string>(100, "default"));
        }

        [Test]
        public void GetValue_TypeMismatch_ReturnsDefault()
        {
            var list = CustomDataList.FromValues("not a number");
            
            Assert.AreEqual(0, list.Get<int>(0));
            Assert.AreEqual(99, list.Get<int>(0, 99));
        }

        [Test]
        public void SetValue_ValidIndex_UpdatesValue()
        {
            var list = CustomDataList.FromValues(1, 2, 3);
            
            list.Set(1, 100);
            
            Assert.AreEqual(100, list.Get<int>(1));
        }

        [Test]
        public void SetValue_InvalidIndex_ThrowsException()
        {
            var list = CustomDataList.FromValues(1, 2, 3);
            
            Assert.Throws<System.ArgumentOutOfRangeException>(() => list.Set(-1, 100));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => list.Set(10, 100));
        }

        [Test]
        public void AddValue_AppendsToList()
        {
            var list = new CustomDataList();
            
            list.AddValue(1);
            list.AddValue("test");
            list.AddValue(3.14f);
            
            Assert.AreEqual(3, list.Count);
            Assert.AreEqual(1, list.Get<int>(0));
            Assert.AreEqual("test", list.Get<string>(1));
            Assert.AreEqual(3.14f, list.Get<float>(2));
        }

        [Test]
        public void InsertValue_InsertsAtIndex()
        {
            var list = CustomDataList.FromValues(1, 3);
            
            list.InsertValue(1, 2);
            
            Assert.AreEqual(3, list.Count);
            Assert.AreEqual(1, list.Get<int>(0));
            Assert.AreEqual(2, list.Get<int>(1));
            Assert.AreEqual(3, list.Get<int>(2));
        }

        #endregion

        #region 转换方法测试

        [Test]
        public void ToList_ConvertsToTypedList()
        {
            var list = CustomDataList.FromValues(1, 2, 3, 4, 5);
            
            List<int> intList = list.ToList<int>();
            
            Assert.AreEqual(5, intList.Count);
            Assert.AreEqual(1, intList[0]);
            Assert.AreEqual(5, intList[4]);
        }

        [Test]
        public void ToArray_ConvertsToObjectArray()
        {
            var list = CustomDataList.FromValues(1, "test", 3.14f);
            
            object[] array = list.ToArray();
            
            Assert.AreEqual(3, array.Length);
            Assert.AreEqual(1, array[0]);
            Assert.AreEqual("test", array[1]);
            Assert.AreEqual(3.14f, array[2]);
        }

        #endregion

        #region List 继承功能测试

        [Test]
        public void Add_InheritsFromList()
        {
            var list = new CustomDataList();
            
            list.Add(CustomDataEntry.CreateValue(100));
            
            Assert.AreEqual(1, list.Count);
        }

        [Test]
        public void Remove_InheritsFromList()
        {
            var entry = CustomDataEntry.CreateValue(100);
            var list = new CustomDataList { entry };
            
            bool removed = list.Remove(entry);
            
            Assert.IsTrue(removed);
            Assert.AreEqual(0, list.Count);
        }

        [Test]
        public void Clear_InheritsFromList()
        {
            var list = CustomDataList.FromValues(1, 2, 3);
            
            list.Clear();
            
            Assert.AreEqual(0, list.Count);
        }

        [Test]
        public void Indexer_InheritsFromList()
        {
            var list = CustomDataList.FromValues(1, 2, 3);
            
            CustomDataEntry entry = list[1];
            
            Assert.AreEqual(2, entry.GetValue());
        }

        [Test]
        public void ForEach_InheritsFromList()
        {
            var list = CustomDataList.FromValues(1, 2, 3);
            int sum = 0;
            
            foreach (var entry in list)
            {
                sum += (int)entry.GetValue();
            }
            
            Assert.AreEqual(6, sum);
        }

        #endregion

        #region 与 CustomDataEntry 集成测试

        [Test]
        public void CreateCollection_WithCustomDataList()
        {
            var list = CustomDataList.FromValues(1, 2, 3);
            
            var entry = CustomDataEntry.CreateEntryList("numbers", list);
            
            Assert.AreEqual(CustomDataType.EntryList, entry.Type);
            Assert.AreEqual(3, entry.EntryListValue.Count);
        }

        [Test]
        public void SetValue_WithCustomDataList()
        {
            var entry = new CustomDataEntry { Key = "data" };
            var list = CustomDataList.FromValues("a", "b", "c");
            
            entry.SetValue(list);
            
            Assert.AreEqual(CustomDataType.EntryList, entry.Type);
            Assert.AreEqual(3, entry.EntryListValue.Count);
        }

        [Test]
        public void NestedInCustomDataCollection()
        {
            var collection = new CustomDataCollection();
            var list = CustomDataList.FromValues(10, 20, 30);
            
            collection.Set("scores", CustomDataEntry.CreateEntryList("scores", list));
            
            Assert.IsTrue(collection.HasValue("scores"));
            var retrieved = collection.Get<CustomDataEntry>("scores");
            Assert.AreEqual(3, retrieved.EntryListValue.Count);
        }

        #endregion

        #region 边界情况测试

        [Test]
        public void EmptyList_Operations()
        {
            var list = new CustomDataList();
            
            Assert.AreEqual(0, list.Count);
            Assert.AreEqual(0, list.Get<int>(0));
            Assert.AreEqual(0, list.ToList<int>().Count);
            Assert.AreEqual(0, list.ToArray().Length);
        }

        [Test]
        public void NullValue_Handling()
        {
            var list = new CustomDataList();
            
            list.Add(null);
            
            Assert.AreEqual(1, list.Count);
        }

        [Test]
        public void MixedTypes_InSameList()
        {
            var list = CustomDataList.FromValues(
                1,                          // int
                "hello",                    // string
                3.14f,                      // float
                true,                       // bool
                new Vector2(1, 2),          // Vector2
                new Vector3(1, 2, 3),       // Vector3
                Color.red                   // Color
            );
            
            Assert.AreEqual(7, list.Count);
            Assert.AreEqual(1, list.Get<int>(0));
            Assert.AreEqual("hello", list.Get<string>(1));
            Assert.AreEqual(3.14f, list.Get<float>(2));
            Assert.AreEqual(true, list.Get<bool>(3));
            Assert.AreEqual(new Vector2(1, 2), list.Get<Vector2>(4));
            Assert.AreEqual(new Vector3(1, 2, 3), list.Get<Vector3>(5));
            Assert.AreEqual(Color.red, list.Get<Color>(6));
        }

        #endregion
    }
}
