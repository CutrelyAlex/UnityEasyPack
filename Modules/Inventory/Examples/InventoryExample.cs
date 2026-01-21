using System.Collections.Generic;
using System.Threading.Tasks;
using EasyPack.Architecture;
using EasyPack.ENekoFramework;
using EasyPack.Serialization;
using UnityEngine;

namespace EasyPack.InventorySystem.Example
{
    /// <summary>
    ///     展示背包系统的使用示例
    /// </summary>
    public class InventoryExample : MonoBehaviour
    {
        private IInventoryService _inventoryService;

        private async void Start()
        {
            // 初始化 InventoryService
            _inventoryService = await EasyPackArchitecture.GetInventoryServiceAsync();
            Debug.Log($"InventoryService 初始化完成，状态: {_inventoryService.State}");

            // 测试
            // Test();
            // 启动实际使用案例展示
            await ShowInventoryUseCases();
        }


        private async Task ShowInventoryUseCases()
        {
            Debug.Log("===== 背包系统使用案例展示 =====");

            // 案例0: 从 EasyPackArchitecture 获取 InventoryService
            await ShowArchitectureServiceAccess();

            // 案例1: 创建玩家背包和各种容器
            ShowContainerCreation();

            // 案例2: 向背包中添加物品
            ShowAddingItems();

            // 案例3: 展示物品堆叠系统
            ShowItemStacking();

            // 案例4: 物品在容器间移动(如从背包移动到储物箱)
            ShowItemTransfer();

            // 案例5: 物品使用和移除
            ShowItemUsage();

            // 案例6: 背包整理功能
            ShowInventorySorting();

            // 案例7: 序列化与反序列化
            await ShowInventorySerialization();

            // 新增扩展示例（源于测试脚本中已有但示例未覆盖的功能）
            ShowOrganizeInventory(); // 案例8: OrganizeInventory 合并+排序
            ShowAdvancedQueriesAndStatistics(); // 案例9: 各类查询与统计
            ShowInventoryManagerBasics(); // 案例10: 容器注册/分类/优先级
            ShowGlobalConditionsDemo(); // 案例11: 全局条件启用/禁用
            ShowCrossContainerAdvancedOps(); // 案例12: Transfer / AutoMove / Batch / Distribute
            ShowGlobalSearchDemo(); // 案例13: 全局搜索与统计
            await ShowConditionAndAttributeSerialization(); // 案例14: 条件 + 属性序列化
            ShowSlotAndCapacityEdgeCases(); // 案例15: 指定槽位/容量/满与空


            Debug.Log("===== 背包系统使用案例展示完成 =====");
        }

        // 案例0: 从 EasyPackArchitecture 获取 InventoryService
        private async Task ShowArchitectureServiceAccess()
        {
            Debug.Log("案例0: 从 EasyPackArchitecture 获取 InventoryService");

            // 获取库存服务实例
            IInventoryService inventoryService = await EasyPackArchitecture.GetInventoryServiceAsync();
            Debug.Log($"成功获取 InventoryService，当前状态: {inventoryService.State}");

            // 如果未初始化，则进行初始化
            if (inventoryService.State == ServiceLifecycleState.Uninitialized)
            {
                await inventoryService.InitializeAsync();
                Debug.Log("InventoryService 初始化完成");
            }

            // 创建并注册容器到服务
            var testBackpack = new LinerContainer("arch_test_backpack", "架构测试背包", "Backpack", 10);
            inventoryService.RegisterContainer(testBackpack, 1, "Test");
            Debug.Log($"已注册容器到服务，当前容器数量: {inventoryService.ContainerCount}");

            // 创建物品并添加到容器
            Item testItem = CreateGameItem("test_item", "测试物品", true, 10, "Item");
            testItem.Count = 5;
            testBackpack.AddItems(testItem);
            Debug.Log("已向容器添加物品");

            // 全局搜索测试
            var searchResults = inventoryService.FindItemGlobally("test_item");
            Debug.Log($"全局搜索找到 {searchResults.Count} 个位置包含该物品");

            // 清理测试容器
            inventoryService.UnregisterContainer("arch_test_backpack");
            Debug.Log("测试容器已清理\n");
        }


        // 案例1: 创建不同类型的容器
        private void ShowContainerCreation()
        {
            Debug.Log("案例1: 创建玩家背包和各种容器");

            // 创建标准玩家背包 - 有限容量
            var playerBackpack = new LinerContainer("player_backpack", "冒险者背包", "Backpack", 20);
            Debug.Log($"创建了玩家背包: {playerBackpack.Name} (容量: {playerBackpack.Capacity}格)");

            // 创建装备栏 - 有特殊物品类型限制
            var equipmentBag = new LinerContainer("player_equipment", "装备栏", "Equipment", 5);
            equipmentBag.ContainerCondition.Add(new ItemTypeCondition("Equipment"));
            Debug.Log($"创建了装备栏: {equipmentBag.Name} (仅接受类型为'Equipment'的物品)");

            // 创建无限大小的储物箱 - 用于玩家住宅存储物品
            var storageChest = new LinerContainer("house_storage", "储物箱", "Storage");
            Debug.Log($"创建了储物箱: {storageChest.Name} (无限容量)");

            Debug.Log("容器创建完成，可用于存储不同类型的物品\n");
        }

        // 案例2: 向背包中添加各类物品
        private void ShowAddingItems()
        {
            Debug.Log("案例2: 向背包中添加物品");

            // 创建玩家背包
            var playerBackpack = new LinerContainer("player_backpack", "冒险者背包", "Backpack", 10);

            // 创建各种游戏物品
            Item healthPotion = CreateGameItem("health_potion", "生命药水", true, 20, "Consumable");
            Item woodSword = CreateGameItem("wood_sword", "木剑", false, 1, "Equipment");
            Item goldCoin = CreateGameItem("gold_coin", "金币", true, 999, "Currency");

            // 向背包中添加物品
            healthPotion.Count = 5;
            (AddItemResult result, _) = playerBackpack.AddItems(healthPotion);
            Debug.Log($"添加5瓶生命药水: {(result == AddItemResult.Success ? "成功" : "失败")}");

            (AddItemResult result, int actualCount) swordResult = playerBackpack.AddItems(woodSword);
            Debug.Log($"添加木剑: {(swordResult.result == AddItemResult.Success ? "成功" : "失败")}");

            goldCoin.Count = 100;
            (AddItemResult result, int actualCount) coinResult = playerBackpack.AddItems(goldCoin);
            Debug.Log($"添加100金币: {(coinResult.result == AddItemResult.Success ? "成功" : "失败")}");

            // 显示背包内容
            DisplayInventoryContents(playerBackpack);
            Debug.Log("");
        }

        // 案例3: 展示物品堆叠系统
        private void ShowItemStacking()
        {
            Debug.Log("案例3: 物品堆叠系统");

            var playerBackpack = new LinerContainer("player_backpack", "冒险者背包", "Backpack", 10);

            // 创建可堆叠物品
            Item arrow = CreateGameItem("arrow", "箭矢", true, 99, "Ammunition");

            // 第一次添加50支箭矢
            arrow.Count = 50;
            playerBackpack.AddItems(arrow);
            Debug.Log("添加了50支箭矢到背包中");
            DisplayInventoryContents(playerBackpack);

            // 再添加60支箭矢 (会自动堆叠到99上限，剩余的放入新格子)
            arrow.Count = 60;
            playerBackpack.AddItems(arrow);
            Debug.Log("又添加了60支箭矢到背包中");
            Debug.Log("由于单格堆叠上限为99，系统自动将箭矢分配到多个格子中");
            DisplayInventoryContents(playerBackpack);

            // 创建不可堆叠物品
            Item uniqueScroll = CreateGameItem("unique_scroll", "稀有卷轴", false, 1, "Quest");

            // 添加多个不可堆叠物品
            playerBackpack.AddItems(uniqueScroll);
            playerBackpack.AddItems(uniqueScroll.Clone());
            Debug.Log("添加了两个相同的稀有卷轴(不可堆叠物品)");
            DisplayInventoryContents(playerBackpack);
            Debug.Log("");
        }

        // 案例4: 物品在容器间的转移
        private void ShowItemTransfer()
        {
            Debug.Log("案例4: 物品在容器间的转移");

            // 创建两个容器代表背包和储物箱
            var playerBackpack = new LinerContainer("player_backpack", "冒险者背包", "Backpack", 10);
            var storageChest = new LinerContainer("storage_chest", "储物箱", "Storage", 20);

            // 向背包添加一些物品
            Item ore = CreateGameItem("iron_ore", "铁矿石", true, 50, "Material");
            Item gem = CreateGameItem("ruby", "红宝石", true, 10, "Gem");
            Item helmet = CreateGameItem("leather_helmet", "皮革头盔", false, 1, "Equipment");

            ore.Count = 40;
            playerBackpack.AddItems(ore);
            gem.Count = 5;
            playerBackpack.AddItems(gem);
            playerBackpack.AddItems(helmet);

            Debug.Log("背包初始内容:");
            DisplayInventoryContents(playerBackpack);

            Debug.Log("储物箱初始内容:");
            DisplayInventoryContents(storageChest);

            // 将物品从背包移动到储物箱
            bool moveOreResult = playerBackpack.MoveItemToContainer(0, storageChest);
            Debug.Log($"将铁矿石从背包移动到储物箱: {(moveOreResult ? "成功" : "失败")}");

            bool moveHelmetResult = playerBackpack.MoveItemToContainer(2, storageChest);
            Debug.Log($"将皮革头盔从背包移动到储物箱: {(moveHelmetResult ? "成功" : "失败")}");

            Debug.Log("移动后背包内容:");
            DisplayInventoryContents(playerBackpack);

            Debug.Log("移动后储物箱内容:");
            DisplayInventoryContents(storageChest);
            Debug.Log("");
        }

        // 案例5: 物品使用和移除
        private void ShowItemUsage()
        {
            Debug.Log("案例5: 物品使用和移除");

            var playerBackpack = new LinerContainer("player_backpack", "冒险者背包", "Backpack", 10);

            // 添加一些物品
            Item healthPotion = CreateGameItem("health_potion", "生命药水", true, 20, "Consumable");
            Item manaPotion = CreateGameItem("mana_potion", "魔法药水", true, 20, "Consumable");
            Item bread = CreateGameItem("bread", "面包", true, 10, "Food");

            healthPotion.Count = 5;
            playerBackpack.AddItems(healthPotion);
            manaPotion.Count = 3;
            playerBackpack.AddItems(manaPotion);
            bread.Count = 7;
            playerBackpack.AddItems(bread);

            Debug.Log("初始背包内容:");
            DisplayInventoryContents(playerBackpack);

            // 使用物品(模拟)
            Debug.Log("玩家使用了1瓶生命药水...");
            RemoveItemResult useHealthPotion = playerBackpack.RemoveItem("health_potion");
            Debug.Log($"使用结果: {(useHealthPotion == RemoveItemResult.Success ? "成功" : "失败")}");

            // 使用多个物品
            Debug.Log("玩家吃了3个面包...");
            RemoveItemResult useBread = playerBackpack.RemoveItem("bread", 3);
            Debug.Log($"使用结果: {(useBread == RemoveItemResult.Success ? "成功" : "失败")}");

            // 丢弃物品
            Debug.Log("玩家丢弃了所有魔法药水...");
            RemoveItemResult dropManaPotion = playerBackpack.RemoveItem("mana_potion", 3);
            Debug.Log($"丢弃结果: {(dropManaPotion == RemoveItemResult.Success ? "成功" : "失败")}");

            // 过度移除
            Debug.Log("试图移除30个面包...");
            useBread = playerBackpack.RemoveItem("bread", 30);
            Debug.Log($"使用结果: {(useBread == RemoveItemResult.Success ? "成功" : "失败")}");

            Debug.Log("物品使用后的背包内容:");
            DisplayInventoryContents(playerBackpack);
            Debug.Log("");
        }

        // 案例6: 背包整理功能
        private void ShowInventorySorting()
        {
            Debug.Log("案例6: 背包整理功能");

            var playerBackpack = new LinerContainer("player_backpack", "冒险者背包", "Backpack", 15);

            // 添加各种类型的物品，故意打乱顺序
            playerBackpack.AddItems(CreateGameItem("iron_sword", "铁剑", false, 1, "B武器"));
            var apple6 = CreateGameItem("apple", "苹果", true, 10, "D食物");
            apple6.Count = 3;
            playerBackpack.AddItems(apple6);
            playerBackpack.AddItems(CreateGameItem("leather_shield", "皮盾", false, 1, "C防具"));
            var hp6 = CreateGameItem("health_potion", "生命药水", true, 10, "A消耗品");
            hp6.Count = 5;
            playerBackpack.AddItems(hp6);
            var mp6 = CreateGameItem("mana_potion", "魔法药水", true, 10, "A消耗品");
            mp6.Count = 2;
            playerBackpack.AddItems(mp6);
            var gold6 = CreateGameItem("gold_coin", "金币", true, 100, "E货币");
            gold6.Count = 65;
            playerBackpack.AddItems(gold6);
            var silver6 = CreateGameItem("silver_coin", "银币", true, 100, "E货币");
            silver6.Count = 32;
            playerBackpack.AddItems(silver6);

            Debug.Log("整理前的背包内容(随机顺序):");
            DisplayInventoryContents(playerBackpack);

            // 执行背包整理
            Debug.Log("玩家点击了'整理背包'按钮");
            playerBackpack.SortInventory();

            Debug.Log("整理后的背包内容(按物品类型和名称排序):");
            DisplayInventoryContents(playerBackpack);
            Debug.Log("物品已按照类型(A消耗品 > B武器 > C防具 > D食物 > E货币)和名称排序");
        }

        /// <summary>
        ///     案例7: 序列化 / 反序列化（含完整 JSON 与精简 ID 模式）
        /// </summary>
        private async Task ShowInventorySerialization()
        {
            Debug.Log("案例7: 序列化 / 反序列化");

            // 1. 创建一个容器并添加物品
            var bag = new LinerContainer("serialize_demo_bag", "序列化演示背包", "Backpack", 12);
            Item potion = CreateGameItem("health_potion", "生命药水", true, 20, "Consumable");
            Item sword = CreateGameItem("iron_sword", "铁剑", false, 1, "Equipment");
            Item gem = CreateGameItem("ruby_gem", "红宝石", true, 50, "Gem");
            gem.SetCustomData("Quality", "Rare");
            gem.SetCustomData("Power", 12.5f);

            potion.Count = 15;
            bag.AddItems(potion);
            bag.AddItems(sword);
            gem.Count = 7;
            bag.AddItems(gem, slotIndex: 3);

            Debug.Log("原始背包内容：");
            DisplayInventoryContents(bag);

            // 2. 从架构获取序列化服务并序列化
            var serializationService = await EasyPackArchitecture.Instance.ResolveAsync<ISerializationService>();
            string fullJson = serializationService.SerializeToJson(bag);
            Debug.Log($"[完整 JSON]\n{fullJson}");

            // 3. 反序列化为新实例并校验
            var bagRestored = serializationService.DeserializeFromJson<Container>(fullJson);
            Debug.Log("反序列化后的背包内容：");
            DisplayInventoryContents(bagRestored);

            Debug.Assert(bagRestored.GetItemTotalCount("health_potion") == 15, "药水数量应保持 15");
            Debug.Assert(bagRestored.GetItemTotalCount("iron_sword") == 1, "铁剑数量应保持 1");
            Debug.Assert(bagRestored.GetItemTotalCount("ruby_gem") == 7, "红宝石数量应保持 7");

            // 4. 修改原背包验证独立性
            bag.RemoveItem("health_potion", 5);
            Debug.Log(
                $"修改原背包后验证独立性：原={bag.GetItemTotalCount("health_potion")}, 反序列化副本={bagRestored.GetItemTotalCount("health_potion")} (应仍为15)");

            Debug.Log("案例7 序列化 / 反序列化 完成\n");
        }

        // 案例8: OrganizeInventory（与 SortInventory 区别：会先合并可堆叠，再排序并紧凑化槽位）
        private void ShowOrganizeInventory()
        {
            Debug.Log("案例8: OrganizeInventory 合并与紧凑整理");
            var bag = new LinerContainer("organize_demo", "整理演示包", "Backpack", 12);

            Item apple = CreateGameItem("apple", "苹果", true, 10, "Food");
            Item potion = CreateGameItem("potion", "药水", true, 20, "Consumable");
            Item sword = CreateGameItem("iron_sword", "铁剑", false, 1, "Weapon");

            apple.Count = 7;
            bag.AddItems(apple, slotIndex: 0); // 0: 7
            var apple2 = apple.Clone();
            apple2.Count = 9;
            bag.AddItems(apple2, slotIndex: 3); // 3: 9 -> 会与0合并成 10 + 6
            potion.Count = 15;
            bag.AddItems(potion, slotIndex: 6); // 6: 15
            var potion2 = potion.Clone();
            potion2.Count = 8;
            bag.AddItems(potion2, slotIndex: 8); // 8: 8 -> 合并 20 + 3
            for (int i = 0; i < 5; i++) bag.AddItems(sword.Clone()); // 若不可堆叠多把占多个槽

            Debug.Log("整理前：");
            DisplayInventoryContents(bag);

            bag.OrganizeInventory();

            Debug.Log("Organize 后（应合并堆叠并紧凑排序）：");
            DisplayInventoryContents(bag);
        }

        // 案例9: 查询与统计（HasItem / 计数 / 按类型/属性/名称 / 自定义条件 / 总重量 / 唯一物品数）
        private void ShowAdvancedQueriesAndStatistics()
        {
            Debug.Log("案例9: 高级查询与统计");
            var bag = new LinerContainer("query_demo", "查询演示包", "Backpack", 10);

            Item ironSword = CreateGameItem("iron_sword", "铁剑", false, 1, "Weapon");
            ironSword.SetCustomData("Damage", 12);
            ironSword.SetCustomData("Material", "Iron");

            Item steelSword = CreateGameItem("steel_sword", "钢剑", false, 1, "Weapon");
            steelSword.SetCustomData("Damage", 18);
            steelSword.SetCustomData("Material", "Steel");

            Item hp = CreateGameItem("health_potion", "生命药水", true, 20, "Potion");
            hp.SetCustomData("Healing", 50);

            Item mp = CreateGameItem("mana_potion", "魔法药水", true, 20, "Potion");
            mp.SetCustomData("Mana", 40);

            bag.AddItems(ironSword);
            bag.AddItems(steelSword);
            hp.Count = 7;
            bag.AddItems(hp);
            mp.Count = 5;
            bag.AddItems(mp);

            Debug.Log($"HasItem(iron_sword) = {bag.HasItem("iron_sword")}");
            Debug.Log($"生命药水总数 = {bag.GetItemTotalCount("health_potion")}");
            Debug.Log($"唯一物品种类数 = {bag.GetUniqueItemCount()}");
            Debug.Log($"总重量 = {bag.GetTotalWeight()}");

            var weapons = bag.GetItemsByType("Weapon");
            Debug.Log($"武器条目数={weapons.Count}");

            var ironStuff = bag.GetItemsByAttribute("Material", "Iron");
            Debug.Log($"材质=Iron 条目数={ironStuff.Count}");

            var nameLike = bag.GetItemsByName("药水");
            Debug.Log($"名称包含 '药水' 的条目数={nameLike.Count}");

            var highDamage = bag.GetItemsWhere(i => i.Type == "Weapon" && i.GetCustomData<int>("Damage") >= 15);
            Debug.Log($"高伤害武器条目数(>=15)={highDamage.Count}");
        }

        // 案例10: InventoryManager 基础（注册 / 分类 / 优先级 / 查询）
        private void ShowInventoryManagerBasics()
        {
            Debug.Log("案例10: InventoryManager 注册 / 分类 / 优先级");
            var bag1 = new LinerContainer("player_bag1", "玩家背包1", "Backpack", 10);
            var bag2 = new LinerContainer("player_bag2", "玩家背包2", "Backpack", 15);
            var chest = new LinerContainer("home_chest", "家用储物箱", "Storage", 30);

            _inventoryService.RegisterContainer(bag1, 100, "Player");
            _inventoryService.RegisterContainer(bag2, 80, "Player");
            _inventoryService.RegisterContainer(chest, 20, "Home");

            var byType = _inventoryService.GetContainersByType("Backpack");
            Debug.Log($"类型=Backpack 数量={byType.Count}");

            var byCat = _inventoryService.GetContainersByCategory("Player");
            Debug.Log($"分类=Player 数量={byCat.Count}");

            var ordered = _inventoryService.GetContainersByPriority();
            Debug.Log($"优先级最高={ordered[0].Name}");
        }

        // 案例11: 全局条件（启用 / 禁用）
        private void ShowGlobalConditionsDemo()
        {
            Debug.Log("案例11: 全局物品条件");

            var bag = new LinerContainer("gcond_bag", "全局条件背包", "Backpack", 8);
            var chest = new LinerContainer("gcond_chest", "全局条件箱", "Storage", 8);
            _inventoryService.RegisterContainer(bag);
            _inventoryService.RegisterContainer(chest);

            // 添加全局条件：只允许 Weapon 且 属性 Rarity == Rare
            var typeCond = new ItemTypeCondition("Weapon");
            var rarityCond = new AttributeCondition("Rarity", "Rare");
            _inventoryService.AddGlobalItemCondition(typeCond);
            _inventoryService.AddGlobalItemCondition(rarityCond);

            // 启用
            _inventoryService.SetGlobalConditionsEnabled(true);

            Item rareSword = CreateGameItem("rare_sword", "稀有剑", false, 1, "Weapon");
            rareSword.SetCustomData("Rarity", "Rare");

            Item commonSword = CreateGameItem("common_sword", "普通剑", false, 1, "Weapon");
            commonSword.SetCustomData("Rarity", "Common");

            Item potion = CreateGameItem("potion", "药水", true, 10, "Consumable");
            potion.SetCustomData("Rarity", "Rare");

            (AddItemResult r1, _) = bag.AddItems(rareSword);
            (AddItemResult r2, _) = bag.AddItems(commonSword);
            (AddItemResult r3, _) = bag.AddItems(potion);

            Debug.Log($"添加稀有剑结果={r1} (应 Success)");
            Debug.Log($"添加普通剑结果={r2} (应 ItemConditionNotMet)");
            Debug.Log($"添加药水结果={r3} (应 ItemConditionNotMet)");

            // 关闭全局条件再试
            _inventoryService.SetGlobalConditionsEnabled(false);
            (AddItemResult r4, _) = bag.AddItems(potion);
            Debug.Log($"关闭后添加药水结果={r4} (应 Success)");
        }

        // 案例12: 跨容器高级操作（Transfer / AutoMove / BatchMove / Distribute）
        private void ShowCrossContainerAdvancedOps()
        {
            Debug.Log("案例12: 跨容器高级操作");
            var src = new LinerContainer("src_bag", "源包", "Backpack", 12);
            var dst = new LinerContainer("dst_bag", "目标包", "Backpack", 12);
            var extra = new LinerContainer("extra_bag", "额外包", "Backpack", 12);
            _inventoryService.RegisterContainer(src);
            _inventoryService.RegisterContainer(dst);
            _inventoryService.RegisterContainer(extra);

            Item apple = CreateGameItem("apple", "苹果", true, 10, "Food");
            apple.Count = 23;
            src.AddItems(apple); // 10 + 10 + 3

            // Transfer 指定数量
            (InventoryService.MoveResult trRes, int movedCount) =
                _inventoryService.TransferItems("apple", 8, "src_bag", "dst_bag");
            Debug.Log($"Transfer 8苹果 结果={trRes} 实际移动={movedCount}");

            // AutoMove 剩余全部
            (InventoryService.MoveResult autoRes, int autoCount) =
                _inventoryService.AutoMoveItem("apple", "src_bag", "dst_bag");
            Debug.Log($"AutoMove 剩余苹果 结果={autoRes} 移动={autoCount}");

            // BatchMove （构造一个移动列表 - 这里移动 dst 的第0槽到 extra）
            var batch = new List<InventoryService.MoveRequest>
            {
                new("dst_bag", 0, "extra_bag"), new("dst_bag", 1, "extra_bag"),
            };
            var batchResults = _inventoryService.BatchMoveItems(batch);
            Debug.Log($"BatchMove 条目数={batchResults.Count}");

            // Distribute 分发（把 125 个苹果按顺序分配回三个容器）
            Item distItem = CreateGameItem("apple", "苹果", true, 10, "Food");
            var distribution = _inventoryService.DistributeItems(distItem, 125,
                new() { "src_bag", "dst_bag", "extra_bag" });
            Debug.Log($"分发苹果结果: {string.Join(";", distribution)}");
        }

        // 案例13: 全局搜索 (FindItemGlobally / GetGlobalItemCount / 按类型/名称/属性搜索)
        private void ShowGlobalSearchDemo()
        {
            Debug.Log("案例13: 全局搜索");
            var b1 = new LinerContainer("gs_b1", "包1", "Backpack", 10);
            var b2 = new LinerContainer("gs_b2", "包2", "Backpack", 10);
            var eq = new LinerContainer("gs_equip", "装备栏", "Equipment", 6);
            _inventoryService.RegisterContainer(b1);
            _inventoryService.RegisterContainer(b2);
            _inventoryService.RegisterContainer(eq);

            Item apple = CreateGameItem("apple", "苹果", true, 10, "Food");
            Item sword = CreateGameItem("sword", "剑", false, 1, "Weapon");
            sword.SetCustomData("Material", "Iron");
            Item potion = CreateGameItem("potion", "药水", true, 20, "Consumable");

            apple.Count = 7;
            b1.AddItems(apple);
            apple.Count = 11;
            b2.AddItems(apple);
            potion.Count = 5;
            b1.AddItems(potion);
            eq.AddItems(sword);

            var applePositions = _inventoryService.FindItemGlobally("apple");
            Debug.Log($"全局找到苹果位置条数={applePositions.Count} 全局数量={_inventoryService.GetGlobalItemCount("apple")}");

            var containersHavingSword = _inventoryService.FindContainersWithItem("sword");
            Debug.Log($"持有剑的容器数={containersHavingSword.Count}");

            var typeSearch = _inventoryService.SearchItemsByType("Weapon");
            Debug.Log($"按类型Weapon搜索数={typeSearch.Count}");

            var nameSearch = _inventoryService.SearchItemsByName("药水");
            Debug.Log($"名称包含'药水'={nameSearch.Count}");

            var attrSearch = _inventoryService.SearchItemsByAttribute("Material", "Iron");
            Debug.Log($"属性 Material=Iron 条目={attrSearch.Count}");
        }

        // 案例14: 条件 + 属性 序列化验证
        private async Task ShowConditionAndAttributeSerialization()
        {
            Debug.Log("案例14: 条件与属性序列化");
            var chest = new LinerContainer("cond_serial_chest", "条件箱", "Chest", 6);
            chest.ContainerCondition.Add(new ItemTypeCondition("Gem"));
            chest.ContainerCondition.Add(new AttributeCondition("Level", 5,
                AttributeComparisonType.GreaterThanOrEqual));

            Item gem = CreateGameItem("mystic_gem", "秘法宝石", true, 30, "Gem");
            gem.SetCustomData("Level", 8);
            gem.SetCustomData("Quality", "Epic");
            gem.Count = 12;
            chest.AddItems(gem);

            // 从架构获取序列化服务
            var serializationService = await EasyPackArchitecture.Instance.ResolveAsync<ISerializationService>();
            string json = serializationService.SerializeToJson(chest);
            Debug.Log("[序列化 JSON]\n" + json);

            var restored = serializationService.DeserializeFromJson<Container>(json);
            Debug.Log("反序列化后内容：");
            DisplayInventoryContents(restored);

            // 验证条件功能仍有效
            Item lowGem = CreateGameItem("low_gem", "低阶宝石", true, 30, "Gem");
            lowGem.SetCustomData("Level", 2);
            (AddItemResult rAdd, _) = restored.AddItems(lowGem);
            Debug.Log($"向反序列化容器添加低阶宝石结果={rAdd} (应 ItemConditionNotMet)");

            // 验证属性保持
            int idx = restored.FindFirstSlotIndex("mystic_gem");
            if (idx >= 0)
            {
                ISlot slot = restored.Slots[idx];
                Debug.Log(
                    $"宝石属性: Level={slot.Item.GetCustomData<int>("Level")}, Quality={slot.Item.GetCustomData("Quality", "")}");
            }
        }

        // 案例15: 指定槽位添加 / 容量限制 / Full 与 IsEmpty
        private void ShowSlotAndCapacityEdgeCases()
        {
            Debug.Log("案例15: 槽位与容量边界");
            var limited = new LinerContainer("limited_bag", "有限包", "Backpack", 3);
            Item itemA = CreateGameItem("a_item", "物品A", false, 1, "Misc");
            Item itemB = CreateGameItem("b_item", "物品B", false, 1, "Misc");

            // 指定槽位
            itemA.Count = 1;
            limited.AddItems(itemA, slotIndex: 2);
            Debug.Log("指定槽位2放入物品A");
            DisplayInventoryContents(limited);

            // 再放不同物品到同槽 -> 应失败
            itemB.Count = 1;
            (AddItemResult resFail, _) = limited.AddItems(itemB, slotIndex: 2);
            Debug.Log($"尝试在已占用不同物品槽添加 => {resFail}");

            // 填满
            limited.AddItems(itemB); // 自动填0
            limited.AddItems(itemB.Clone()); // 自动填1
            Debug.Log($"Full = {limited.Full} (应为 True)");
            DisplayInventoryContents(limited);

            // 移除全部
            limited.RemoveItemAtIndex(0);
            limited.RemoveItemAtIndex(1);
            limited.RemoveItemAtIndex(2);
            Debug.Log($"清空后 IsEmpty={limited.IsEmpty()} Full={limited.Full}");
        }


        // 辅助方法: 创建游戏物品
        private Item CreateGameItem(string id, string name, bool isStackable, int maxStack = 1,
                                    string type = "Default") =>
            new()
            {
                ID = id,
                Name = name,
                IsStackable = isStackable,
                MaxStackCount = maxStack,
                Type = type,
                Description = $"{name}的描述信息", // 添加简单描述
            };

        // 辅助方法: 显示容器内容
        private void DisplayInventoryContents(Container container)
        {
            if (container.Slots.Count == 0)
            {
                Debug.Log("容器是空的");
                return;
            }

            Debug.Log($"容器 '{container.Name}' 内容 ({container.Slots.Count}格):");
            for (int i = 0; i < container.Slots.Count; i++)
            {
                ISlot slot = container.Slots[i];
                if (slot.IsOccupied)
                {
                    Debug.Log($"  [{i}] {slot.Item.Name} x{slot.ItemCount} ({slot.Item.Type})");
                }
                else
                {
                    Debug.Log($"  [{i}] 空");
                }
            }
        }
    }
}