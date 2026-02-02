using NUnit.Framework;
using EasyPack.InventorySystem;
using System.Threading.Tasks;
using UnityEngine;
using EasyPack.Architecture;
using EasyPack.Serialization;

namespace EasyPack.InventoryTests
{
    /// <summary>
    /// 网格容器测试
    /// </summary>
    [TestFixture]
    public class GridContainerTest
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
                    Debug.Log("[GridContainerTest] 开始初始化...");

                    var architecture = EasyPackArchitecture.Instance;
                    if (architecture == null)
                    {
                        Debug.LogError("[GridContainerTest] EasyPackArchitecture.Instance 为 null");
                        Assert.Fail("EasyPackArchitecture.Instance 为 null");
                        return;
                    }

                    // 获取库存服务
                    Debug.Log("[GridContainerTest] 获取 InventoryService...");
                    _inventoryService = await EasyPackArchitecture.GetInventoryServiceAsync();
                    if (_inventoryService == null)
                    {
                        Debug.LogError("[GridContainerTest] InventoryService 为 null");
                        Assert.Fail("InventoryService 为 null");
                        return;
                    }

                    Debug.Log($"[GridContainerTest] InventoryService 获取成功，当前状态: {_inventoryService.State}");

                    // 如果未初始化，则进行初始化
                    if (_inventoryService.State == EasyPack.ENekoFramework.ServiceLifecycleState.Uninitialized)
                    {
                        Debug.Log("[GridContainerTest] InventoryService 未初始化，开始初始化...");
                        await _inventoryService.InitializeAsync();
                        Debug.Log("[GridContainerTest] InventoryService 初始化完成");
                    }

                    // 获取序列化服务
                    Debug.Log("[GridContainerTest] 解析序列化服务...");
                    _serializationService = await architecture.ResolveAsync<ISerializationService>();

                    if (_serializationService == null)
                    {
                        Debug.LogError("[GridContainerTest] 序列化服务解析失败");
                        Assert.Fail("序列化服务解析失败");
                        return;
                    }

                    Debug.Log("[GridContainerTest] 初始化成功");
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[GridContainerTest] 初始化失败: {ex.Message}\n{ex.StackTrace}");
                    throw;
                }
            }).GetAwaiter().GetResult();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            // 清理
        }

        [SetUp]
        public void Setup()
        {
            // 初始化设置
        }

        [TearDown]
        public void TearDown()
        {
            // 清理工作
        }

        [Test]
        public void Test_RunAllGridTests()
        {
            Test_CreateGridContainer();
            Test_AddGridItem();
            Test_AddGridItemAt();
            Test_AddMultipleGridItems();
            Test_RemoveGridItem();
            Test_GridItemRotation();
            Test_GridBoundaryCheck();
            Test_GridItemOverlap();
            Test_MixedItemTypes();
            Test_InventoryManagerIntegration();
            Test_GridContainerSerialization();
            Test_GridItemSerialization();
            Test_Shape_LShape();
            Test_Shape_BoundaryCheck();
            Test_GridItem_Stacking();
            Test_GridItem_StackOverflow();
            Test_GridItem_NonStackable();
            Test_GridItem_MetadataMismatch();
        }
        

        /// <summary>
        /// 测试：创建网格容器
        /// </summary>
        [Test]
        public void Test_CreateGridContainer()
        {
            var grid = new GridContainer("test_grid", "Test Grid", "Grid", 5, 4);

            Assert.AreEqual(5, grid.GridWidth, "Grid width should be 5");
            Assert.AreEqual(4, grid.GridHeight, "Grid height should be 4");
            Assert.AreEqual(20, grid.Capacity, "Grid capacity should be 20 (5*4)");
            Assert.AreEqual(20, grid.FreeSlots, "All slots should be empty");
        }

        /// <summary>
        /// 测试：添加网格物品（自动放置）
        /// </summary>
        [Test]
        public void Test_AddGridItem()
        {
            var grid = new GridContainer("test_grid", "Test Grid", "Grid", 5, 4);
            var sword = new GridItem
            {
                ID = "big_sword",
                Name = "大剑",
                Type = "Weapon",
                Shape = GridItem.CreateRectangleShape(2, 3),
                Count = 1
            };

            var (result, count) = grid.AddItems(sword);

            Assert.AreEqual(AddItemResult.Success, result, $"Should add successfully, got: {result}");
            Assert.AreEqual(1, count, "Should add 1 item");
            Assert.AreEqual(1, grid.GetItemTotalCount("big_sword"), "Should have 1 sword");
            Assert.AreEqual(20 - 6, grid.FreeSlots, "Should occupy 6 slots (2*3)");

            // 验证物品在左上角
            var itemAt00 = grid.GetItemAt(0, 0);
            Assert.IsNotNull(itemAt00, "Item should be at (0,0)");
            Assert.AreEqual("big_sword", itemAt00.ID, "Item should be at (0,0)");
        }

        /// <summary>
        /// 测试：在指定位置添加网格物品
        /// </summary>
        [Test]
        public void Test_AddGridItemAt()
        {
            var grid = new GridContainer("test_grid", "Test Grid", "Grid", 5, 4);
            var shield = new GridItem
            {
                ID = "shield",
                Name = "盾牌",
                Type = "Armor",
                Shape = GridItem.CreateRectangleShape(2, 2),
                Count = 1
            };

            // 在 (3, 1) 位置放置
            var (result, count) = grid.AddItemAt(shield, 3, 1);

            Assert.AreEqual(AddItemResult.Success, result, $"Should add successfully, got: {result}");
            Assert.AreEqual(1, count, "Should add 1 item");

            var itemAt31 = grid.GetItemAt(3, 1);
            Assert.IsNotNull(itemAt31, "Item should be at (3,1)");
            Assert.AreEqual("shield", itemAt31.ID, "Item should be at (3,1)");

            // 验证占用区域
            var itemAt41 = grid.GetItemAt(4, 1);
            var itemAt32 = grid.GetItemAt(3, 2);
            var itemAt42 = grid.GetItemAt(4, 2);
            Assert.IsNotNull(itemAt41, "Should occupy (4,1)");
            Assert.AreEqual("shield", itemAt41.ID, "Should occupy (4,1)");
            Assert.IsNotNull(itemAt32, "Should occupy (3,2)");
            Assert.AreEqual("shield", itemAt32.ID, "Should occupy (3,2)");
            Assert.IsNotNull(itemAt42, "Should occupy (4,2)");
            Assert.AreEqual("shield", itemAt42.ID, "Should occupy (4,2)");
        }

        /// <summary>
        /// 测试：添加多个网格物品
        /// </summary>
        [Test]
        public void Test_AddMultipleGridItems()
        {
            var grid = new GridContainer("test_grid", "Test Grid", "Grid", 5, 5);

            // 添加第一个物品
            var sword = new GridItem { ID = "sword", Name = "剑", Shape = GridItem.CreateRectangleShape(1, 3), Count = 1 };
            grid.AddItems(sword);

            // 添加第二个物品
            var axe = new GridItem { ID = "axe", Name = "斧", Shape = GridItem.CreateRectangleShape(2, 2), Count = 1 };
            var (result2, count2) = grid.AddItems(axe);

            Assert.AreEqual(AddItemResult.Success, result2, "Should add second item");
            Assert.AreEqual(1, grid.GetItemTotalCount("sword"), "Should have 1 sword");
            Assert.AreEqual(1, grid.GetItemTotalCount("axe"), "Should have 1 axe");
            Assert.AreEqual(25 - 3 - 4, grid.FreeSlots, "Should occupy 7 slots total");
        }

        /// <summary>
        /// 测试：移除网格物品
        /// </summary>
        [Test]
        public void Test_RemoveGridItem()
        {
            var grid = new GridContainer("test_grid", "Test Grid", "Grid", 5, 4);
            var hammer = new GridItem
            {
                ID = "hammer",
                Name = "锤子",
                Shape = GridItem.CreateRectangleShape(2, 2),
                Count = 1
            };

            grid.AddItemAt(hammer, 1, 1);

            // 移除物品
            var result = grid.RemoveItem("hammer", 1);

            Assert.AreEqual(RemoveItemResult.Success, result, "Should remove successfully");
            Assert.AreEqual(0, grid.GetItemTotalCount("hammer"), "Should have no hammer");
            Assert.AreEqual(20, grid.FreeSlots, "All slots should be empty again");

            // 验证区域已清空
            Assert.IsNull(grid.GetItemAt(1, 1), "(1,1) should be empty");
            Assert.IsNull(grid.GetItemAt(2, 1), "(2,1) should be empty");
            Assert.IsNull(grid.GetItemAt(1, 2), "(1,2) should be empty");
            Assert.IsNull(grid.GetItemAt(2, 2), "(2,2) should be empty");
        }

        /// <summary>
        /// 测试：网格物品旋转
        /// </summary>
        [Test]
        public void Test_GridItemRotation()
        {
            var grid = new GridContainer("test_grid", "Test Grid", "Grid", 5, 5);
            var spear = new GridItem
            {
                ID = "spear",
                Name = "长矛",
                Shape = GridItem.CreateRectangleShape(1, 3),
                CanRotate = true,
                Count = 1
            };

            grid.AddItemAt(spear, 0, 0);

            Assert.AreEqual(RotationAngle.Rotate0, spear.Rotation, "Should start at 0°");
            Assert.AreEqual(1, spear.ActualWidth, "Should be 1x3 initially");
            Assert.AreEqual(3, spear.ActualHeight, "Should be 1x3 initially");

            // 旋转到90度
            bool rotated1 = grid.TryRotateItemAt(0, 0);
            Assert.IsTrue(rotated1, "Should rotate to 90°");
            Assert.AreEqual(RotationAngle.Rotate90, spear.Rotation, "Should be at 90°");
            Assert.AreEqual(3, spear.ActualWidth, "Should be 3x1 at 90°");
            Assert.AreEqual(1, spear.ActualHeight, "Should be 3x1 at 90°");

            // 旋转到180度
            bool rotated2 = grid.TryRotateItemAt(0, 0);
            Assert.IsTrue(rotated2, "Should rotate to 180°");
            Assert.AreEqual(RotationAngle.Rotate180, spear.Rotation, "Should be at 180°");
            Assert.AreEqual(1, spear.ActualWidth, "Should be 1x3 at 180°");
            Assert.AreEqual(3, spear.ActualHeight, "Should be 1x3 at 180°");

            // 旋转到270度
            bool rotated3 = grid.TryRotateItemAt(0, 0);
            Assert.IsTrue(rotated3, "Should rotate to 270°");
            Assert.AreEqual(RotationAngle.Rotate270, spear.Rotation, "Should be at 270°");
            Assert.AreEqual(3, spear.ActualWidth, "Should be 3x1 at 270°");
            Assert.AreEqual(1, spear.ActualHeight, "Should be 3x1 at 270°");

            // 旋转回0度
            bool rotated4 = grid.TryRotateItemAt(0, 0);
            Assert.IsTrue(rotated4, "Should rotate back to 0°");
            Assert.AreEqual(RotationAngle.Rotate0, spear.Rotation, "Should be back at 0°");
            Assert.AreEqual(1, spear.ActualWidth, "Should be 1x3 at 0°");
            Assert.AreEqual(3, spear.ActualHeight, "Should be 1x3 at 0°");
        }

        /// <summary>
        /// 测试：网格边界检查
        /// </summary>
        [Test]
        public void Test_GridBoundaryCheck()
        {
            var grid = new GridContainer("test_grid", "Test Grid", "Grid", 5, 4);
            var bigItem = new GridItem
            {
                ID = "big_item",
                Name = "大物品",
                Shape = GridItem.CreateRectangleShape(3, 3),
                Count = 1
            };

            // 尝试在超出边界的位置放置
            var (result1, count1) = grid.AddItemAt(bigItem, 3, 2); // 会超出右下边界
            Assert.AreNotEqual(AddItemResult.Success, result1, "Should fail due to boundary");

            var (result2, count2) = grid.AddItemAt(bigItem, -1, 0); // 负坐标
            Assert.AreNotEqual(AddItemResult.Success, result2, "Should fail due to negative coordinate");

            var (result3, count3) = grid.AddItemAt(bigItem, 0, 0); // 正确位置
            Assert.AreEqual(AddItemResult.Success, result3, "Should succeed within boundary");
        }

        /// <summary>
        /// 测试：网格物品重叠检测
        /// </summary>
        [Test]
        public void Test_GridItemOverlap()
        {
            var grid = new GridContainer("test_grid", "Test Grid", "Grid", 5, 5);

            var item1 = new GridItem { ID = "item1", Name = "物品1", Shape = GridItem.CreateRectangleShape(2, 2), Count = 1 };
            var item2 = new GridItem { ID = "item2", Name = "物品2", Shape = GridItem.CreateRectangleShape(2, 2), Count = 1 };

            grid.AddItemAt(item1, 1, 1);

            // 尝试在重叠位置放置第二个物品
            var (result, count) = grid.AddItemAt(item2, 2, 2); // 会与item1重叠
            Assert.AreNotEqual(AddItemResult.Success, result, "Should fail due to overlap");

            // 在不重叠位置放置
            var (result2, count2) = grid.AddItemAt(item2, 3, 1);
            Assert.AreEqual(AddItemResult.Success, result2, "Should succeed without overlap");
        }

        /// <summary>
        /// 测试：混合物品类型（普通物品 + 网格物品）
        /// </summary>
        [Test]
        public void Test_MixedItemTypes()
        {
            var grid = new GridContainer("test_grid", "Test Grid", "Grid", 5, 5);

            // 添加普通物品（占1格）
            var potion = new Item
            {
                ID = "potion",
                Name = "药水",
                IsStackable = true,
                Count = 3
            };
            grid.AddItems(potion); // 添加3个药水

            // 添加网格物品
            var armor = new GridItem
            {
                ID = "armor",
                Name = "盔甲",
                Shape = GridItem.CreateRectangleShape(2, 2),
                Count = 1
            };
            var (result, count) = grid.AddItems(armor);

            Assert.AreEqual(AddItemResult.Success, result, "Should add grid item with normal items");
            Assert.AreEqual(3, grid.GetItemTotalCount("potion"), "Should have 3 potions");
            Assert.AreEqual(1, grid.GetItemTotalCount("armor"), "Should have 1 armor");
        }

        /// <summary>
        /// 测试：与 InventoryManager 的集成
        /// </summary>
        [Test]
        public void Test_InventoryManagerIntegration()
        {
            // 使用已初始化的 InventoryService
            var manager = _inventoryService;
            Assert.IsNotNull(manager, "InventoryService 应该在 OneTimeSetUp 中初始化");

            // 记录测试前的容器数量
            int containerCountBefore = manager.GetAllContainers().Count;

            // 创建网格容器并注册
            var gridBag = new GridContainer("grid_bag_integration", "网格背包", "Grid", 4, 4);
            manager.RegisterContainer(gridBag);

            // 创建线性容器并注册
            var linearBag = new LinerContainer("linear_bag_integration", "线性背包", "Linear", 10);
            manager.RegisterContainer(linearBag);

            // 在网格容器中添加物品
            var weapon = new GridItem { ID = "weapon", Name = "武器", Shape = GridItem.CreateRectangleShape(2, 3), Count = 1 };
            gridBag.AddItems(weapon);

            // 验证 InventoryManager 可以找到容器
            var foundContainer = manager.GetContainer("grid_bag_integration");
            Assert.IsNotNull(foundContainer, "Should find grid container");
            Assert.AreSame(gridBag, foundContainer, "Should be the same container");

            // 验证可以查询物品 - 应该增加了2个容器
            var allContainers = manager.GetAllContainers();
            Assert.AreEqual(containerCountBefore + 2, allContainers.Count, "Should have added 2 containers");
            
            // 清理：注销测试中注册的容器
            manager.UnregisterContainer(gridBag.ID);
            manager.UnregisterContainer(linearBag.ID);
        }

        /// <summary>
        /// 测试：GridContainer 序列化
        /// </summary>
        [Test]
        public void Test_GridContainerSerialization()
        {
            // 使用 OneTimeSetUp 中初始化的序列化服务
            Assert.IsNotNull(_serializationService, "序列化服务应该在 OneTimeSetUp 中初始化");

            // 创建网格容器并添加物品
            var original = new GridContainer("test_grid_ser", "序列化测试", "Grid", 4, 4);
            var sword = new GridItem
            {
                ID = "serialize_sword",
                Name = "可序列化的剑",
                Type = "Weapon",
                Shape = GridItem.CreateRectangleShape(2, 3),
                Count = 1
            };
            var potion = new GridItem
            {
                ID = "serialize_potion",
                Name = "药水",
                Type = "Consumable",
                Shape = GridItem.CreateRectangleShape(1, 1),
                Count = 1
            };

            original.AddItems(sword);
            original.AddItemAt(potion, 3, 0);

            // 序列化
            string json = _serializationService.SerializeToJson(original, typeof(GridContainer));
            Assert.IsNotNull(json, "Serialization should produce non-empty JSON");
            Assert.IsNotEmpty(json, "Serialization should produce non-empty JSON");

            // 反序列化
            var deserialized = _serializationService.DeserializeFromJson(json, typeof(GridContainer)) as GridContainer;
            Assert.IsNotNull(deserialized, "Should deserialize successfully");
            Assert.AreEqual(original.ID, deserialized.ID, "ID should match");
            Assert.AreEqual(1, deserialized.GetItemTotalCount("serialize_sword"), "Should have 1 sword");
            Assert.AreEqual(1, deserialized.GetItemTotalCount("serialize_potion"), "Should have 1 potion");

            // 验证物品位置
            var deserializedSword = deserialized.GetItemAt(0, 0);
            Assert.IsNotNull(deserializedSword, "Sword should be at (0,0)");
            Assert.AreEqual("serialize_sword", deserializedSword.ID, "Sword should be at (0,0)");

            var deserializedPotion = deserialized.GetItemAt(3, 0);
            Assert.IsNotNull(deserializedPotion, "Potion should be at (3,0)");
            Assert.AreEqual("serialize_potion", deserializedPotion.ID, "Potion should be at (3,0)");
        }

        /// <summary>
        /// 测试：GridItem 序列化
        /// </summary>
        [Test]
        public void Test_GridItemSerialization()
        {
            // 使用 OneTimeSetUp 中初始化的序列化服务
            Assert.IsNotNull(_serializationService, "序列化服务应该在 OneTimeSetUp 中初始化");

            // 创建GridItem
            var original = new GridItem
            {
                ID = "test_grid_item",
                Name = "测试网格物品",
                Type = "TestType",
                Description = "这是一个测试物品",
                Shape = GridItem.CreateRectangleShape(2, 3),
                CanRotate = true,
                Rotation = RotationAngle.Rotate0,
                Weight = 5.5f
            };

            // 添加自定义属性
            original.SetCustomData("Attack", 100);
            original.SetCustomData("Durability", 50);

            // 序列化
            string json = _serializationService.SerializeToJson(original, typeof(GridItem));
            Assert.IsNotNull(json, "Serialization should produce non-empty JSON");
            Assert.IsNotEmpty(json, "Serialization should produce non-empty JSON");

            // 反序列化
            var deserialized = _serializationService.DeserializeFromJson(json, typeof(GridItem)) as GridItem;
            Assert.IsNotNull(deserialized, "Should deserialize successfully");
            Assert.AreEqual(original.ID, deserialized.ID, "ID should match");
            Assert.AreEqual(original.CanRotate, deserialized.CanRotate, "CanRotate should match");
            Assert.AreEqual(original.Rotation, deserialized.Rotation, "Rotation should match");
            Assert.AreEqual(original.Weight, deserialized.Weight, "Weight should match");

            // 验证自定义属性
            Assert.AreEqual(100, deserialized.GetCustomData<int>("Attack", 0), "Attack attribute should be 100");
            Assert.AreEqual(50, deserialized.GetCustomData<int>("Durability", 0), "Durability attribute should be 50");
        }

        #region 任意形状GridItem测试

        /// <summary>
        /// 测试：L形状物品（任意形状支持）
        /// </summary>
        [Test]
        public void Test_Shape_LShape()
        {
            var grid = new GridContainer("test_grid", "Test Grid", "Grid", 5, 5);

            // 创建L形状物品：
            //  ##
            //  #
            //  #
            var lShapeItem = new GridItem
            {
                ID = "l_shape",
                Name = "L形物品",
                Type = "Tool",
                Shape = new System.Collections.Generic.List<(int x, int y)>
                {
                    (0, 0), (1, 0),  // 上横
                    (0, 1),          // 中
                    (0, 2)           // 下
                },
                Count = 1
            };

            var (result, count) = grid.AddItems(lShapeItem);

            Assert.AreEqual(AddItemResult.Success, result, "Should add L-shape successfully");
            Assert.AreEqual(1, count, "Should add 1 item");
            Assert.AreEqual(25 - 4, grid.FreeSlots, "Should occupy 4 cells");

            // 验证形状占用
            Assert.IsNotNull(grid.GetItemAt(0, 0), "Should occupy (0,0)");
            Assert.IsNotNull(grid.GetItemAt(1, 0), "Should occupy (1,0)");
            Assert.IsNotNull(grid.GetItemAt(0, 1), "Should occupy (0,1)");
            Assert.IsNotNull(grid.GetItemAt(0, 2), "Should occupy (0,2)");
            Assert.IsNull(grid.GetItemAt(1, 1), "Should not occupy (1,1)");
            Assert.IsNull(grid.GetItemAt(1, 2), "Should not occupy (1,2)");
        }

        /// <summary>
        /// 测试：移除任意形状物品并验证缓存更新
        /// </summary>
        [Test]
        public void Test_Shape_RemoveAndCacheUpdate()
        {
            var grid = new GridContainer("test_grid", "Test Grid", "Grid", 5, 5);

            // 创建十字形状物品：
            //   #
            //  ###
            //   #
            var crossItem = new GridItem
            {
                ID = "cross",
                Name = "十字物品",
                Type = "Tool",
                Shape = new System.Collections.Generic.List<(int x, int y)>
                {
                    (1, 0),                    // 上
                    (0, 1), (1, 1), (2, 1),   // 中横
                    (1, 2)                     // 下
                },
                Count = 1
            };

            grid.AddItems(crossItem);
            Assert.AreEqual(1, grid.GetItemTotalCount("cross"), "Should have cross item");
            Assert.AreEqual(25 - 5, grid.FreeSlots, "Should occupy 5 cells");

            // 移除物品
            var removeResult = grid.RemoveItem("cross");
            Assert.AreEqual(RemoveItemResult.Success, removeResult, "Should remove successfully");

            // 验证缓存已更新
            Assert.AreEqual(0, grid.GetItemTotalCount("cross"), "Should have no cross item");
            Assert.IsFalse(grid.HasItem("cross"), "Should not have cross item");
            Assert.AreEqual(25, grid.FreeSlots, "All slots should be free");

            // 验证所有槽位已清空
            for (int y = 0; y < 3; y++)
            {
                for (int x = 0; x < 3; x++)
                {
                    Assert.IsNull(grid.GetItemAt(x, y), $"Slot ({x},{y}) should be empty");
                }
            }
        }

        /// <summary>
        /// 测试：复杂形状边界检测
        /// </summary>
        [Test]
        public void Test_Shape_BoundaryCheck()
        {
            var grid = new GridContainer("test_grid", "Test Grid", "Grid", 3, 3);

            // 创建超出边界的L形物品
            var largeL = new GridItem
            {
                ID = "large_l",
                Name = "大L",
                Type = "Tool",
                Shape = new System.Collections.Generic.List<(int x, int y)>
                {
                    (0, 0), (1, 0), (2, 0),
                    (0, 1),
                    (0, 2)
                },
                Count = 1
            };

            // 应该成功添加到(0,0)
            var (result1, _) = grid.AddItemAt(largeL, 0, 0);
            Assert.AreEqual(AddItemResult.Success, result1, "Should add at (0,0)");

            // 清除
            grid.RemoveItem("large_l");

            // 尝试添加到(1,0) - 应该失败因为超出右边界
            var (result2, _) = grid.AddItemAt(largeL, 1, 0);
            Assert.AreNotEqual(AddItemResult.Success, result2, "Should fail at (1,0) due to boundary");

            // 尝试添加到(0,1) - 应该失败因为超出下边界
            var (result3, _) = grid.AddItemAt(largeL, 0, 1);
            Assert.AreNotEqual(AddItemResult.Success, result3, "Should fail at (0,1) due to boundary");
        }
        /// <summary>
        /// 测试：可堆叠网格物品的堆叠
        /// </summary>
        [Test]
        public void Test_GridItem_Stacking()
        {
            var grid = new GridContainer("test_grid", "Test Grid", "Grid", 5, 5);

            // 创建可堆叠的网格物品
            var stackableItem1 = new GridItem
            {
                ID = "ammo",
                Name = "Ammo",
                Type = "Consumable",
                IsStackable = true,
                MaxStackCount = 100,
                Shape = new System.Collections.Generic.List<(int x, int y)> { (0, 0) },
                Count = 30
            };

            // 第一次添加30个
            var (result1, count1) = grid.AddItems(stackableItem1);
            Assert.AreEqual(AddItemResult.Success, result1, "Should add first stack");
            Assert.AreEqual(30, count1, "Should add 30 items");
            Assert.AreEqual(30, grid.GetItemTotalCount("ammo"), "Total count should be 30");

            // 再添加40个到同一槽位（堆叠）
            var stackableItem2 = new GridItem
            {
                ID = "ammo",
                Name = "Ammo",
                Type = "Consumable",
                IsStackable = true,
                MaxStackCount = 100,
                Shape = new System.Collections.Generic.List<(int x, int y)> { (0, 0) },
                Count = 40
            };

            var (result2, count2) = grid.AddItems(stackableItem2);
            Assert.AreEqual(AddItemResult.Success, result2, "Should stack items");
            Assert.AreEqual(40, count2, "Should add 40 items");
            Assert.AreEqual(70, grid.GetItemTotalCount("ammo"), "Total count should be 70");
            Assert.AreEqual(1, grid.UsedSlots, "Should use only 1 slot");
        }

        /// <summary>
        /// 测试：网格物品堆叠溢出
        /// </summary>
        [Test]
        public void Test_GridItem_StackOverflow()
        {
            var grid = new GridContainer("test_grid", "Test Grid", "Grid", 5, 5);

            // 添加第一堆（80个）
            var item1 = new GridItem
            {
                ID = "arrows",
                Name = "Arrows",
                Type = "Ammo",
                IsStackable = true,
                MaxStackCount = 100,
                Shape = new System.Collections.Generic.List<(int x, int y)> { (0, 0) },
                Count = 80
            };

            var (result1, count1) = grid.AddItems(item1);
            Assert.AreEqual(AddItemResult.Success, result1, "Should add first stack");
            Assert.AreEqual(80, count1, "Should add 80 items");

            // 再添加50个（会溢出：20+30）
            var item2 = new GridItem
            {
                ID = "arrows",
                Name = "Arrows",
                Type = "Ammo",
                IsStackable = true,
                MaxStackCount = 100,
                Shape = new System.Collections.Generic.List<(int x, int y)> { (0, 0) },
                Count = 50
            };

            var (result2, count2) = grid.AddItems(item2);
            Assert.AreEqual(AddItemResult.Success, result2, "Should handle overflow");
            Assert.AreEqual(50, count2, "Should add all 50 items");
            Assert.AreEqual(130, grid.GetItemTotalCount("arrows"), "Total count should be 130");
            Assert.AreEqual(2, grid.UsedSlots, "Should use 2 slots due to overflow");
        }

        /// <summary>
        /// 测试：不可堆叠网格物品
        /// </summary>
        [Test]
        public void Test_GridItem_NonStackable()
        {
            var grid = new GridContainer("test_grid", "Test Grid", "Grid", 5, 5);

            // 添加第一个不可堆叠物品
            var item1 = new GridItem
            {
                ID = "sword",
                Name = "Sword",
                Type = "Weapon",
                IsStackable = false,
                Shape = new System.Collections.Generic.List<(int x, int y)> { (0, 0), (1, 0) },
                Count = 1
            };

            var (result1, count1) = grid.AddItems(item1);
            Assert.AreEqual(AddItemResult.Success, result1, "Should add first sword");
            Assert.AreEqual(1, count1, "Should add 1 item");

            // 尝试再添加一个相同的不可堆叠物品（应该占用新槽位）
            var item2 = new GridItem
            {
                ID = "sword",
                Name = "Sword",
                Type = "Weapon",
                IsStackable = false,
                Shape = new System.Collections.Generic.List<(int x, int y)> { (0, 0), (1, 0) },
                Count = 1
            };

            var (result2, count2) = grid.AddItems(item2);
            Assert.AreEqual(AddItemResult.Success, result2, "Should add second sword to new slot");
            Assert.AreEqual(1, count2, "Should add 1 item");
            Assert.AreEqual(2, grid.GetItemTotalCount("sword"), "Total count should be 2");
            // 每把剑占用2个格子 (Shape = {(0,0), (1,0)}), 所以2把剑占用4个槽位
            Assert.AreEqual(4, grid.UsedSlots, "Should use 4 slots (2 swords × 2 cells each)");
        }

        /// <summary>
        /// 测试：元数据不匹配阻止堆叠
        /// 注意：正确的物品创建方式是通过 ItemFactory.CreateItem，但 GridItem 是特殊类型，
        /// 需要手动设置 InventoryService、分配 UID 并注册到 CategoryManager
        /// </summary>
        [Test]
        public void Test_GridItem_MetadataMismatch()
        {
            var grid = new GridContainer("test_grid_metadata", "Test Grid", "Grid", 5, 5);
            _inventoryService.RegisterContainer(grid);

            // 添加第一个带元数据的可堆叠物品
            var item1 = new GridItem
            {
                ID = "potion",
                Name = "Health Potion",
                Type = "Consumable",
                IsStackable = true,
                MaxStackCount = 50,
                Shape = new System.Collections.Generic.List<(int x, int y)> { (0, 0) },
                Count = 10
            };
            
            // GridItem 需要手动完成完整的注册流程：
            // 1. 设置 InventoryService
            // 2. 分配 UID
            // 3. 注册到 CategoryManager（使用 Type 作为 Category）
            item1.InventoryService = _inventoryService;
            _inventoryService.AssignItemUID(item1);
            _inventoryService.CategoryManager.RegisterEntity(item1.ItemUID, item1, item1.Type);
            item1.SetCustomData("Quality", "Common");

            var (result1, count1) = grid.AddItems(item1);
            Assert.AreEqual(AddItemResult.Success, result1, "Should add first potion");
            Assert.AreEqual(10, count1, "Should add 10 items");

            // 尝试添加相同ID但不同元数据的可堆叠物品（应该占用新槽位）
            var item2 = new GridItem
            {
                ID = "potion",
                Name = "Health Potion",
                Type = "Consumable",
                IsStackable = true,
                MaxStackCount = 50,
                Shape = new System.Collections.Generic.List<(int x, int y)> { (0, 0) },
                Count = 10
            };
            
            // 同样完成完整的注册流程
            item2.InventoryService = _inventoryService;
            _inventoryService.AssignItemUID(item2);
            _inventoryService.CategoryManager.RegisterEntity(item2.ItemUID, item2, item2.Type);
            item2.SetCustomData("Quality", "Rare"); // 不同的元数据

            var (result2, count2) = grid.AddItems(item2);
            Assert.AreEqual(AddItemResult.Success, result2, "Should add second potion to new slot");
            Assert.AreEqual(10, count2, "Should add 10 items");
            Assert.AreEqual(20, grid.GetItemTotalCount("potion"), "Total count should be 20");
            Assert.AreEqual(2, grid.UsedSlots, "Should use 2 slots due to metadata mismatch");
            
            // 清理注册的容器
            _inventoryService.UnregisterContainer(grid.ID);
        }

        #endregion
    }
}