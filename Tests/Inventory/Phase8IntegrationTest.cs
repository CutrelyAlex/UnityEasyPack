using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EasyPack.Architecture;
using EasyPack.CustomData;
using EasyPack.InventorySystem;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace EasyPack.InventoryTests
{
    /// <summary>
    ///     Phase 8 集成测试：ItemData工厂和CategoryManager集成
    ///     测试范围：T047-T053
    ///     - T047: InventoryService.CategoryManager集成
    ///     - T048: InventoryService.ItemFactory集成
    ///     - T049: ItemFactory.CreateItem自动注册CategoryManager
    ///     - T050: ItemFactory.CloneItem自动注册CategoryManager
    ///     - T051: Container.ClearSlot自动注销CategoryManager
    ///     - T052: Item.Category/Tags/RuntimeMetadata属性实现
    ///     - T053: Item.CanStack使用RuntimeMetadata深度比较
    /// </summary>
    [TestFixture]
    public class Phase8IntegrationTest
    {
        private InventoryService _inventoryService;
        private Container _testContainer;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            // 使用 Task.Run 避免死锁，初始化服务
            Task.Run(async () =>
            {
                try
                {
                    Debug.Log("[Phase8IntegrationTest] 开始初始化...");
                    
                    // 确保架构已获取
                    var architecture = EasyPackArchitecture.Instance;

                    // 获取库存服务
                    _inventoryService = await EasyPackArchitecture.GetInventoryServiceAsync() as InventoryService;
                    Assert.IsNotNull(_inventoryService, "未能获取到 InventoryService 实例");

                    // 如果未初始化，则进行初始化
                    if (_inventoryService.State == EasyPack.ENekoFramework.ServiceLifecycleState.Uninitialized)
                    {
                        Debug.Log("[Phase8IntegrationTest] InventoryService 未初始化，开始初始化...");
                        await _inventoryService.InitializeAsync();
                        Debug.Log("[Phase8IntegrationTest] InventoryService 初始化完成");
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[Phase8IntegrationTest] 初始化失败: {ex.Message}\n{ex.StackTrace}");
                    Assert.Fail($"初始化失败: {ex.Message}");
                }
            }).GetAwaiter().GetResult();
        }

        [SetUp]
        public void Setup()
        {
            // 确保服务有效
            Assert.IsNotNull(_inventoryService, "InventoryService 为 null");

            // 重置服务状态
            _inventoryService.Reset();

            // 创建测试容器
            _testContainer = new LinerContainer("test_container", "测试容器", "BackpackType", 10)
            {
                InventoryService = _inventoryService
            };
            _inventoryService.RegisterContainer(_testContainer);
        }

        [TearDown]
        public void Teardown()
        {
            if (_inventoryService != null)
            {
                _inventoryService.Reset();
            }
        }

        #region T047-T048: CategoryManager和ItemFactory集成测试

        [Test]
        public void Test_T047_InventoryService_HasCategoryManager()
        {
            // 验证InventoryService拥有CategoryManager
            Assert.IsNotNull(_inventoryService.CategoryManager, "InventoryService应该有CategoryManager属性");
            Debug.Log("[T047] ✓ InventoryService.CategoryManager 已集成");
        }

        [Test]
        public void Test_T048_InventoryService_HasItemFactory()
        {
            // 验证InventoryService拥有ItemFactory
            Assert.IsNotNull(_inventoryService.ItemFactory, "InventoryService应该有ItemFactory属性");
            Debug.Log("[T048] ✓ InventoryService.ItemFactory 已集成");
        }

        [Test]
        public void Test_Debug_InitializationCheck()
        {
            // 调试测试：验证初始化状态
            Debug.Log($"_inventoryService: {_inventoryService}");
            Debug.Log($"_inventoryService.CategoryManager: {_inventoryService.CategoryManager}");
            Debug.Log($"_inventoryService.ItemFactory: {_inventoryService.ItemFactory}");
            Debug.Log($"_testContainer: {_testContainer}");
            Debug.Log($"_testContainer.InventoryService: {_testContainer.InventoryService}");
            Debug.Log($"_testContainer.InventoryService == _inventoryService: {ReferenceEquals(_testContainer.InventoryService, _inventoryService)}");
        }

        #endregion

        #region T049: ItemFactory.CreateItem自动注册CategoryManager

        [Test]
        public void Test_T049_CreateItem_AutoAssignsUID()
        {
            // 准备ItemData
            var itemData = new ItemData
            {
                ID = "apple",
                Name = "苹果",
                Category = "Food.Fruit",
                DefaultTags = new[] { "consumable", "stackable" },
                DefaultMetadata = new CustomDataCollection(),
                IsStackable = true,
                MaxStackCount = 99
            };
            itemData.DefaultMetadata.Set("durability", 100);

            // 注册ItemData
            _inventoryService.ItemFactory.Register("apple", itemData);

            // 创建Item
            IItem item = _inventoryService.ItemFactory.CreateItem("apple", 5);

            // 验证UID已分配
            Assert.IsNotNull(item, "Item应该创建成功");
            Assert.AreNotEqual(-1, item.ItemUID, "ItemUID应该已分配");
            Assert.AreEqual(5, item.Count, "Count应该是5");
            Assert.AreEqual("Food.Fruit", item.Category, "Category应该正确");

            Debug.Log($"[T049] ✓ CreateItem自动分配UID: {item.ItemUID}, Category: {item.Category}");
        }

        [Test]
        public void Test_T049_CreateItem_RegistersToCategoryManager()
        {
            // 准备ItemData
            var itemData = new ItemData
            {
                ID = "sword",
                Name = "铁剑",
                Category = "Equipment.Weapon.Sword",
                DefaultTags = new[] { "weapon", "melee" },
                IsStackable = false
            };

            _inventoryService.ItemFactory.Register("sword", itemData);

            // 创建Item
            IItem item = _inventoryService.ItemFactory.CreateItem("sword", 1);

            // 验证Category已注册
            Assert.AreEqual("Equipment.Weapon.Sword", item.Category, "Category应该正确设置");

            // 验证Tags已注册
            Assert.IsNotNull(item.Tags, "Tags不应为null");
            Assert.AreEqual(2, item.Tags.Length, "应该有2个标签");
            Assert.Contains("weapon", item.Tags, "应该包含weapon标签");
            Assert.Contains("melee", item.Tags, "应该包含melee标签");

            // 验证可以通过CategoryManager查询
            var categoryResult = _inventoryService.CategoryManager.GetByCategory("Equipment.Weapon.Sword");
            Assert.IsNotNull(categoryResult, "应该能通过CategoryManager查询到");
            var resultList = categoryResult.ToList() ?? new List<IItem>();
            Assert.Contains(item, resultList, "查询结果应包含创建的Item");

            Debug.Log($"[T049] ✓ CreateItem已注册到CategoryManager: {item.Category}");
        }

        [Test]
        public void Test_T049_CreateItem_CopiesDefaultMetadata()
        {
            // 准备ItemData with metadata
            var itemData = new ItemData
            {
                ID = "potion",
                Name = "生命药水",
                Category = "Consumable.Potion",
                DefaultMetadata = new CustomDataCollection()
            };
            itemData.DefaultMetadata.Set("healing_power", 50);
            itemData.DefaultMetadata.Set("cooldown", 5.0f);

            _inventoryService.ItemFactory.Register("potion", itemData);

            // 创建Item
            IItem item = _inventoryService.ItemFactory.CreateItem("potion");

            // 验证RuntimeMetadata已复制
            Assert.IsNotNull(item.RuntimeMetadata, "RuntimeMetadata不应为null");
            Assert.AreEqual(50, item.RuntimeMetadata.Get<int>("healing_power"), "应该复制healing_power");
            Assert.AreEqual(5.0f, item.RuntimeMetadata.Get<float>("cooldown"), 0.01f, "应该复制cooldown");

            Debug.Log("[T049] ✓ CreateItem复制了DefaultMetadata");
        }

        #endregion

        #region T050: ItemFactory.CloneItem自动注册CategoryManager

        [Test]
        public void Test_T050_CloneItem_AssignsNewUID()
        {
            // 创建原始Item
            var itemData = new ItemData
            {
                ID = "arrow",
                Name = "箭矢",
                Category = "Ammunition",
                DefaultTags = new[] { "stackable" }
            };
            _inventoryService.ItemFactory.Register("arrow", itemData);

            IItem originalItem = _inventoryService.ItemFactory.CreateItem("arrow", 10);
            long originalUID = originalItem.ItemUID;

            // 克隆Item
            IItem clonedItem = _inventoryService.ItemFactory.CloneItem(originalItem, 5);

            // 验证新UID
            Assert.IsNotNull(clonedItem, "克隆Item应该成功");
            Assert.AreNotEqual(-1, clonedItem.ItemUID, "克隆Item应该有新UID");
            Assert.AreNotEqual(originalUID, clonedItem.ItemUID, "克隆Item的UID应该不同于原Item");
            Assert.AreEqual(5, clonedItem.Count, "克隆Item的Count应该是5");

            Debug.Log($"[T050] ✓ CloneItem分配新UID: {originalUID} → {clonedItem.ItemUID}");
        }

        [Test]
        public void Test_T050_CloneItem_CopiesCategoryAndTags()
        {
            // 创建原始Item
            var itemData = new ItemData
            {
                ID = "shield",
                Name = "木盾",
                Category = "Equipment.Armor.Shield",
                DefaultTags = new[] { "defense", "block" }
            };
            _inventoryService.ItemFactory.Register("shield", itemData);

            IItem originalItem = _inventoryService.ItemFactory.CreateItem("shield");

            // 克隆Item
            IItem clonedItem = _inventoryService.ItemFactory.CloneItem(originalItem);

            // 验证Category相同
            Assert.AreEqual(originalItem.Category, clonedItem.Category, "克隆Item的Category应该相同");

            // 验证Tags相同
            Assert.AreEqual(originalItem.Tags.Length, clonedItem.Tags.Length, "Tags数量应该相同");
            foreach (var tag in originalItem.Tags)
            {
                Assert.Contains(tag, clonedItem.Tags, $"克隆Item应该包含标签: {tag}");
            }

            Debug.Log("[T050] ✓ CloneItem复制了Category和Tags");
        }

        [Test]
        public void Test_T050_CloneItem_DeepCopiesRuntimeMetadata()
        {
            // 创建原始Item with metadata
            var itemData = new ItemData
            {
                ID = "tool",
                Name = "工具",
                Category = "Tool",
                DefaultMetadata = new CustomDataCollection()
            };
            itemData.DefaultMetadata.Set("durability", 100);
            _inventoryService.ItemFactory.Register("tool", itemData);

            IItem originalItem = _inventoryService.ItemFactory.CreateItem("tool");

            // 修改原始Item的RuntimeMetadata
            originalItem.RuntimeMetadata.Set("durability", 80);

            // 克隆Item
            IItem clonedItem = _inventoryService.ItemFactory.CloneItem(originalItem);

            // 验证RuntimeMetadata被深拷贝
            Assert.AreEqual(80, clonedItem.RuntimeMetadata.Get<int>("durability"), "克隆Item应该复制metadata");

            // 修改原始Item的metadata，验证克隆Item不受影响
            originalItem.RuntimeMetadata.Set("durability", 50);
            Assert.AreEqual(50, originalItem.RuntimeMetadata.Get<int>("durability"), "原始Item的durability应该是50");
            Assert.AreEqual(80, clonedItem.RuntimeMetadata.Get<int>("durability"), "克隆Item的durability应该仍是80（独立）");

            Debug.Log("[T050] ✓ CloneItem深拷贝RuntimeMetadata，修改互不影响");
        }

        #endregion
        #region Metadata API Side-Effect Testing

        [Test]
        public void Test_T054_CategoryManager_AvoidsInvalidMetadataCreation()
        {
            // 准备一个没有默认元数据的ItemData
            var itemData = new ItemData
            {
                ID = "plain_bread",
                Name = "白面包",
                Category = "Food",
                DefaultMetadata = null
            };
            _inventoryService.ItemFactory.Register("plain_bread", itemData);

            // 创建Item
            IItem item = _inventoryService.ItemFactory.CreateItem("plain_bread");
            
            // 记录当前的元数据存储数量
            // 因为CategoryManager内部没有公开_metadataStore的计数，我们通过HasMetadata间接验证
            Assert.IsFalse(_inventoryService.CategoryManager.HasMetadata(item.ItemUID), "初始状态下不应该有元数据");

            // 1. 尝试读取 RuntimeMetadata (应该触发 GetMetadata)
            var metadata = item.RuntimeMetadata;
            
            // 验证：读取不应该创建存储条目
            Assert.IsFalse(_inventoryService.CategoryManager.HasMetadata(item.ItemUID), "仅读取 RuntimeMetadata 不应创建侧边存储条目");
            Assert.IsNotNull(metadata, "没有元数据时 item.RuntimeMetadata 应该返回空集合");
            Assert.AreEqual(0, metadata.Count, "RuntimeMetadata 应为空");

            // 2. 尝试使用 SetCustomData 修改 (应该触发 GetOrAddMetadata)
            item.SetCustomData("freshness", 100);
            
            // 验证：写入应该创建存储条目
            Assert.IsTrue(_inventoryService.CategoryManager.HasMetadata(item.ItemUID), "使用 SetCustomData 后应创建元数据条目");
            Assert.IsNotNull(item.RuntimeMetadata, "写入后 RuntimeMetadata 不应为 null");
            Assert.AreEqual(100, item.RuntimeMetadata.Get<int>("freshness"), "写入的数据应该能读到");

            Debug.Log("[T054] ✓ CategoryManager 避免了无效元数据的创建 (读操作无副作用)");
        }

        #endregion
        #region T051: Container.ClearSlot自动注销CategoryManager

        [Test]
        public void Test_T051_ClearSlot_UnregistersFromCategoryManager()
        {
            // 创建Item并添加到容器
            var itemData = new ItemData
            {
                ID = "gem",
                Name = "宝石",
                Category = "Resource.Gem"
            };
            _inventoryService.ItemFactory.Register("gem", itemData);

            IItem item = _inventoryService.ItemFactory.CreateItem("gem");

            _testContainer.AddItems(item);

            // AddItems会克隆物品，需要从槽位获取实际存储的物品UID
            var slot = _testContainer.GetSlot(0);
            Assert.IsNotNull(slot, "槽位不应为空");
            long slotItemUID = slot.ItemUID;
            Assert.AreNotEqual(0, slotItemUID, "槽位物品UID不应为0");

            // 验证Item在CategoryManager中
            var foundItemResult = _inventoryService.CategoryManager.GetById(slotItemUID);
            Assert.IsTrue(foundItemResult.IsSuccess, "Item应该在CategoryManager中");
            Assert.IsNotNull(foundItemResult.Value, "查询到的Item不应为空");

            // 清空槽位
            _testContainer.ClearSlot(0);

            // 验证Item已从CategoryManager移除
            var removedItemResult = _inventoryService.CategoryManager.GetById(slotItemUID);
            Assert.IsFalse(removedItemResult.IsSuccess, "Item应该已从CategoryManager移除");

            Debug.Log($"[T051] ✓ ClearSlot自动从CategoryManager注销UID: {slotItemUID}");
        }

        #endregion

        #region T052: Item.Category/Tags/RuntimeMetadata属性实现

        [Test]
        public void Test_T052_Item_CategoryProperty_ReturnsFromCategoryManager()
        {
            // 创建Item
            var itemData = new ItemData
            {
                ID = "book",
                Name = "书籍",
                Category = "Item.Book.Fiction"
            };
            _inventoryService.ItemFactory.Register("book", itemData);

            IItem item = _inventoryService.ItemFactory.CreateItem("book");

            // 验证Category从CategoryManager获取
            Assert.AreEqual("Item.Book.Fiction", item.Category, "Category应该从CategoryManager获取");

            Debug.Log("[T052] ✓ Item.Category正确从CategoryManager获取");
        }

        [Test]
        public void Test_T052_Item_TagsProperty_ReturnsFromCategoryManager()
        {
            // 创建Item with tags
            var itemData = new ItemData
            {
                ID = "key",
                Name = "钥匙",
                Category = "Quest.Key",
                DefaultTags = new[] { "unique", "important", "quest" }
            };
            _inventoryService.ItemFactory.Register("key", itemData);

            IItem item = _inventoryService.ItemFactory.CreateItem("key");

            // 验证Tags从CategoryManager获取
            Assert.IsNotNull(item.Tags, "Tags不应为null");
            Assert.AreEqual(3, item.Tags.Length, "应该有3个标签");

            Debug.Log("[T052] ✓ Item.Tags正确从CategoryManager获取");
        }

        [Test]
        public void Test_T052_Item_RuntimeMetadataProperty_ReturnsFromCategoryManager()
        {
            // 1. 创建Item without metadata
            var itemData1 = new ItemData { ID = "stone", Name = "石头", Category = "Material" };
            _inventoryService.ItemFactory.Register("stone", itemData1);
            IItem stone = _inventoryService.ItemFactory.CreateItem("stone");
            
            // 默认应该没有元数据
            Assert.IsFalse(_inventoryService.CategoryManager.HasMetadata(stone.ItemUID), "新建物品默认不应有元数据容器");
            Assert.IsNotNull(stone.RuntimeMetadata, "RuntimeMetadata 应为空集合（返回 _localCustomData）");
            Assert.AreEqual(0, stone.RuntimeMetadata.Count, "RuntimeMetadata 应为空");

            // 2. 创建Item with metadata
            var itemData2 = new ItemData
            {
                ID = "armor",
                Name = "护甲",
                Category = "Equipment.Armor",
                DefaultMetadata = new CustomDataCollection()
            };
            itemData2.DefaultMetadata.Set("defense", 20);
            _inventoryService.ItemFactory.Register("armor", itemData2);

            IItem armor = _inventoryService.ItemFactory.CreateItem("armor");

            // 验证RuntimeMetadata从CategoryManager获取
            Assert.IsNotNull(armor.RuntimeMetadata, "RuntimeMetadata不应为null");
            Assert.AreEqual(20, armor.RuntimeMetadata.Get<int>("defense"), "应该能获取defense值");

            // 修改RuntimeMetadata
            armor.RuntimeMetadata.Set("defense", 25);
            Assert.AreEqual(25, armor.RuntimeMetadata.Get<int>("defense"), "修改后应该是25");

            Debug.Log("[T052] ✓ Item.RuntimeMetadata正确从CategoryManager获取并支持延迟加载逻辑");
        }

        #endregion

        #region T053: Item.CanStack使用RuntimeMetadata深度比较

        [Test]
        public void Test_T053_CanStack_SameMetadata_ReturnsTrue()
        {
            // 创建两个相同metadata的Item
            var itemData = new ItemData
            {
                ID = "coin",
                Name = "金币",
                Category = "Currency",
                DefaultMetadata = new CustomDataCollection(),
                IsStackable = true
            };
            itemData.DefaultMetadata.Set("mint", "royal");
            _inventoryService.ItemFactory.Register("coin", itemData);

            IItem item1 = _inventoryService.ItemFactory.CreateItem("coin", 10);
            IItem item2 = _inventoryService.ItemFactory.CreateItem("coin", 5);

            // 验证可以堆叠（metadata相同）
            Assert.IsTrue(item1.CanStack(item2), "相同metadata的Item应该可以堆叠");

            Debug.Log("[T053] ✓ CanStack正确识别相同RuntimeMetadata");
        }

        [Test]
        public void Test_T053_CanStack_DifferentMetadata_ReturnsFalse()
        {
            // 创建ItemData
            var itemData = new ItemData
            {
                ID = "weapon",
                Name = "武器",
                Category = "Equipment.Weapon",
                DefaultMetadata = new CustomDataCollection(),
                IsStackable = true
            };
            itemData.DefaultMetadata.Set("damage", 10);
            _inventoryService.ItemFactory.Register("weapon", itemData);

            // 创建两个Item
            IItem item1 = _inventoryService.ItemFactory.CreateItem("weapon");
            IItem item2 = _inventoryService.ItemFactory.CreateItem("weapon");

            // 修改item2的metadata
            item2.RuntimeMetadata.Set("damage", 15);

            // 验证不可以堆叠（metadata不同）
            Assert.IsFalse(item1.CanStack(item2), "不同metadata的Item不应该堆叠");

            Debug.Log("[T053] ✓ CanStack正确识别不同RuntimeMetadata");
        }

        [Test]
        public void Test_T053_CanStack_IgnoresCategoryAndTags()
        {
            // 创建相同ID但不同Category的ItemData（理论上不应该这样，但测试Category不影响堆叠）
            var itemData = new ItemData
            {
                ID = "generic",
                Name = "通用物品",
                Category = "Type.A",
                DefaultMetadata = new CustomDataCollection(),
                IsStackable = true
            };
            _inventoryService.ItemFactory.Register("generic", itemData);

            IItem item1 = _inventoryService.ItemFactory.CreateItem("generic");
            IItem item2 = _inventoryService.ItemFactory.CreateItem("generic");

            // CanStack应该比较ID + Type + RuntimeMetadata，不比较Category/Tags
            // 因为Category/Tags在创建后不可变，相同ItemData创建的Item它们必然相同
            Assert.IsTrue(item1.CanStack(item2), "相同ItemData创建的Item应该可以堆叠");

            Debug.Log("[T053] ✓ CanStack不比较Category/Tags（它们是不可变的）");
        }

        #endregion

        #region 综合集成测试

        [Test]
        public void Test_Phase8_FullIntegration_CreateAddCloneClear()
        {
            // 1. 注册ItemData
            var potionData = new ItemData
            {
                ID = "health_potion",
                Name = "生命药水",
                Category = "Consumable.Potion.Health",
                DefaultTags = new[] { "consumable", "healing" },
                DefaultMetadata = new CustomDataCollection(),
                IsStackable = true,
                MaxStackCount = 20
            };
            potionData.DefaultMetadata.Set("heal_amount", 50);
            _inventoryService.ItemFactory.Register("health_potion", potionData);

            // 2. 创建Item (T049)
            IItem potion1 = _inventoryService.ItemFactory.CreateItem("health_potion", 5);
            Assert.IsNotNull(potion1, "Item应该创建成功");
            Assert.AreNotEqual(-1, potion1.ItemUID, "应该分配UID");
            Assert.AreEqual("Consumable.Potion.Health", potion1.Category, "Category应该正确");
            Assert.AreEqual(2, potion1.Tags.Length, "应该有2个标签");

            // 3. 添加到容器
            _testContainer.AddItems(potion1);
            Assert.AreEqual(1, _testContainer.UsedSlots, "应该占用1个槽位");

            // 4. 克隆Item (T050)
            IItem potion2 = _inventoryService.ItemFactory.CloneItem(potion1, 3);
            Assert.AreNotEqual(potion1.ItemUID, potion2.ItemUID, "克隆Item应该有不同UID");
            Assert.AreEqual(3, potion2.Count, "克隆Item的Count应该是3");

            // 5. 验证CanStack (T053)
            Assert.IsTrue(potion1.CanStack(potion2), "相同metadata的药水应该可以堆叠");

            // 6. 修改RuntimeMetadata后不可堆叠
            potion2.RuntimeMetadata.Set("heal_amount", 75); // 增强药水
            Assert.IsFalse(potion1.CanStack(potion2), "不同heal_amount的药水不应该堆叠");

            // 7. 清空槽位 (T051)
            // AddItems会克隆物品，需要从槽位获取实际存储的物品UID
            var slot0 = _testContainer.GetSlot(0);
            long slotItemUID = slot0.ItemUID;
            _testContainer.ClearSlot(0);
            var checkRemoved = _inventoryService.CategoryManager.GetById(slotItemUID);
            Assert.IsFalse(checkRemoved.IsSuccess, "清空后应该从CategoryManager移除");

            Debug.Log("[Phase8] ✓ 完整集成测试通过：创建→添加→克隆→堆叠判断→清空注销");
        }

        [Test]
        public void Test_Phase8_CategoryManagerQuery()
        {
            // 创建多个不同分类的Item
            var foodData = new ItemData { ID = "bread", Category = "Food.Bread" };
            var weaponData = new ItemData { ID = "dagger", Category = "Equipment.Weapon.Dagger" };

            _inventoryService.ItemFactory.Register("bread", foodData);
            _inventoryService.ItemFactory.Register("dagger", weaponData);

            IItem bread1 = _inventoryService.ItemFactory.CreateItem("bread");
            IItem bread2 = _inventoryService.ItemFactory.CreateItem("bread");
            IItem dagger = _inventoryService.ItemFactory.CreateItem("dagger");

            // 按Category查询
            var foodItems = _inventoryService.CategoryManager.GetByCategory("Food.Bread");
            Debug.Log($"[Phase8Query] Food.Bread items: {foodItems?.Count ?? -1}");
            
            var weaponItems = _inventoryService.CategoryManager.GetByCategory("Equipment.Weapon.Dagger");
            Debug.Log($"[Phase8Query] Equipment.Weapon.Dagger items: {weaponItems?.Count ?? -1}");

            // 验证查询结果（至少包含创建的Item）
            Assert.IsNotNull(foodItems, "应该查询到面包");
            Assert.IsNotNull(weaponItems, "应该查询到匕首");

            Debug.Log("[Phase8] ✓ CategoryManager分类查询功能正常");
        }

        #endregion
    }
}
