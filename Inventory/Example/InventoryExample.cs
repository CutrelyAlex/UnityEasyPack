using EasyPack;
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 展示背包系统的使用示例
/// </summary>
public class InventoryExample : MonoBehaviour
{
    private void Start()
    {
        // 测试
        // Test();
        // 启动实际使用案例展示
         ShowInventoryUseCases();
    }


    private void Test()
    {
        Debug.Log("===== 测试嵌套背包和事件系统 =====");

        // 1. 测试嵌套背包
        TestNestedContainers();

        // 2. 测试事件系统
        TestEventSystem();

        Debug.Log("===== 测试完成 =====\n");
    }

    /// <summary>
    /// 测试嵌套容器功能
    /// </summary>
    private void TestNestedContainers()
    {
        Debug.Log("【测试1】嵌套容器功能");

        // 创建主背包
        var playerBackpack = new LinerContainer("main_backpack", "冒险者背包", "MainBag", 10);

        // 创建可作为容器的物品
        var backpackItem = new Item
        {
            ID = "small_backpack",
            Name = "小背包",
            Type = "Container",
            IsStackable = false,
            Description = "一个小型背包，可以存放物品",
            isContanierItem = true // 标记为容器物品
        };

        // 为容器物品创建内部存储容器
        var innerContainer = new LinerContainer("inner_container", "小背包内部", "Bag", 5);
        backpackItem.Containers = new List<IContainer> { innerContainer };

        // 向主背包添加这个背包物品
        Debug.Log("1.1 将小背包添加到主背包中");
        playerBackpack.AddItems(backpackItem);
        DisplayInventoryContents(playerBackpack);

        // 创建一些物品添加到内部容器
        Debug.Log("1.2 向小背包(内部容器)添加物品");
        var apple = CreateGameItem("apple", "苹果", true, 10, "Food");
        var potion = CreateGameItem("potion", "药水", true, 5, "Consumable");

        // 向内部容器添加物品
        innerContainer.AddItems(apple, 3);
        innerContainer.AddItems(potion, 2);

        // 显示内部容器的内容
        Debug.Log("小背包内部内容:");
        DisplayInventoryContents(innerContainer);

        // 测试从内部容器移除物品
        Debug.Log("1.3 从小背包中移除物品");
        var removeResult = innerContainer.RemoveItem("apple", 1);
        Debug.Log($"从小背包移除1个苹果: {(removeResult == RemoveItemResult.Success ? "成功" : "失败")}");
        DisplayInventoryContents(innerContainer);

        // 测试在背包间移动物品
        Debug.Log("1.4 从主背包取出物品放入小背包");
        var bread = CreateGameItem("bread", "面包", true, 5, "Food");
        playerBackpack.AddItems(bread, 2);
        Debug.Log("主背包添加了2个面包:");
        DisplayInventoryContents(playerBackpack);

        // 从主背包移动到内部背包
        Debug.Log("将面包从主背包移动到小背包中:");
        var bread_slot = -1;
        for (int i = 0; i < playerBackpack.Slots.Count; i++)
        {
            if (playerBackpack.Slots[i].IsOccupied && playerBackpack.Slots[i].Item.ID == "bread")
            {
                bread_slot = i;
                break;
            }
        }

        if (bread_slot != -1)
        {
            var moveResult = playerBackpack.MoveItemToContainer(bread_slot, innerContainer);
            Debug.Log($"移动结果: {(moveResult ? moveResult : "失败: " + moveResult)}");

            Debug.Log("移动后主背包内容:");
            DisplayInventoryContents(playerBackpack);

            Debug.Log("移动后小背包内容:");
            DisplayInventoryContents(innerContainer);
        }

        Debug.Log("1.5 从主背包中取出小背包");
        var backpack_slot = -1;
        for (int i = 0; i < playerBackpack.Slots.Count; i++)
        {
            if (playerBackpack.Slots[i].IsOccupied && playerBackpack.Slots[i].Item.ID == "small_backpack")
            {
                backpack_slot = i;
                break;
            }
        }

        if (backpack_slot != -1)
        {
            Debug.Log($"移除小背包前，内部物品数量：药水x{innerContainer.GetItemTotalCount("potion")}，苹果x{innerContainer.GetItemTotalCount("apple")}，面包x{innerContainer.GetItemTotalCount("bread")}");
            var removeResult2 = playerBackpack.RemoveItemAtIndex(backpack_slot);
            Debug.Log($"移除小背包结果: {removeResult2}");
            DisplayInventoryContents(playerBackpack);

            Debug.Log("小背包被移除后，其内容仍然保持：");
            DisplayInventoryContents(innerContainer);
        }
    }

    /// <summary>
    /// 测试背包事件系统
    /// </summary>
    private void TestEventSystem()
    {
        Debug.Log("\n【测试2】背包事件系统");

        // 创建背包容器
        var backpack = new LinerContainer("event_backpack", "事件测试背包", "Backpack", 5);

        // 为了便于在日志中识别，添加前缀
        Debug.Log("2.1 注册事件监听器");

        // 注册添加物品结果事件（统一处理成功和失败）
        backpack.OnItemAddResult += (item, requestedCount, actualCount, result, slots) => {
            if (result == AddItemResult.Success)
            {
                Debug.Log($"[事件] 添加成功: {item.Name} x{actualCount}/{requestedCount} 到槽位: {string.Join(",", slots)}");
            }
            else
            {
                Debug.Log($"[事件] 添加失败: {item?.Name ?? "null"} x{requestedCount}, 原因: {result}");
            }
        };

        // 注册移除物品结果事件（统一处理成功和失败）
        backpack.OnItemRemoveResult += (itemId, requestedCount, actualCount, result, slots) => {
            if (result == RemoveItemResult.Success)
            {
                Debug.Log($"[事件] 移除成功: 物品ID {itemId} x{actualCount}/{requestedCount} 从槽位: {string.Join(",", slots)}");
            }
            else
            {
                Debug.Log($"[事件] 移除失败: 物品ID {itemId} x{requestedCount}, 原因: {result}");
            }
        };

        // 注册物品数量变更事件
        backpack.OnSlotCountChanged += (slotIndex, item, oldCount, newCount) => {
            Debug.Log($"[事件] 数量变更: 槽位 {slotIndex} 中 {item.Name} 从 {oldCount} 变为 {newCount}");
        };

        // 注册物品总数变更事件
        backpack.OnItemTotalCountChanged += (itemId, item, oldTotal, newTotal) => {
            Debug.Log($"[事件] 物品总量变更: {item?.Name ?? itemId} 总数从 {oldTotal} 变为 {newTotal}");
        };

        // 2.2 测试添加物品触发事件
        Debug.Log("2.2 测试添加物品触发事件");
        var apple = CreateGameItem("apple", "苹果", true, 5, "Food");
        var sword = CreateGameItem("sword", "剑", false, 1, "Weapon");

        Debug.Log("添加3个苹果到背包");
        backpack.AddItems(apple, 3);

        Debug.Log("添加1把剑到背包");
        backpack.AddItems(sword);

        // 2.3 测试添加失败事件
        Debug.Log("2.3 测试添加失败事件");
        Debug.Log("尝试添加null物品，应触发添加失败事件");
        backpack.AddItems(null, 1);

        // 添加超过容量的物品
        var shield = CreateGameItem("shield", "盾", false, 1, "Weapon");
        var bow = CreateGameItem("bow", "弓", false, 1, "Weapon");
        var axe = CreateGameItem("axe", "斧", false, 1, "Weapon");
        var hammer = CreateGameItem("hammer", "锤", false, 1, "Weapon");

        Debug.Log("填满背包");
        backpack.AddItems(shield);
        backpack.AddItems(bow);
        backpack.AddItems(axe);

        Debug.Log("尝试添加锤到已满背包，应触发添加失败事件");
        backpack.AddItems(hammer);

        // 2.4 测试移除物品事件
        Debug.Log("2.4 测试移除物品事件");
        Debug.Log("从背包中移除1个苹果");
        backpack.RemoveItem("apple", 1);

        // 2.5 测试移除失败事件
        Debug.Log("2.5 测试移除失败事件");
        Debug.Log("尝试移除不存在的物品，应触发移除失败事件");
        backpack.RemoveItem("banana", 1);

        Debug.Log("尝试移除数量过多的物品，应触发移除失败事件");
        backpack.RemoveItem("apple", 10);

        // 2.6 测试物品总量变更事件
        Debug.Log("2.6 测试物品总量变更事件");

        // 创建新容器，专门测试总量事件
        var testBag = new LinerContainer("total_test_bag", "总量测试背包", "Backpack", 10);

        // 注册总量变更事件
        testBag.OnItemTotalCountChanged += (itemId, item, oldTotal, newTotal) => {
            Debug.Log($"[总量事件] 物品 {item?.Name ?? itemId} 总数从 {oldTotal} 变为 {newTotal}");
        };

        // 测试场景1：添加新物品
        var potion = CreateGameItem("potion", "药水", true, 20, "Consumable");
        Debug.Log("测试场景1: 添加新物品 - 5瓶药水");
        testBag.AddItems(potion, 5);

        // 测试场景2：添加已有物品，应该触发总量变化
        Debug.Log("测试场景2: 再添加3瓶药水，总量应从5变为8");
        testBag.AddItems(potion, 3);

        // 测试场景3：添加不同物品
        var gold = CreateGameItem("gold", "金币", true, 999, "Currency");
        Debug.Log("测试场景3: 添加100金币");
        testBag.AddItems(gold, 100);

        // 测试场景4：移除部分物品
        Debug.Log("测试场景4: 移除2瓶药水，总量应从8变为6");
        testBag.RemoveItem("potion", 2);

        // 测试场景5：移除全部物品
        Debug.Log("测试场景5: 移除全部药水，总量应从6变为0");
        testBag.RemoveItem("potion", 6);

        // 测试场景6：移除部分金币
        Debug.Log("测试场景6: 移除50金币，总量应从100变为50");
        testBag.RemoveItem("gold", 50);

        // 测试场景7：多槽位测试
        Debug.Log("测试场景7: 添加多个堆叠上限为10的物品");
        var gem = CreateGameItem("gem", "宝石", true, 10, "Gem");

        // 添加25个宝石，应该分布在3个槽位中(10+10+5)
        testBag.AddItems(gem, 25);
        DisplayInventoryContents(testBag);

        // 移除5个宝石，总数应从25变为20
        Debug.Log("移除5个宝石，总量应变化");
        testBag.RemoveItem("gem", 5);
        DisplayInventoryContents(testBag);

        // 移除11个宝石，应该移除一个满槽位，剩余9个在一个槽位中
        Debug.Log("移除11个宝石");
        testBag.RemoveItem("gem", 11);
        DisplayInventoryContents(testBag);

        // 显示最终背包内容
        Debug.Log("最终背包内容:");
        DisplayInventoryContents(backpack);
    }

    private void ShowInventoryUseCases()
    {
        Debug.Log("===== 背包系统使用案例展示 =====");

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

        Debug.Log("===== 背包系统使用案例展示完成 =====");
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
        var storageChest = new LinerContainer("house_storage", "储物箱", "Storage", -1);
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
        var healthPotion = CreateGameItem("health_potion", "生命药水", true, 20, "Consumable");
        var woodSword = CreateGameItem("wood_sword", "木剑", false, 1, "Equipment");
        var goldCoin = CreateGameItem("gold_coin", "金币", true, 999, "Currency");

        // 向背包中添加物品
        var (result, _) = playerBackpack.AddItems(healthPotion, 5);
        Debug.Log($"添加5瓶生命药水: {(result == AddItemResult.Success ? "成功" : "失败")}");

        var swordResult = playerBackpack.AddItems(woodSword);
        Debug.Log($"添加木剑: {(swordResult.result == AddItemResult.Success ? "成功" : "失败")}");

        var coinResult = playerBackpack.AddItems(goldCoin, 100);
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
        var arrow = CreateGameItem("arrow", "箭矢", true, 99, "Ammunition");

        // 第一次添加50支箭矢
        playerBackpack.AddItems(arrow, 50);
        Debug.Log("添加了50支箭矢到背包中");
        DisplayInventoryContents(playerBackpack);

        // 再添加60支箭矢 (会自动堆叠到99上限，剩余的放入新格子)
        playerBackpack.AddItems(arrow, 60);
        Debug.Log("又添加了60支箭矢到背包中");
        Debug.Log("由于单格堆叠上限为99，系统自动将箭矢分配到多个格子中");
        DisplayInventoryContents(playerBackpack);

        // 创建不可堆叠物品
        var uniqueScroll = CreateGameItem("unique_scroll", "稀有卷轴", false, 1, "Quest");

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
        var ore = CreateGameItem("iron_ore", "铁矿石", true, 50, "Material");
        var gem = CreateGameItem("ruby", "红宝石", true, 10, "Gem");
        var helmet = CreateGameItem("leather_helmet", "皮革头盔", false, 1, "Equipment");

        playerBackpack.AddItems(ore, 40);
        playerBackpack.AddItems(gem, 5);
        playerBackpack.AddItems(helmet);

        Debug.Log("背包初始内容:");
        DisplayInventoryContents(playerBackpack);

        Debug.Log("储物箱初始内容:");
        DisplayInventoryContents(storageChest);

        // 将物品从背包移动到储物箱
        var moveOreResult = playerBackpack.MoveItemToContainer(0, storageChest);
        Debug.Log($"将铁矿石从背包移动到储物箱: {(moveOreResult ? "成功" : "失败")}");

        var moveHelmetResult = playerBackpack.MoveItemToContainer(2, storageChest);
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
        var healthPotion = CreateGameItem("health_potion", "生命药水", true, 20, "Consumable");
        var manaPotion = CreateGameItem("mana_potion", "魔法药水", true, 20, "Consumable");
        var bread = CreateGameItem("bread", "面包", true, 10, "Food");

        playerBackpack.AddItems(healthPotion, 5);
        playerBackpack.AddItems(manaPotion, 3);
        playerBackpack.AddItems(bread, 7);

        Debug.Log("初始背包内容:");
        DisplayInventoryContents(playerBackpack);

        // 使用物品(模拟)
        Debug.Log("玩家使用了1瓶生命药水...");
        var useHealthPotion = playerBackpack.RemoveItem("health_potion", 1);
        Debug.Log($"使用结果: {(useHealthPotion == RemoveItemResult.Success ? "成功" : "失败")}");

        // 使用多个物品
        Debug.Log("玩家吃了3个面包...");
        var useBread = playerBackpack.RemoveItem("bread", 3);
        Debug.Log($"使用结果: {(useBread == RemoveItemResult.Success ? "成功" : "失败")}");

        // 丢弃物品
        Debug.Log("玩家丢弃了所有魔法药水...");
        var dropManaPotion = playerBackpack.RemoveItem("mana_potion", 3);
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
        playerBackpack.AddItems(CreateGameItem("apple", "苹果", true, 10, "D食物"), 3);
        playerBackpack.AddItems(CreateGameItem("leather_shield", "皮盾", false, 1, "C防具"));
        playerBackpack.AddItems(CreateGameItem("health_potion", "生命药水", true, 10, "A消耗品"), 5);
        playerBackpack.AddItems(CreateGameItem("mana_potion", "魔法药水", true, 10, "A消耗品"), 2);
        playerBackpack.AddItems(CreateGameItem("gold_coin", "金币", true, 100, "E货币"), 65);
        playerBackpack.AddItems(CreateGameItem("silver_coin", "银币", true, 100, "E货币"), 32);

        Debug.Log("整理前的背包内容(随机顺序):");
        DisplayInventoryContents(playerBackpack);

        // 执行背包整理
        Debug.Log("玩家点击了'整理背包'按钮");
        playerBackpack.SortInventory();

        Debug.Log("整理后的背包内容(按物品类型和名称排序):");
        DisplayInventoryContents(playerBackpack);
        Debug.Log("物品已按照类型(A消耗品 > B武器 > C防具 > D食物 > E货币)和名称排序");
    }

    // 辅助方法: 创建游戏物品
    private IItem CreateGameItem(string id, string name, bool isStackable, int maxStack = 1, string type = "Default")
    {
        return new Item
        {
            ID = id,
            Name = name,
            IsStackable = isStackable,
            MaxStackCount = maxStack,
            Type = type,
            Description = $"{name}的描述信息" // 添加简单描述
        };
    }

    // 辅助方法: 显示容器内容
    private void DisplayInventoryContents(IContainer container)
    {
        if (container.Slots.Count == 0)
        {
            Debug.Log("容器是空的");
            return;
        }

        Debug.Log($"容器 '{container.Name}' 内容 ({container.Slots.Count}格):");
        for (int i = 0; i < container.Slots.Count; i++)
        {
            var slot = container.Slots[i];
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