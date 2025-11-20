# CategoryService 序列化架构说明

## 概述

CategoryService 的序列化系统完全集成了 EasyPack 的统一序列化架构，遵循以下设计原则：

1. **继承 `JsonSerializerBase<T>`**：所有序列化器继承自 EasyPack 的基类
2. **使用 `ISerializable` 标记 DTO**：序列化数据结构实现 `ISerializable` 接口
3. **统一注册管理**：通过 `SerializationService` 统一管理序列化器
4. **支持泛型类型**：CategoryService 是泛型类型，支持不同实体类型的序列化

## 架构组件

### 1. 序列化器 (CategoryServiceJsonSerializer<T>)

**位置**: `Assets/EasyPack/Services/CategoryService/Runtime/Serializers/CategoryServiceJsonSerializer.cs`

**职责**:
- 继承 `JsonSerializerBase<CategoryService<T>>`
- 实现 `SerializeToJson(CategoryService<T>)` 方法
- 实现 `DeserializeFromJson(string)` 方法
- 使用反射访问私有字段（`_tagIndex`, `_metadataStore`）

**特点**:
```csharp
public class CategoryServiceJsonSerializer<T> : JsonSerializerBase<CategoryService<T>>
{
    private readonly Func<T, string> _idExtractor;
    
    public override string SerializeToJson(CategoryService<T> service) { ... }
    public override CategoryService<T> DeserializeFromJson(string json) { ... }
}
```

### 2. 可序列化 DTO (SerializableCategoryServiceState<T>)

**职责**:
- 实现 `ISerializable` 接口标记
- 定义可 JSON 序列化的数据结构
- 包含版本号、实体列表、分类列表、标签列表、元数据列表

**结构**:
```csharp
[Serializable]
public class SerializableCategoryServiceState<T> : ISerializable
{
    public int Version;
    public List<SerializedEntity> Entities;
    public List<SerializedCategory> Categories;
    public List<SerializedTag> Tags;
    public List<SerializedMetadata> Metadata;
}
```

### 3. 序列化初始化器 (CategoryServiceSerializationInitializer)

**位置**: `Assets/EasyPack/Services/CategoryService/Runtime/Serializers/CategoryServiceSerializationInitializer.cs`

**职责**:
- 提供静态方法注册序列化器到 `SerializationService`
- 支持泛型类型的动态注册
- 模仿 `GamePropertySerializationInitializer` 的设计

**用法**:
```csharp
// 在 SerializationService 初始化时调用
CategoryServiceSerializationInitializer.RegisterSerializer<Item>(
    serializationService,
    item => item.Id
);
```

## 使用方式

### 方式 1: 直接序列化（推荐用于简单场景）

```csharp
var service = new CategoryService<Item>(item => item.Id);

// 注册实体
service.RegisterEntity(item, "Category").Complete();

// 序列化
string json = service.SerializeToJson();

// 从 JSON 创建新实例
var newService = CategoryService<Item>.CreateFromJson(json, item => item.Id);
```

### 方式 2: 使用 SerializationService（推荐用于大型项目）

```csharp
// 1. 获取序列化服务
var serializationService = this.GetService<ISerializationService>();

// 2. 注册序列化器
CategoryServiceSerializationInitializer.RegisterSerializer<Item>(
    serializationService,
    item => item.Id
);

// 3. 使用统一接口序列化
string json = serializationService.Serialize(service);

// 4. 使用统一接口反序列化
var newService = serializationService.Deserialize<CategoryService<Item>>(json);
```

## 序列化数据格式

```json
{
    "Version": 1,
    "Entities": [
        {
            "Id": "sword",
            "EntityJson": "{\"Id\":\"sword\",\"Name\":\"神剑\",\"Price\":1000}",
            "Category": "Equipment.Weapon"
        }
    ],
    "Categories": [
        { "Name": "Equipment.Weapon" }
    ],
    "Tags": [
        {
            "TagName": "legendary",
            "EntityIds": ["sword"]
        }
    ],
    "Metadata": [
        {
            "EntityId": "sword",
            "MetadataJson": "{\"Entries\":[...]}"
        }
    ]
}
```

## 版本兼容性

- **当前版本**: 1
- **向后兼容**: 支持读取旧版本数据
- **向前兼容**: 拒绝读取高版本数据，抛出 `SerializationException`

## 与其他系统对比

| 特性 | GameProperty | CategoryService |
|------|--------------|-----------------|
| 基类 | `JsonSerializerBase<GameProperty>` | `JsonSerializerBase<CategoryService<T>>` |
| DTO | `SerializableGameProperty` | `SerializableCategoryServiceState<T>` |
| 初始化器 | `GamePropertySerializationInitializer` | `CategoryServiceSerializationInitializer` |
| 泛型支持 | 否 | 是（需要指定实体类型） |
| 依赖注入 | 自动注册 | 手动注册（因泛型） |

## 注意事项

1. **泛型限制**：CategoryService 是泛型类型，无法在 `SerializationService.OnInitializeAsync()` 中预注册所有类型
2. **手动注册**：需要在使用前手动注册具体的泛型实例，如 `CategoryService<Item>`
3. **反射使用**：序列化器使用反射访问私有字段，确保字段名称不变
4. **新实例创建**：`DeserializeFromJson` 创建新的 CategoryService 实例，不修改原实例

## 测试示例

详见：
- `Phase5To9Test.cs` - 功能测试
- `CategoryServiceSerializationExample.cs` - 集成示例

## 相关文件

- `CategoryServiceJsonSerializer.cs` - 序列化器实现
- `CategoryServiceSerializationInitializer.cs` - 注册器
- `CategoryService.cs` - 集成序列化方法
- `SerializationService.cs` - EasyPack 序列化服务
- `TypeSerializerBase.cs` - EasyPack 序列化基类
