using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using EasyPack.InventorySystem;
using EasyPack.Architecture;
using EasyPack.Serialization;

namespace EasyPack.InventoryTests
{
    /// <summary>
    /// Inventory 序列化测试示例（使用 EasyPack 架构初始化）
    /// </summary>
    [TestFixture]
    public class InventorySerializationTest
    {
        private IInventoryService _inventoryService;
        private ISerializationService _serializationService;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            Debug.Log("OneTimeSetup 开始");

            // 使用 Task.Run 避免死锁（将异步操作转移到线程池）
            Task.Run(async () =>
            {
                // 获取 InventoryService（现在 ResolveAsync 会自动创建实例并初始化）
                _inventoryService = await EasyPackArchitecture.GetInventoryServiceAsync();
                Assert.IsNotNull(_inventoryService, "InventoryService 应该通过 ResolveAsync 自动创建");
                Debug.Log($"InventoryService 获取成功，当前状态: {_inventoryService.State}");

                // 如果未初始化，则进行初始化
                if (_inventoryService.State == EasyPack.ENekoFramework.ServiceLifecycleState.Uninitialized)
                {
                    Debug.Log("InventoryService 未初始化，开始初始化...");
                    await _inventoryService.InitializeAsync();
                    Debug.Log("InventoryService 初始化完成");
                }

                // 获取序列化服务
                _serializationService = await EasyPackArchitecture.Instance.ResolveAsync<ISerializationService>();
                Assert.IsNotNull(_serializationService, "序列化服务应该成功解析");
                Debug.Log("OneTimeSetup 完成");
            }).GetAwaiter().GetResult();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            // 清理工作
        }

        [Test]
        public void Test_BasicConditionSerialization()
        {
            // ItemType条件
            var container1 = new LinerContainer("cond_test1", "条件测试1", "Test", 10);
            container1.ContainerCondition.Add(new ItemTypeCondition("Equipment"));

            string json1 = _serializationService.SerializeToJson(container1);
            var restored1 = _serializationService.DeserializeFromJson<Container>(json1);

            Assert.AreEqual(1, restored1.ContainerCondition.Count, "ItemType条件序列化失败");
            Assert.IsInstanceOf<ItemTypeCondition>(restored1.ContainerCondition[0], "ItemType条件类型错误");
        }

        [Test]
        public void Test_CompositeConditionSerialization()
        {
            // 创建复杂的嵌套条件
            var container = new LinerContainer("composite_test", "组合条件测试", "Test", 10);

            var allCondition = new AllCondition(
                new ItemTypeCondition("Equipment"),
                new AttributeCondition("Level", 10, AttributeComparisonType.GreaterThanOrEqual)
            );
            container.ContainerCondition.Add(allCondition);

            // 序列化
            string json = _serializationService.SerializeToJson(container);

            // 反序列化
            var restored = _serializationService.DeserializeFromJson<Container>(json);
            Assert.AreEqual(1, restored.ContainerCondition.Count, "组合条件数量不匹配");
            Assert.IsInstanceOf<AllCondition>(restored.ContainerCondition[0], "组合条件类型错误");

            var restoredAll = restored.ContainerCondition[0] as AllCondition;
            Assert.AreEqual(2, restoredAll.Children.Count, "嵌套条件数量不匹配");
        }

        [Test]
        public void Test_ContainerSerialization()
        {
            // 创建带条件的容器
            var container = new LinerContainer("test_bag", "测试背包", "Backpack", 20);
            container.ContainerCondition.Add(new ItemTypeCondition("Equipment"));
            container.ContainerCondition.Add(new AttributeCondition("Level", 10, AttributeComparisonType.GreaterThanOrEqual));

            // 添加物品
            var item1 = new Item { ID = "sword_001", Name = "铁剑", Type = "Equipment", IsStackable = false };
            item1.SetCustomData("Level", 15);
            container.AddItems(item1);

            var item2 = new Item { ID = "potion_001", Name = "生命药水", Type = "Consumable", IsStackable = true, MaxStackCount = 20, Count = 5 };
            container.AddItems(item2);

            // 序列化
            string json = _serializationService.SerializeToJson(container);

            // 反序列化
            var restored = _serializationService.DeserializeFromJson<Container>(json);
            Assert.IsNotNull(restored, "容器反序列化失败");
            Assert.AreEqual("test_bag", restored.ID, "容器ID不匹配");
            Assert.AreEqual(2, restored.ContainerCondition.Count, "容器条件数量不匹配");
            Assert.AreEqual(1, restored.GetItemTotalCount("sword_001"), "物品数量不匹配");
        }

        [Test]
        public void Test_ItemSerialization()
        {
            // 创建物品
            var item = new Item
            {
                ID = "epic_sword",
                Name = "史诗之剑",
                Type = "Equipment",
                IsStackable = false
            };
            item.SetCustomData("Rarity", "Epic");
            item.SetCustomData("Level", 50);
            item.SetCustomData("Attack", 100);

            // 序列化
            string json = _serializationService.SerializeToJson(item);

            // 反序列化
            var restored = _serializationService.DeserializeFromJson<Item>(json);
            Assert.IsNotNull(restored, "物品反序列化失败");
            Assert.AreEqual("epic_sword", restored.ID, "物品ID不匹配");
            Assert.AreEqual(3, restored.RuntimeMetadata.Count, "物品属性数量不匹配");
            Assert.AreEqual("Epic", restored.GetCustomData<string>("Rarity"), "物品属性值不匹配");
        }

        [Test]
        public void Test_ItemSerialization_WithItemUID()
        {
            // 创建物品并通过容器分配UID
            var container = new LinerContainer("uid_test", "UID测试容器", "Test", 10);
            _inventoryService.RegisterContainer(container);  // 注册容器到 InventoryService
            
            var item = new Item { ID = "uid_sword", Name = "UID剑", Type = "Equipment", IsStackable = false };
            
            // 添加物品到容器，会自动分配UID
            container.AddItems(item);
            
            long originalUID = item.ItemUID;
            Assert.AreNotEqual(-1, originalUID, "物品添加后应该分配有效的UID");

            // 序列化物品
            string json = _serializationService.SerializeToJson(item);

            // 反序列化物品
            var restored = _serializationService.DeserializeFromJson<Item>(json);
            Assert.IsNotNull(restored, "物品反序列化失败");
            Assert.AreEqual(originalUID, restored.ItemUID, "物品ItemUID应该被保留");
            Assert.AreEqual("uid_sword", restored.ID, "物品ID不匹配");
        }

        [Test]
        public void Test_ContainerSerialization_WithItemUID()
        {
            // 创建容器并添加物品
            var container = new LinerContainer("container_uid_test", "容器UID测试", "Test", 10);
            _inventoryService.RegisterContainer(container);  // 注册容器到 InventoryService
            
            var item1 = new Item { ID = "item_001", Name = "物品1", Type = "Equipment", IsStackable = false };
            var item2 = new Item { ID = "item_002", Name = "物品2", Type = "Consumable", IsStackable = true, MaxStackCount = 20, Count = 5 };
            
            // 添加物品，应该自动分配UID（AddItems会克隆物品）
            container.AddItems(item1);
            container.AddItems(item2);
            
            // 从槽位获取实际存储的物品UID（克隆后的UID）
            var slot1 = container.GetSlot(0);
            var slot2 = container.GetSlot(1);
            long uid1 = slot1.ItemUID;
            long uid2 = slot2.ItemUID;
            
            Assert.AreNotEqual(-1, uid1, "物品1应该分配有效UID");
            Assert.AreNotEqual(-1, uid2, "物品2应该分配有效UID");

            // 序列化容器
            string json = _serializationService.SerializeToJson(container);

            // 反序列化容器
            var restored = _serializationService.DeserializeFromJson<Container>(json);
            Assert.IsNotNull(restored, "容器反序列化失败");
            Assert.AreEqual(2, restored.UsedSlots, "容器物品数量不匹配");
            
            // 检查反序列化后的物品UID
            var restoredSlot1 = restored.GetSlot(0);
            var restoredSlot2 = restored.GetSlot(1);
            
            Assert.IsNotNull(restoredSlot1?.Item, "第一个槽位应该有物品");
            Assert.IsNotNull(restoredSlot2?.Item, "第二个槽位应该有物品");
            
            // ItemUID应该被保留
            Assert.AreEqual(uid1, restoredSlot1.Item.ItemUID, "第一个物品的UID应该被保留");
            Assert.AreEqual(uid2, restoredSlot2.Item.ItemUID, "第二个物品的UID应该被保留");
        }
    }
}
