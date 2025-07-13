using EasyPack;
using System;
using UnityEngine;

/// <summary>
/// 展示背包系统的使用示例
/// </summary>
public class InventoryExample : MonoBehaviour
{
    private void Start()
    {
        // 启动实际使用案例展示
        ShowInventoryUseCases();
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
        var potionResult = playerBackpack.AddItems(healthPotion, 5);
        Debug.Log($"添加5瓶生命药水: {(potionResult.result == AddItemResult.Success ? "成功" : "失败")}");

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
        Debug.Log($"将铁矿石从背包移动到储物箱: {(moveOreResult.Success ? "成功" : "失败")}");

        var moveHelmetResult = playerBackpack.MoveItemToContainer(2, storageChest);
        Debug.Log($"将皮革头盔从背包移动到储物箱: {(moveHelmetResult.Success ? "成功" : "失败")}");

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
        Debug.Log("");
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