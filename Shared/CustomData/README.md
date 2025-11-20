# CustomData 系统

高性能的 Unity 自定义数据存储和查询系统。支持多种数据类型、O(1) 查询、智能缓存和灵活的序列化。

## 核心类

| 类 | 用途 |
|-----|------|
| `CustomDataCollection` | 数据存储容器，基于双缓存架构 |
| `CustomDataEntry` | 单个数据条目 |
| `CustomDataUtility` | 工具方法集 |

## 快速开始

### 基础使用

```csharp
// 创建集合
var data = new CustomDataCollection();

// 设置数据
data.SetValue("health", 100f);
data.SetValue("level", 10);

// 获取数据
float health = CustomDataUtility.GetValue(data, "health", 0f);
int level = CustomDataUtility.GetValue(data, "level", 1);
```

### 查询

```csharp
// 单个查询
float value = CustomDataUtility.GetValue(data, "key", 0f);

// 批量查询
var values = CustomDataUtility.GetValues(data, keys, 0f);

// 热点缓存
var cache = new Dictionary<string, float>();
for (int i = 0; i < frames; i++)
{
    float hp = CustomDataUtility.GetValueCached(data, "hp", cache);
}

// 条件查询
var valuable = CustomDataUtility.GetFirstValue(data, (k, v) => v > 1000, 0f);
```
## 最佳实践

### 1. 热点访问（UI 更新），性能最好
- 注意过期数据风险
- 使用cache.remove(key)或cache.clear()刷新缓存
```csharp
private Dictionary<string, float> statsCache = new();

void Update()
{
    float hp = CustomDataUtility.GetValueCached(playerData, "hp", statsCache);
    float mp = CustomDataUtility.GetValueCached(playerData, "mp", statsCache);
}
```

### 2. 初始化多个属性

```csharp
// 一次性查询多个键比多次单查更快
var keys = new[] { "hp", "mp", "speed", "attack" };
var stats = CustomDataUtility.GetValues(playerData, keys, 0f);
```

### 3. 偶尔查询

```csharp
float level = CustomDataUtility.GetValue(playerData, "level", 1);
```

### 4. 条件查询

```csharp
// 查找满足条件的第一个值
var loot = CustomDataUtility.GetFirstValue(
    itemData, 
    (key, value) => value > 1000,
    0
);
```

## 支持的数据类型

- 原始类型：`int`, `float`, `bool`, `string`
- Unity 类型：`Vector2`, `Vector3`, `Color`
- 自定义类型：JSON 序列化（通过 `ICustomDataSerializer`）

```csharp
// 使用快捷方法
CustomDataUtility.SetInt(data, "level", 10);
CustomDataUtility.SetFloat(data, "health", 100f);
CustomDataUtility.SetVector3(data, "position", Vector3.zero);

var level = CustomDataUtility.GetInt(data, "level", 1);
var health = CustomDataUtility.GetFloat(data, "health", 100f);
var pos = CustomDataUtility.GetVector3(data, "position");
```

## 批量操作

```csharp
// 批量设置
var values = new Dictionary<string, object>
{
    { "hp", 100f },
    { "mp", 50f },
    { "level", 10 }
};
CustomDataUtility.SetValues(data, values);

// 批量获取
var hp = CustomDataUtility.GetValue(data, "hp", 0f);
var mp = CustomDataUtility.GetValue(data, "mp", 0f);

// 合并数据
CustomDataUtility.Merge(target, source);

// 深拷贝
var cloned = CustomDataUtility.Clone(original);
```

## 条件操作

```csharp
// 如果存在则执行
CustomDataUtility.IfHasValue<string>(data, "skill", skill => 
{
    UseSkill(skill);
});

// If-Else 操作
CustomDataUtility.IfElse<int>(data, "gold",
    gold => Debug.Log($"金币：{gold}"),
    () => Debug.Log("无金币")
);
```

## 内部架构

```
CustomDataCollection
├─ _list              : List<CustomDataEntry>      // 存储层
├─ _keyIndexMap       : Dict<string, int>          // 索引缓存（O(1) 删除）
└─ _entryCache        : Dict<string, Entry>        // 对象缓存（O(1) 读取）
```

### 性能特性

| 操作 | 复杂度 | 备注 |
|------|--------|------|
| 查询 (GetValue) | O(1) | 通过 _entryCache |
| 添加 (SetValue) | O(1) | 同步两个缓存 |
| 删除 (RemoveValue) | O(1) | 交换删除法 |
| 批删 (RemoveValues) | O(N+K) | 数组压缩 |
