# Inventory System - API 参考文档 (Part 3)

**适用EasyPack版本：** EasyPack v1.7.0  
**最后更新：** 2025-11-10

---

## 概述

本文档是 **Inventory System** API 参考文档的第三部分，包含物品与网格系统。

**文档系列：**
- **Part 1 - 核心接口与服务类**：系统的服务层和核心接口定义
- **Part 2 - 容器实现类**：容器的具体实现类和缓存服务
- **Part 3 - 物品与网格系统**（当前文档）：物品类、网格物品和查询服务
- **Part 4 - 条件、工具与序列化**：条件系统、工具类和序列化支持

---

## 目录

- [概述](#概述)
- [物品类](#物品类)
  - [Item 类](#item-类)
  - [GridItem 类](#griditem-类)
- [管理器类](#管理器类)
  - [InventoryManager 类](#inventorymanager-类)
- [查询服务](#查询服务)
  - [ItemQueryService 类](#itemqueryservice-类)

---

## 物品类

### Item 类

**命名空间：** `EasyPack.InventorySystem`

**继承关系：**
```
System.Object
  └─ Item (implements IItem)
```

**说明：**  
代表游戏中的物品实例，包含物品的基本属性（ID、名称、类型）、堆叠设置、重量、自定义属性等。

---

#### 属性

##### `string ID { get; set; }`

**说明：** 物品的唯一标识符，用于区分不同物品。

**类型：** `string`

**默认值：** `null`

**使用示例：**
```csharp
var item = new Item { ID = "health_potion" };
```

---

##### `string Name { get; set; }`

**说明：** 物品的显示名称。

**类型：** `string`

**默认值：** `null`

---

##### `string Type { get; set; }`

**说明：** 物品类型，用于分类和条件过滤。

**类型：** `string`

**默认值：** `"Default"`

**使用示例：**
```csharp
var sword = new Item { ID = "sword", Type = "Weapon" };
var potion = new Item { ID = "potion", Type = "Consumable" };
```

---

##### `string Description { get; set; }`

**说明：** 物品描述文本。

**类型：** `string`

**默认值：** `""`

---

##### `float Weight { get; set; }`

**说明：** 物品重量，可用于负重系统。

**类型：** `float`

**默认值：** `1f`

---

##### `bool IsStackable { get; set; }`

**说明：** 物品是否可堆叠。

**类型：** `bool`

**默认值：** `true`

**使用示例：**
```csharp
var arrow = new Item { IsStackable = true, MaxStackCount = 20 };
var sword = new Item { IsStackable = false };
```

---

##### `int MaxStackCount { get; set; }`

**说明：** 单个槽位的最大堆叠数量，`-1` 表示无限堆叠。

**类型：** `int`

**默认值：** `-1`

---

##### `List<CustomDataEntry> CustomData { get; set; }`

**说明：** 物品自定义数据列表，使用 EasyPack 的 CustomDataEntry 系统存储多种类型的数据。  
支持的类型包括：int、float、bool、string、Vector2、Vector3、Color 以及自定义 JSON 序列化对象。

**类型：** `List<CustomDataEntry>`

**默认值：** 空列表

**使用示例：**
```csharp
var item = new Item();

// 设置自定义数据
item.SetCustomData("Rarity", "Legendary");
item.SetCustomData("Level", 50);
item.SetCustomData("Durability", 100f);
item.SetCustomData("Position", new Vector3(1, 2, 3));

// 获取自定义数据
string rarity = item.GetCustomData<string>("Rarity");
int level = item.GetCustomData<int>("Level", defaultValue: 1);
float durability = item.GetCustomData<float>("Durability");

// 检查是否存在
if (item.HasCustomData("Level"))
{
    Debug.Log("物品有等级属性");
}

// 移除自定义数据
item.RemoveCustomData("Durability");
```

**迁移说明：**  
旧版本使用 `Dictionary<string, object> Attributes`，新版本改用 `List<CustomDataEntry> CustomData`。  
如果需要访问旧数据，请使用 `GetCustomData<T>()` 和 `SetCustomData()` 方法。

---

##### `bool IsContainerItem { get; set; }`

**说明：** 标记该物品是否为容器类物品（如背包、箱子）。

**类型：** `bool`

**默认值：** `false`

---

##### `List<string> ContainerIds { get; set; }`

**说明：** 容器类物品关联的容器 ID 列表。

**类型：** `List<string>`

**默认值：** `null`

---

#### 方法

##### `IItem Clone()`

**说明：** 创建物品的深拷贝副本，包括所有自定义数据。

**返回值：**
- **类型：** `IItem`
- **说明：** 返回一个新的物品实例，包含所有属性和自定义数据的副本

**使用示例：**
```csharp
var originalItem = new Item { ID = "potion", Name = "药水" };
originalItem.SetCustomData("Quality", 5);

var clonedItem = originalItem.Clone();

// 修改克隆不影响原物品
clonedItem.Name = "高级药水";
Debug.Log(originalItem.Name); // "药水"
```

---

##### `T GetCustomData<T>(string id, T defaultValue = default)`

**说明：** 获取指定 ID 的自定义数据值。

**参数：**
- `id` - 自定义数据的标识符
- `defaultValue` - 如果数据不存在时返回的默认值

**返回值：**
- **类型：** `T`
- **说明：** 返回指定类型的数据值，如果不存在或类型不匹配则返回默认值

**使用示例：**
```csharp
int level = item.GetCustomData<int>("Level", defaultValue: 1);
string rarity = item.GetCustomData<string>("Rarity", defaultValue: "Common");
```

---

##### `void SetCustomData(string id, object value)`

**说明：** 设置自定义数据值。如果 ID 已存在，则更新值；否则添加新条目。

**参数：**
- `id` - 自定义数据的标识符
- `value` - 要存储的值（支持 int、float、bool、string、Vector2、Vector3、Color 及 JSON 序列化对象）

**使用示例：**
```csharp
item.SetCustomData("Level", 50);
item.SetCustomData("Rarity", "Legendary");
item.SetCustomData("Position", new Vector3(1, 2, 3));
```

---

##### `bool RemoveCustomData(string id)`

**说明：** 移除指定 ID 的自定义数据。

**参数：**
- `id` - 要移除的自定义数据标识符

**返回值：**
- **类型：** `bool`
- **说明：** 如果成功移除返回 `true`，如果数据不存在返回 `false`

**使用示例：**
```csharp
bool removed = item.RemoveCustomData("Durability");
```

---

##### `bool HasCustomData(string id)`

**说明：** 检查是否存在指定 ID 的自定义数据。

**参数：**
- `id` - 要检查的自定义数据标识符

**返回值：**
- **类型：** `bool`
- **说明：** 如果存在返回 `true`，否则返回 `false`

**使用示例：**
```csharp
if (item.HasCustomData("Level"))
{
    int level = item.GetCustomData<int>("Level");
    Debug.Log($"物品等级: {level}");
}
```

---



### GridItem 类

**命名空间：** `EasyPack.InventorySystem`

**继承关系：**
```
Item
  └─ GridItem
```

**说明：**  
网格物品，继承自 `Item` 并添加形状、旋转等网格布局属性。支持矩形和任意复杂形状。

---

#### 属性

##### `List<(int x, int y)> Shape { get; set; }`

**说明：** 物品的形状，由占据的单元格坐标列表组成（相对于左上角原点）。默认为 1x1 单格物品。

**类型：** `List<(int x, int y)>`

**默认值：** `new List<(int x, int y)> { (0, 0) }`

**使用示例：**

```csharp
// 创建 2x2 矩形物品
var armor = new GridItem
{
    ID = "plate_armor",
    Name = "板甲",
    Shape = GridItem.CreateRectangleShape(2, 2)
};

// 创建 L 形物品
var lShapedKey = new GridItem
{
    ID = "l_shaped_key",
    Name = "L形钥匙",
    Shape = new List<(int x, int y)>
    {
        (0, 0), (1, 0),  // 水平的两格
        (0, 1)           // 下方的一格
    }
};

// 创建十字形物品
var cross = new GridItem
{
    ID = "cross_item",
    Name = "十字形物品",
    Shape = new List<(int x, int y)>
    {
        (1, 0),          // 上
        (0, 1), (1, 1), (2, 1),  // 中间行
        (1, 2)           // 下
    }
};
```

---

##### `bool CanRotate { get; set; }`

**说明：** 物品是否可以旋转。

**类型：** `bool`

**默认值：** `false`

---

##### `RotationAngle Rotation { get; set; }`

**说明：** 物品当前旋转角度。

**类型：** `RotationAngle` (枚举值：`Rotate0`、`Rotate90`、`Rotate180`、`Rotate270`)

**默认值：** `RotationAngle.Rotate0`

**使用示例：**

```csharp
var staff = new GridItem
{
    ID = "staff",
    Name = "法杖",
    Shape = GridItem.CreateRectangleShape(1, 3),  // 竖直 1x3
    CanRotate = true
};

// 旋转 90 度（变为 3x1）
staff.Rotate();
Debug.Log($"旋转后 - 宽度: {staff.ActualWidth}, 高度: {staff.ActualHeight}");
```

---

##### `int ActualWidth { get; }`

**说明：** 物品当前实际占用的宽度（考虑旋转）。

**类型：** `int`

**访问权限：** 只读

---

##### `int ActualHeight { get; }`

**说明：** 物品当前实际占用的高度（考虑旋转）。

**类型：** `int`

**访问权限：** 只读

---

#### 方法

##### `static List<(int x, int y)> CreateRectangleShape(int width, int height)`

**说明：** 辅助方法，快速创建矩形形状的单元格列表。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `width` | `int` | 必填 | 矩形宽度 | - |
| `height` | `int` | 必填 | 矩形高度 | - |

**返回值：**
- **类型：** `List<(int x, int y)>`
- **说明：** 包含所有单元格坐标的列表

**使用示例：**

```csharp
// 快速创建 3x2 矩形
var shape = GridItem.CreateRectangleShape(3, 2);
// 结果：[(0,0), (1,0), (2,0), (0,1), (1,1), (2,1)]

var backpack = new GridItem
{
    ID = "backpack",
    Name = "背包",
    Shape = GridItem.CreateRectangleShape(2, 3)
};
```

---

##### `List<(int x, int y)> GetOccupiedCells()`

**说明：** 获取物品当前占据的单元格列表（考虑旋转）。

**返回值：**
- **类型：** `List<(int x, int y)>`
- **说明：** 占据的单元格坐标列表

**使用示例：**

```csharp
var item = new GridItem
{
    ID = "item",
    Shape = GridItem.CreateRectangleShape(2, 2),
    CanRotate = true
};

var cells = item.GetOccupiedCells();
Debug.Log($"物品占据 {cells.Count} 个格子");

item.Rotate();  // 旋转 90 度
var cellsAfterRotation = item.GetOccupiedCells();
Debug.Log($"旋转后仍占据 {cellsAfterRotation.Count} 个格子");
```

---

##### `bool Rotate()`

**说明：** 顺时针旋转物品 90 度（如果启用了 `CanRotate`）。

**返回值：**
- **类型：** `bool`
- **说明：** 旋转是否成功（如果未启用 `CanRotate` 则返回 `false`）

**使用示例：**

```csharp
var staff = new GridItem
{
    ID = "staff",
    Name = "法杖",
    Shape = GridItem.CreateRectangleShape(1, 3),
    CanRotate = true
};

if (staff.Rotate())
{
    Debug.Log($"旋转成功！新大小：{staff.ActualWidth}x{staff.ActualHeight}");
}
```

---

##### `bool SetRotation(RotationAngle angle)`

**说明：** 设置物品的旋转角度。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `angle` | `RotationAngle` | 必填 | 目标旋转角度 | - |

**返回值：**
- **类型：** `bool`
- **说明：** 设置是否成功

**使用示例：**

```csharp
var item = new GridItem
{
    ID = "item",
    Shape = GridItem.CreateRectangleShape(1, 3),
    CanRotate = true
};

// 直接设置为 180 度旋转
item.SetRotation(RotationAngle.Rotate180);
Debug.Log($"当前旋转角度: {item.Rotation}");
```

---

##### `new GridItem Clone()`

**说明：** 克隆网格物品（包括形状、旋转、所有属性）。

**返回值：**
- **类型：** `GridItem`
- **说明：** 新的物品副本

**使用示例：**

```csharp
var original = new GridItem { ID = "sword", Shape = GridItem.CreateRectangleShape(1, 2) };
var copy = original.Clone();
copy.ID = "sword_copy";
```

---



---

## 管理器类

### InventoryManager 类

**命名空间：** `EasyPack.InventorySystem`

**说明：**  
管理多个容器的中央系统，提供容器注册、跨容器操作、全局搜索等功能。

---

#### 方法

##### `bool RegisterContainer(Container container, int priority = 0, string category = "Default")`

**说明：** 注册容器到管理器。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `container` | `Container` | 必填 | 要注册的容器 | - |
| `priority` | `int` | 可选 | 容器优先级（数值越高优先级越高） | `0` |
| `category` | `string` | 可选 | 容器分类 | `"Default"` |

**返回值：**
- **类型：** `bool`
- **说明：** 注册是否成功

**使用示例：**

```csharp
var manager = new InventoryManager();
var backpack = new LinerContainer("bp", "背包", "Backpack", 20);

manager.RegisterContainer(backpack, priority: 1, category: "Player");
```

---

##### `bool UnregisterContainer(string containerId)`

**说明：** 注销指定容器。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `containerId` | `string` | 必填 | 容器 ID | - |

**返回值：**
- **类型：** `bool`
- **说明：** 注销是否成功

---

##### `Container GetContainer(string containerId)`

**说明：** 获取指定 ID 的容器。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `containerId` | `string` | 必填 | 容器 ID | - |

**返回值：**
- **类型：** `Container`
- **说明：** 容器实例，未找到时返回 `null`

---

##### `List<Container> GetAllContainers()`

**说明：** 获取所有已注册的容器列表。

**返回值：**
- **类型：** `List<Container>`
- **说明：** 所有注册容器的列表，按优先级排序

**使用示例：**

```csharp
var allContainers = manager.GetAllContainers();
Debug.Log($"总共有 {allContainers.Count} 个容器");

foreach (var container in allContainers)
{
    Debug.Log($"{container.Name}: {container.UsedSlots}/{container.Capacity} 槽位");
}
```

---

##### `List<Container> GetContainersByType(string containerType)`

**说明：** 按类型获取容器列表。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `containerType` | `string` | 必填 | 容器类型 | - |

**返回值：**
- **类型：** `List<Container>`
- **说明：** 指定类型的容器列表

---

##### `List<Container> GetContainersByCategory(string category)`

**说明：** 按分类获取容器列表。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `category` | `string` | 必填 | 容器分类 | - |

**返回值：**
- **类型：** `List<Container>`
- **说明：** 指定分类的容器列表

---

##### `bool TransferItems(string sourceContainerId, string targetContainerId, string itemId, int count)`

**说明：** 在两个容器间转移物品。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `sourceContainerId` | `string` | 必填 | 源容器 ID | - |
| `targetContainerId` | `string` | 必填 | 目标容器 ID | - |
| `itemId` | `string` | 必填 | 物品 ID | - |
| `count` | `int` | 必填 | 转移数量 | - |

**返回值：**
- **类型：** `bool`
- **说明：** 转移是否成功

**使用示例：**

```csharp
bool success = manager.TransferItems("backpack", "warehouse", "iron_ore", 50);
if (success)
{
    Debug.Log("转移成功");
}
```

---

##### `List<Container> FindItemInContainers(string itemId)`

**说明：** 在所有注册的容器中搜索包含指定物品的容器。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `itemId` | `string` | 必填 | 物品 ID | - |

**返回值：**
- **类型：** `List<Container>`
- **说明：** 包含该物品的容器列表

---

##### `int GetItemTotalCountAcrossContainers(string itemId)`

**说明：** 获取物品在所有容器中的总数量。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `itemId` | `string` | 必填 | 物品 ID | - |

**返回值：**
- **类型：** `int`
- **说明：** 总数量

**使用示例：**

```csharp
int totalGold = manager.GetItemTotalCountAcrossContainers("gold_coin");
Debug.Log($"所有容器中的金币总数：{totalGold}");
```

---

##### `Dictionary<string, int> DistributeItems(IItem item, int totalCount, List<string> targetContainerIds)`

**说明：** 将物品分配到多个容器中（按优先级和剩余空间分配）。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `item` | `IItem` | 必填 | 要分配的物品 | - |
| `totalCount` | `int` | 必填 | 总数量 | - |
| `targetContainerIds` | `List<string>` | 必填 | 目标容器 ID 列表 | - |

**返回值：**
- **类型：** `Dictionary<string, int>`
- **说明：** 分配结果，键为容器 ID，值为分配到的数量

**使用示例：**

```csharp
var coinItem = new Item { ID = "coin", Name = "金币", IsStackable = true };
var distribution = inventoryService.DistributeItems(coinItem, 100, new List<string> { "bp1", "bp2" });
foreach (var kvp in distribution)
{
    Debug.Log($"容器 {kvp.Key} 分配到 {kvp.Value} 个金币");
}
```

---

##### `int GetGlobalItemCount(string itemId)`

**说明：** 获取指定物品在所有容器中的总数量。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `itemId` | `string` | 必填 | 物品 ID | - |

**返回值：**
- **类型：** `int`
- **说明：** 物品总数量

---

##### `Dictionary<string, int> FindContainersWithItem(string itemId)`

**说明：** 查找包含指定物品的所有容器及其数量。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `itemId` | `string` | 必填 | 物品 ID | - |

**返回值：**
- **类型：** `Dictionary<string, int>`
- **说明：** 键为容器 ID，值为物品数量

---

##### `List<GlobalItemResult> SearchItemsByCondition(System.Func<IItem, bool> condition)`

**说明：** 根据条件在所有容器中搜索物品。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `condition` | `System.Func<IItem, bool>` | 必填 | 搜索条件 | - |

**返回值：**
- **类型：** `List<GlobalItemResult>`
- **说明：** 符合条件的物品结果列表

---

##### `List<GlobalItemResult> SearchItemsByType(string itemType)`

**说明：** 根据物品类型在所有容器中搜索物品。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `itemType` | `string` | 必填 | 物品类型 | - |

**返回值：**
- **类型：** `List<GlobalItemResult>`
- **说明：** 指定类型的物品结果列表

---

##### `List<GlobalItemResult> SearchItemsByName(string namePattern)`

**说明：** 根据名称模式在所有容器中搜索物品。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `namePattern` | `string` | 必填 | 名称模式 | - |

**返回值：**
- **类型：** `List<GlobalItemResult>`
- **说明：** 名称匹配的物品结果列表

---

##### `List<GlobalItemResult> SearchItemsByAttribute(string attributeName, object attributeValue)`

**说明：** 根据属性在所有容器中搜索物品。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `attributeName` | `string` | 必填 | 属性名称 | - |
| `attributeValue` | `object` | 必填 | 属性值 | - |

**返回值：**
- **类型：** `List<GlobalItemResult>`
- **说明：** 属性匹配的物品结果列表

---

##### `List<(MoveRequest request, MoveResult result, int movedCount)> BatchMoveItems(List<MoveRequest> requests)`

**说明：** 批量移动多个物品。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `requests` | `List<MoveRequest>` | 必填 | 移动请求列表 | - |

**返回值：**
- **类型：** `List<(MoveRequest request, MoveResult result, int movedCount)>`
- **说明：** 每个请求的移动结果

---

##### `(MoveResult result, int transferredCount) AutoMoveItem(string itemId, string fromContainerId, string toContainerId)`

**说明：** 自动在容器之间移动物品。

**参数：**

| 参数名 | 类型 | 必填/可选 | 说明 | 默认值 |
|--------|------|----------|------|--------|
| `itemId` | `string` | 必填 | 物品 ID | - |
| `fromContainerId` | `string` | 必填 | 源容器 ID | - |
| `toContainerId` | `string` | 必填 | 目标容器 ID | - |

**返回值：**
- **类型：** `(MoveResult result, int transferredCount)`
- **说明：** 移动结果和转移数量

---

#### 事件

##### `event Action<Container> OnContainerRegistered`

**说明：** 容器注册事件。

**参数：** `Container container` - 注册的容器

---

##### `event Action<Container> OnContainerUnregistered`

**说明：** 容器注销事件。

**参数：** `Container container` - 注销的容器

---



---

## 查询服务

### ItemQueryService 类

**命名空间：** `EasyPack.InventorySystem`

**说明：**  
物品查询服务实现，提供高效的物品查询功能。通过集成缓存服务来加速查询操作，支持基础查询、位置查询、高级查询和聚合查询。


