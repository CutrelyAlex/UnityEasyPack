using System;
using System.Collections.Generic;
using EasyPack.CustomData;
using NUnit.Framework;

namespace EasyPack.CustomDataTests
{
    /// <summary>
    /// CustomDataList 功能测试
    /// 测试 CustomDataList 和 CustomDataCollection 的混用
    /// 包括：
    /// 1. CustomDataList 包含 CustomDataList 类型的 Entry（嵌套列表）
    /// 2. CustomDataList 中包含其他类型数据
    /// 3. CustomDataCollection 中包含 CustomDataList 类型的 Entry
    /// 4. CustomDataCollection 中的 CustomDataList Entry 包含其他类型数据
    /// 5. 验证序列化和反序列化的正常性
    /// </summary>
    [TestFixture]
    public class CustomEntryListTest
    {
        private CustomDataList _mainList;
        private CustomDataCollection _mainCollection;

        [SetUp]
        public void Setup()
        {
            _mainList = new CustomDataList();
            _mainCollection = new CustomDataCollection();
        }

        #region 基础 CustomDataList 测试

        /// <summary>
        /// 测试创建空的 CustomDataList
        /// </summary>
        [Test]
        public void CreateEmptyCustomDataList()
        {
            var list = new CustomDataList();
            Assert.AreEqual(0, list.Count);
        }

        /// <summary>
        /// 测试创建包含基础类型数据的 CustomDataList
        /// </summary>
        [Test]
        public void CreateCustomDataListWithBasicTypes()
        {
            var list = new CustomDataList
            {
                CustomDataEntry.CreateInt("id", 1),
                CustomDataEntry.CreateString("name", "Item1"),
                CustomDataEntry.CreateFloat("value", 3.14f),
                CustomDataEntry.CreateBool("active", true)
            };

            Assert.AreEqual(4, list.Count);
            Assert.AreEqual(1, list[0].IntValue);
            Assert.AreEqual("Item1", list[1].StringValue);
            Assert.AreEqual(3.14f, list[2].FloatValue);
            Assert.AreEqual(true, list[3].BoolValue);
        }

        /// <summary>
        /// 测试使用 FromValues 工厂方法创建 CustomDataList
        /// </summary>
        [Test]
        public void CreateCustomDataListFromValues()
        {
            var list = CustomDataList.FromValues(10, "test", 2.5f, true);

            Assert.AreEqual(4, list.Count);
            Assert.AreEqual(10, list[0].IntValue);
            Assert.AreEqual("test", list[1].StringValue);
            Assert.AreEqual(2.5f, list[2].FloatValue);
            Assert.AreEqual(true, list[3].BoolValue);
        }

        /// <summary>
        /// 测试使用 FromEntries 工厂方法创建 CustomDataList
        /// </summary>
        [Test]
        public void CreateCustomDataListFromEntries()
        {
            var entries = new[]
            {
                CustomDataEntry.CreateInt("a", 1),
                CustomDataEntry.CreateString("b", "test"),
                CustomDataEntry.CreateFloat("c", 1.5f)
            };

            var list = CustomDataList.FromEntries(entries);

            Assert.AreEqual(3, list.Count);
            Assert.AreEqual(1, list[0].IntValue);
            Assert.AreEqual("test", list[1].StringValue);
            Assert.AreEqual(1.5f, list[2].FloatValue);
        }

        /// <summary>
        /// 测试 CustomDataList 的 Get 和 Set 方法
        /// </summary>
        [Test]
        public void GetAndSetCustomDataListValues()
        {
            var list = new CustomDataList
            {
                CustomDataEntry.CreateInt("x", 10),
                CustomDataEntry.CreateString("y", "hello")
            };

            // 测试 Get
            Assert.AreEqual(10, list.Get<int>(0));
            Assert.AreEqual("hello", list.Get<string>(1));
            Assert.AreEqual(-1, list.Get(2, -1)); // 超出范围

            // 测试 Set
            list.Set(0, 20);
            list.Set(1, "world");
            Assert.AreEqual(20, list[0].IntValue);
            Assert.AreEqual("world", list[1].StringValue);
        }

        /// <summary>
        /// 测试 CustomDataList 的 AddValue 和 InsertValue 方法
        /// </summary>
        [Test]
        public void AddAndInsertValuesToCustomDataList()
        {
            var list = new CustomDataList();
            
            list.AddValue(100);
            list.AddValue("test");
            Assert.AreEqual(2, list.Count);

            list.InsertValue(1, 50);
            Assert.AreEqual(3, list.Count);
            Assert.AreEqual(50, list[1].IntValue);
        }

        #endregion

        #region CustomDataList 嵌套列表测试

        /// <summary>
        /// 测试 CustomDataList 包含 CustomDataList 类型的 Entry（单层嵌套）
        /// </summary>
        [Test]
        public void CustomDataListWithNestedCustomDataList()
        {
            // 创建内层列表
            var innerList = new CustomDataList
            {
                CustomDataEntry.CreateInt("a", 1),
                CustomDataEntry.CreateString("b", "inner")
            };

            var entry = CustomDataEntry.CreateEntryList("nested", innerList);
            _mainList.Add(entry);

            Assert.AreEqual(1, _mainList.Count);
            var retrievedEntry = _mainList[0];
            Assert.AreEqual(CustomDataType.EntryList, retrievedEntry.Type);
            Assert.AreEqual(2, retrievedEntry.EntryListValue.Count);
            Assert.AreEqual(1, retrievedEntry.EntryListValue[0].IntValue);
            Assert.AreEqual("inner", retrievedEntry.EntryListValue[1].StringValue);
        }

        /// <summary>
        /// 测试 CustomDataList 包含混合数据（其他类型和嵌套列表）
        /// </summary>
        [Test]
        public void CustomDataListWithMixedDataAndNestedList()
        {
            var innerList = new CustomDataList
            {
                CustomDataEntry.CreateInt("x", 100),
                CustomDataEntry.CreateFloat("y", 99.5f)
            };

            _mainList.Add(CustomDataEntry.CreateInt("id", 42));
            _mainList.Add(CustomDataEntry.CreateString("name", "mixed"));
            _mainList.Add(CustomDataEntry.CreateEntryList("coordinates", innerList));
            _mainList.Add(CustomDataEntry.CreateBool("valid", true));

            Assert.AreEqual(4, _mainList.Count);
            Assert.AreEqual(42, _mainList[0].IntValue);
            Assert.AreEqual("mixed", _mainList[1].StringValue);
            Assert.AreEqual(CustomDataType.EntryList, _mainList[2].Type);
            Assert.AreEqual(true, _mainList[3].BoolValue);

            // 验证嵌套列表内容
            var nestedList = _mainList[2].EntryListValue;
            Assert.AreEqual(2, nestedList.Count);
            Assert.AreEqual(100, nestedList[0].IntValue);
            Assert.AreEqual(99.5f, nestedList[1].FloatValue);
        }

        /// <summary>
        /// 测试 CustomDataList 的多层嵌套
        /// </summary>
        [Test]
        public void CustomDataListWithMultipleLevelNesting()
        {
            // 创建第三层列表
            var level3List = new CustomDataList
            {
                CustomDataEntry.CreateInt("value3", 333)
            };

            // 创建第二层列表（包含第三层）
            var level2List = new CustomDataList
            {
                CustomDataEntry.CreateInt("value2", 222),
                CustomDataEntry.CreateEntryList("level3", level3List)
            };

            // 创建第一层列表（包含第二层）
            var level1List = new CustomDataList
            {
                CustomDataEntry.CreateInt("value1", 111),
                CustomDataEntry.CreateEntryList("level2", level2List)
            };

            _mainList = level1List;

            // 验证三层嵌套
            Assert.AreEqual(2, _mainList.Count);
            Assert.AreEqual(111, _mainList[0].IntValue);
            
            var level2 = _mainList[1].EntryListValue;
            Assert.AreEqual(2, level2.Count);
            Assert.AreEqual(222, level2[0].IntValue);

            var level3 = level2[1].EntryListValue;
            Assert.AreEqual(1, level3.Count);
            Assert.AreEqual(333, level3[0].IntValue);
        }

        #endregion

        #region CustomDataCollection 与 CustomDataList 混用测试

        /// <summary>
        /// 测试 CustomDataCollection 中包含 CustomDataList 类型的 Entry
        /// </summary>
        [Test]
        public void CustomDataCollectionWithCustomDataListEntry()
        {
            var list = new CustomDataList
            {
                CustomDataEntry.CreateInt("item_id", 10),
                CustomDataEntry.CreateString("item_name", "Sword")
            };

            var entry = CustomDataEntry.CreateEntryList("items", list);
            _mainCollection.Add(entry);

            Assert.AreEqual(1, _mainCollection.Count);
            var retrievedEntry = _mainCollection["items"];
            Assert.AreEqual(CustomDataType.EntryList, retrievedEntry.Type);
            Assert.AreEqual(2, retrievedEntry.EntryListValue.Count);
        }

        /// <summary>
        /// 测试 CustomDataCollection 中包含混合数据类型
        /// </summary>
        [Test]
        public void CustomDataCollectionWithMixedTypes()
        {
            var itemList = new CustomDataList
            {
                CustomDataEntry.CreateString("item1", "Sword"),
                CustomDataEntry.CreateString("item2", "Shield"),
                CustomDataEntry.CreateString("item3", "Potion")
            };

            _mainCollection.Add(CustomDataEntry.CreateInt("player_id", 1));
            _mainCollection.Add(CustomDataEntry.CreateString("player_name", "Hero"));
            _mainCollection.Add(CustomDataEntry.CreateEntryList("inventory", itemList));
            _mainCollection.Add(CustomDataEntry.CreateBool("is_alive", true));
            _mainCollection.Add(CustomDataEntry.CreateFloat("health", 95.5f));

            Assert.AreEqual(5, _mainCollection.Count);
            Assert.AreEqual(1, _mainCollection["player_id"].IntValue);
            Assert.AreEqual("Hero", _mainCollection["player_name"].StringValue);
            Assert.AreEqual(CustomDataType.EntryList, _mainCollection["inventory"].Type);
            Assert.AreEqual(true, _mainCollection["is_alive"].BoolValue);
            Assert.AreEqual(95.5f, _mainCollection["health"].FloatValue);

            var inventory = _mainCollection["inventory"].EntryListValue;
            Assert.AreEqual(3, inventory.Count);
        }

        /// <summary>
        /// 测试 CustomDataCollection 中的 CustomDataList 包含 CustomDataCollection
        /// </summary>
        [Test]
        public void CustomDataCollectionWithListContainingCollections()
        {
            // 创建内层 Collection
            var innerCollection = new CustomDataCollection
            {
                CustomDataEntry.CreateInt("config_id", 100),
                CustomDataEntry.CreateString("config_name", "Setting1")
            };

            // 创建包含 Collection 的 List
            var listWithCollections = new CustomDataList
            {
                CustomDataEntry.CreateEntryList("config", innerCollection)
            };

            // 添加到主 Collection
            _mainCollection.Add(CustomDataEntry.CreateEntryList("settings", listWithCollections));

            var settings = _mainCollection["settings"].EntryListValue;
            Assert.AreEqual(1, settings.Count);

            var config = settings[0].EntryListValue;
            Assert.AreEqual(2, config.Count);
            Assert.AreEqual(100, config[0].IntValue);
            Assert.AreEqual("Setting1", config[1].StringValue);
        }

        /// <summary>
        /// 测试 CustomDataCollection 中多个 CustomDataList Entry
        /// </summary>
        [Test]
        public void CustomDataCollectionWithMultipleListEntries()
        {
            var list1 = new CustomDataList { CustomDataEntry.CreateInt("a", 1) };
            var list2 = new CustomDataList { CustomDataEntry.CreateString("b", "test") };
            var list3 = new CustomDataList { CustomDataEntry.CreateFloat("c", 3.14f) };

            _mainCollection.Add(CustomDataEntry.CreateEntryList("list1", list1));
            _mainCollection.Add(CustomDataEntry.CreateEntryList("list2", list2));
            _mainCollection.Add(CustomDataEntry.CreateEntryList("list3", list3));

            Assert.AreEqual(3, _mainCollection.Count);
            Assert.AreEqual(1, _mainCollection["list1"].EntryListValue[0].IntValue);
            Assert.AreEqual("test", _mainCollection["list2"].EntryListValue[0].StringValue);
            Assert.AreEqual(3.14f, _mainCollection["list3"].EntryListValue[0].FloatValue);
        }

        #endregion

        #region 序列化和反序列化测试

        /// <summary>
        /// 测试 CustomDataList 的序列化
        /// </summary>
        [Test]
        public void SerializeCustomDataList()
        {
            var list = new CustomDataList
            {
                CustomDataEntry.CreateInt("id", 1),
                CustomDataEntry.CreateString("name", "Test"),
                CustomDataEntry.CreateFloat("value", 42.5f)
            };

            var entry = CustomDataEntry.CreateEntryList("data", list);
            string serialized = entry.SerializeValue();

            Assert.IsFalse(string.IsNullOrEmpty(serialized));
            // 应该包含序列化的数据
            Assert.IsTrue(serialized.Length > 0);
        }

        /// <summary>
        /// 测试 CustomDataList 的反序列化
        /// </summary>
        [Test]
        public void DeserializeCustomDataList()
        {
            // 创建原始数据并序列化
            var original = new CustomDataList
            {
                CustomDataEntry.CreateInt("x", 100),
                CustomDataEntry.CreateString("y", "point"),
                CustomDataEntry.CreateBool("z", true)
            };

            var entry = CustomDataEntry.CreateEntryList("coordinate", original);
            string serialized = entry.SerializeValue();

            // 反序列化到新 Entry
            var deserializedEntry = new CustomDataEntry { Key = "coordinate" };
            bool success = deserializedEntry.TryDeserializeValue(serialized, CustomDataType.EntryList);

            Assert.IsTrue(success);
            Assert.AreEqual(CustomDataType.EntryList, deserializedEntry.Type);
            Assert.AreEqual(3, deserializedEntry.EntryListValue.Count);
            Assert.AreEqual(100, deserializedEntry.EntryListValue[0].IntValue);
            Assert.AreEqual("point", deserializedEntry.EntryListValue[1].StringValue);
            Assert.AreEqual(true, deserializedEntry.EntryListValue[2].BoolValue);
        }

        /// <summary>
        /// 测试嵌套 CustomDataList 的序列化和反序列化
        /// </summary>
        [Test]
        public void SerializeDeserializeNestedCustomDataList()
        {
            // 创建嵌套结构
            var innerList = new CustomDataList
            {
                CustomDataEntry.CreateInt("inner_id", 999),
                CustomDataEntry.CreateString("inner_name", "Inner")
            };

            var outerList = new CustomDataList
            {
                CustomDataEntry.CreateInt("outer_id", 111),
                CustomDataEntry.CreateEntryList("inner", innerList),
                CustomDataEntry.CreateString("outer_name", "Outer")
            };

            var entry = CustomDataEntry.CreateEntryList("nested_data", outerList);
            string serialized = entry.SerializeValue();

            // 反序列化
            var deserializedEntry = new CustomDataEntry { Key = "nested_data" };
            bool success = deserializedEntry.TryDeserializeValue(serialized, CustomDataType.EntryList);

            Assert.IsTrue(success);
            Assert.AreEqual(3, deserializedEntry.EntryListValue.Count);

            // 验证外层数据
            Assert.AreEqual(111, deserializedEntry.EntryListValue[0].IntValue);
            Assert.AreEqual(CustomDataType.EntryList, deserializedEntry.EntryListValue[1].Type);
            Assert.AreEqual("Outer", deserializedEntry.EntryListValue[2].StringValue);

            // 验证内层数据
            var innerDeserialized = deserializedEntry.EntryListValue[1].EntryListValue;
            Assert.AreEqual(2, innerDeserialized.Count);
            Assert.AreEqual(999, innerDeserialized[0].IntValue);
            Assert.AreEqual("Inner", innerDeserialized[1].StringValue);
        }

        /// <summary>
        /// 测试 CustomDataCollection 包含 CustomDataList 的序列化和反序列化
        /// </summary>
        [Test]
        public void SerializeDeserializeCollectionWithList()
        {
            var itemList = new CustomDataList
            {
                CustomDataEntry.CreateString("item1", "Apple"),
                CustomDataEntry.CreateString("item2", "Banana"),
                CustomDataEntry.CreateString("item3", "Orange")
            };

            _mainCollection.Add(CustomDataEntry.CreateInt("store_id", 5));
            _mainCollection.Add(CustomDataEntry.CreateString("store_name", "Market"));
            _mainCollection.Add(CustomDataEntry.CreateEntryList("products", itemList));

            // 序列化整个 Entry
            var entry = CustomDataEntry.CreateEntryList("store_info", _mainCollection);
            string serialized = entry.SerializeValue();

            // 反序列化
            var deserializedEntry = new CustomDataEntry { Key = "store_info" };
            bool success = deserializedEntry.TryDeserializeValue(serialized, CustomDataType.EntryList);

            Assert.IsTrue(success);
            Assert.AreEqual(3, deserializedEntry.EntryListValue.Count);

            // 验证反序列化的数据
            Assert.AreEqual(5, deserializedEntry.EntryListValue[0].IntValue);
            Assert.AreEqual("Market", deserializedEntry.EntryListValue[1].StringValue);
            
            var productsEntry = deserializedEntry.EntryListValue[2];
            Assert.AreEqual(CustomDataType.EntryList, productsEntry.Type);
            Assert.AreEqual(3, productsEntry.EntryListValue.Count);
        }

        /// <summary>
        /// 测试复杂的多层嵌套结构的序列化和反序列化
        /// </summary>
        [Test]
        public void SerializeDeserializeComplexNestedStructure()
        {
            // 创建复杂的嵌套结构：
            // Game
            //  ├─ player_id
            //  ├─ player_name
            //  └─ levels (List)
            //      ├─ level_info (Collection)
            //      │   ├─ level_id
            //      │   └─ enemies (List)
            //      │       ├─ enemy1
            //      │       └─ enemy2
            //      └─ rewards (List)

            // 最内层：敌人列表
            var enemyList = new CustomDataList
            {
                CustomDataEntry.CreateString("enemy1", "Goblin"),
                CustomDataEntry.CreateString("enemy2", "Orc")
            };

            // 中间层：等级信息（Collection）
            var levelInfo = new CustomDataCollection
            {
                CustomDataEntry.CreateInt("level_id", 1),
                CustomDataEntry.CreateEntryList("enemies", enemyList)
            };

            // 奖励列表
            var rewardList = new CustomDataList
            {
                CustomDataEntry.CreateInt("gold", 1000),
                CustomDataEntry.CreateInt("exp", 5000)
            };

            // 等级列表（包含 Collection 和其他 List）
            var levelsList = new CustomDataList
            {
                CustomDataEntry.CreateEntryList("level_info", levelInfo),
                CustomDataEntry.CreateEntryList("rewards", rewardList)
            };

            // 游戏数据（Collection）
            _mainCollection.Add(CustomDataEntry.CreateInt("player_id", 1));
            _mainCollection.Add(CustomDataEntry.CreateString("player_name", "Adventurer"));
            _mainCollection.Add(CustomDataEntry.CreateEntryList("levels", levelsList));

            // 序列化
            var gameEntry = CustomDataEntry.CreateEntryList("game_data", _mainCollection);
            string serialized = gameEntry.SerializeValue();

            // 反序列化
            var deserializedEntry = new CustomDataEntry { Key = "game_data" };
            bool success = deserializedEntry.TryDeserializeValue(serialized, CustomDataType.EntryList);

            Assert.IsTrue(success);

            // 验证顶层
            var gameData = deserializedEntry.EntryListValue;
            Assert.AreEqual(3, gameData.Count);
            Assert.AreEqual(1, gameData[0].IntValue);
            Assert.AreEqual("Adventurer", gameData[1].StringValue);

            // 验证等级列表
            var levelsData = gameData[2].EntryListValue;
            Assert.AreEqual(2, levelsData.Count);

            // 验证等级信息（Collection）
            var levelInfoData = levelsData[0].EntryListValue;
            Assert.AreEqual(2, levelInfoData.Count);
            Assert.AreEqual(1, levelInfoData[0].IntValue);

            // 验证敌人列表
            var enemyData = levelInfoData[1].EntryListValue;
            Assert.AreEqual(2, enemyData.Count);
            Assert.AreEqual("Goblin", enemyData[0].StringValue);
            Assert.AreEqual("Orc", enemyData[1].StringValue);

            // 验证奖励列表
            var rewardData = levelsData[1].EntryListValue;
            Assert.AreEqual(2, rewardData.Count);
            Assert.AreEqual(1000, rewardData[0].IntValue);
            Assert.AreEqual(5000, rewardData[1].IntValue);
        }

        #endregion

        #region 修改和清空测试

        /// <summary>
        /// 测试修改 CustomDataList 中的嵌套列表
        /// </summary>
        [Test]
        public void ModifyNestedCustomDataListInList()
        {
            var innerList = new CustomDataList
            {
                CustomDataEntry.CreateInt("a", 1),
                CustomDataEntry.CreateInt("b", 2)
            };

            var entry = CustomDataEntry.CreateEntryList("data", innerList);
            _mainList.Add(entry);

            // 修改嵌套列表
            var nested = _mainList[0].EntryListValue;
            ((CustomDataEntry)nested[0]).IntValue = 10;
            ((CustomDataEntry)nested[1]).IntValue = 20;

            Assert.AreEqual(10, nested[0].IntValue);
            Assert.AreEqual(20, nested[1].IntValue);
        }

        /// <summary>
        /// 测试清空 CustomDataList
        /// </summary>
        [Test]
        public void ClearCustomDataList()
        {
            var list = new CustomDataList
            {
                CustomDataEntry.CreateInt("a", 1),
                CustomDataEntry.CreateInt("b", 2),
                CustomDataEntry.CreateInt("c", 3)
            };

            Assert.AreEqual(3, list.Count);
            list.Clear();
            Assert.AreEqual(0, list.Count);
        }

        /// <summary>
        /// 测试从 CustomDataList 中移除元素
        /// </summary>
        [Test]
        public void RemoveFromCustomDataList()
        {
            var list = new CustomDataList
            {
                CustomDataEntry.CreateInt("a", 1),
                CustomDataEntry.CreateString("b", "test"),
                CustomDataEntry.CreateFloat("c", 3.14f)
            };

            list.RemoveAt(1);
            Assert.AreEqual(2, list.Count);
            Assert.AreEqual(1, list[0].IntValue);
            Assert.AreEqual(3.14f, list[1].FloatValue);
        }

        #endregion

        #region 容器转换测试

        /// <summary>
        /// 测试将 CustomDataList 转换为类型化列表
        /// </summary>
        [Test]
        public void ConvertCustomDataListToTypedList()
        {
            var list = new CustomDataList
            {
                CustomDataEntry.CreateInt("a", 10),
                CustomDataEntry.CreateInt("b", 20),
                CustomDataEntry.CreateInt("c", 30)
            };

            var intList = list.ToList<int>();
            Assert.AreEqual(3, intList.Count);
            Assert.AreEqual(10, intList[0]);
            Assert.AreEqual(20, intList[1]);
            Assert.AreEqual(30, intList[2]);
        }

        /// <summary>
        /// 测试将 CustomDataList 转换为对象数组
        /// </summary>
        [Test]
        public void ConvertCustomDataListToObjectArray()
        {
            var list = new CustomDataList
            {
                CustomDataEntry.CreateInt("a", 42),
                CustomDataEntry.CreateString("b", "hello"),
                CustomDataEntry.CreateFloat("c", 1.5f),
                CustomDataEntry.CreateBool("d", true)
            };

            var array = list.ToArray();
            Assert.AreEqual(4, array.Length);
            Assert.AreEqual(42, array[0]);
            Assert.AreEqual("hello", array[1]);
            Assert.AreEqual(1.5f, array[2]);
            Assert.AreEqual(true, array[3]);
        }

        #endregion

        #region 边界情况测试

        /// <summary>
        /// 测试空 CustomDataList 的序列化和反序列化
        /// </summary>
        [Test]
        public void SerializeDeserializeEmptyCustomDataList()
        {
            var emptyList = new CustomDataList();
            var entry = CustomDataEntry.CreateEntryList("empty", emptyList);
            string serialized = entry.SerializeValue();

            var deserializedEntry = new CustomDataEntry { Key = "empty" };
            bool success = deserializedEntry.TryDeserializeValue(serialized, CustomDataType.EntryList);

            Assert.IsTrue(success);
            Assert.AreEqual(0, deserializedEntry.EntryListValue.Count);
        }

        /// <summary>
        /// 测试 CustomDataList 中的空 Entry
        /// </summary>
        [Test]
        public void CustomDataListWithEmptyNestedList()
        {
            var emptyInnerList = new CustomDataList();
            
            _mainList.Add(CustomDataEntry.CreateInt("id", 1));
            _mainList.Add(CustomDataEntry.CreateEntryList("empty_list", emptyInnerList));
            _mainList.Add(CustomDataEntry.CreateString("name", "test"));

            Assert.AreEqual(3, _mainList.Count);
            Assert.AreEqual(0, _mainList[1].EntryListValue.Count);
        }

        /// <summary>
        /// 测试非常大的 CustomDataList
        /// </summary>
        [Test]
        public void LargeCustomDataList()
        {
            var largeList = new CustomDataList();
            const int count = 1000;

            for (int i = 0; i < count; i++)
            {
                largeList.AddValue(i);
            }

            Assert.AreEqual(count, largeList.Count);
            Assert.AreEqual(0, largeList[0].IntValue);
            Assert.AreEqual(count - 1, largeList[count - 1].IntValue);
        }

        #endregion
    }
}
