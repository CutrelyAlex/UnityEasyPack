using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using EasyPack.InventorySystem;
using UnityEngine;
using EasyPack.Architecture;

namespace EasyPack.InventoryTests
{
    /// <summary>
    /// 物品系统测试
    /// </summary>
    [TestFixture]
    public class InventoryTest
    {
        private InventoryService _inventoryService;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            // 使用 Task.Run 避免死锁
            Task.Run(async () =>
            {
                try
                {
                    Debug.Log("[InventoryTest] 开始初始化...");

                    var architecture = EasyPackArchitecture.Instance;
                    if (architecture == null)
                    {
                        Debug.LogError("[InventoryTest] EasyPackArchitecture.Instance 为 null");
                        Assert.Fail("EasyPackArchitecture.Instance 为 null");
                        return;
                    }

                    // 获取库存服务
                    Debug.Log("[InventoryTest] 获取 InventoryService...");
                    _inventoryService = await EasyPackArchitecture.GetInventoryServiceAsync() as InventoryService;
                    if (_inventoryService == null)
                    {
                        Debug.LogError("[InventoryTest] InventoryService 为 null");
                        Assert.Fail("InventoryService 为 null");
                        return;
                    }

                    Debug.Log($"[InventoryTest] InventoryService 获取成功，当前状态: {_inventoryService.State}");

                    // 如果未初始化，则进行初始化
                    if (_inventoryService.State == EasyPack.ENekoFramework.ServiceLifecycleState.Uninitialized)
                    {
                        Debug.Log("[InventoryTest] InventoryService 未初始化，开始初始化...");
                        await _inventoryService.InitializeAsync();
                        Debug.Log("[InventoryTest] InventoryService 初始化完成");
                    }

                    Debug.Log("[InventoryTest] 初始化成功");
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[InventoryTest] 初始化失败: {ex.Message}\n{ex.StackTrace}");
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
        /// 创建并初始化一个新的 InventoryService 实例（辅助方法）
        /// </summary>
        private InventoryService CreateInitializedInventoryService()
        {
            // 使用 OneTimeSetUp 中已初始化的服务
            Assert.IsNotNull(_inventoryService, "InventoryService 应该在 OneTimeSetUp 中初始化");
            return _inventoryService;
        }

        [SetUp]
        public void Setup()
        {
            // 每个测试前重置服务状态，清除历史数据
            _inventoryService?.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            // 清理工作
        }

        // ============= InventoryManager 测试方法 =============

        [Test]
        public void Test_InventoryManager_ContainerRegistration()
        {
            var manager = CreateInitializedInventoryService();

            // 创建容器
            var backpack = new LinerContainer("player_backpack", "背包", "Backpack", 10);
            var chest = new LinerContainer("storage_chest", "储物箱", "Storage", 20);
            var equipment = new LinerContainer("player_equipment", "装备栏", "Equipment", 6);

            // 容器注册
            bool result1 = manager.RegisterContainer(backpack, 100, "Player");
            bool result2 = manager.RegisterContainer(chest, 50, "Storage");
            bool result3 = manager.RegisterContainer(equipment, 200, "Player");

            Assert.IsTrue(result1, "注册背包应当成功");
            Assert.IsTrue(result2, "注册储物箱应当成功");
            Assert.IsTrue(result3, "注册装备栏应当成功");
            Assert.AreEqual(3, manager.ContainerCount, $"容器总数应为3，实际: {manager.ContainerCount}");

            // 测试重复注册 - 应替换原容器
            var newBackpack = new LinerContainer("player_backpack", "新背包", "Backpack", 15);
            bool result4 = manager.RegisterContainer(newBackpack, 150, "Player");
            Assert.IsTrue(result4, "重复注册应成功并替换原容器");
            Assert.AreEqual(3, manager.ContainerCount, $"容器总数仍应为3，实际: {manager.ContainerCount}");

            var retrievedBackpack = manager.GetContainer("player_backpack");
            Assert.AreEqual("新背包", retrievedBackpack.Name, "应返回新注册的背包");

            // 测试注册 null 容器
            bool result5 = manager.RegisterContainer(null);
            Assert.IsFalse(result5, "注册 null 容器应失败");

            // 测试注册 ID 为 null 的容器
            var invalidContainer = new LinerContainer(null, "无效容器", "Invalid", 5);
            bool result6 = manager.RegisterContainer(invalidContainer);
            Assert.IsFalse(result6, "注册 ID 为 null 的容器应失败");

            // 测试注销
            bool unregResult1 = manager.UnregisterContainer("storage_chest");
            Assert.IsTrue(unregResult1, "注销已存在的容器应成功");
            Assert.AreEqual(2, manager.ContainerCount, $"注销后应剩余2个容器，实际: {manager.ContainerCount}");

            bool unregResult2 = manager.UnregisterContainer("non_existent");
            Assert.IsFalse(unregResult2, "注销不存在的容器应失败");

            bool unregResult3 = manager.UnregisterContainer("");
            Assert.IsFalse(unregResult3, "注销空 ID 的容器应失败");
        }

        [Test]
        public void Test_InventoryManager_ContainerQuery()
        {
            var manager = CreateInitializedInventoryService();

            // 创建不同类型和分类的容器
            var backpack1 = new LinerContainer("backpack1", "背包1", "Backpack", 10);
            var backpack2 = new LinerContainer("backpack2", "背包2", "Backpack", 15);
            var chest1 = new LinerContainer("chest1", "储物箱1", "Storage", 20);
            var chest2 = new LinerContainer("chest2", "储物箱2", "Storage", 25);
            var equipment = new LinerContainer("equipment", "装备栏", "Equipment", 6);

            // 注册并设置不同优先级与分类
            manager.RegisterContainer(backpack1, 100, "Player");
            manager.RegisterContainer(backpack2, 80, "Player");
            manager.RegisterContainer(chest1, 50, "Storage");
            manager.RegisterContainer(chest2, 60, "Storage");
            manager.RegisterContainer(equipment, 200, "Player");

            // 通过 ID 获取容器
            var retrievedBackpack = manager.GetContainer("backpack1");
            Assert.IsNotNull(retrievedBackpack, "应当能通过ID获取容器");
            Assert.AreEqual("backpack1", retrievedBackpack.ID, "应能通过ID获取容器");

            var nonExistent = manager.GetContainer("non_existent");
            Assert.IsNull(nonExistent, "获取不存在的容器应返回 null");

            var nullId = manager.GetContainer(null);
            Assert.IsNull(nullId, "使用 null ID 获取容器应返回 null");

            // 获取所有容器
            var allContainers = manager.GetAllContainers();
            Assert.AreEqual(5, allContainers.Count, $"应有 5 个容器，实际: {allContainers.Count}");

            // 按类型获取容器
            var backpacks = manager.GetContainersByType("Backpack");
            Assert.AreEqual(2, backpacks.Count, $"应有 2 个背包，实际: {backpacks.Count}");

            var storages = manager.GetContainersByType("Storage");
            Assert.AreEqual(2, storages.Count, $"应有 2 个储物箱，实际: {storages.Count}");

            var equipments = manager.GetContainersByType("Equipment");
            Assert.AreEqual(1, equipments.Count, $"应有 1 个装备容器，实际: {equipments.Count}");

            var nonExistentType = manager.GetContainersByType("NonExistent");
            Assert.AreEqual(0, nonExistentType.Count, "不存在的类型应返回空列表");

            // 按分类获取容器
            var playerContainers = manager.GetContainersByCategory("Player");
            Assert.AreEqual(3, playerContainers.Count, $"Player 分类应有 3 个容器，实际: {playerContainers.Count}");

            var storageContainers = manager.GetContainersByCategory("Storage");
            Assert.AreEqual(2, storageContainers.Count, $"Storage 分类应有 2 个容器，实际: {storageContainers.Count}");

            // 按优先级获取容器（降序）
            var containersByPriority = manager.GetContainersByPriority(true);
            Assert.AreEqual(5, containersByPriority.Count, "应返回按优先级排序的列表");
            Assert.AreEqual("equipment", containersByPriority[0].ID, "最高优先级应为装备容器");
            Assert.AreEqual("backpack1", containersByPriority[1].ID, "第二优先级应为背包1");

            // 按优先级获取容器（升序）
            var containersByPriorityAsc = manager.GetContainersByPriority(false);
            Assert.AreEqual("chest1", containersByPriorityAsc[0].ID, "升序第一应为 chest1");

            // 注册状态
            Assert.IsTrue(manager.IsContainerRegistered("backpack1"), "背包1应已注册");
            Assert.IsFalse(manager.IsContainerRegistered("non_existent"), "不存在的容器应未注册");
            Assert.IsFalse(manager.IsContainerRegistered(null), "null ID 应未注册");
        }

        [Test]
        public void Test_InventoryManager_ContainerConfiguration()
        {
            var manager = CreateInitializedInventoryService();
            var container = new LinerContainer("test_container", "测试容器", "Test", 10);
            manager.RegisterContainer(container, 100, "TestCategory");

            // 设置优先级
            bool setPriorityResult1 = manager.SetContainerPriority("test_container", 150);
            Assert.IsTrue(setPriorityResult1, "设置已注册容器的优先级应成功");
            Assert.AreEqual(150, manager.GetContainerPriority("test_container"), "应获取到设置的优先级");

            bool setPriorityResult2 = manager.SetContainerPriority("non_existent", 200);
            Assert.IsFalse(setPriorityResult2, "设置未注册容器优先级应失败");

            int nonExistentPriority = manager.GetContainerPriority("non_existent");
            Assert.AreEqual(0, nonExistentPriority, "未注册容器优先级应返回 0");

            int nullPriority = manager.GetContainerPriority(null);
            Assert.AreEqual(0, nullPriority, "null ID 的优先级应返回 0");

            // 设置分类
            bool setCategoryResult1 = manager.SetContainerCategory("test_container", "NewCategory");
            Assert.IsTrue(setCategoryResult1, "设置已注册容器的分类应成功");
            Assert.AreEqual("NewCategory", manager.GetContainerCategory("test_container"), "应获取到设置的分类");

            bool setCategoryResult2 = manager.SetContainerCategory("non_existent", "SomeCategory");
            Assert.IsFalse(setCategoryResult2, "设置未注册容器的分类应失败");

            string nonExistentCategory = manager.GetContainerCategory("non_existent");
            Assert.AreEqual("Default", nonExistentCategory, "未注册容器的分类应返回 Default");

            // 设置为 null 分类
            bool setCategoryResult3 = manager.SetContainerCategory("test_container", null);
            Assert.IsTrue(setCategoryResult3, "设置 null 分类应成功");
            Assert.AreEqual("Default", manager.GetContainerCategory("test_container"), "null 分类应被重置为 Default");
        }

        [Test]
        public void Test_InventoryManager_GlobalConditions()
        {
            var manager = CreateInitializedInventoryService();

            // 创建容器
            var container1 = new LinerContainer("container1", "容器1", "Test", 5);
            var container2 = new LinerContainer("container2", "容器2", "Test", 5);
            manager.RegisterContainer(container1);
            manager.RegisterContainer(container2);

            // 全局条件
            var weaponCondition = new ItemTypeCondition("Weapon");
            var rarityCondition = new AttributeCondition("Rarity", "Rare");

            // 初始状态为未启用
            Assert.IsFalse(manager.IsGlobalConditionsEnabled, "初始状态全局条件应未启用");

            manager.AddGlobalItemCondition(weaponCondition);
            manager.AddGlobalItemCondition(rarityCondition);

            // 测试物品
            var weapon = CreateTestItem("sword", "剑", false, 1, "Weapon");
            weapon.SetCustomData("Rarity", "Rare");

            var potion = CreateTestItem("potion", "药水", true, 10, "Consumable");
            potion.SetCustomData("Rarity", "Common");

            // 未启用全局条件时校验应通过
            Assert.IsTrue(manager.ValidateGlobalItemConditions(weapon), "未启用全局条件时校验应通过");
            Assert.IsTrue(manager.ValidateGlobalItemConditions(potion), "未启用全局条件时校验应通过");

            // 启用全局条件
            manager.SetGlobalConditionsEnabled(true);
            Assert.IsTrue(manager.IsGlobalConditionsEnabled, "设置后全局条件状态应为 true");

            // 校验条件
            Assert.IsTrue(manager.ValidateGlobalItemConditions(weapon), "满足全局条件的物品应校验通过");
            Assert.IsFalse(manager.ValidateGlobalItemConditions(potion), "不满足全局条件的物品应校验失败");

            // 校验条件应应用到已注册容器
            Assert.IsTrue(container1.ContainerCondition.Contains(weaponCondition), "容器1应包含武器类型条件");
            Assert.IsTrue(container1.ContainerCondition.Contains(rarityCondition), "容器1应包含稀有度条件");
            Assert.IsTrue(container2.ContainerCondition.Contains(weaponCondition), "容器2应包含武器类型条件");

            // 新注册容器应自动应用全局条件
            var container3 = new LinerContainer("container3", "容器3", "Test", 5);
            manager.RegisterContainer(container3);
            Assert.IsTrue(container3.ContainerCondition.Contains(weaponCondition), "新注册容器应自动应用全局条件");

            // 移除全局条件
            bool removeResult = manager.RemoveGlobalItemCondition(weaponCondition);
            Assert.IsTrue(removeResult, "移除已存在的全局条件应成功");
            Assert.IsFalse(container1.ContainerCondition.Contains(weaponCondition), "移除后容器1不应包含该条件");
            Assert.IsFalse(container2.ContainerCondition.Contains(weaponCondition), "移除后容器2不应包含该条件");

            bool removeResult2 = manager.RemoveGlobalItemCondition(weaponCondition);
            Assert.IsFalse(removeResult2, "移除不存在的条件应失败");

            // 关闭全局条件
            manager.SetGlobalConditionsEnabled(false);
            Assert.IsFalse(manager.IsGlobalConditionsEnabled, "设置后全局条件状态应为 false");
            Assert.IsFalse(container1.ContainerCondition.Contains(rarityCondition), "关闭后容器条件中不应保留全局条件");

            // 添加 null 条件（应忽略，不抛异常）
            manager.AddGlobalItemCondition(null);

            // 移除 null 条件
            bool removeNullResult = manager.RemoveGlobalItemCondition(null);
            Assert.IsFalse(removeNullResult, "移除 null 条件应失败");
        }

        [Test]
        public void Test_InventoryManager_CrossContainerOperations()
        {
            var manager = CreateInitializedInventoryService();

            // 创建容器
            var sourceContainer = new LinerContainer("source", "源容器", "Storage", 10);
            var targetContainer = new LinerContainer("target", "目标容器", "Storage", 10);
            manager.RegisterContainer(sourceContainer);
            manager.RegisterContainer(targetContainer);

            // 添加物品到源容器
            var apple = CreateTestItem("apple", "苹果", true, 10, "Food");
            var sword = CreateTestItem("sword", "剑", false, 1, "Weapon");

            apple.Count = 15;
            sourceContainer.AddItems(apple); // 分散占用两个槽位 (10+5)
            sourceContainer.AddItems(sword);     // 槽位2

            // 移动存在的槽位
            var moveResult1 = manager.MoveItem("source", 2, "target", 0);
            Assert.AreEqual(MoveResult.Success, moveResult1, $"移动应成功，实际结果: {moveResult1}");
            Assert.IsFalse(sourceContainer.HasItem("sword"), "源容器不应再包含剑");
            Assert.IsTrue(targetContainer.HasItem("sword"), "目标容器应包含剑");

            // 移动不存在的槽位
            var moveResult2 = manager.MoveItem("source", 10, "target", 0);
            Assert.AreEqual(MoveResult.SourceSlotNotFound, moveResult2, $"移动不存在槽位应失败，实际结果: {moveResult2}");

            // 移动空槽位
            var moveResult3 = manager.MoveItem("source", 2, "target", 1); // 槽位2已经为空
            Assert.AreEqual(MoveResult.SourceSlotEmpty, moveResult3, $"移动空槽位应失败，实际结果: {moveResult3}");

            // 移动到不存在的容器
            var moveResult4 = manager.MoveItem("source", 0, "non_existent", 0);
            Assert.AreEqual(MoveResult.TargetContainerNotFound, moveResult4, $"目标容器不存在应失败，实际结果: {moveResult4}");

            // 从不存在的源容器移动
            var moveResult5 = manager.MoveItem("non_existent", 0, "target", 0);
            Assert.AreEqual(MoveResult.SourceContainerNotFound, moveResult5, $"源容器不存在应失败，实际结果: {moveResult5}");
        }

        [Test]
        public void Test_InventoryManager_GlobalSearch()
        {
            var manager = CreateInitializedInventoryService();

            // 创建容器
            var backpack = new LinerContainer("backpack", "背包", "Backpack", 10);
            var chest = new LinerContainer("chest", "储物箱", "Storage", 15);
            var equipment = new LinerContainer("equipment", "装备栏", "Equipment", 6);

            manager.RegisterContainer(backpack);
            manager.RegisterContainer(chest);
            manager.RegisterContainer(equipment);

            // 添加物品并分散到不同容器
            var apple = CreateTestItem("apple", "苹果", true, 15, "Food");
            var sword = CreateTestItem("sword", "剑", false, 1, "Weapon");
            var potion = CreateTestItem("potion", "药水", true, 20, "Consumable");

            apple.Count = 8;
            backpack.AddItems(apple);   // 背包: 8个苹果
            potion.Count = 5;
            backpack.AddItems(potion);  // 背包: 5瓶药水
            apple.Count = 12;
            chest.AddItems(apple);     // 储物箱: 12个苹果
            sword.Count = 1;
            chest.AddItems(sword);         // 储物箱: 1把剑
            equipment.AddItems(sword.Clone()); // 装备栏: 1把剑

            // 全局查找物品
            var appleResults = manager.FindItemGlobally("apple");
            Assert.AreEqual(2, appleResults.Count, $"应在2个位置找到苹果，实际: {appleResults.Count}");
            Assert.IsTrue(appleResults.Any(r => r.ContainerId == "backpack" && r.IndexCount == 8), "背包应有8个苹果");
            Assert.IsTrue(appleResults.Any(r => r.ContainerId == "chest" && r.IndexCount == 12), "储物箱应有12个苹果");

            var swordResults = manager.FindItemGlobally("sword");
            Assert.AreEqual(2, swordResults.Count, $"应在2个位置找到剑，实际: {swordResults.Count}");

            var nonExistentResults = manager.FindItemGlobally("non_existent");
            Assert.AreEqual(0, nonExistentResults.Count, "不存在的物品应返回空结果");

            var nullResults = manager.FindItemGlobally(null);
            Assert.AreEqual(0, nullResults.Count, "null 物品ID应返回空结果");

            // 全局计数
            int globalAppleCount = manager.GetGlobalItemCount("apple");
            Assert.AreEqual(20, globalAppleCount, $"全局苹果数量应为 20，实际: {globalAppleCount}");

            int globalSwordCount = manager.GetGlobalItemCount("sword");
            Assert.AreEqual(2, globalSwordCount, $"全局剑的数量应为 2，实际: {globalSwordCount}");

            int nonExistentCount = manager.GetGlobalItemCount("non_existent");
            Assert.AreEqual(0, nonExistentCount, "不存在的物品全局计数应为 0");

            // 查找包含指定物品的容器
            var containersWithApple = manager.FindContainersWithItem("apple");
            Assert.AreEqual(2, containersWithApple.Count, $"应在2个容器找到苹果，实际: {containersWithApple.Count}");
            Assert.AreEqual(8, containersWithApple["backpack"], "背包应有8个苹果");
            Assert.AreEqual(12, containersWithApple["chest"], "储物箱应有12个苹果");

            var containersWithSword = manager.FindContainersWithItem("sword");
            Assert.AreEqual(2, containersWithSword.Count, $"应在2个容器找到剑，实际: {containersWithSword.Count}");

            // 按类型搜索
            var weaponResults = manager.SearchItemsByType("Weapon");
            Assert.AreEqual(2, weaponResults.Count, $"应找到2个武器，实际: {weaponResults.Count}");

            var foodResults = manager.SearchItemsByType("Food");
            Assert.AreEqual(2, foodResults.Count, $"应找到2个食物项，实际: {foodResults.Count}");

            // 按名称搜索
            var appleNameResults = manager.SearchItemsByName("苹果");
            Assert.AreEqual(2, appleNameResults.Count, $"按名称“苹果”应找到2条记录，实际: {appleNameResults.Count}");

            var swordNameResults = manager.SearchItemsByName("剑");
            Assert.AreEqual(2, swordNameResults.Count, $"按名称“剑”应找到2条记录，实际: {swordNameResults.Count}");

            // 按属性搜索 - 需要获取容器中的物品引用来设置属性
            var chestSword = chest.GetSlot(1)?.Item;
            Assert.IsNotNull(chestSword, "储物箱中应有剑");
            chestSword.SetCustomData("Material", "Iron");
            var ironResults = manager.SearchItemsByAttribute("Material", "Iron");
            Assert.GreaterOrEqual(ironResults.Count, 1, "应至少找到1条带有 Material=Iron 的物品");

            // 按自定义条件搜索
            var stackableResults = manager.SearchItemsByCondition(item => item.IsStackable);
            Assert.GreaterOrEqual(stackableResults.Count, 2, "应至少找到2条可堆叠物品");
        }

        [Test]
        public void Test_InventoryManager_ItemTransfer()
        {
            var manager = CreateInitializedInventoryService();

            // 创建容器
            var sourceContainer = new LinerContainer("source", "源容器", "Storage", 10);
            var targetContainer = new LinerContainer("target", "目标容器", "Storage", 10);
            manager.RegisterContainer(sourceContainer);
            manager.RegisterContainer(targetContainer);

            // 添加物品到源容器
            var apple = CreateTestItem("apple", "苹果", true, 10, "Food");
            apple.Count = 25;
            sourceContainer.AddItems(apple); // 分散到3个槽位 (10+10+5)

            // 指定数量转移
            var (transferResult1, transferredCount1) = manager.TransferItems("apple", 8, "source", "target");
            Assert.AreEqual(MoveResult.Success, transferResult1, $"转移8个苹果应成功，实际: {transferResult1}");
            Assert.AreEqual(8, transferredCount1, $"应转移8个苹果，实际: {transferredCount1}");
            Assert.AreEqual(17, sourceContainer.GetItemTotalCount("apple"), $"源容器应剩余17个苹果，实际: {sourceContainer.GetItemTotalCount("apple")}");
            Assert.AreEqual(8, targetContainer.GetItemTotalCount("apple"), $"目标容器应有8个苹果，实际: {targetContainer.GetItemTotalCount("apple")}");

            // 转移超出数量
            var (transferResult2, transferredCount2) = manager.TransferItems("apple", 30, "source", "target");
            Assert.AreEqual(MoveResult.InsufficientQuantity, transferResult2, $"转移超出数量应失败，实际: {transferResult2}");
            Assert.AreEqual(0, transferredCount2, "转移失败时应返回0");

            // 转移不存在的物品
            var (transferResult3, transferredCount3) = manager.TransferItems("banana", 5, "source", "target");
            Assert.AreEqual(MoveResult.ItemNotFound, transferResult3, $"转移不存在的物品应失败，实际: {transferResult3}");

            // 自动移动全部
            var (autoResult1, autoCount1) = manager.AutoMoveItem("apple", "source", "target");
            Assert.AreEqual(MoveResult.Success, autoResult1, $"自动移动苹果应成功，实际: {autoResult1}");
            Assert.AreEqual(17, autoCount1, $"应自动移动17个苹果，实际: {autoCount1}");
            Assert.AreEqual(0, sourceContainer.GetItemTotalCount("apple"), "源容器苹果应被全部移动");
            Assert.AreEqual(25, targetContainer.GetItemTotalCount("apple"), "目标容器应有25个苹果");

            // 自动移动不存在物品
            var (autoResult2, autoCount2) = manager.AutoMoveItem("banana", "source", "target");
            Assert.AreEqual(MoveResult.ItemNotFound, autoResult2, "自动移动不存在的物品应失败");

            // 目标容器已满
            var fullTarget = new LinerContainer("full_target", "满容器", "Storage", 1);
            manager.RegisterContainer(fullTarget);

            var sword = CreateTestItem("sword", "剑", false, 1, "Weapon");
            fullTarget.AddItems(sword); // 已满

            apple.Count = 5;
            sourceContainer.AddItems(apple); // 给源容器补一些苹果
            var (transferResult4, transferredCount4) = manager.TransferItems("apple", 5, "source", "full_target");
            Assert.AreEqual(MoveResult.TargetContainerFull, transferResult4, "转移到已满的容器应失败");
        }

        [Test]
        public void Test_InventoryManager_ItemTransfer_PreservesItemUID()
        {
            var manager = CreateInitializedInventoryService();

            // 创建容器
            var sourceContainer = new LinerContainer("source_uid", "源容器", "Storage", 10);
            var targetContainer = new LinerContainer("target_uid", "目标容器", "Storage", 10);
            manager.RegisterContainer(sourceContainer);
            manager.RegisterContainer(targetContainer);

            // 添加物品到源容器，获得UID
            var item = CreateTestItem("transfer_item", "转移物品", false, 1, "Test");
            item.Count = 1;
            sourceContainer.AddItems(item);
            
            // 获取源容器中的物品引用（AddItems 会克隆物品，所以要从容器获取）
            var sourceSlot = sourceContainer.GetSlot(0);
            Assert.IsNotNull(sourceSlot?.Item, "源容器应该有物品");
            long originalUID = sourceSlot.Item.ItemUID;
            Assert.AreNotEqual(-1, originalUID, "物品应该被分配UID");

            // 移动物品到目标容器
            var moveResult = manager.MoveItem("source_uid", 0, "target_uid", 0);
            Assert.AreEqual(MoveResult.Success, moveResult, "移动物品应成功");

            // 验证源容器槽位为空
            var emptySourceSlot = sourceContainer.GetSlot(0);
            Assert.IsFalse(emptySourceSlot?.IsOccupied ?? true, "源容器槽位应为空");

            // 验证目标容器的物品UID保持不变
            var targetSlot = targetContainer.GetSlot(0);
            Assert.IsNotNull(targetSlot?.Item, "目标容器应该有物品");
            Assert.AreEqual(originalUID, targetSlot.Item.ItemUID, "转移后物品UID应该保持不变");
            Assert.AreEqual("transfer_item", targetSlot.Item.ID, "转移后物品ID应该相同");
        }

        [Test]
        public void Test_InventoryManager_BatchOperations()
        {
            var manager = CreateInitializedInventoryService();
            // 已经在 CreateInitializedInventoryService() 中初始化，无需再次初始化

            // 创建容器
            var container1 = new LinerContainer("container1", "容器1", "Storage", 10);
            var container2 = new LinerContainer("container2", "容器2", "Storage", 10);
            var container3 = new LinerContainer("container3", "容器3", "Storage", 10);

            manager.RegisterContainer(container1);
            manager.RegisterContainer(container2);
            manager.RegisterContainer(container3);

            // 容器1添加多种物品
            var apple = CreateTestItem("apple", "苹果", true, 10, "Food");
            var bread = CreateTestItem("bread", "面包", true, 5, "Food");
            var sword = CreateTestItem("sword", "剑", false, 1, "Weapon");

            apple.Count = 15;
            container1.AddItems(apple);  // 槽位0,1
            bread.Count = 8;
            container1.AddItems(bread);   // 槽位2,3
            container1.AddItems(sword);      // 槽位4

            // 准备批量移动请求
            var requests = new List<InventoryService.MoveRequest>
            {
                // 移动整槽
                new InventoryService.MoveRequest("container1", 4, "container2", 0), // 移动剑
                // 按数量移动
                new InventoryService.MoveRequest("container1", 0, "container2", -1, 5, "apple"), // 移动5个苹果
                new InventoryService.MoveRequest("container1", 2, "container3", -1, 3, "bread"), // 移动3个面包
                // 无效请求
                new InventoryService.MoveRequest("container1", 10, "container2"), // 无效槽位
                new InventoryService.MoveRequest("non_existent", 0, "container2"), // 无效容器
            };

            // 执行批量移动
            var results = manager.BatchMoveItems(requests);
            Assert.AreEqual(5, results.Count, $"应返回5条结果，实际: {results.Count}");

            // 结果校验
            Assert.AreEqual(MoveResult.Success, results[0].result, "移动剑应成功");
            Assert.AreEqual(MoveResult.Success, results[1].result, "移动苹果应成功");
            Assert.AreEqual(MoveResult.Success, results[2].result, "移动面包应成功");
            Assert.AreEqual(MoveResult.SourceSlotNotFound, results[3].result, "无效槽位应失败");
            Assert.AreEqual(MoveResult.SourceContainerNotFound, results[4].result, "无效容器应失败");

            // 物品确实移动了
            Assert.IsTrue(container2.HasItem("sword"), "容器2应包含剑");
            Assert.IsTrue(container2.HasItem("apple"), "容器2应包含苹果");
            Assert.IsTrue(container3.HasItem("bread"), "容器3应包含面包");

            Assert.AreEqual(10, container1.GetItemTotalCount("apple"), "容器1应剩余10个苹果");
            Assert.AreEqual(5, container1.GetItemTotalCount("bread"), "容器1应剩余5个面包");
            Assert.IsFalse(container1.HasItem("sword"), "容器1不应再有剑");
        }

        [Test]
        public void Test_InventoryManager_ItemDistribution()
        {
            var manager = CreateInitializedInventoryService();

            // 创建不同优先级的容器
            var highPriority = new LinerContainer("high", "高优先级", "Storage", 5);
            var mediumPriority = new LinerContainer("medium", "中优先级", "Storage", 5);
            var lowPriority = new LinerContainer("low", "低优先级", "Storage", 5);

            manager.RegisterContainer(highPriority, 100);
            manager.RegisterContainer(mediumPriority, 50);
            manager.RegisterContainer(lowPriority, 10);

            // 测试物品
            var apple = CreateTestItem("apple", "苹果", true, 10, "Food");

            // 分发物品（应按优先级分布）
            var targetContainers = new List<string> { "high", "medium", "low" };
            var distribution1 = manager.DistributeItems(apple, 35, targetContainers);

            Assert.GreaterOrEqual(distribution1.Count, 1, "分发结果键数应至少为1");
            Assert.IsTrue(distribution1.ContainsKey("high"), "应分发到高优先级容器");

            // 验证优先级分发（高优先级应先装满）
            Assert.Greater(highPriority.GetItemTotalCount("apple"), 0, "高优先级容器应获得物品");

            int totalDistributed = distribution1.Values.Sum();
            Assert.AreEqual(35, totalDistributed, $"应分发35个物品，实际分发: {totalDistributed}");

            // 分发到已被占用的容器
            var breadItem = CreateTestItem("bread", "面包", true, 10);
            breadItem.Count = 20;
            mediumPriority.AddItems(breadItem); // 占满容器

            var distribution2 = manager.DistributeItems(apple, 10, new List<string> { "medium", "low" });
            int actualDistributed = distribution2.Values.Sum();
            Assert.GreaterOrEqual(actualDistributed, 0, "分发数量应大于等于0");

            // 分发 null 物品
            var distribution3 = manager.DistributeItems(null, 10, targetContainers);
            Assert.AreEqual(0, distribution3.Count, "分发 null 物品应返回空结果");

            // 分发到空目标列表
            var distribution4 = manager.DistributeItems(apple, 10, new List<string>());
            Assert.AreEqual(0, distribution4.Count, "分发到空容器列表应返回空结果");

            // 分发全局条件不允许的物品
            manager.AddGlobalItemCondition(new ItemTypeCondition("Weapon"));
            manager.SetGlobalConditionsEnabled(true);

            var distribution5 = manager.DistributeItems(apple, 10, targetContainers); // 苹果非 Weapon
            Assert.AreEqual(0, distribution5.Count, "全局条件不允许的物品分发应失败");
        }

        [Test]
        public void Test_InventoryManager_Events()
        {
            var manager = CreateInitializedInventoryService();

            // 事件统计
            int containerRegisteredCount = 0;
            int containerUnregisteredCount = 0;
            int priorityChangedCount = 0;
            int categoryChangedCount = 0;
            int globalConditionAddedCount = 0;
            int globalConditionRemovedCount = 0;
            int itemMovedCount = 0;
            int itemsTransferredCount = 0;
            int batchMoveCompletedCount = 0;
            int itemsDistributedCount = 0;

            // 注册事件监听器
            manager.OnContainerRegistered += (container) => { containerRegisteredCount++; };
            manager.OnContainerUnregistered += (container) => { containerUnregisteredCount++; };
            manager.OnContainerPriorityChanged += (id, priority) => { priorityChangedCount++; };
            manager.OnContainerCategoryChanged += (id, oldCat, newCat) => { categoryChangedCount++; };
            manager.OnGlobalConditionAdded += (condition) => { globalConditionAddedCount++; };
            manager.OnGlobalConditionRemoved += (condition) => { globalConditionRemovedCount++; };
            manager.OnItemMoved += (fromId, fromSlot, toId, item, count) => { itemMovedCount++; };
            manager.OnItemsTransferred += (fromId, toId, itemId, count) => { itemsTransferredCount++; };
            manager.OnBatchMoveCompleted += (results) => { batchMoveCompletedCount++; };
            manager.OnItemsDistributed += (item, total, distribution, remaining) => { itemsDistributedCount++; };

            // 容器注册事件
            var container1 = new LinerContainer("container1", "容器1", "Test", 5);
            var container2 = new LinerContainer("container2", "容器2", "Test", 5);

            manager.RegisterContainer(container1);
            manager.RegisterContainer(container2);
            Assert.AreEqual(2, containerRegisteredCount, $"应触发2次注册事件，实际: {containerRegisteredCount}");

            // 容器注销事件
            manager.UnregisterContainer("container1");
            Assert.AreEqual(1, containerUnregisteredCount, $"应触发1次注销事件，实际: {containerUnregisteredCount}");

            // 优先级变更事件
            manager.SetContainerPriority("container2", 100);
            Assert.AreEqual(1, priorityChangedCount, $"应触发1次优先级变更事件，实际: {priorityChangedCount}");

            // 分类变更事件
            manager.SetContainerCategory("container2", "NewCategory");
            Assert.AreEqual(1, categoryChangedCount, $"应触发1次分类变更事件，实际: {categoryChangedCount}");

            // 全局条件变更事件
            var condition = new ItemTypeCondition("Weapon");
            manager.AddGlobalItemCondition(condition);
            Assert.AreEqual(1, globalConditionAddedCount, $"应触发1次全局条件添加事件，实际: {globalConditionAddedCount}");

            manager.RemoveGlobalItemCondition(condition);
            Assert.AreEqual(1, globalConditionRemovedCount, $"应触发1次全局条件移除事件，实际: {globalConditionRemovedCount}");

            // 物品移动/转移事件
            var container3 = new LinerContainer("container3", "容器3", "Test", 5);
            manager.RegisterContainer(container3);

            var apple = CreateTestItem("apple", "苹果", true, 10, "Food");
            apple.Count = 5;
            container2.AddItems(apple);

            // 触发移动事件
            manager.MoveItem("container2", 0, "container3", 0);
            Assert.AreEqual(1, itemMovedCount, $"应触发1次物品移动事件，实际: {itemMovedCount}");

            // 触发转移事件
            apple.Count = 3;
            container2.AddItems(apple);
            manager.TransferItems("apple", 2, "container2", "container3");
            Assert.AreEqual(1, itemsTransferredCount, $"应触发1次物品转移事件，实际: {itemsTransferredCount}");

            // 批量移动事件
            var requests = new List<InventoryService.MoveRequest>
            {
                new InventoryService.MoveRequest("container2", 0, "container3")
            };
            manager.BatchMoveItems(requests);
            Assert.AreEqual(1, batchMoveCompletedCount, $"应触发1次批量移动完成事件，实际: {batchMoveCompletedCount}");

            // 分发事件
            var bread = CreateTestItem("bread", "面包", true, 5, "Food");
            bread.Count = 1;
            container2.AddItems(bread); // 给容器2增加一些面包
            manager.DistributeItems(bread, 10, new List<string> { "container2", "container3" });
            Assert.AreEqual(1, itemsDistributedCount, $"应触发1次物品分发事件，实际: {itemsDistributedCount}");
        }

        [Test]
        public void Test_InventoryManager_ErrorHandling()
        {
            var manager = CreateInitializedInventoryService();

            // 准备一个源容器与物品以便后续移动与转移
            var sourceContainer = new LinerContainer("source", "源容器", "Test", 5);
            manager.RegisterContainer(sourceContainer);

            var testItem = CreateTestItem("test", "测试物品", false);
            sourceContainer.AddItems(testItem);

            // 各种 null/空值 错误处理

            // GetContainer with invalid inputs
            Assert.IsNull(manager.GetContainer(null), "null ID 应返回 null");
            Assert.IsNull(manager.GetContainer(""), "空 ID 应返回 null");
            Assert.IsNull(manager.GetContainer("non_existent"), "不存在的 ID 应返回 null");

            // GetContainersByType with invalid inputs
            var emptyList1 = manager.GetContainersByType(null);
            Assert.AreEqual(0, emptyList1.Count, "null 类型应返回空列表");

            var emptyList2 = manager.GetContainersByType("");
            Assert.AreEqual(0, emptyList2.Count, "空类型应返回空列表");

            // GetContainersByCategory with invalid inputs
            var emptyList3 = manager.GetContainersByCategory(null);
            Assert.AreEqual(0, emptyList3.Count, "null 分类应返回空列表");

            // IsContainerRegistered with invalid inputs
            Assert.IsFalse(manager.IsContainerRegistered(null), "null ID 应返回 false");
            Assert.IsFalse(manager.IsContainerRegistered(""), "空 ID 应返回 false");

            // 设置属性错误处理
            Assert.IsFalse(manager.SetContainerPriority(null, 100), "设置 null 容器优先级应失败");
            Assert.IsFalse(manager.SetContainerPriority("", 100), "设置空 ID 容器优先级应失败");
            Assert.IsFalse(manager.SetContainerCategory(null, "Test"), "设置 null 容器分类应失败");

            Assert.AreEqual(0, manager.GetContainerPriority(null), "获取 null 容器优先级应返回 0");
            Assert.AreEqual("Default", manager.GetContainerCategory(null), "获取 null 容器分类应返回 Default");

            // 全局条件与校验
            manager.AddGlobalItemCondition(null); // 应忽略
            Assert.IsFalse(manager.RemoveGlobalItemCondition(null), "移除 null 条件应失败");
            Assert.IsFalse(manager.ValidateGlobalItemConditions(null), "校验 null 物品应失败");

            // 跨容器操作
            var moveResult1 = manager.MoveItem(null, 0, "target", 0);
            Assert.AreEqual(MoveResult.SourceContainerNotFound, moveResult1, "null 源容器应失败");

            var moveResult2 = manager.MoveItem("source", 0, null, 0);
            Assert.AreEqual(MoveResult.TargetContainerNotFound, moveResult2, "null 目标容器应失败");

            var (transferResult, _) = manager.TransferItems(null, 5, "source", "target");
            Assert.AreEqual(MoveResult.ItemNotFound, transferResult, "null 物品ID转移应失败");

            var (autoResult, _) = manager.AutoMoveItem(null, "source", "target");
            Assert.AreEqual(MoveResult.ItemNotFound, autoResult, "null 物品ID自动移动应失败");

            // 全局查找相关
            var emptyResults1 = manager.FindItemGlobally(null);
            Assert.AreEqual(0, emptyResults1.Count, "null 物品ID查找应返回空结果");

            var emptyResults2 = manager.FindItemGlobally("");
            Assert.AreEqual(0, emptyResults2.Count, "空物品ID查找应返回空结果");

            Assert.AreEqual(0, manager.GetGlobalItemCount(null), "null 物品ID全局计数应返回 0");
            Assert.AreEqual(0, manager.GetGlobalItemCount(""), "空物品ID全局计数应返回 0");

            var emptyContainers = manager.FindContainersWithItem(null);
            Assert.AreEqual(0, emptyContainers.Count, "null 物品ID容器查找应返回空结果");

            // 搜索接口
            var emptySearch1 = manager.SearchItemsByType(null);
            Assert.AreEqual(0, emptySearch1.Count, "null 类型搜索应返回空结果");

            var emptySearch2 = manager.SearchItemsByName(null);
            Assert.AreEqual(0, emptySearch2.Count, "null 名称搜索应返回空结果");

            var emptySearch3 = manager.SearchItemsByAttribute(null, "value");
            Assert.AreEqual(0, emptySearch3.Count, "null 属性名搜索应返回空结果");

            var emptySearch4 = manager.SearchItemsByCondition(null);
            Assert.AreEqual(0, emptySearch4.Count, "null 条件搜索应返回空结果");

            // 批量移动
            var emptyBatchResults = manager.BatchMoveItems(new List<InventoryService.MoveRequest>());
            Assert.AreEqual(0, emptyBatchResults.Count, "空的批量请求应返回空结果");

            var nullBatchResults = manager.BatchMoveItems(null);
            Assert.AreEqual(0, nullBatchResults.Count, "null 的批量请求应返回空结果");
        }

        [Test]
        public void Test_InventoryManager_PerformanceAndEdgeCases()
        {
            var manager = CreateInitializedInventoryService();

            // 大量容器注册
            const int containerCount = 100;
            for (int i = 0; i < containerCount; i++)
            {
                var container = new LinerContainer($"container_{i}", $"容器{i}", "Test", 10);
                bool result = manager.RegisterContainer(container, i % 10, $"Category_{i % 5}");
                Assert.IsTrue(result, $"注册容器{i}应成功");
            }

            Assert.AreEqual(containerCount, manager.ContainerCount, $"应有 {containerCount} 个容器");

            // 大量容器查询能力
            var allContainers = manager.GetAllContainers();
            Assert.AreEqual(containerCount, allContainers.Count, "获取所有容器应返回正确数量");

            var testTypeContainers = manager.GetContainersByType("Test");
            Assert.AreEqual(containerCount, testTypeContainers.Count, "类型查询应返回所有容器");

            var category0Containers = manager.GetContainersByCategory("Category_0");
            Assert.AreEqual(containerCount / 5, category0Containers.Count, "分类查询应返回正确数量");

            // 优先级排序正确性
            var sortedContainers = manager.GetContainersByPriority(true);
            Assert.AreEqual(containerCount, sortedContainers.Count, "优先级排序应返回所有容器");

            for (int i = 0; i < sortedContainers.Count - 1; i++)
            {
                int currentPriority = manager.GetContainerPriority(sortedContainers[i].ID);
                int nextPriority = manager.GetContainerPriority(sortedContainers[i + 1].ID);
                Assert.GreaterOrEqual(currentPriority, nextPriority, "应按优先级从高到低排序");
            }

            // 边界条件

            // 容量设为 -1（无限容量）
            var infiniteContainer = new LinerContainer("infinite", "无限容器", "Test", -1);
            manager.RegisterContainer(infiniteContainer);
            Assert.IsFalse(infiniteContainer.Full, "无限容器应永不为满");

            // 添加海量物品验证
            var testItem = CreateTestItem("test", "测试", true, 100);
            testItem.Count = 10000;
            infiniteContainer.AddItems(testItem);
            Assert.AreEqual(10000, infiniteContainer.GetItemTotalCount("test"), "无限容器应能容纳大量物品");

            // 全局统计能力
            var globalResults = manager.FindItemGlobally("test");
            Assert.GreaterOrEqual(globalResults.Count, 1, "全局查找应能找到该物品");

            var globalCount = manager.GetGlobalItemCount("test");
            Assert.AreEqual(10000, globalCount, "全局计数应正确");

            // 其它边界：空批量移动
            var emptyBatchResults = manager.BatchMoveItems(new List<InventoryService.MoveRequest>());
            Assert.AreEqual(0, emptyBatchResults.Count, "空的批量请求应返回空结果");

            // null 批量移动
            var nullBatchResults = manager.BatchMoveItems(null);
            Assert.AreEqual(0, nullBatchResults.Count, "null 的批量请求应返回空结果");

            // 分发到不存在容器
            var invalidDistribution = manager.DistributeItems(testItem, 100,
                new List<string> { "non_existent1", "non_existent2" });
            Assert.AreEqual(0, invalidDistribution.Count, "分发到不存在的容器应返回空结果");

            // 极端优先级值
            manager.SetContainerPriority("container_0", int.MaxValue);
            manager.SetContainerPriority("container_1", int.MinValue);

            var priorityTestContainers = manager.GetContainersByPriority(true);
            Assert.AreEqual("container_0", priorityTestContainers[0].ID, "最高优先级应位于第一");

            // 刷新与校验全局缓存（不应抛异常）
            manager.RefreshGlobalCache();
            manager.ValidateGlobalCache();
        }

        [Test]
        public void Test_AddItems_Basic()
        {
            // 创建容器
            var container = new LinerContainer("test_container", "测试容器", "Backpack", 10);

            // 测试物品
            var item = CreateTestItem("item1", "测试物品1", true, 10);

            // 添加单件物品
            var (result1, count1) = container.AddItems(item);
            Assert.AreEqual(AddItemResult.Success, result1, $"添加单个物品应成功，实际: {result1}");
            Assert.AreEqual(1, count1, $"应添加1个物品，实际添加: {count1} 个");

            // 校验已添加
            Assert.IsTrue(container.HasItem("item1"), "容器应已包含该物品");
            Assert.AreEqual(1, container.GetItemTotalCount("item1"), $"物品总数应为 1，实际: {container.GetItemTotalCount("item1")}");

            // 添加多个
            item.Count = 5;
            var (result2, count2) = container.AddItems(item);
            Assert.AreEqual(AddItemResult.Success, result2, $"添加多个物品应成功，实际: {result2}");
            Assert.AreEqual(5, count2, $"应添加5个物品，实际: {count2} 个");
            Assert.AreEqual(6, container.GetItemTotalCount("item1"), $"物品总数应为 6，实际: {container.GetItemTotalCount("item1")}");

            // 添加 null 物品
            var (result3, count3) = container.AddItems((IItem)null);
            Assert.AreEqual(AddItemResult.ItemIsNull, result3, $"添加 null 物品应返回 ItemIsNull，实际: {result3}");
            Assert.AreEqual(0, count3, $"应添加 0 个物品，实际: {count3} 个");
        }

        [Test]
        public void Test_AddItems_Stacking()
        {
            // 创建容器
            var container = new LinerContainer("test_container", "测试容器", "Backpack", 5);

            // 可堆叠物品（堆叠上限 10）
            var stackableItem = CreateTestItem("stackable", "可堆叠物品", true, 10);

            // 先添加8个
            stackableItem.Count = 8;
            container.AddItems(stackableItem);
            Assert.AreEqual(8, container.GetItemTotalCount("stackable"), $"应为 8 个物品，实际: {container.GetItemTotalCount("stackable")}");
            Assert.AreEqual(5, container.Slots.Count, $"初始容量 5 个槽位，实际槽位数: {container.Slots.Count} 个");

            // 再添加5个 -> 分布到两个槽位（10+3）
            stackableItem.Count = 5;
            container.AddItems(stackableItem);
            Assert.AreEqual(13, container.GetItemTotalCount("stackable"), $"应为 13 个，实际: {container.GetItemTotalCount("stackable")}");

            // 槽位分布
            var items = container.GetAllItems();
            Assert.AreEqual(2, items.Count, $"应占用 2 个槽位，实际占用: {items.Count} 个");
            Assert.AreEqual(10, items[0].count, $"第一个槽位应为 10 个，实际: {items[0].count} 个");
            Assert.AreEqual(3, items[1].count, $"第二个槽位应为 3 个，实际: {items[1].count} 个");

            // 不可堆叠
            var nonStackableItem = CreateTestItem("non_stackable", "不可堆叠物品", false);

            nonStackableItem.Count = 3;
            container.AddItems(nonStackableItem);
            Assert.AreEqual(3, container.GetItemTotalCount("non_stackable"), $"应为3个不可堆叠物品，实际: {container.GetItemTotalCount("non_stackable")}");
            Assert.AreEqual(5, container.Slots.Count, $"应使用 5 个槽位（2堆叠+3非堆叠），实际槽位: {container.Slots.Count} 个");
        }

        [Test]
        public void Test_AddItems_ToSpecificSlot()
        {
            // 创建容器
            var container = new LinerContainer("test_container", "测试容器", "Backpack", 5);

            // 物品
            var item1 = CreateTestItem("item1", "测试物品1", true, 10);
            var item2 = CreateTestItem("item2", "测试物品2", true, 10);

            // 指定槽位添加 - 使用新API
            int targetSlot = 2; // 指定槽位索引为2
            item1.Count = 5;
            var (result, count) = container.AddItems(item1, slotIndex: targetSlot);

            Assert.AreEqual(AddItemResult.Success, result, $"指定槽位添加应成功，实际: {result}");
            Assert.AreEqual(5, count, $"应添加5个物品，实际: {count} 个");

            // 校验槽位
            Assert.Greater(container.Slots.Count, targetSlot, "容器应至少有 3 个槽位");
            Assert.IsTrue(container.Slots[targetSlot].IsOccupied, "指定槽位应被占用");
            Assert.AreEqual("item1", container.Slots[targetSlot].Item.ID, $"指定槽位应为 item1，实际: {container.Slots[targetSlot].Item.ID}");
            Assert.AreEqual(5, container.Slots[targetSlot].Item.Count, $"指定槽位应为 5 个，实际: {container.Slots[targetSlot].Item.Count} 个");

            // 同槽位相同物品 -> 堆叠
            var item1b = CreateTestItem("item1", "测试物品1", true, 10);
            item1b.Count = 3;
            container.AddItems(item1b, slotIndex: targetSlot);
            Assert.AreEqual(8, container.Slots[targetSlot].Item.Count, $"堆叠后应为 8 个，实际: {container.Slots[targetSlot].Item.Count} 个");

            // 同槽位不同物品 -> 失败
            item2.Count = 1;
            var (result2, count2) = container.AddItems(item2, slotIndex: targetSlot);
            Assert.AreEqual(AddItemResult.NoSuitableSlotFound, result2, $"不同物品占用同槽位应失败，实际: {result2}");
            Assert.AreEqual(0, count2, $"不应成功添加任何物品，实际: {count2} 个");
        }

        [Test]
        public void Test_AddItems_MaxCapacity()
        {
            // 有限容量
            int capacity = 3;
            var container = new LinerContainer("limited_container", "有限容器", "Backpack", capacity);

            // 不可堆叠
            var item = CreateTestItem("item", "测试物品", false);

            // 按容量逐一添加
            for (int i = 0; i < capacity; i++)
            {
                var (result, _) = container.AddItems(item.Clone());
                Assert.AreEqual(AddItemResult.Success, result, $"添加第 {i + 1} 个物品应成功");
            }

            // 已满
            Assert.IsTrue(container.Full, "容器应为满");
            Assert.AreEqual(capacity, container.Slots.Count, $"槽位数应等于容量({capacity})");

            // 再添加 -> 失败
            var (result2, count2) = container.AddItems(item.Clone());
            Assert.AreEqual(AddItemResult.ContainerIsFull, result2, $"满容器添加应返回 ContainerIsFull，实际: {result2}");
            Assert.AreEqual(0, count2, "不应添加任何物品");

            // 无限容量
            var unlimitedContainer = new LinerContainer("unlimited", "无限容器", "Chest", -1);

            for (int i = 0; i < 100; i++)
            {
                var (result, _) = unlimitedContainer.AddItems(item.Clone());
                Assert.AreEqual(AddItemResult.Success, result, $"第 {i + 1} 次添加到无限容器应成功");
            }

            Assert.IsFalse(unlimitedContainer.Full, "无限容器应不为满");
            Assert.AreEqual(100, unlimitedContainer.Slots.Count, "应使用 100 个槽位");
        }

        [Test]
        public void Test_RemoveItem_ById()
        {
            // 创建容器并添加物品
            var container = new LinerContainer("test_container", "测试容器", "Backpack", 10);
            var item = CreateTestItem("remove_test", "移除测试物品", true, 20);
            item.Count = 15;
            container.AddItems(item); // 共 15 个

            // 移除部分
            var result1 = container.RemoveItem("remove_test", 5);
            Assert.AreEqual(RemoveItemResult.Success, result1, $"移除 5 个应成功，实际: {result1}");
            Assert.AreEqual(10, container.GetItemTotalCount("remove_test"), $"应剩余 10 个，实际: {container.GetItemTotalCount("remove_test")} 个");

            // 移除剩余全部
            var result2 = container.RemoveItem("remove_test", 10);
            Assert.AreEqual(RemoveItemResult.Success, result2, $"移除全部剩余应成功，实际: {result2}");
            Assert.AreEqual(0, container.GetItemTotalCount("remove_test"), $"应剩余 0 个，实际: {container.GetItemTotalCount("remove_test")} 个");
            Assert.IsFalse(container.HasItem("remove_test"), "容器中不应再有该物品");

            // 移除不存在物品
            var result3 = container.RemoveItem("non_existent", 1);
            Assert.AreEqual(RemoveItemResult.ItemNotFound, result3, $"移除不存在物品应返回 ItemNotFound，实际: {result3}");

            // 移除超过数量
            item.Count = 3;
            container.AddItems(item);
            var result4 = container.RemoveItem("remove_test", 5);
            Assert.AreEqual(RemoveItemResult.InsufficientQuantity, result4, $"移除数量不足应返回 InsufficientQuantity，实际: {result4}");
            Assert.AreEqual(3, container.GetItemTotalCount("remove_test"), "物品数量应未改变");

            // 移除无效ID
            var result5 = container.RemoveItem("", 1);
            Assert.AreEqual(RemoveItemResult.InvalidItemId, result5, $"移除空ID物品应返回 InvalidItemId，实际: {result5}");
        }

        [Test]
        public void Test_RemoveItemAtIndex()
        {
            // 创建容器
            var container = new LinerContainer("test_container", "测试容器", "Backpack", 5);

            // 添加不同物品不同槽位
            var item1 = CreateTestItem("item1", "测试物品1", true, 10);
            var item2 = CreateTestItem("item2", "测试物品2", false);
            var item3 = CreateTestItem("item3", "测试物品3", true, 5);

            item1.Count = 8;
            container.AddItems(item1); // 槽位0
            container.AddItems(item2);    // 槽位1
            item3.Count = 4;
            container.AddItems(item3); // 槽位2

            // 从槽位1移除
            var result1 = container.RemoveItemAtIndex(1);
            Assert.AreEqual(RemoveItemResult.Success, result1, $"从槽位1移除应成功，实际: {result1}");
            Assert.IsFalse(container.Slots[1].IsOccupied, "槽位1应为空");
            Assert.IsFalse(container.HasItem("item2"), "容器中不应再有 item2");

            // 槽位0移除部分
            var result2 = container.RemoveItemAtIndex(0, 3);
            Assert.AreEqual(RemoveItemResult.Success, result2, $"从槽位0移除部分应成功，实际: {result2}");
            Assert.AreEqual(5, container.Slots[0].Item.Count, $"槽位0应剩 5 个，实际: {container.Slots[0].Item.Count} 个");
            Assert.AreEqual(5, container.GetItemTotalCount("item1"), $"item1 总数应为 5，实际: {container.GetItemTotalCount("item1")}");

            // 指定物品ID验证
            var result3 = container.RemoveItemAtIndex(2, 2, "item3");
            Assert.AreEqual(RemoveItemResult.Success, result3, $"使用正确ID移除应成功，实际: {result3}");
            Assert.AreEqual(2, container.Slots[2].Item.Count, $"槽位2应剩 2 个，实际: {container.Slots[2].Item.Count} 个");

            // 错误物品ID
            var result4 = container.RemoveItemAtIndex(2, 1, "wrong_id");
            Assert.AreEqual(RemoveItemResult.InvalidItemId, result4, $"错误ID移除应返回 InvalidItemId，实际: {result4}");
            Assert.AreEqual(2, container.Slots[2].Item.Count, "物品数量应未改变");

            // 无效槽位
            var result5 = container.RemoveItemAtIndex(10);
            Assert.AreEqual(RemoveItemResult.SlotNotFound, result5, $"无效槽位应返回 SlotNotFound，实际: {result5}");

            // 超量移除
            var result6 = container.RemoveItemAtIndex(2, 5);
            Assert.AreEqual(RemoveItemResult.InsufficientQuantity, result6, $"移除数量不足应返回 InsufficientQuantity，实际: {result6}");
        }

        [Test]
        public void Test_ContainerCondition()
        {
            // 仅能放武器的容器
            var container = new LinerContainer("weapon_container", "武器容器", "Equipment", 5);
            container.ContainerCondition.Add(new ItemTypeCondition("Weapon"));

            // 符合条件的物品
            var sword = CreateTestItem("sword", "剑", false, 1, "Weapon");
            var bow = CreateTestItem("bow", "弓", false, 1, "Weapon");

            // 不符合条件的物品
            var potion = CreateTestItem("potion", "药水", true, 10, "Consumable");
            var armor = CreateTestItem("armor", "护甲", false, 1, "Armor");

            // 添加符合条件的物品
            var (result1, _) = container.AddItems(sword);
            var (result2, _) = container.AddItems(bow);
            Assert.AreEqual(AddItemResult.Success, result1, "添加符合类型的武器应成功");
            Assert.AreEqual(AddItemResult.Success, result2, "添加符合类型的武器应成功");

            // 添加不符合条件的物品
            var (result3, _) = container.AddItems(potion);
            var (result4, _) = container.AddItems(armor);
            Assert.AreEqual(AddItemResult.ItemConditionNotMet, result3, $"添加药水应返回 ItemConditionNotMet，实际: {result3}");
            Assert.AreEqual(AddItemResult.ItemConditionNotMet, result4, $"添加护甲应返回 ItemConditionNotMet，实际: {result4}");

            // 添加空物品
            var (result5, _) = container.AddItems((IItem)null);
            Assert.AreEqual(AddItemResult.ItemIsNull, result5, $"添加 null 物品应返回 ItemIsNull，实际: {result5}");

            // 复合条件容器（武器 且 重量<5）
            var lightWeaponContainer = new LinerContainer("light_weapon", "轻型武器容器", "SpecialEquip", 5);
            lightWeaponContainer.ContainerCondition.Add(new ItemTypeCondition("Weapon"));
            lightWeaponContainer.ContainerCondition.Add(new AttributeCondition("Weight", 5.0f, AttributeComparisonType.LessThan));

            var lightSword = CreateTestItem("light_sword", "轻剑", false, 1, "Weapon");
            lightSword.SetCustomData("Weight", 3.5f);

            var heavySword = CreateTestItem("heavy_sword", "重剑", false, 1, "Weapon");
            heavySword.SetCustomData("Weight", 8.0f);

            var (result6, _) = lightWeaponContainer.AddItems(lightSword);
            Assert.AreEqual(AddItemResult.Success, result6, "添加轻剑应成功");

            var (result7, _) = lightWeaponContainer.AddItems(heavySword);
            Assert.AreEqual(AddItemResult.ItemConditionNotMet, result7, $"添加重剑应返回 ItemConditionNotMet，实际: {result7}");
        }

        [Test]
        public void Test_ItemQuery()
        {
            // 创建容器并添加物品
            var container = new LinerContainer("test_container", "测试容器", "Backpack", 10);

            var ironSword = CreateTestItem("iron_sword", "铁剑", false, 1, "Weapon");
            ironSword.SetCustomData("Material", "Iron");
            ironSword.SetCustomData("Damage", 10);

            var steelSword = CreateTestItem("steel_sword", "钢剑", false, 1, "Weapon");
            steelSword.SetCustomData("Material", "Steel");
            steelSword.SetCustomData("Damage", 15);

            var healthPotion = CreateTestItem("health_potion", "生命药水", true, 20, "Potion");
            healthPotion.SetCustomData("Healing", 50);

            var manaPotion = CreateTestItem("mana_potion", "魔法药水", true, 20, "Potion");
            manaPotion.SetCustomData("Mana", 40);

            // 添加
            container.AddItems(ironSword);
            container.AddItems(steelSword);
            healthPotion.Count = 5;
            container.AddItems(healthPotion);
            manaPotion.Count = 3;
            container.AddItems(manaPotion);

            // HasItem
            Assert.IsTrue(container.HasItem("iron_sword"), "容器应包含铁剑");
            Assert.IsFalse(container.HasItem("gold_sword"), "容器不应包含 gold_sword");

            // GetItemCount
            Assert.AreEqual(5, container.GetItemTotalCount("health_potion"), $"生命药水数量应为 5，实际: {container.GetItemTotalCount("health_potion")}");
            Assert.AreEqual(0, container.GetItemTotalCount("non_existent"), "不存在物品数量应为 0");

            // GetItemsByType
            var weapons = container.GetItemsByType("Weapon");
            Assert.AreEqual(2, weapons.Count, $"应找到 2 个武器，实际: {weapons.Count} 个");
            Assert.IsTrue(weapons.Any(w => w.item.ID == "iron_sword"), "结果中应包含铁剑");
            Assert.IsTrue(weapons.Any(w => w.item.ID == "steel_sword"), "结果中应包含钢剑");

            // GetItemsByAttribute - 精确匹配
            var ironItems = container.GetItemsByAttribute("Material", "Iron");
            Assert.AreEqual(1, ironItems.Count, $"应找到 1 个材质为 Iron 的物品，实际: {ironItems.Count} 个");
            Assert.AreEqual("iron_sword", ironItems[0].item.ID, $"该物品应为铁剑，实际: {ironItems[0].item.ID}");

            // GetItemsWhere - 自定义条件
            var highDamageWeapons = container.GetItemsWhere(item =>
                item.Type == "Weapon" &&
                item.GetCustomData<int>("Damage", 0) >= 15);

            Assert.AreEqual(1, highDamageWeapons.Count, $"应找到 1 个高伤武器，实际: {highDamageWeapons.Count} 个");
            Assert.AreEqual("steel_sword", highDamageWeapons[0].item.ID, $"高伤武器应为钢剑，实际: {highDamageWeapons[0].item.ID}");

            // GetItemsByName
            var potions = container.GetItemsByName("药水");
            Assert.AreEqual(2, potions.Count, $"应找到 2 条药水记录，实际: {potions.Count}");
            Assert.AreEqual(8, potions.Sum(p => p.count), $"药水总数应为 8，实际: {potions.Sum(p => p.count)}");
        }

        [Test]
        public void Test_ContainerEvents()
        {
            // 创建容器并注册
            var manager = CreateInitializedInventoryService();
            var container = new LinerContainer("event_container", "事件测试容器", "Backpack", 5);
            manager.RegisterContainer(container);

            // 事件统计
            int itemAddResultCount = 0;
            int itemRemoveResultCount = 0;
            int addSuccessCount = 0;
            int addFailCount = 0;
            int removeSuccessCount = 0;
            int removeFailCount = 0;
            int slotCountChangedCount = 0;
            int itemTotalCountChangedCount = 0;

            // 注册统一事件监听
            container.OnItemAddResult += (item, requestedCount, actualCount, result, slots) =>
            {
                itemAddResultCount++;
                if (result == AddItemResult.Success)
                {
                    addSuccessCount++;
                }
                else
                {
                    addFailCount++;
                }
            };

            container.OnItemRemoveResult += (itemId, requestedCount, actualCount, result, slots) =>
            {
                itemRemoveResultCount++;
                if (result == RemoveItemResult.Success)
                {
                    removeSuccessCount++;
                }
                else
                {
                    removeFailCount++;
                }
            };

            container.OnSlotCountChanged += (slotIndex, item, oldCount, newCount) => { slotCountChangedCount++; };
            container.OnItemTotalCountChanged += (itemId, item, oldCount, newCount) => { itemTotalCountChangedCount++; };

            // 添加
            var item = CreateTestItem("event_item", "事件测试物品", true, 10);
            var nonExistentItem = "non_existent";

            // 添加成功事件
            item.Count = 5;
            container.AddItems(item);
            Assert.AreEqual(1, addSuccessCount, $"添加成功应为 1 次，实际: {addSuccessCount} 次");
            Assert.AreEqual(1, itemAddResultCount, $"OnItemAddResult 应为 1 次，实际: {itemAddResultCount} 次");
            Assert.AreEqual(1, slotCountChangedCount, $"OnSlotCountChanged 应为 1 次，实际: {slotCountChangedCount} 次");
            Assert.AreEqual(1, itemTotalCountChangedCount, $"OnItemTotalCountChanged 应为 1 次，实际: {itemTotalCountChangedCount} 次");

            // 添加失败事件
            container.AddItems((IItem)null);
            Assert.AreEqual(1, addFailCount, $"添加失败应为 1 次，实际: {addFailCount} 次");
            Assert.AreEqual(2, itemAddResultCount, $"OnItemAddResult 应为 2 次，实际: {itemAddResultCount} 次");

            // 重置计数
            slotCountChangedCount = 0;
            itemTotalCountChangedCount = 0;

            // 移除成功事件
            container.RemoveItem("event_item", 2);
            Assert.AreEqual(1, removeSuccessCount, $"移除成功应为 1 次，实际: {removeSuccessCount} 次");
            Assert.AreEqual(1, itemRemoveResultCount, $"OnItemRemoveResult 应为 1 次，实际: {itemRemoveResultCount} 次");
            Assert.AreEqual(1, slotCountChangedCount, $"OnSlotCountChanged 应为 1 次，实际: {slotCountChangedCount} 次");
            Assert.AreEqual(1, itemTotalCountChangedCount, $"OnItemTotalCountChanged 应为 1 次，实际: {itemTotalCountChangedCount} 次");

            // 移除失败事件
            container.RemoveItem(nonExistentItem);
            Assert.AreEqual(1, removeFailCount, $"移除失败应为 1 次，实际: {removeFailCount} 次");
            Assert.AreEqual(2, itemRemoveResultCount, $"OnItemRemoveResult 应为 2 次，实际: {itemRemoveResultCount} 次");

            // 数量变化事件 - 移除剩余
            slotCountChangedCount = 0;
            itemTotalCountChangedCount = 0;
            container.RemoveItem("event_item", 3); // 移除剩余全部
            Assert.AreEqual(1, slotCountChangedCount, $"OnSlotCountChanged 应为 1 次，实际: {slotCountChangedCount} 次");
            Assert.AreEqual(1, itemTotalCountChangedCount, $"OnItemTotalCountChanged 应为 1 次，实际: {itemTotalCountChangedCount} 次");
            Assert.AreEqual(2, removeSuccessCount, $"移除成功应为 2 次，实际: {removeSuccessCount} 次");
        }

        [Test]
        public void Test_FullAndEmptyStates()
        {
            // 有限容器
            var container = new LinerContainer("state_container", "状态测试容器", "Backpack", 3);

            // 初始应为空且不满
            container.RebuildCaches();
            Assert.IsTrue(container.IsEmpty(), "容器应为空");
            Assert.IsFalse(container.Full, "容器应不为满");

            // 添加一个不可堆叠物品
            var item = CreateTestItem("test_item", "测试物品", false);
            container.AddItems(item);
            Assert.IsFalse(container.IsEmpty(), "添加物品后容器应不为空");
            Assert.IsFalse(container.Full, "仅有一个物品容器不应为满");

            // 添加至满
            container.AddItems(item.Clone());
            container.AddItems(item.Clone());
            Assert.IsTrue(container.Full, "添加至容量后容器应为满");

            // 移除全部
            for (int i = 0; i < 3; i++)
            {
                container.RemoveItemAtIndex(0);
            }

            // 可堆叠物品与"满"状态
            var stackableContainer = new LinerContainer("stackable_container", "堆叠测试容器", "Backpack", 2);
            var stackableItem = CreateTestItem("stackable", "可堆叠物品", true, 5);

            // 填满两个槽位
            stackableItem.Count = 5;
            stackableContainer.AddItems(stackableItem);
            stackableItem.Count = 5;
            stackableContainer.AddItems(stackableItem);

            // 此时两个槽位均达上限 -> 满
            Assert.IsTrue(stackableContainer.Full, "所有槽位达到堆叠上限时容器应为满");

            // 移除1个 -> 有剩余堆叠空间 -> 不满
            stackableContainer.RemoveItem("stackable", 1);
            Assert.IsFalse(stackableContainer.Full, "有任一槽位可继续堆叠时不应为满");
        }

        [Test]
        public void Test_GetItemSlotIndices()
        {
            // 创建容器
            var container = new LinerContainer("slot_index_container", "槽位索引测试容器", "Backpack", 10);

            // 添加同一物品至多个槽位
            var item = CreateTestItem("test_item", "测试物品", true, 5);
            item.Count = 5;
            container.AddItems(item);      // 槽位0
            item.Count = 5;
            container.AddItems(item);      // 槽位1
            item.Count = 3;
            container.AddItems(item);      // 槽位2

            // FindSlotIndices
            var indices = container.FindSlotIndices("test_item");
            Assert.AreEqual(3, indices.Count, $"应找到 3 个该物品槽位，实际: {indices.Count} 个");
            Assert.IsTrue(indices.Contains(0) && indices.Contains(1) && indices.Contains(2),
                    "应包含槽位 0,1,2");

            // FindFirstSlotIndex
            var firstIndex = container.FindFirstSlotIndex("test_item");
            Assert.AreEqual(0, firstIndex, $"第一个槽位应为 0，实际: {firstIndex}");

            // 不存在的物品
            var nonExistentIndices = container.FindSlotIndices("non_existent");
            Assert.AreEqual(0, nonExistentIndices.Count, "不存在物品的槽位列表应为空");

            var nonExistentFirstIndex = container.FindFirstSlotIndex("non_existent");
            Assert.AreEqual(-1, nonExistentFirstIndex, "不存在物品的第一个槽位应为 -1");

            // 清空一个槽位后再次查询
            container.RemoveItemAtIndex(1, 5);

            var indicesSkipEmpty = container.FindSlotIndices("test_item");
            Assert.AreEqual(2, indicesSkipEmpty.Count, "应找到 2 个槽位");
            Assert.IsTrue(indicesSkipEmpty.Contains(0) && indicesSkipEmpty.Contains(2),
                    "应仅包含槽位 0 与 2");
        }

        [Test]
        public void Test_FindItemsByDifferentCriteria()
        {
            // 创建容器
            var container = new LinerContainer("find_container", "查找测试容器", "Backpack", 10);

            // 添加不同物品
            var woodenSword = CreateTestItem("wooden_sword", "木剑", false, 1, "Weapon");
            woodenSword.SetCustomData("Material", "Wood");
            woodenSword.SetCustomData("Damage", 5);
            woodenSword.SetCustomData("Rarity", "Common");
            woodenSword.Weight = 3.0f;

            var ironSword = CreateTestItem("iron_sword", "铁剑", false, 1, "Weapon");
            ironSword.SetCustomData("Material", "Iron");
            ironSword.SetCustomData("Damage", 10);
            ironSword.SetCustomData("Rarity", "Uncommon");
            ironSword.Weight = 5.0f;

            var leatherArmor = CreateTestItem("leather_armor", "皮甲", false, 1, "Armor");
            leatherArmor.SetCustomData("Material", "Leather");
            leatherArmor.SetCustomData("Defense", 8);
            leatherArmor.SetCustomData("Rarity", "Common");
            leatherArmor.Weight = 8.0f;

            var apple = CreateTestItem("apple", "苹果", true, 10, "Food");
            apple.SetCustomData("Healing", 2);
            apple.Weight = 0.5f;

            // 添加
            container.AddItems(woodenSword);
            container.AddItems(ironSword);
            container.AddItems(leatherArmor);
            apple.Count = 5;
            container.AddItems(apple);

            // 按类型
            var weapons = container.GetItemsByType("Weapon");
            Assert.AreEqual(2, weapons.Count, $"应找到 2 个武器，实际: {weapons.Count} 个");
            Assert.IsTrue(weapons.Any(w => w.item.ID == "wooden_sword") && weapons.Any(w => w.item.ID == "iron_sword"),
                    "结果应包含木剑与铁剑");

            // 按属性值
            var commonItems = container.GetItemsByAttribute("Rarity", "Common");
            Assert.AreEqual(2, commonItems.Count, $"应找到 2 个普通物品，实际: {commonItems.Count} 个");
            Assert.IsTrue(commonItems.Any(i => i.item.ID == "wooden_sword") && commonItems.Any(i => i.item.ID == "leather_armor"),
                    "普通物品应包含木剑与皮甲");

            // 按属性存在性
            var materialItems = container.GetItemsByAttribute("Material", null);
            Assert.AreEqual(3, materialItems.Count, $"应找到 3 个包含 Material 属性的物品，实际: {materialItems.Count} 个");

            // 自定义条件：重量
            var heavyItems = container.GetItemsWhere(item => item.Weight >= 5.0f);
            Assert.AreEqual(2, heavyItems.Count, $"应找到 2 个较重物品，实际: {heavyItems.Count} 个");
            Assert.IsTrue(heavyItems.Any(i => i.item.ID == "iron_sword") && heavyItems.Any(i => i.item.ID == "leather_armor"),
                    "较重物品应包含铁剑与皮甲");

            // 名称查询
            var swordItems = container.GetItemsByName("剑");
            Assert.AreEqual(2, swordItems.Count, $"应找到 2 条包含\"剑\"的记录，实际: {swordItems.Count}");

            // 组合条件
            var ironWeapons = container.GetItemsWhere(item =>
                item.Type == "Weapon" &&
                item.GetCustomData<string>("Material", "") == "Iron");
            Assert.AreEqual(1, ironWeapons.Count, $"应找到 1 条铁制武器，实际: {ironWeapons.Count}");
            Assert.AreEqual("iron_sword", ironWeapons[0].item.ID, "铁制武器应为铁剑");
        }

        [Test]
        public void Test_ItemTotalCount()
        {
            // 创建容器
            var container = new LinerContainer("count_container", "计数测试容器", "Backpack", 10);

            // 可堆叠物品
            var apple = CreateTestItem("apple", "苹果", true, 10);

            // 初始为 0
            Assert.AreEqual(0, container.GetItemTotalCount("apple"), "苹果总数应为 0");

            // 添加到多个槽位
            apple.Count = 8;
            container.AddItems(apple);  // 槽位0: 8
            apple.Count = 15;
            container.AddItems(apple); // 槽位1: 10, 槽位2: 5

            // 总数
            Assert.AreEqual(23, container.GetItemTotalCount("apple"), $"苹果总数应为 23，实际: {container.GetItemTotalCount("apple")}");

            // HasEnoughItems
            Assert.IsTrue(container.HasEnoughItems("apple", 20), "应有足够苹果(>=20)");
            Assert.IsFalse(container.HasEnoughItems("apple", 30), "不应有足够苹果(>=30)");
            Assert.IsFalse(container.HasEnoughItems("banana", 1), "不存在的物品应不足");

            // 移除部分
            container.RemoveItem("apple", 10);
            Assert.AreEqual(13, container.GetItemTotalCount("apple"), $"移除后苹果总数应为 13，实际: {container.GetItemTotalCount("apple")}");

            // GetUniqueItemCount
            var banana = CreateTestItem("banana", "香蕉", true, 10);
            banana.Count = 5;
            container.AddItems(banana);
            Assert.AreEqual(2, container.GetUniqueItemCount(), $"应为 2 种不同物品，实际: {container.GetUniqueItemCount()}");

            // 移除全部苹果
            container.RemoveItem("apple", 13);
            Assert.AreEqual(0, container.GetItemTotalCount("apple"), $"移除全部后苹果总数应为 0，实际: {container.GetItemTotalCount("apple")}");
            Assert.AreEqual(1, container.GetUniqueItemCount(), $"应为 1 种不同物品，实际: {container.GetUniqueItemCount()}");
        }

        [Test]
        public void Test_GetAllItemCounts()
        {
            // 创建容器
            var container = new LinerContainer("all_counts_container", "总计数测试容器", "Backpack", 10);

            // 空容器
            var emptyCounts = container.GetAllItemCountsDict();
            Assert.AreEqual(0, emptyCounts.Count, "空容器的计数字典应为空");

            // 添加多种
            var apple = CreateTestItem("apple", "苹果", true, 10);
            var banana = CreateTestItem("banana", "香蕉", true, 10);
            var sword = CreateTestItem("sword", "剑", false);

            apple.Count = 15;
            container.AddItems(apple); // 分散到多个槽位
            banana.Count = 7;
            container.AddItems(banana);
            container.AddItems(sword);

            // GetAllItemCounts
            var allCounts = container.GetAllItemCountsDict();
            Assert.AreEqual(3, allCounts.Count, $"应有 3 条记录，实际: {allCounts.Count}");
            Assert.AreEqual(15, allCounts["apple"], $"苹果总数应为 15，实际: {allCounts["apple"]}");
            Assert.AreEqual(7, allCounts["banana"], $"香蕉总数应为 7，实际: {allCounts["banana"]}");
            Assert.AreEqual(1, allCounts["sword"], $"剑数量应为 1，实际: {allCounts["sword"]}");

            // GetAllItems
            var allItems = container.GetAllItems();
            Assert.AreEqual(4, allItems.Count, $"应有 4 条物品记录（分散导致多个槽位），实际: {allItems.Count}");

            // GetTotalWeight
            float expectedWeight = 15 * apple.Weight + 7 * banana.Weight + sword.Weight;
            float actualWeight = container.GetTotalWeight();
            Assert.AreEqual(expectedWeight, actualWeight, $"总重量应为 {expectedWeight}，实际: {actualWeight}");

            // 验证重量计算精度
            Assert.AreEqual(expectedWeight, actualWeight, 0.001f, "重量计算应精确到小数点后3位");
        }

        [Test]
        public void Test_OrganizeInventory()
        {
            // 创建容器并注册
            var manager = CreateInitializedInventoryService();
            var container = new LinerContainer("organize_container", "整理测试容器", "Backpack", 15);
            manager.RegisterContainer(container);

            // 准备分散、多样的物品 - 使用新API
            var apple1 = CreateTestItem("apple", "苹果", true, 10, "Food");
            apple1.Count = 3;
            var sword = CreateTestItem("sword", "剑", false, 1, "Weapon");
            sword.Count = 1;
            var apple2 = CreateTestItem("apple", "苹果", true, 10, "Food");
            apple2.Count = 7;
            var potion1 = CreateTestItem("potion", "药水", true, 20, "Consumable");
            potion1.Count = 15;
            var bread = CreateTestItem("bread", "面包", true, 5, "Food");
            bread.Count = 4;
            var apple3 = CreateTestItem("apple", "苹果", true, 10, "Food");
            apple3.Count = 5;
            var potion2 = CreateTestItem("potion", "药水", true, 20, "Consumable");
            potion2.Count = 8;

            // 故意分散 - 使用新API (item, slotIndex)
            container.AddItems(apple1, slotIndex: 0);      // 0: 苹果 x3
            container.AddItems(sword, slotIndex: 1);       // 1: 剑 x1
            container.AddItems(apple2, slotIndex: 2);      // 2: 苹果 x7
            container.AddItems(potion1, slotIndex: 3);     // 3: 药水 x15
            container.AddItems(bread, slotIndex: 4);       // 4: 面包 x4
            container.AddItems(apple3, slotIndex: 5);      // 5: 苹果 x5
            container.AddItems(potion2, slotIndex: 6);     // 6: 药水 x8

            // 整理前校验
            var appleSlotsBefore = container.FindSlotIndices("apple");
            var potionSlotsBefore = container.FindSlotIndices("potion");
            Assert.AreEqual(3, appleSlotsBefore.Count, $"整理前苹果应分散于 3 个槽位，实际: {appleSlotsBefore.Count}");
            Assert.AreEqual(2, potionSlotsBefore.Count, $"整理前药水应分散于 2 个槽位，实际: {potionSlotsBefore.Count}");
            Assert.AreEqual(15, container.GetItemTotalCount("apple"), $"苹果总数应为 15，实际: {container.GetItemTotalCount("apple")}");
            Assert.AreEqual(23, container.GetItemTotalCount("potion"), $"药水总数应为 23，实际: {container.GetItemTotalCount("potion")}");

            // 执行整理
            container.OrganizeInventory();

            // 合并校验
            var appleSlotsAfter = container.FindSlotIndices("apple");
            var potionSlotsAfter = container.FindSlotIndices("potion");
            Assert.AreEqual(2, appleSlotsAfter.Count, $"整理后苹果应合并为 2 个槽位(10+5)，实际: {appleSlotsAfter.Count}");
            Assert.AreEqual(2, potionSlotsAfter.Count, $"整理后药水应合并为 2 个槽位(20+3)，实际: {potionSlotsAfter.Count}");

            // 数量不变
            Assert.AreEqual(15, container.GetItemTotalCount("apple"), $"整理后苹果总数应为 15，实际: {container.GetItemTotalCount("apple")}");
            Assert.AreEqual(23, container.GetItemTotalCount("potion"), $"整理后药水总数应为 23，实际: {container.GetItemTotalCount("potion")}");
            Assert.AreEqual(4, container.GetItemTotalCount("bread"), $"整理后面包总数应为 4，实际: {container.GetItemTotalCount("bread")}");
            Assert.AreEqual(1, container.GetItemTotalCount("sword"), $"整理后剑总数应为 1，实际: {container.GetItemTotalCount("sword")}");

            // 排序校验（按类型->名称）
            var allItems = container.GetAllItems();
            var occupiedItems = allItems.Where(item => item.item != null).ToList();

            bool sortedCorrectly = true;
            for (int i = 0; i < occupiedItems.Count - 1; i++)
            {
                var current = occupiedItems[i].item;
                var next = occupiedItems[i + 1].item;

                int typeCompare = string.Compare(current.Type, next.Type);
                if (typeCompare > 0)
                {
                    sortedCorrectly = false;
                    break;
                }
                else if (typeCompare == 0)
                {
                    // 同类型按名称
                    if (string.Compare(current.Name, next.Name) > 0)
                    {
                        sortedCorrectly = false;
                        break;
                    }
                }
            }
            Assert.IsTrue(sortedCorrectly, "整理后物品应按类型与名称正确排序");

            // 堆叠不超上限
            foreach (var it in occupiedItems)
            {
                if (it.item.IsStackable && it.item.MaxStackCount > 0)
                {
                    Assert.GreaterOrEqual(it.item.MaxStackCount, it.count,
                        $"物品 {it.item.Name} 数量({it.count})不应超过堆叠上限({it.item.MaxStackCount})");
                }
            }
        }

        // ============= US1 测试 - 基础物品添加到容器 =============

        /// <summary>
        /// T019 [US1] 测试添加带数量的物品到容器
        /// Given: Item(ID="apple", Count=5, UID=-1)
        /// When: container.AddItems(item)
        /// Then: Item在槽位中，UID已分配，slot.Item.Count == 5
        /// </summary>
        [Test]
        public void Test_US1_AddItemWithCount()
        {
            var manager = CreateInitializedInventoryService();
            var container = new LinerContainer("us1_test", "US1测试容器", "Backpack", 5);
            manager.RegisterContainer(container); // 注册容器以启用UID分配
            
            // 创建带数量的物品
            var apple = CreateTestItem("apple", "苹果", true, 10);
            apple.Count = 5; // 设置物品数量
            
            // 验证添加前UID为-1
            Assert.AreEqual(-1, apple.ItemUID, "添加前UID应为-1");
            
            // 添加物品到容器
            var (result, addedCount) = container.AddItems(apple);
            
            // 验证添加结果
            Assert.AreEqual(AddItemResult.Success, result, "添加应成功");
            Assert.AreEqual(5, addedCount, "应添加5个物品");
            
            // 验证槽位状态
            Assert.IsTrue(container.Slots[0].IsOccupied, "槽位应被占用");
            Assert.AreEqual("apple", container.Slots[0].Item.ID, "槽位物品应为apple");
            Assert.AreEqual(5, container.Slots[0].Item.Count, "槽位物品数量应为5");
            Assert.AreNotEqual(-1, container.Slots[0].Item.ItemUID, "UID应已分配");
        }

        /// <summary>
        /// T020 [US1] 测试查询物品数量
        /// Given: Item(Count=5)在容器槽位1
        /// When: 查询 slot.Item.Count
        /// Then: 返回 5
        /// </summary>
        [Test]
        public void Test_US1_QueryItemCount()
        {
            var container = new LinerContainer("us1_query", "US1查询测试", "Backpack", 5);
            
            var potion = CreateTestItem("potion", "药水", true, 20);
            potion.Count = 5;
            
            // 添加到指定槽位1 - 使用新API
            container.AddItems(potion, slotIndex: 1);
            
            // 验证通过slot.Item.Count查询数量
            Assert.IsTrue(container.Slots[1].IsOccupied, "槽位1应被占用");
            Assert.AreEqual(5, container.Slots[1].Item.Count, "通过slot.Item.Count查询应返回5");
            
            // 验证通过容器方法查询
            Assert.AreEqual(5, container.GetItemTotalCount("potion"), "容器中药水总数应为5");
        }

        /// <summary>
        /// T021 [US1] 测试不可堆叠物品验证
        /// Given: Item(IsStackable=false, Count=1)
        /// When: Container验证添加请求
        /// Then: 验证通过（每槽Count=1）
        /// </summary>
        [Test]
        public void Test_US1_NonStackableValidation()
        {
            var container = new LinerContainer("us1_nonstackable", "US1不可堆叠测试", "Backpack", 5);
            
            // 创建不可堆叠物品
            var sword = CreateTestItem("sword", "剑", false, 1, "Weapon");
            sword.Count = 1;
            
            // 添加不可堆叠物品
            var (result1, count1) = container.AddItems(sword);
            Assert.AreEqual(AddItemResult.Success, result1, "添加不可堆叠物品应成功");
            Assert.AreEqual(1, count1, "应添加1个物品");
            Assert.AreEqual(1, container.Slots[0].Item.Count, "不可堆叠物品Count应为1");
            
            // 再次添加同类型物品应到新槽位
            var sword2 = CreateTestItem("sword", "剑", false, 1, "Weapon");
            sword2.Count = 1;
            var (result2, count2) = container.AddItems(sword2);
            Assert.AreEqual(AddItemResult.Success, result2, "添加第二把剑应成功");
            Assert.AreEqual(1, count2, "应添加1个物品");
            Assert.IsTrue(container.Slots[1].IsOccupied, "第二把剑应在槽位1");
            Assert.AreEqual("sword", container.Slots[1].Item.ID, "槽位1物品应为sword");
        }

        // ============= US2 测试 - 可堆叠物品的堆叠合并 =============

        /// <summary>
        /// T024 [US2] 测试堆叠合并
        /// Given: 槽位A包含Item(ID="potion", Count=5, UID=100)
        /// When: 添加Item(ID="potion", Count=3, UID=-1)
        /// Then: 槽位A的Item.Count == 8，UID保持100
        /// </summary>
        [Test]
        public void Test_US2_StackMerge()
        {
            var manager = CreateInitializedInventoryService();
            var container = new LinerContainer("us2_stack", "US2堆叠测试", "Backpack", 5);
            manager.RegisterContainer(container);
            
            // 添加第一批药水
            var potion1 = CreateTestItem("potion", "药水", true, 20);
            potion1.Count = 5;
            container.AddItems(potion1);
            
            // 记录原始UID
            var originalUID = container.Slots[0].Item.ItemUID;
            Assert.AreNotEqual(-1, originalUID, "原始UID应已分配");
            
            // 添加第二批药水
            var potion2 = CreateTestItem("potion", "药水", true, 20);
            potion2.Count = 3;
            var (result, addedCount) = container.AddItems(potion2);
            
            // 验证堆叠结果
            Assert.AreEqual(AddItemResult.Success, result, "堆叠应成功");
            Assert.AreEqual(3, addedCount, "应添加3个物品");
            Assert.AreEqual(8, container.Slots[0].Item.Count, "合并后数量应为8");
            Assert.AreEqual(originalUID, container.Slots[0].Item.ItemUID, "UID应保持不变");
            
            // 验证只使用了一个槽位
            Assert.IsFalse(container.Slots[1].IsOccupied, "槽位1应为空（物品已堆叠）");
        }

        /// <summary>
        /// T025 [US2] 测试堆叠溢出
        /// Given: 槽位A包含Item(Count=98, MaxStackCount=99)
        /// When: 添加Item(Count=5)
        /// Then: 槽位A填满99，槽位B包含4
        /// </summary>
        [Test]
        public void Test_US2_MaxStackCountOverflow()
        {
            var manager = CreateInitializedInventoryService();
            var container = new LinerContainer("us2_overflow", "US2溢出测试", "Backpack", 5);
            manager.RegisterContainer(container);
            
            // 添加接近满的物品堆
            var potion1 = CreateTestItem("potion", "药水", true, 99);
            potion1.Count = 98;
            container.AddItems(potion1);
            
            Assert.AreEqual(98, container.Slots[0].Item.Count, "初始数量应为98");
            
            // 添加超过容量的物品
            var potion2 = CreateTestItem("potion", "药水", true, 99);
            potion2.Count = 5;
            var (result, addedCount) = container.AddItems(potion2);
            
            // 验证溢出处理
            Assert.AreEqual(AddItemResult.Success, result, "添加应成功");
            Assert.AreEqual(5, addedCount, "应添加5个物品");
            Assert.AreEqual(99, container.Slots[0].Item.Count, "槽位A应填满99");
            Assert.IsTrue(container.Slots[1].IsOccupied, "溢出部分应在槽位B");
            Assert.AreEqual(4, container.Slots[1].Item.Count, "槽位B应包含4个物品");
            
            // 验证总数正确
            Assert.AreEqual(103, container.GetItemTotalCount("potion"), "总数应为103");
        }

        /// <summary>
        /// T026 [US2] 测试RuntimeMetadata不匹配时不堆叠
        /// Given: 槽位A包含Item(RuntimeMetadata={"durability":100})
        /// When: 添加Item(RuntimeMetadata={"durability":80})
        /// Then: 不堆叠，分别存放
        /// </summary>
        [Test]
        public void Test_US2_RuntimeMetadataMismatch()
        {
            var manager = CreateInitializedInventoryService();
            var container = new LinerContainer("us2_metadata", "US2元数据测试", "Backpack", 5);
            manager.RegisterContainer(container);
            
            // 使用ItemFactory创建物品以确保正确注册到CategoryManager
            var potionData = new ItemData
            {
                ID = "potion",
                Name = "药水",
                IsStackable = true,
                MaxStackCount = 20,
                Category = "Consumable.Potion"
            };
            manager.ItemFactory.Register("potion", potionData);
            
            // 添加第一个物品
            var potion1 = manager.ItemFactory.CreateItem("potion", 5);
            container.AddItems(potion1);
            
            // 为第一个物品设置RuntimeMetadata
            var slot0Item = container.Slots[0].Item;
            Assert.IsNotNull(slot0Item, "槽位0应有物品");
            slot0Item.SetCustomData("durability", 100);
            
            // 添加第二个物品（由于第一个有metadata，第二个没有，不应堆叠）
            var potion2 = manager.ItemFactory.CreateItem("potion", 3);
            var (result, addedCount) = container.AddItems(potion2);
            
            // 验证添加成功（由于metadata不匹配，不堆叠，放入新槽位）
            Assert.AreEqual(AddItemResult.Success, result, "添加应成功");
            Assert.AreEqual(3, addedCount, "应添加3个物品");
            
            // 验证分别存放在不同槽位
            Assert.AreEqual(5, container.Slots[0].Item.Count, "槽位A数量应保持5");
            Assert.IsTrue(container.Slots[1].IsOccupied, "槽位B应被占用");
            Assert.AreEqual(3, container.Slots[1].Item.Count, "槽位B数量应为3");
            
            // 为第二个物品设置不同的RuntimeMetadata
            var slot1Item = container.Slots[1].Item;
            Assert.IsNotNull(slot1Item, "槽位1应有物品");
            slot1Item.SetCustomData("durability", 80);
            
            // 验证元数据独立
            Assert.AreEqual(100, container.Slots[0].Item.RuntimeMetadata["durability"].IntValue, "槽位A元数据应保持100");
            Assert.AreEqual(80, container.Slots[1].Item.RuntimeMetadata["durability"].IntValue, "槽位B元数据应为80");
            
            // 验证总数正确
            Assert.AreEqual(8, container.GetItemTotalCount("potion"), "总数应为8");
        }

        // 创建测试物品
        private Item CreateTestItem(string id, string name, bool isStackable, int maxStackCount = 1, string type = "Default")
        {
            var item = new Item
            {
                ID = id,
                Name = name,
                IsStackable = isStackable,
                MaxStackCount = isStackable ? maxStackCount : 1,
                Type = type,
                Weight = 1.0f
            };
            return item;
        }
    }
}
