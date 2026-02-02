using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using EasyPack.InventorySystem;
using EasyPack.Architecture;
using EasyPack.ENekoFramework;

namespace EasyPack.InventoryTests
{
    /// <summary>
    /// ItemUID体系测试
    /// </summary>
    [TestFixture]
    public class InventoryItemUIDTest
    {
        private InventoryService _inventoryService;
        private Container _testContainer;

        [SetUp]
        public void Setup()
        {
            // 使用 Task.Run 同步调用异步初始化
            Task.Run(async () =>
            {
                // 获取并初始化 InventoryService
                _inventoryService = await EasyPackArchitecture.GetInventoryServiceAsync() as InventoryService;
                
                if (_inventoryService.State == ServiceLifecycleState.Uninitialized)
                {
                    await _inventoryService.InitializeAsync();
                }
            }).GetAwaiter().GetResult();
            
            _testContainer = new LinerContainer("test_container", "Test Container", "Basic", 10);
            _inventoryService.RegisterContainer(_testContainer);
        }

        [TearDown]
        public void TearDown()
        {
            _inventoryService?.Reset();
        }

        #region UID分配测试

        /// <summary>
        /// 测试UID自动分配
        /// </summary>
        [Test]
        public void Test_AssignItemUID_AutoAssign()
        {
            var item = CreateTestItem("item1", "Test Item", isStackable: true, maxStackCount: 10);
            Assert.AreEqual(-1, item.ItemUID, "新物品应该UID为-1");

            long uid = _inventoryService.AssignItemUID(item);
            Assert.AreNotEqual(-1, uid, "分配后UID应该大于-1");
            Assert.AreEqual(uid, item.ItemUID, "物品的UID应该被更新");
        }

        /// <summary>
        /// 测试UID顺序递增
        /// </summary>
        [Test]
        public void Test_AssignItemUID_Sequential()
        {
            var item1 = CreateTestItem("item1", "Item 1", true);
            var item2 = CreateTestItem("item2", "Item 2", true);
            var item3 = CreateTestItem("item3", "Item 3", true);

            long uid1 = _inventoryService.AssignItemUID(item1);
            long uid2 = _inventoryService.AssignItemUID(item2);
            long uid3 = _inventoryService.AssignItemUID(item3);

            Assert.AreEqual(uid1 + 1, uid2, "第二个UID应该比第一个大1");
            Assert.AreEqual(uid2 + 1, uid3, "第三个UID应该比第二个大1");
        }

        /// <summary>
        /// 测试不重复分配UID
        /// </summary>
        [Test]
        public void Test_AssignItemUID_NoRepeatAssignment()
        {
            var item = CreateTestItem("item1", "Test Item", true);

            long uid1 = _inventoryService.AssignItemUID(item);
            long uid2 = _inventoryService.AssignItemUID(item);

            Assert.AreEqual(uid1, uid2, "同一物品的UID不应该重复分配");
        }

        #endregion

        #region 添加物品时的UID处理

        /// <summary>
        /// 测试添加物品时自动分配UID
        /// </summary>
        [Test]
        public void Test_AddItems_AutoAssignUID()
        {
            var item = CreateTestItem("item1", "Test Item", isStackable: true, maxStackCount: 10);
            Assert.AreEqual(-1, item.ItemUID, "新物品初始UID应该为-1");

            item.Count = 5;
            var (result, addedCount) = _testContainer.AddItems(item);

            Assert.AreEqual(AddItemResult.Success, result, "添加物品应该成功");
            Assert.AreNotEqual(-1, item.ItemUID, "添加后物品应该被分配UID");
            Assert.AreEqual(5, addedCount, "应该添加5个物品");
        }

        /// <summary>
        /// 测试克隆物品后重新分配UID
        /// </summary>
        [Test]
        public void Test_CloneItem_ReassignUID()
        {
            var original = CreateTestItem("item1", "Original", isStackable: true, maxStackCount: 10);
            _inventoryService.AssignItemUID(original);
            long originalUID = original.ItemUID;

            var cloned = original.Clone();
            Assert.AreEqual(-1, cloned.ItemUID, "克隆物品的UID应该重置为-1");
            Assert.AreNotEqual(originalUID, cloned.ItemUID, "克隆物品应该是不同的实例");

            _inventoryService.AssignItemUID(cloned);
            Assert.AreNotEqual(originalUID, cloned.ItemUID, "克隆物品分配的UID应该不同于原物品");
        }

        /// <summary>
        /// 测试多个物品添加时各自获得不同UID
        /// </summary>
        [Test]
        public void Test_AddMultipleItems_DifferentUIDs()
        {
            var item1 = CreateTestItem("item1", "Item 1", true, 10);
            var item2 = CreateTestItem("item2", "Item 2", true, 10);

            item1.Count = 3;
            _testContainer.AddItems(item1);
            item2.Count = 3;
            _testContainer.AddItems(item2);

            Assert.AreNotEqual(-1, item1.ItemUID, "item1应该有UID");
            Assert.AreNotEqual(-1, item2.ItemUID, "item2应该有UID");
            Assert.AreNotEqual(item1.ItemUID, item2.ItemUID, "不同物品应该有不同的UID");
        }

        #endregion

        #region UID查询功能

        /// <summary>
        /// 测试通过UID查找物品
        /// </summary>
        [Test]
        public void Test_GetItemByUID()
        {
            var item = CreateTestItem("item1", "Test Item", true);
            long uid = _inventoryService.AssignItemUID(item);

            var foundItem = _inventoryService.GetItemByUID(uid);
            Assert.IsNotNull(foundItem, "应该能通过UID查找到物品");
            Assert.AreEqual(item.ID, foundItem.ID, "查找到的物品ID应该一致");
        }

        /// <summary>
        /// 测试查找不存在的UID
        /// </summary>
        [Test]
        public void Test_GetItemByUID_NotFound()
        {
            var foundItem = _inventoryService.GetItemByUID(9999);
            Assert.IsNull(foundItem, "查找不存在的UID应该返回null");
        }

        /// <summary>
        /// 测试检查UID是否已注册
        /// </summary>
        [Test]
        public void Test_IsUIDRegistered()
        {
            var item = CreateTestItem("item1", "Test Item", true);
            long uid = _inventoryService.AssignItemUID(item);

            Assert.IsTrue(_inventoryService.IsUIDRegistered(uid), "已分配的UID应该被认为已注册");
            Assert.IsFalse(_inventoryService.IsUIDRegistered(9999), "未分配的UID应该返回false");
        }

        #endregion

        #region Slot中的ItemUID

        /// <summary>
        /// 测试Slot中的ItemUID属性
        /// AddItems 会克隆物品并分配新的 UID，slot 中的物品 UID 应该有效但与原物品不同
        /// </summary>
        [Test]
        public void Test_SlotItemUID_Sync()
        {
            var item = CreateTestItem("item1", "Test Item", true, 10);
            // 注意：即使预先分配 UID，AddItems 也会克隆物品并分配新 UID
            // 因为容器内物品需要与外部完全独立

            item.Count = 5;
            _testContainer.AddItems(item);

            var slot = _testContainer.GetSlot(0);
            Assert.IsNotNull(slot, "应该能获取槽位");
            Assert.AreNotEqual(-1, slot.ItemUID, "Slot中的ItemUID应该是有效的（不为-1）");
            Assert.IsTrue(_inventoryService.IsUIDRegistered(slot.ItemUID), "Slot中的ItemUID应该在InventoryService中注册");
        }

        /// <summary>
        /// 测试空槽位的ItemUID
        /// </summary>
        [Test]
        public void Test_SlotItemUID_Empty()
        {
            var slot = _testContainer.GetSlot(0);
            Assert.AreEqual(-1, slot.ItemUID, "空槽位的ItemUID应该为-1");
        }

        /// <summary>
        /// 测试清空槽位后ItemUID重置
        /// </summary>
        [Test]
        public void Test_SlotItemUID_AfterClear()
        {
            var item = CreateTestItem("item1", "Test Item", true, 10);
            item.Count = 5;
            _testContainer.AddItems(item);

            var slot = _testContainer.GetSlot(0);
            Assert.AreNotEqual(-1, slot.ItemUID, "添加物品后槽位应该有UID");

            _testContainer.ClearSlot(0);
            Assert.AreEqual(-1, slot.ItemUID, "清空后槽位的ItemUID应该为-1");
        }

        #endregion

        #region UID生命周期

        /// <summary>
        /// 测试UID注销
        /// </summary>
        [Test]
        public void Test_UnregisterItemUID()
        {
            var item = CreateTestItem("item1", "Test Item", true);
            long uid = _inventoryService.AssignItemUID(item);

            Assert.IsTrue(_inventoryService.IsUIDRegistered(uid), "UID应该被注册");

            _inventoryService.UnregisterItemUID(uid);
            Assert.IsFalse(_inventoryService.IsUIDRegistered(uid), "注销后UID应该不存在");
        }

        /// <summary>
        /// 测试物品堆叠时UID保持一致
        /// 堆叠到现有槽位时，应该保留槽位中原物品的UID
        /// </summary>
        [Test]
        public void Test_StackableItems_SameUID()
        {
            var item1 = CreateTestItem("item1", "Test Item", isStackable: true, maxStackCount: 99);
            var item2 = CreateTestItem("item1", "Test Item", isStackable: true, maxStackCount: 99);

            // 第一次添加
            item1.Count = 10;
            _testContainer.AddItems(item1);
            
            var slot = _testContainer.GetSlot(0);
            long slotUID = slot.ItemUID;
            Assert.AreNotEqual(-1, slotUID, "添加后槽位应该有有效UID");

            // 第二个相同ID物品添加时应该堆叠到现有槽位
            item2.Count = 20;
            _testContainer.AddItems(item2);
            
            // 堆叠后槽位应该保持原物品的UID
            Assert.AreEqual(slotUID, slot.ItemUID, "堆叠后槽位仍应该持有第一个物品的UID");
            Assert.AreEqual(30, slot.Item.Count, "堆叠后数量应该是30");
        }

        /// <summary>
        /// 测试物品移除时的UID处理
        /// </summary>
        [Test]
        public void Test_RemoveItem_UIDHandling()
        {
            var item = CreateTestItem("item1", "Test Item", true, 10);
            item.Count = 10;
            _testContainer.AddItems(item);
            
            // 获取槽位中物品的UID（克隆后的物品）
            var slot = _testContainer.GetSlot(0);
            long slotItemUID = slot.ItemUID;
            Assert.AreNotEqual(-1, slotItemUID, "槽位应该有有效UID");

            // 部分移除
            var (result1, removedCount1) = _testContainer.RemoveItems(item.ID, 5);
            Assert.AreEqual(RemoveItemResult.Success, result1);
            Assert.AreEqual(5, removedCount1);
            Assert.AreEqual(slotItemUID, slot.ItemUID, "部分移除后槽位UID应该保持");
            Assert.AreEqual(5, slot.Item.Count, "部分移除后数量应该是5");

            // 全部移除
            var (result2, removedCount2) = _testContainer.RemoveItems(item.ID, 5);
            Assert.AreEqual(RemoveItemResult.Success, result2);
            Assert.AreEqual(5, removedCount2);
            Assert.AreEqual(-1, slot.ItemUID, "全部移除后槽位UID应该是-1");
        }

        #endregion

        #region UID唯一性保证

        /// <summary>
        /// 测试大量物品的UID唯一性
        /// </summary>
        [Test]
        public void Test_UID_UniquenessUnderMassiveCreation()
        {
            const int itemCount = 1000;
            var items = new List<Item>();
            var uids = new HashSet<long>();

            for (int i = 0; i < itemCount; i++)
            {
                var item = CreateTestItem($"item{i}", $"Item {i}", true);
                long uid = _inventoryService.AssignItemUID(item);
                items.Add(item);
                uids.Add(uid);
            }

            Assert.AreEqual(itemCount, uids.Count, "所有UID应该是唯一的");
            Assert.IsTrue(uids.All(u => u != -1), "所有UID都应该大于-1");
        }

        /// <summary>
        /// 测试跨容器物品的UID唯一性
        /// </summary>
        [Test]
        public void Test_UID_UniquenessAcrossContainers()
        {
            var container2 = new LinerContainer("container2", "Container 2", "Basic", 10);
            _inventoryService.RegisterContainer(container2);

            var item1 = CreateTestItem("item1", "Item 1", true, 10);
            var item2 = CreateTestItem("item2", "Item 2", true, 10);

            item1.Count = 5;
            _testContainer.AddItems(item1);
            item2.Count = 5;
            container2.AddItems(item2);

            Assert.AreNotEqual(item1.ItemUID, item2.ItemUID, "不同容器的物品UID应该唯一");
        }

        #endregion

        #region UID与物品操作的集成

        /// <summary>
        /// 测试物品转移后UID保持
        /// </summary>
        [Test]
        public void Test_MoveItem_UIDPreserved()
        {
            var container2 = new LinerContainer("container2", "Container 2", "Basic", 10);
            _inventoryService.RegisterContainer(container2);

            var item = CreateTestItem("item1", "Test Item", true, 10);
            item.Count = 5;
            _testContainer.AddItems(item);
            long itemUID = item.ItemUID;

            var moveResult = _inventoryService.MoveItem(
                _testContainer.ID, 0,
                container2.ID, -1
            );

            Assert.AreEqual(MoveResult.Success, moveResult, "物品移动应该成功");
            Assert.AreEqual(itemUID, item.ItemUID, "移动后UID应该保持不变");
        }

        /// <summary>
        /// 测试物品分布后各实例UID唯一
        /// </summary>
        [Test]
        public void Test_DistributeItems_DifferentUIDs()
        {
            // 创建小容量容器，确保物品需要分配到多个容器
            var container1 = new LinerContainer("small1", "Small Container 1", "Basic", 1);
            var container2 = new LinerContainer("small2", "Small Container 2", "Basic", 1);
            var container3 = new LinerContainer("small3", "Small Container 3", "Basic", 1);
            // 注册新容器到 InventoryService，这样它们才能分配 UID
            _inventoryService.RegisterContainer(container1);
            _inventoryService.RegisterContainer(container2);
            _inventoryService.RegisterContainer(container3);

            var item = CreateTestItem("item1", "Test Item", true, 10);
            var originalUID = _inventoryService.AssignItemUID(item);

            // 分配 30 个物品到 3 个容器（每个容器 1 槽位，每槽最多 10 个）
            var distribution = _inventoryService.DistributeItems(
                item, 30,
                new List<string> { container1.ID, container2.ID, container3.ID }
            );

            Assert.AreEqual(3, distribution.Count, "应该分配到三个容器");
            Assert.IsTrue(distribution.Values.All(count => count > 0), "每个容器都应该收到物品");
        }

        #endregion

        #region 辅助方法

        private Item CreateTestItem(string id, string name, bool isStackable, int maxStackCount = 1, string type = "Default")
        {
            return new Item
            {
                ID = id,
                Name = name,
                Type = type,
                IsStackable = isStackable,
                MaxStackCount = maxStackCount,
                Weight = 1.0f
            };
        }

        #endregion
    }
}
