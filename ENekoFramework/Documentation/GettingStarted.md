# ENekoFramework 快速入门指南

## 目录

1. [概述](#概述)
2. [安装](#安装)
3. [核心概念](#核心概念)
4. [快速开始](#快速开始)
5. [完整示例](#完整示例)
6. [下一步](#下一步)

---

## 概述

**ENekoFramework** 是一个为 Unity 设计的轻量级架构框架

## 核心概念

### 1. 架构 (Architecture)

继承 `ENekoArchitecture<T>` 创建你的游戏架构：

```csharp
public class GameArchitecture : ENekoArchitecture<GameArchitecture>
{
    protected override void OnInit()
    {
        // 在这里注册所有服务
        RegisterService<IBuffSystem, BuffSystem>();
        RegisterService<IInventorySystem, InventorySystem>();
    }
}
```

**单例访问**：`GameArchitecture.Instance`

### 2. 服务 (Service)

服务是框架的核心模块，必须实现 `IService` 接口：

```csharp
public interface IMyService : IService
{
    void DoSomething();
}

public class MyService : BaseService, IMyService
{
    public void DoSomething()
    {
        // 业务逻辑
    }
}
```

**注册服务**：在 `OnInit()` 中调用 `RegisterService<TInterface, TImplementation>()`

**获取服务**：`await architecture.GetServiceAsync<IMyService>()`

### 3. 命令 (Command)

命令用于 **异步修改状态**，支持超时和取消：

```csharp
public class ApplyDamageCommand : ICommand<bool>
{
    private readonly IHealthSystem _healthSystem;
    private readonly int _damage;

    public ApplyDamageCommand(IHealthSystem healthSystem, int damage)
    {
        _healthSystem = healthSystem;
        _damage = damage;
    }

    public async Task<bool> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        await Task.Delay(100, cancellationToken); // 模拟网络请求
        _healthSystem.TakeDamage(_damage);
        return true;
    }
}
```

**执行命令**：`await architecture.SendCommandAsync(new ApplyDamageCommand(healthSystem, 50))`

**超时设置**：`await architecture.SendCommandAsync(command, timeoutSeconds: 10)`

### 4. 查询 (Query)

查询用于 **同步读取数据**：

```csharp
public class GetHealthQuery : IQuery<int>
{
    private readonly IHealthSystem _healthSystem;

    public GetHealthQuery(IHealthSystem healthSystem)
    {
        _healthSystem = healthSystem;
    }

    public int Execute()
    {
        return _healthSystem.GetCurrentHealth();
    }
}
```

**执行查询**：`int health = architecture.ExecuteQuery(new GetHealthQuery(healthSystem))`

### 5. 事件 (Event)

事件用于 **At-most-once 广播**，使用 WeakReference 防止内存泄漏：

```csharp
public class PlayerDeathEvent : IEvent
{
    public DateTime Timestamp { get; }
    public string PlayerId { get; set; }

    public PlayerDeathEvent(string playerId)
    {
        Timestamp = DateTime.UtcNow;
        PlayerId = playerId;
    }
}
```

**发布事件**：`architecture.PublishEvent(new PlayerDeathEvent("player_001"))`

**订阅事件**：`architecture.SubscribeEvent<PlayerDeathEvent>(OnPlayerDeath)`

**取消订阅**：`architecture.UnsubscribeEvent<PlayerDeathEvent>(OnPlayerDeath)`

---

## 快速开始

### Step 1: 定义服务接口

```csharp
public interface IScoreSystem : IService
{
    void AddScore(int points);
    int GetScore();
}
```

### Step 2: 实现服务

```csharp
public class ScoreSystem : BaseService, IScoreSystem
{
    private int _score = 0;

    public void AddScore(int points)
    {
        _score += points;
    }

    public int GetScore()
    {
        return _score;
    }
}
```

### Step 3: 创建架构并注册服务

```csharp
public class GameArchitecture : ENekoArchitecture<GameArchitecture>
{
    protected override void OnInit()
    {
        RegisterService<IScoreSystem, ScoreSystem>();
    }
}
```

### Step 4: 在 MonoBehaviour 中使用

```csharp
public class GameController : MonoBehaviour
{
    private async void Start()
    {
        // 获取架构实例
        var architecture = GameArchitecture.Instance;

        // 解析服务
        var scoreSystem = await architecture.GetServiceAsync<IScoreSystem>();

        // 使用服务
        scoreSystem.AddScore(100);
        Debug.Log($"当前分数: {scoreSystem.GetScore()}");
    }
}
```

---

## 完整工作示例

将以下代码复制到 Unity 项目的任意 MonoBehaviour 脚本中即可运行：

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;
using EasyPack.ENekoFramework;
using UnityEngine;

// 1. 定义服务
public interface IScoreSystem : IService
{
    void AddScore(int points);
    int GetScore();
}

public class ScoreSystem : BaseService, IScoreSystem
{
    private int _score = 0;
    public void AddScore(int points) => _score += points;
    public int GetScore() => _score;
}

// 2. 定义命令/查询/事件
public class AddScoreCommand : ICommand<bool>
{
    private readonly IScoreSystem _scoreSystem;
    private readonly int _points;

    public AddScoreCommand(IScoreSystem scoreSystem, int points)
    {
        _scoreSystem = scoreSystem;
        _points = points;
    }

    public async Task<bool> ExecuteAsync(CancellationToken ct = default)
    {
        await Task.Delay(50, ct);
        _scoreSystem.AddScore(_points);
        return true;
    }
}

public class GetScoreQuery : IQuery<int>
{
    private readonly IScoreSystem _scoreSystem;
    public GetScoreQuery(IScoreSystem scoreSystem) => _scoreSystem = scoreSystem;
    public int Execute() => _scoreSystem.GetScore();
}

public class ScoreChangedEvent : IEvent
{
    public DateTime Timestamp { get; }
    public int NewScore { get; set; }
    public ScoreChangedEvent(int newScore)
    {
        Timestamp = DateTime.UtcNow;
        NewScore = newScore;
    }
}

// 3. 创建架构
public class GameArchitecture : ENekoArchitecture<GameArchitecture>
{
    protected override void OnInit()
    {
        RegisterService<IScoreSystem, ScoreSystem>();
    }
}

// 4. 使用示例
public class ExampleController : MonoBehaviour
{
    private async void Start()
    {
        var arch = GameArchitecture.Instance;
        var scoreSystem = await arch.GetServiceAsync<IScoreSystem>();

        // 订阅事件
        arch.SubscribeEvent<ScoreChangedEvent>(evt => 
            Debug.Log($"分数变化: {evt.NewScore}"));

        // 执行命令
        await arch.SendCommandAsync(new AddScoreCommand(scoreSystem, 100));

        // 执行查询
        int score = arch.ExecuteQuery(new GetScoreQuery(scoreSystem));
        Debug.Log($"当前分数: {score}");

        // 发布事件
        arch.PublishEvent(new ScoreChangedEvent(score));
    }
}
```

---

## 下一步

### Q: 如何在服务间通信？

**A**: 使用 Command/Query/Event 模式：

```csharp
// 在 BuffSystem 中广播事件
architecture.PublishEvent(new BuffAppliedEvent("strength", 50));

// 在 InventorySystem 中订阅事件
architecture.SubscribeEvent<BuffAppliedEvent>(evt => {
    Debug.Log($"检测到 Buff: {evt.BuffId}");
});
```

### Q: 如何处理异步初始化？

**A**: 在 `BaseService` 的 `OnInitializeAsync()` 中实现：

```csharp
public class DataService : BaseService, IDataService
{
    protected override async Task OnInitializeAsync()
    {
        await LoadDataFromServer();
    }
}
```

### Q: 如何避免内存泄漏？

**A**: 框架自动使用 WeakReference 管理事件订阅，但仍建议在 `OnDestroy()` 中手动取消订阅：

```csharp
private void OnDestroy()
{
    GameArchitecture.Instance.UnsubscribeEvent<MyEvent>(OnMyEvent);
}
```

---

Happy Coding! 🐱
