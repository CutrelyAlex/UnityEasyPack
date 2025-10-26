# Inventory System - 用户使用指南

**适用EasyPack版本:** EasyPack v1.5.30  
**最后更新:** 2025-10-26

---

## 概述

**Inventory System** 是一个灵活、高性能的物品管理系统，支持线性容器、网格容器、物品堆叠、条件过滤、跨容器操作和序列化等功能。适用于 RPG、生存、模拟经营等多种游戏类型的背包、仓库、装备栏等场景。

### 核心特性

- ✅ **多容器类型**：支持线性容器（LinerContainer）和网格容器（GridContainer）
- ✅ **物品堆叠**：自动堆叠可堆叠物品，支持最大堆叠数限制
- ✅ **条件系统**：支持物品类型、属性、自定义条件等多种过滤规则
- ✅ **容器管理**：InventoryManager 统一管理多个容器，支持优先级、分类
- ✅ **批量操作**：批量添加、移除、转移、分配物品，优化性能
- ✅ **序列化支持**：完整的 JSON 序列化/反序列化，支持存档系统
- ✅ **事件系统**：丰富的事件回调，实时响应容器和物品变化
- ✅ **性能优化**：缓存系统、索引加速、批处理，适合大规模物品管理

### 适用场景

- RPG 背包系统（玩家背包、装备栏、仓库）
- 生存游戏物品管理
- 模拟经营游戏
- 网格背包系统

---

## 目录

- [概述](#概述)
- [快速开始](#快速开始)
  - [前置要求](#前置要求)
  - [导入命名空间](#导入命名空间)
  - [第一示例](#第一示例)
  - [验证集成](#验证集成)
- [常见场景](#常见场景)
  - [场景 1：创建容器并添加物品](#场景-1创建容器并添加物品)
  - [场景 2：物品堆叠和数量管理](#场景-2物品堆叠和数量管理)
  - [场景 3：使用条件限制容器接受的物品类型](#场景-3使用条件限制容器接受的物品类型)
  - [场景 4：物品在容器间转移](#场景-4物品在容器间转移)
  - [场景 5：使用 InventoryManager 管理多个容器](#场景-5使用-inventorymanager-管理多个容器)
- [进阶用法](#进阶用法)
  - [进阶 1：网格容器系统](#进阶-1网格容器系统)
  - [进阶 2：批量操作优化](#进阶-2批量操作优化)
  - [进阶 3：序列化与持久化](#进阶-3序列化与持久化)
  - [进阶 4：自定义条件和属性查询](#进阶-4自定义条件和属性查询)
- [故障排查](#故障排查)
- [术语表](#术语表)
- [最佳实践](#最佳实践)
- [延伸阅读](#延伸阅读)

---

## 快速开始

### 前置要求

- Unity 2021.3 或更高版本
- .NET Standard 2.1
- 无外部依赖（系统完全独立）

### 导入命名空间

在脚本文件头部添加以下引用：

```csharp
using EasyPack.InventorySystem;
using UnityEngine;
```

### 第一示例

创建一个简单的玩家背包并添加物品：

```csharp
using EasyPack.InventorySystem;
using UnityEngine;

public class InventoryQuickStart : MonoBehaviour
{
    void Start()
    {
        // 步骤 1：创建一个 20 格容量的玩家背包
        var playerBackpack = new LinerContainer("player_backpack", "冒险者背包", "Backpack", 20);
        Debug.Log($"创建背包：{playerBackpack.Name}，容量：{playerBackpack.Capacity}");
        
        // 步骤 2：创建物品
        var healthPotion = new Item
        {
            ID = "health_potion",
            Name = "生命药水",
            Type = "Consumable",
            IsStackable = true,
            MaxStackCount = 20
        };
        
        // 步骤 3：添加 5 瓶生命药水到背包
        var (result, addedCount) = playerBackpack.AddItems(healthPotion, 5);
        
        // 步骤 4：验证结果
        if (result == AddItemResult.Success)
        {
            Debug.Log($"成功添加 {addedCount} 瓶生命药水");
            Debug.Log($"背包已使用槽位：{playerBackpack.UsedSlots}/{playerBackpack.Capacity}");
        }
        else
        {
            Debug.LogError($"添加失败：{result}");
        }
    }
}
```

**运行结果：**
```
创建背包：冒险者背包，容量：20
成功添加 5 瓶生命药水
背包已使用槽位：1/20
```

### 验证集成

如果看到预期输出，说明系统已成功集成！接下来可以探索 [常见场景](#常见场景) 了解更多用法。

---

## 常见场景

### 场景 1：创建容器并添加物品

**使用场景：** 游戏初始化时创建玩家背包、装备栏、仓库等容器

**代码示例：**

```csharp
using EasyPack.InventorySystem;
using UnityEngine;

public class ContainerCreationExample : MonoBehaviour
{
    void Start()
    {
        // 创建玩家背包 - 有限容量
        var backpack = new LinerContainer("player_backpack", "冒险者背包", "Backpack", 20);
        
        // 创建装备栏 - 小容量，用于装备
        var equipmentSlots = new LinerContainer("player_equipment", "装备栏", "Equipment", 5);
        
        // 创建仓库 - 无限容量
        var warehouse = new LinerContainer("player_warehouse", "仓库", "Storage", -1);
        
        // 创建物品
        var sword = new Item
        {
            ID = "iron_sword",
            Name = "铁剑",
            Type = "Weapon",
            IsStackable = false,
            Weight = 3.5f
        };
        
        var goldCoin = new Item
        {
            ID = "gold_coin",
            Name = "金币",
            Type = "Currency",
            IsStackable = true,
            MaxStackCount = 999
        };
        
        // 添加物品到背包
        backpack.AddItems(sword, 1);
        backpack.AddItems(goldCoin, 100);
        
        Debug.Log($"背包物品数：{backpack.UsedSlots}，剩余空格：{backpack.FreeSlots}");
    }
}
```

**关键要点：**
- 容器容量设为 `-1` 表示无限容量
- `IsStackable` 决定物品是否可堆叠
- `MaxStackCount` 控制单个槽位的最大堆叠数量（-1 表示无限）

---

### 场景 2：物品堆叠和数量管理

**使用场景：** 处理可堆叠物品（如药水、材料、货币）的自动堆叠和数量管理

**代码示例：**

```csharp
using EasyPack.InventorySystem;
using UnityEngine;

public class ItemStackingExample : MonoBehaviour
{
    void Start()
    {
        var backpack = new LinerContainer("backpack", "背包", "Backpack", 10);
        
        // 创建可堆叠物品（最大堆叠 20）
        var arrow = new Item
        {
            ID = "arrow",
            Name = "箭",
            Type = "Ammo",
            IsStackable = true,
            MaxStackCount = 20
        };
        
        // 第一次添加 15 支箭
        var (result1, count1) = backpack.AddItems(arrow, 15);
        Debug.Log($"第一次添加：{result1}，添加数量：{count1}，已用槽位：{backpack.UsedSlots}");
        
        // 第二次添加 10 支箭（会先填满第一个槽位，剩余的放到新槽位）
        var (result2, count2) = backpack.AddItems(arrow, 10);
        Debug.Log($"第二次添加：{result2}，添加数量：{count2}，已用槽位：{backpack.UsedSlots}");
        
        // 查询箭的总数量
        int totalArrows = backpack.GetItemTotalCount("arrow");
        Debug.Log($"箭的总数量：{totalArrows}");
        
        // 移除 18 支箭
        var (removeResult, removedCount) = backpack.RemoveItems("arrow", 18);
        Debug.Log($"移除结果：{removeResult}，移除数量：{removedCount}");
        Debug.Log($"剩余箭数量：{backpack.GetItemTotalCount("arrow")}");
    }
}
```

**关键要点：**
- 系统自动处理堆叠逻辑，优先填满已有槽位
- `GetItemTotalCount()` 统计物品在所有槽位的总数量
- 移除物品时会自动从多个槽位中扣除

---

### 场景 3：使用条件限制容器接受的物品类型

**使用场景：** 装备栏只接受装备、弹药袋只接受弹药等限制性容器

**代码示例：**

```csharp
using EasyPack.InventorySystem;
using UnityEngine;

public class ContainerConditionExample : MonoBehaviour
{
    void Start()
    {
        // 创建装备栏，只接受 "Equipment" 类型的物品
        var equipmentSlots = new LinerContainer("equipment", "装备栏", "Equipment", 5);
        equipmentSlots.ContainerCondition.Add(new ItemTypeCondition("Equipment"));
        
        // 创建弹药袋，只接受 "Ammo" 类型的物品
        var ammoBag = new LinerContainer("ammo_bag", "弹药袋", "AmmoBag", 3);
        ammoBag.ContainerCondition.Add(new ItemTypeCondition("Ammo"));
        
        // 创建物品
        var helmet = new Item { ID = "helmet", Name = "头盔", Type = "Equipment" };
        var arrow = new Item { ID = "arrow", Name = "箭", Type = "Ammo" };
        var potion = new Item { ID = "potion", Name = "药水", Type = "Consumable" };
        
        // 测试添加物品
        var (result1, _) = equipmentSlots.AddItems(helmet, 1);
        Debug.Log($"头盔添加到装备栏：{result1}"); // Success
        
        var (result2, _) = equipmentSlots.AddItems(arrow, 1);
        Debug.Log($"箭添加到装备栏：{result2}"); // ItemConditionNotMet
        
        var (result3, _) = ammoBag.AddItems(arrow, 1);
        Debug.Log($"箭添加到弹药袋：{result3}"); // Success
        
        var (result4, _) = ammoBag.AddItems(potion, 1);
        Debug.Log($"药水添加到弹药袋：{result4}"); // ItemConditionNotMet
    }
}
```

**关键要点：**
- `ContainerCondition` 是一个条件列表，所有条件必须满足才能添加物品
- `ItemTypeCondition` 是内置条件，用于限制物品类型
- 还可以使用 `AttributeCondition`、`CustomItemCondition` 等自定义条件

---

### 场景 4：物品在容器间转移

**使用场景：** 玩家将背包物品存入仓库，或从仓库取出物品

**代码示例：**

```csharp
using EasyPack.InventorySystem;
using UnityEngine;

public class ItemTransferExample : MonoBehaviour
{
    void Start()
    {
        // 创建玩家背包和仓库
        var backpack = new LinerContainer("backpack", "背包", "Backpack", 10);
        var warehouse = new LinerContainer("warehouse", "仓库", "Storage", 50);
        
        // 在背包中添加物品
        var ironOre = new Item
        {
            ID = "iron_ore",
            Name = "铁矿石",
            Type = "Material",
            IsStackable = true,
            MaxStackCount = 50
        };
        
        backpack.AddItems(ironOre, 30);
        Debug.Log($"背包铁矿石数量：{backpack.GetItemTotalCount("iron_ore")}");
        
        // 方法 1：使用 MoveItemToContainer 从槽位转移
        int slotIndex = backpack.FindItemSlotIndex("iron_ore");
        if (slotIndex >= 0 && backpack is LinerContainer linerBackpack)
        {
            bool success = linerBackpack.MoveItemToContainer(slotIndex, warehouse);
            Debug.Log($"槽位 {slotIndex} 转移到仓库：{success}");
        }
        
        Debug.Log($"转移后背包铁矿石：{backpack.GetItemTotalCount("iron_ore")}");
        Debug.Log($"转移后仓库铁矿石：{warehouse.GetItemTotalCount("iron_ore")}");
        
        // 方法 2：使用 RemoveItems + AddItems 手动转移
        var (removeResult, removedCount) = backpack.RemoveItems("iron_ore", 10);
        if (removeResult == RemoveItemResult.Success)
        {
            warehouse.AddItems(ironOre, removedCount);
            Debug.Log($"手动转移 {removedCount} 个铁矿石");
        }
    }
}
```

**关键要点：**
- `MoveItemToContainer()` 会移动整个槽位的物品到目标容器
- 手动转移时需要先 `RemoveItems()` 再 `AddItems()`
- 推荐使用 `InventoryManager.TransferItems()` 进行跨容器转移（见场景 5）

---

### 场景 5：使用 InventoryManager 管理多个容器

**使用场景：** 游戏中存在多个容器（背包、装备、仓库），需要统一管理和跨容器操作

**代码示例：**

```csharp
using EasyPack.InventorySystem;
using UnityEngine;

public class InventoryManagerExample : MonoBehaviour
{
    private InventoryManager inventoryManager = new InventoryManager();
    
    void Start()
    {
        // 创建并注册容器
        var backpack = new LinerContainer("backpack", "背包", "Backpack", 20);
        var equipment = new LinerContainer("equipment", "装备栏", "Equipment", 5);
        var warehouse = new LinerContainer("warehouse", "仓库", "Storage", -1);
        
        // 注册容器到管理器（带优先级和分类）
        inventoryManager.RegisterContainer(backpack, priority: 1, category: "Player");
        inventoryManager.RegisterContainer(equipment, priority: 2, category: "Player");
        inventoryManager.RegisterContainer(warehouse, priority: 0, category: "Home");
        
        // 创建物品
        var healthPotion = new Item
        {
            ID = "health_potion",
            Name = "生命药水",
            Type = "Consumable",
            IsStackable = true,
            MaxStackCount = 20
        };
        
        // 添加物品到背包
        backpack.AddItems(healthPotion, 15);
        
        // 跨容器转移（从背包到仓库）
        bool transferSuccess = inventoryManager.TransferItems(
            "backpack", "warehouse", "health_potion", 10
        );
        Debug.Log($"转移成功：{transferSuccess}");
        
        // 全局搜索物品
        var foundContainers = inventoryManager.FindItemInContainers("health_potion");
        Debug.Log($"药水在 {foundContainers.Count} 个容器中");
        
        // 获取玩家分类的所有容器
        var playerContainers = inventoryManager.GetContainersByCategory("Player");
        Debug.Log($"玩家拥有 {playerContainers.Count} 个容器");
        
        // 获取药水的全局总数
        int totalPotions = inventoryManager.GetItemTotalCountAcrossContainers("health_potion");
        Debug.Log($"药水总数：{totalPotions}");
    }
}
```

**关键要点：**
- `InventoryManager` 集中管理多个容器，支持优先级和分类
- `TransferItems()` 简化跨容器物品转移
- `FindItemInContainers()` 可全局搜索物品位置
- 容器优先级影响自动分配和批量操作的顺序

---

## 进阶用法

### 进阶 1：网格容器系统

**适用场景：** 实现类似《暗黑破坏神》《逃离塔科夫》的网格背包，物品占据多个格子

**代码示例：**

```csharp
using EasyPack.InventorySystem;
using UnityEngine;

public class GridContainerExample : MonoBehaviour
{
    void Start()
    {
        // 创建 5x4 的网格容器（总计 20 格）
        var gridBackpack = new GridContainer("grid_backpack", "网格背包", "GridBackpack", 5, 4);
        
        // 创建网格物品（2x2 大小的盔甲）
        var armor = new GridItem
        {
            ID = "plate_armor",
            Name = "板甲",
            Type = "Equipment",
            IsStackable = false,
            GridWidth = 2,
            GridHeight = 2
        };
        
        // 尝试在 (0, 0) 位置放置盔甲
        var (result, addedCount) = gridBackpack.AddItemsAtPosition(armor, 1, 0, 0);
        
        if (result == AddItemResult.Success)
        {
            Debug.Log("盔甲成功放置在 (0,0)，占据 2x2 格子");
            Debug.Log($"已用槽位：{gridBackpack.UsedSlots}/20");
        }
        
        // 查询指定位置的物品
        var itemAt00 = gridBackpack.GetItemAt(0, 0);
        Debug.Log($"(0,0) 位置的物品：{itemAt00?.Name}");
        
        // 创建 1x1 的小物品
        var ring = new GridItem
        {
            ID = "gold_ring",
            Name = "金戒指",
            Type = "Equipment",
            IsStackable = false,
            GridWidth = 1,
            GridHeight = 1
        };
        
        // 在 (3, 0) 放置戒指
        gridBackpack.AddItemsAtPosition(ring, 1, 3, 0);
        
        // 移动物品到新位置
        bool moveSuccess = gridBackpack.MoveItemToPosition(0, 0, 2, 2);
        Debug.Log($"盔甲移动到 (2,2)：{moveSuccess}");
    }
}
```

**性能优化建议：**
- 网格容器适合小型背包（< 10x10），大型背包建议使用线性容器
- 使用 `GetItemAt()` 前先检查坐标是否合法
- 批量操作时使用 `BeginBatch()` / `EndBatch()` 减少事件触发

---

### 进阶 2：批量操作优化

**适用场景：** 一次性添加/移除大量物品时，减少事件触发次数，提升性能

**代码示例：**

```csharp
using EasyPack.InventorySystem;
using UnityEngine;
using System.Collections.Generic;

public class BatchOperationsExample : MonoBehaviour
{
    void Start()
    {
        var warehouse = new LinerContainer("warehouse", "仓库", "Storage", 100);
        
        // 创建大量物品
        var materials = new List<(Item item, int count)>
        {
            (new Item { ID = "wood", Name = "木材", IsStackable = true }, 500),
            (new Item { ID = "stone", Name = "石料", IsStackable = true }, 300),
            (new Item { ID = "iron", Name = "铁矿", IsStackable = true }, 200),
            (new Item { ID = "gold", Name = "金矿", IsStackable = true }, 50)
        };
        
        // 开启批处理模式
        warehouse.BeginBatch();
        
        foreach (var (item, count) in materials)
        {
            warehouse.AddItems(item, count);
        }
        
        // 结束批处理，一次性触发所有事件
        warehouse.EndBatch();
        
        Debug.Log($"批量添加完成，仓库已用槽位：{warehouse.UsedSlots}");
        
        // 批量分配物品到多个容器
        var inventoryManager = new InventoryManager();
        var backpack1 = new LinerContainer("bp1", "背包1", "Backpack", 10);
        var backpack2 = new LinerContainer("bp2", "背包2", "Backpack", 10);
        
        inventoryManager.RegisterContainer(backpack1);
        inventoryManager.RegisterContainer(backpack2);
        
        var coinItem = new Item { ID = "coin", Name = "金币", IsStackable = true };
        
        // 分配 100 个金币到所有容器（平均分配）
        inventoryManager.DistributeItems(coinItem, 100, new[] { "bp1", "bp2" });
        
        Debug.Log($"背包1 金币：{backpack1.GetItemTotalCount("coin")}");
        Debug.Log($"背包2 金币：{backpack2.GetItemTotalCount("coin")}");
    }
}
```

**性能优化建议：**
- 添加 > 10 个物品时建议使用批处理
- 批处理会延迟事件触发，UI 刷新在 `EndBatch()` 后统一处理
- `DistributeItems()` 可根据容器剩余空间自动分配

---

### 进阶 3：序列化与持久化

**适用场景：** 保存玩家背包数据到本地文件或数据库

**代码示例：**

```csharp
using EasyPack.InventorySystem;
using EasyPack;
using UnityEngine;
using System.IO;

public class SerializationExample : MonoBehaviour
{
    void Start()
    {
        // 创建并填充容器
        var backpack = new LinerContainer("player_backpack", "背包", "Backpack", 20);
        
        var sword = new Item { ID = "sword", Name = "剑", Type = "Weapon" };
        var potion = new Item { ID = "potion", Name = "药水", Type = "Consumable", IsStackable = true, MaxStackCount = 20 };
        
        backpack.AddItems(sword, 1);
        backpack.AddItems(potion, 10);
        
        Debug.Log($"原始背包：{backpack.Name}，物品数量：{backpack.UsedSlots}");
        
        // 使用统一序列化服务序列化容器到 JSON
        string json = SerializationServiceManager.SerializeToJson(backpack);
        Debug.Log($"序列化结果：\n{json}");
        
        // 保存到文件
        string savePath = Path.Combine(Application.persistentDataPath, "backpack.json");
        File.WriteAllText(savePath, json);
        Debug.Log($"保存到：{savePath}");
        
        // 从文件加载
        string loadedJson = File.ReadAllText(savePath);
        
        // 使用统一序列化服务反序列化
        var loadedBackpack = SerializationServiceManager.DeserializeFromJson<Container>(loadedJson);
        
        Debug.Log($"加载的背包：{loadedBackpack.Name}");
        Debug.Log($"物品数量：{loadedBackpack.UsedSlots}");
        Debug.Log($"药水数量：{loadedBackpack.GetItemTotalCount("potion")}");
        
        // 验证数据完整性
        bool integrityCheck = loadedBackpack.GetItemTotalCount("potion") == 10 
            && loadedBackpack.GetItemTotalCount("sword") == 1;
        Debug.Log($"数据完整性验证：{(integrityCheck ? "通过" : "失败")}");
    }
}
```

**关键要点：**
- 使用 `SerializationServiceManager.SerializeToJson()` 进行序列化
- 使用 `SerializationServiceManager.DeserializeFromJson<Container>()` 进行反序列化
- 统一序列化服务自动处理线性容器和网格容器
- 物品属性（Attributes）会自动序列化
- 物品条件需要实现 `ISerializableCondition` 接口才能序列化

---

### 进阶 4：自定义条件和属性查询

**适用场景：** 根据物品属性（如等级、品质）过滤，或自定义复杂查询逻辑

**代码示例：**

```csharp
using EasyPack.InventorySystem;
using UnityEngine;
using System.Collections.Generic;

public class CustomConditionExample : MonoBehaviour
{
    void Start()
    {
        var backpack = new LinerContainer("backpack", "背包", "Backpack", 20);
        
        // 创建带属性的物品
        var legendaryAxe = new Item
        {
            ID = "legendary_axe",
            Name = "传奇战斧",
            Type = "Weapon",
            Attributes = new Dictionary<string, object>
            {
                { "Rarity", "Legendary" },
                { "Level", 50 },
                { "Damage", 120 }
            }
        };
        
        var commonSword = new Item
        {
            ID = "common_sword",
            Name = "普通剑",
            Type = "Weapon",
            Attributes = new Dictionary<string, object>
            {
                { "Rarity", "Common" },
                { "Level", 10 },
                { "Damage", 30 }
            }
        };
        
        backpack.AddItems(legendaryAxe, 1);
        backpack.AddItems(commonSword, 1);
        
        // 使用属性条件查询（只接受传奇品质）
        var legendaryCondition = new AttributeCondition("Rarity", "Legendary");
        bool isLegendary = legendaryCondition.CheckCondition(legendaryAxe);
        Debug.Log($"战斧是传奇：{isLegendary}"); // True
        
        // 自定义条件：等级 >= 40
        var highLevelCondition = new CustomItemCondition(item =>
        {
            if (item.Attributes.TryGetValue("Level", out var level))
            {
                return level is int lvl && lvl >= 40;
            }
            return false;
        });
        
        bool axeMeetsLevel = highLevelCondition.CheckCondition(legendaryAxe);
        bool swordMeetsLevel = highLevelCondition.CheckCondition(commonSword);
        
        Debug.Log($"战斧满足等级要求：{axeMeetsLevel}"); // True
        Debug.Log($"剑满足等级要求：{swordMeetsLevel}"); // False
        
        // 查询所有传奇武器
        var legendaryWeapons = backpack.FindItemsByCondition(legendaryCondition);
        Debug.Log($"找到 {legendaryWeapons.Count} 件传奇武器");
    }
}
```

**关键要点：**
- `Attributes` 是 `Dictionary<string, object>`，可存储任意类型的属性
- `AttributeCondition` 用于简单的键值匹配
- `CustomItemCondition` 支持 lambda 表达式自定义复杂逻辑
- `AllCondition` / `AnyCondition` 可组合多个条件

---

## 故障排查

### 常见问题

#### 问题 1：编译错误 - 找不到类型 `Container`

**症状：**
```
The type or namespace name 'Container' could not be found (are you missing a using directive or an assembly reference?)
```

**原因：** 缺少命名空间引用

**解决方法：**
1. 在文件头部添加 `using EasyPack.InventorySystem;`
2. 确认已正确导入 Inventory 系统文件夹到项目中

---

#### 问题 2：运行时错误 - AddItems 返回 `ItemConditionNotMet`

**症状：** 物品无法添加到容器，返回 `AddItemResult.ItemConditionNotMet`

**原因：** 容器的 `ContainerCondition` 不满足

**解决方法：**
1. 检查容器的 `ContainerCondition` 列表，确认物品满足所有条件
2. 使用 `container.ValidateItemCondition(item)` 手动测试条件
3. 临时移除条件进行测试：`container.ContainerCondition.Clear()`

---

#### 问题 3：性能问题 - 频繁添加物品导致卡顿

**症状：** 一次性添加大量物品时出现明显卡顿

**原因：** 每次 `AddItems()` 都会触发事件和缓存更新

**解决方法：**
1. 使用批处理模式：
   ```csharp
   container.BeginBatch();
   // 批量添加物品
   container.EndBatch();
   ```
2. 禁用不需要的事件监听
3. 使用 `InventoryManager.DistributeItems()` 代替循环添加

---

#### 问题 4：序列化错误 - 反序列化后物品丢失

**症状：** 保存并加载容器后，部分物品或属性丢失

**原因：** 自定义条件或特殊属性未正确序列化

**解决方法：**
1. 使用统一序列化服务：
   ```csharp
   // 序列化
   string json = SerializationServiceManager.SerializeToJson(container);
   
   // 反序列化
   var loaded = SerializationServiceManager.DeserializeFromJson<Container>(json);
   ```
2. 确保自定义条件实现 `ISerializableCondition` 接口
3. 检查物品 `Attributes` 中的值类型是否支持 JSON 序列化
4. 使用 `ConditionTypeRegistry.Register()` 注册自定义条件类型

---

#### 问题 5：网格容器放置失败 - 无法放置物品

**症状：** `AddItemsAtPosition()` 返回失败

**原因：** 目标位置被占用或超出边界

**解决方法：**
1. 使用 `IsPositionAvailable(x, y, width, height)` 预先检查位置
2. 确认物品的 `GridWidth` 和 `GridHeight` 正确设置
3. 检查坐标是否超出容器边界：`x + width <= GridWidth`

---

### FAQ 更新记录

*本节持续更新，记录用户反馈的新问题。*

#### 问题 X：（待补充）

*如遇到未列出的问题，请提交 GitHub Issue 或联系维护者。*

---

## 术语表

### 容器（Container）
物品存储的基本单位，分为线性容器（LinerContainer）和网格容器（GridContainer）。每个容器有固定或无限的容量，可设置物品条件限制。

**示例：**
```csharp
var container = new LinerContainer("id", "名称", "类型", 20);
```

---

### 槽位（Slot）
容器内的单个存储位置，可存放一个物品及其数量。线性容器的槽位按索引访问，网格容器的槽位按二维坐标访问。

---

### 物品（Item）
游戏中的可存储对象，具有 ID、名称、类型、堆叠属性等。实现 `IItem` 接口。

---

### 堆叠（Stacking）
将相同 ID 的可堆叠物品放置在同一槽位，减少占用空间。通过 `IsStackable` 和 `MaxStackCount` 控制。

---

### 物品条件（Item Condition）
限制容器接受物品的规则，如类型过滤、属性要求等。实现 `IItemCondition` 接口。

**相关术语：** `ItemTypeCondition`、`AttributeCondition`、`CustomItemCondition`

---

### InventoryManager（库存管理器）
管理多个容器的中央系统，提供跨容器操作、优先级管理、全局搜索等功能。

---

### 批处理（Batch Processing）
延迟事件触发和缓存更新，提升批量操作性能。通过 `BeginBatch()` / `EndBatch()` 使用。

---

### 网格物品（Grid Item）
占据多个格子的物品（如 2x2 的盔甲），仅用于 `GridContainer`。继承自 `Item` 并添加 `GridWidth` 和 `GridHeight` 属性。

---

## 最佳实践

### 1. 优先使用 InventoryManager 管理多个容器

**推荐做法：**
```csharp
// ✅ 推荐：统一管理
var manager = new InventoryManager();
manager.RegisterContainer(backpack);
manager.RegisterContainer(warehouse);
manager.TransferItems("backpack", "warehouse", "item_id", 10);
```

**不推荐做法：**
```csharp
// ❌ 不推荐：手动管理多个容器
var (removed, count) = backpack.RemoveItems("item_id", 10);
warehouse.AddItems(itemRef, count);
```

---

### 2. 大量操作时使用批处理

**推荐做法：**
```csharp
// ✅ 推荐：批处理减少事件触发
container.BeginBatch();
foreach (var item in items)
{
    container.AddItems(item, 1);
}
container.EndBatch();
```

**不推荐做法：**
```csharp
// ❌ 不推荐：每次添加都触发事件
foreach (var item in items)
{
    container.AddItems(item, 1); // 触发 N 次事件
}
```

---

### 3. 使用统一序列化服务

**推荐做法：**
```csharp
// ✅ 推荐：使用统一序列化服务
string json = SerializationServiceManager.SerializeToJson(container);
var loaded = SerializationServiceManager.DeserializeFromJson<Container>(json);

// 注册自定义条件类型
void Awake()
{
    ConditionTypeRegistry.Register<MyCustomCondition>("MyCondition");
}
```

**不推荐做法：**
```csharp
// ❌ 不推荐：使用已弃用的序列化器
string json = ContainerJsonSerializer.ToJson(container); // 已弃用
```

---

### 4. 使用只读属性访问槽位

**推荐做法：**
```csharp
// ✅ 推荐：通过只读接口访问
IReadOnlyList<ISlot> slots = container.Slots;
```

**不推荐做法：**
```csharp
// ❌ 不推荐：直接修改内部列表（如果暴露）
container._slots[0].ClearSlot(); // 破坏封装
```

---

## 延伸阅读

- [API 参考文档](./APIReference.md) - 查阅完整 API 方法签名和参数
- [Mermaid 图集](./Diagrams.md) - 查看系统架构和数据流图

---

**维护者：** NEKOPACK 团队  
**贡献者：** CutrelyAlex  
**反馈渠道：** [GitHub Issues](https://github.com/CutrelyAlex/NEKOPACK-GITHUB/issues)
