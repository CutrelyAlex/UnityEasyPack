using NUnit.Framework;
using EasyPack.InventorySystem;
using System.Threading.Tasks;
using UnityEngine;
using EasyPack.Architecture;
using EasyPack.Serialization;

namespace EasyPack.InventoryTests
{
    /// <summary>
    /// 条件序列化测试 - 验证嵌套条件的序列化和反序列化
    /// </summary>
    [TestFixture]
    public class ConditionSerializationTest
    {
        private ISerializationService _serializationService;
        private IInventoryService _inventoryService;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            // 使用 Task.Run 避免死锁
            Task.Run(async () =>
            {
                try
                {
                    // 确保架构实例存在
                    Debug.Log("[ConditionSerializationTest] 开始初始化...");

                    var architecture = EasyPackArchitecture.Instance;
                    if (architecture == null)
                    {
                        Debug.LogError("[ConditionSerializationTest] EasyPackArchitecture.Instance 为 null");
                        Assert.Fail("EasyPackArchitecture.Instance 为 null");
                        return;
                    }

                    // 获取库存服务（ResolveAsync 现在会自动创建实例并初始化）
                    Debug.Log("[ConditionSerializationTest] 获取 InventoryService...");
                    _inventoryService = await EasyPackArchitecture.GetInventoryServiceAsync();
                    if (_inventoryService == null)
                    {
                        Debug.LogError("[ConditionSerializationTest] InventoryService 为 null（ResolveAsync 应该自动创建实例）");
                        Assert.Fail("InventoryService 为 null");
                        return;
                    }

                    Debug.Log($"[ConditionSerializationTest] InventoryService 获取成功，当前状态: {_inventoryService.State}");

                    // 如果未初始化，则进行初始化
                    if (_inventoryService.State == EasyPack.ENekoFramework.ServiceLifecycleState.Uninitialized)
                    {
                        Debug.Log("[ConditionSerializationTest] InventoryService 未初始化，开始初始化...");
                        await _inventoryService.InitializeAsync();
                        Debug.Log("[ConditionSerializationTest] InventoryService 初始化完成");
                    }

                    // 获取序列化服务
                    Debug.Log("[ConditionSerializationTest] 解析序列化服务...");
                    _serializationService = await architecture.ResolveAsync<ISerializationService>();

                    if (_serializationService == null)
                    {
                        Debug.LogError("[ConditionSerializationTest] 序列化服务解析失败");
                        Assert.Fail("序列化服务解析失败");
                        return;
                    }

                    Debug.Log("[ConditionSerializationTest] 初始化成功");
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[ConditionSerializationTest] 初始化失败: {ex.Message}\n{ex.StackTrace}");
                    throw;
                }
            }).GetAwaiter().GetResult();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            // 清理
        }

        /// <summary>
        /// 测试：ItemTypeCondition 序列化
        /// </summary>
        [Test]
        public void Test_ItemTypeCondition_Serialization()
        {
            var original = new ItemTypeCondition("Weapon");

            // 序列化
            var json = _serializationService.SerializeToJson(original);
            Assert.IsNotNull(json, "序列化结果不应为 null");

            // 反序列化
            var restored = _serializationService.DeserializeFromJson<ItemTypeCondition>(json);
            Assert.IsNotNull(restored, "反序列化结果不应为 null");
            Assert.AreEqual("Weapon", restored.ItemType, "ItemType 应该匹配");
        }

        /// <summary>
        /// 测试：AttributeCondition 序列化
        /// </summary>
        [Test]
        public void Test_AttributeCondition_Serialization()
        {
            var original = new AttributeCondition("Level", 10, AttributeComparisonType.GreaterThanOrEqual);

            // 序列化
            var json = _serializationService.SerializeToJson(original);
            Assert.IsNotNull(json, "序列化结果不应为 null");

            // 反序列化
            var restored = _serializationService.DeserializeFromJson<AttributeCondition>(json);
            Assert.IsNotNull(restored, "反序列化结果不应为 null");
            Assert.AreEqual("Level", restored.AttributeName, "AttributeName 应该匹配");
            Assert.AreEqual(10, restored.AttributeValue, "AttributeValue 应该匹配");
            Assert.AreEqual(AttributeComparisonType.GreaterThanOrEqual, restored.ComparisonType, "ComparisonType 应该匹配");
        }

        /// <summary>
        /// 测试：AllCondition 嵌套序列化
        /// </summary>
        [Test]
        public void Test_AllCondition_Nested_Serialization()
        {
            // 创建嵌套条件：All(ItemType("Weapon"), Attr("Level", >=5))
            var original = new AllCondition(
                new ItemTypeCondition("Weapon"),
                new AttributeCondition("Level", 5, AttributeComparisonType.GreaterThanOrEqual)
            );

            // 序列化
            var json = _serializationService.SerializeToJson(original);
            Assert.IsNotNull(json, "序列化结果不应为 null");
            Debug.Log($"AllCondition JSON: {json}");

            // 反序列化
            var restored = _serializationService.DeserializeFromJson<AllCondition>(json);
            Assert.IsNotNull(restored, "反序列化结果不应为 null");
            Assert.AreEqual(2, restored.Children.Count, "子条件数量应该为 2");

            // 验证子条件类型
            Assert.IsInstanceOf<ItemTypeCondition>(restored.Children[0], "第一个子条件应该是 ItemTypeCondition");
            Assert.IsInstanceOf<AttributeCondition>(restored.Children[1], "第二个子条件应该是 AttributeCondition");

            // 验证子条件内容
            var itemTypeCond = restored.Children[0] as ItemTypeCondition;
            Assert.AreEqual("Weapon", itemTypeCond.ItemType);

            var attrCond = restored.Children[1] as AttributeCondition;
            Assert.AreEqual("Level", attrCond.AttributeName);
            Assert.AreEqual(5, attrCond.AttributeValue);
        }

        /// <summary>
        /// 测试：AnyCondition 嵌套序列化
        /// </summary>
        [Test]
        public void Test_AnyCondition_Nested_Serialization()
        {
            // 创建嵌套条件：Any(ItemType("Potion"), ItemType("Food"))
            var original = new AnyCondition(
                new ItemTypeCondition("Potion"),
                new ItemTypeCondition("Food")
            );

            // 序列化
            var json = _serializationService.SerializeToJson(original);
            Assert.IsNotNull(json, "序列化结果不应为 null");

            // 反序列化
            var restored = _serializationService.DeserializeFromJson<AnyCondition>(json);
            Assert.IsNotNull(restored, "反序列化结果不应为 null");
            Assert.AreEqual(2, restored.Children.Count, "子条件数量应该为 2");

            var cond1 = restored.Children[0] as ItemTypeCondition;
            var cond2 = restored.Children[1] as ItemTypeCondition;
            Assert.AreEqual("Potion", cond1.ItemType);
            Assert.AreEqual("Food", cond2.ItemType);
        }

        /// <summary>
        /// 测试：NotCondition 嵌套序列化
        /// </summary>
        [Test]
        public void Test_NotCondition_Nested_Serialization()
        {
            // 创建嵌套条件：Not(ItemType("Trash"))
            var original = new NotCondition(new ItemTypeCondition("Trash"));

            // 序列化
            var json = _serializationService.SerializeToJson(original);
            Assert.IsNotNull(json, "序列化结果不应为 null");

            // 反序列化
            var restored = _serializationService.DeserializeFromJson<NotCondition>(json);
            Assert.IsNotNull(restored, "反序列化结果不应为 null");
            Assert.IsNotNull(restored.Inner, "内部条件不应为 null");
            Assert.IsInstanceOf<ItemTypeCondition>(restored.Inner, "内部条件应该是 ItemTypeCondition");

            var innerCond = restored.Inner as ItemTypeCondition;
            Assert.AreEqual("Trash", innerCond.ItemType);
        }

        /// <summary>
        /// 测试：复杂嵌套条件序列化 - All(Any(...), Not(...))
        /// </summary>
        [Test]
        public void Test_Complex_Nested_Conditions_Serialization()
        {
            // 创建复杂嵌套条件：
            // All(
            //   Any(ItemType("Weapon"), ItemType("Armor")),
            //   Not(Attr("Broken", true)),
            //   Attr("Level", >=10)
            // )
            var original = new AllCondition(
                new AnyCondition(
                    new ItemTypeCondition("Weapon"),
                    new ItemTypeCondition("Armor")
                ),
                new NotCondition(
                    new AttributeCondition("Broken", true, AttributeComparisonType.Equal)
                ),
                new AttributeCondition("Level", 10, AttributeComparisonType.GreaterThanOrEqual)
            );

            // 序列化
            var json = _serializationService.SerializeToJson(original);
            Assert.IsNotNull(json, "序列化结果不应为 null");
            Debug.Log($"Complex Nested Condition JSON: {json}");

            // 反序列化
            var restored = _serializationService.DeserializeFromJson<AllCondition>(json);
            Assert.IsNotNull(restored, "反序列化结果不应为 null");
            Assert.AreEqual(3, restored.Children.Count, "子条件数量应该为 3");

            // 验证第一个子条件：AnyCondition
            Assert.IsInstanceOf<AnyCondition>(restored.Children[0], "第一个子条件应该是 AnyCondition");
            var anyCond = restored.Children[0] as AnyCondition;
            Assert.AreEqual(2, anyCond.Children.Count, "Any 条件应该有 2 个子条件");

            // 验证第二个子条件：NotCondition
            Assert.IsInstanceOf<NotCondition>(restored.Children[1], "第二个子条件应该是 NotCondition");
            var notCond = restored.Children[1] as NotCondition;
            Assert.IsNotNull(notCond.Inner, "Not 条件的内部条件不应为 null");
            Assert.IsInstanceOf<AttributeCondition>(notCond.Inner, "Not 条件的内部应该是 AttributeCondition");

            // 验证第三个子条件：AttributeCondition
            Assert.IsInstanceOf<AttributeCondition>(restored.Children[2], "第三个子条件应该是 AttributeCondition");
            var attrCond = restored.Children[2] as AttributeCondition;
            Assert.AreEqual("Level", attrCond.AttributeName);
            Assert.AreEqual(10, attrCond.AttributeValue);
        }

        /// <summary>
        /// 测试：条件功能验证 - 确保序列化后条件仍然有效
        /// </summary>
        [Test]
        public void Test_Condition_Functionality_After_Serialization()
        {
            // 创建条件：All(ItemType("Weapon"), Attr("Attack", >=50))
            var original = new AllCondition(
                new ItemTypeCondition("Weapon"),
                new AttributeCondition("Attack", 50, AttributeComparisonType.GreaterThanOrEqual)
            );

            // 序列化和反序列化
            var json = _serializationService.SerializeToJson(original);
            var restored = _serializationService.DeserializeFromJson<AllCondition>(json);

            // 创建测试物品
            var validItem = new Item
            {
                ID = "sword",
                Name = "神剑",
                Type = "Weapon"
            };
            validItem.SetCustomData("Attack", 100);

            var invalidItem1 = new Item
            {
                ID = "potion",
                Name = "药水",
                Type = "Potion"
            };

            var invalidItem2 = new Item
            {
                ID = "weak_sword",
                Name = "弱剑",
                Type = "Weapon"
            };
            invalidItem2.SetCustomData("Attack", 10);

            // 验证条件功能
            Assert.IsTrue(restored.CheckCondition(validItem), "有效物品应该通过条件检查");
            Assert.IsFalse(restored.CheckCondition(invalidItem1), "错误类型的物品应该失败");
            Assert.IsFalse(restored.CheckCondition(invalidItem2), "攻击力不足的物品应该失败");
        }

        /// <summary>
        /// 测试：深度嵌套条件 - All(All(All(...)))
        /// </summary>
        [Test]
        public void Test_Deep_Nested_Conditions()
        {
            // 创建深度嵌套：All(All(All(ItemType("Weapon"))))
            var level3 = new AllCondition(new ItemTypeCondition("Weapon"));
            var level2 = new AllCondition(level3);
            var original = new AllCondition(level2);

            // 序列化和反序列化
            var json = _serializationService.SerializeToJson(original);
            var restored = _serializationService.DeserializeFromJson<AllCondition>(json);

            Assert.IsNotNull(restored);
            Assert.AreEqual(1, restored.Children.Count);
            Assert.IsInstanceOf<AllCondition>(restored.Children[0]);

            var restoredLevel2 = restored.Children[0] as AllCondition;
            Assert.AreEqual(1, restoredLevel2.Children.Count);
            Assert.IsInstanceOf<AllCondition>(restoredLevel2.Children[0]);

            var restoredLevel3 = restoredLevel2.Children[0] as AllCondition;
            Assert.AreEqual(1, restoredLevel3.Children.Count);
            Assert.IsInstanceOf<ItemTypeCondition>(restoredLevel3.Children[0]);
        }

        /// <summary>
        /// 测试：容器条件序列化 - 实际使用场景
        /// </summary>
        [Test]
        public void Test_Container_Condition_Serialization()
        {
            // 创建带条件的容器
            var container = new LinerContainer("weapon_chest", "武器箱", "Chest", 10);
            container.ContainerCondition.Add(new ItemTypeCondition("Weapon"));
            container.ContainerCondition.Add(new AttributeCondition("Level", 5, AttributeComparisonType.GreaterThanOrEqual));

            // 添加物品
            var sword = new Item { ID = "sword", Name = "剑", Type = "Weapon" };
            sword.SetCustomData("Level", 10);
            container.AddItems(sword);

            // 序列化容器
            var json = _serializationService.SerializeToJson(container);
            Assert.IsNotNull(json);
            Debug.Log($"Container with Conditions JSON: {json}");

            // 反序列化容器
            var restored = _serializationService.DeserializeFromJson<Container>(json);
            Assert.IsNotNull(restored);
            Assert.AreEqual(2, restored.ContainerCondition.Count, "容器条件数量应该为 2");

            // 验证条件功能
            var lowLevelSword = new Item { ID = "weak", Name = "弱剑", Type = "Weapon" };
            lowLevelSword.SetCustomData("Level", 2);

            var (result, _) = restored.AddItems(lowLevelSword);
            Assert.AreEqual(AddItemResult.ItemConditionNotMet, result, "低等级武器应该被条件拒绝");
        }
    }
}
