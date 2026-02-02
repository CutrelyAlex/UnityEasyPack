using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EasyPack.ENekoFramework;

namespace TestScripts.ENekoFramework.Mocks
{
    // ============================================================
    // Mock Services (用于服务容器测试)
    // ============================================================

    public interface IMockService : IService
    {
        string GetData();
    }

    public class MockService : BaseService, IMockService
    {
        public string GetData() => "MockData";
    }

    // ============================================================
    // Mock Commands (用于命令调度器测试)
    // ============================================================

    /// <summary>
    /// 简单的 Mock 命令，返回字符串
    /// </summary>
    public class MockCommand : ICommand<string>
    {
        private readonly int _delayMs;

        public MockCommand(int delayMs = 100)
        {
            _delayMs = delayMs;
        }

        public async Task<string> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            await Task.Delay(_delayMs, cancellationToken);
            return "CommandExecuted";
        }
    }

    /// <summary>
    /// 超时 Mock 命令 (延迟10秒，用于测试超时)
    /// </summary>
    public class MockTimeoutCommand : ICommand<string>
    {
        public async Task<string> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            await Task.Delay(10000, cancellationToken); // 10 seconds
            return "ShouldNotReachHere";
        }
    }

    // ============================================================
    // Mock Queries (用于查询执行器测试)
    // ============================================================

    /// <summary>
    /// 简单的整数查询
    /// </summary>
    public class MockQuery : IQuery<int>
    {
        public int Execute() => 42;
    }

    /// <summary>
    /// 字符串查询
    /// </summary>
    public class MockStringQuery : IQuery<string>
    {
        public string Execute() => "QueryResult";
    }

    /// <summary>
    /// 复杂对象查询
    /// </summary>
    public class MockDataQuery : IQuery<MockData>
    {
        public MockData Execute()
        {
            return new MockData
            {
                Id = 100,
                Name = "TestData"
            };
        }
    }

    public class MockData
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    // ============================================================
    // Mock Events (用于事件总线测试)
    // ============================================================

    public class MockEvent : IEvent
    {
        public DateTime Timestamp { get; }
        public string Message { get; set; }

        public MockEvent(string message)
        {
            Timestamp = DateTime.UtcNow;
            Message = message;
        }
    }

    /// <summary>
    /// Test 事件 (用于事件监控测试)
    /// </summary>
    public class TestEvent : IEvent
    {
        public DateTime Timestamp { get; }
        public string Message { get; set; }

        public TestEvent()
        {
            Timestamp = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Buff 变化事件 (用于事件监控测试)
    /// </summary>
    public class BuffsChangedEvent : IEvent
    {
        public DateTime Timestamp { get; }
        public int BuffCount { get; set; }
        public List<string> RemovedBuffs { get; set; }

        public BuffsChangedEvent()
        {
            Timestamp = DateTime.UtcNow;
            RemovedBuffs = new List<string>();
        }
    }

    // ============================================================
    // Mock Systems (用于集成测试)
    // ============================================================

    /// <summary>
    /// Mock Buff 系统 (模拟 Buff/属性修改)
    /// </summary>
    public interface IBuffSystem : IService
    {
        void ApplyBuff(string buffId, int value);
        int GetBuffValue(string buffId);
        List<string> GetActiveBuffs();
    }

    public class MockBuffSystem : BaseService, IBuffSystem
    {
        private readonly Dictionary<string, int> _buffs = new Dictionary<string, int>();

        public void ApplyBuff(string buffId, int value)
        {
            _buffs[buffId] = value;
        }

        public int GetBuffValue(string buffId)
        {
            return _buffs.TryGetValue(buffId, out var value) ? value : 0;
        }

        public List<string> GetActiveBuffs()
        {
            return new List<string>(_buffs.Keys);
        }
    }

    /// <summary>
    /// Mock Inventory 系统 (模拟物品/库存管理)
    /// </summary>
    public interface IInventorySystem : IService
    {
        void AddItem(string itemId, int count);
        int GetItemCount(string itemId);
        List<string> GetAllItems();
    }

    public class MockInventorySystem : BaseService, IInventorySystem
    {
        private readonly Dictionary<string, int> _inventory = new Dictionary<string, int>();

        public void AddItem(string itemId, int count)
        {
            if (_inventory.ContainsKey(itemId))
            {
                _inventory[itemId] += count;
            }
            else
            {
                _inventory[itemId] = count;
            }
        }

        public int GetItemCount(string itemId)
        {
            return _inventory.TryGetValue(itemId, out var count) ? count : 0;
        }

        public List<string> GetAllItems()
        {
            return new List<string>(_inventory.Keys);
        }
    }

    // ============================================================
    // Mock Commands/Queries/Events for Integration Tests
    // ============================================================

    /// <summary>
    /// 应用 Buff 的命令（性能测试专用 - 无延迟版本）
    /// </summary>
    public class FastApplyBuffCommand : ICommand<bool>
    {
        private readonly IBuffSystem _buffSystem;
        private readonly string _buffId;
        private readonly int _value;

        public FastApplyBuffCommand(IBuffSystem buffSystem, string buffId, int value)
        {
            _buffSystem = buffSystem;
            _buffId = buffId;
            _value = value;
        }

        public Task<bool> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            _buffSystem.ApplyBuff(_buffId, _value);
            return Task.FromResult(true);
        }
    }

    /// <summary>
    /// 应用 Buff 的命令
    /// </summary>
    public class ApplyBuffCommand : ICommand<bool>
    {
        private readonly IBuffSystem _buffSystem;
        private readonly string _buffId;
        private readonly int _value;

        public ApplyBuffCommand(IBuffSystem buffSystem, string buffId, int value)
        {
            _buffSystem = buffSystem;
            _buffId = buffId;
            _value = value;
        }

        public async Task<bool> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            await Task.Delay(50, cancellationToken); // 模拟异步操作
            _buffSystem.ApplyBuff(_buffId, _value);
            return true;
        }
    }

    /// <summary>
    /// 添加物品的命令（性能测试专用 - 无延迟版本）
    /// </summary>
    public class FastAddItemCommand : ICommand<bool>
    {
        private readonly IInventorySystem _inventorySystem;
        private readonly string _itemId;
        private readonly int _count;

        public FastAddItemCommand(IInventorySystem inventorySystem, string itemId, int count)
        {
            _inventorySystem = inventorySystem;
            _itemId = itemId;
            _count = count;
        }

        public Task<bool> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            _inventorySystem.AddItem(_itemId, _count);
            return Task.FromResult(true);
        }
    }

    /// <summary>
    /// 添加物品的命令
    /// </summary>
    public class AddItemCommand : ICommand<bool>
    {
        private readonly IInventorySystem _inventorySystem;
        private readonly string _itemId;
        private readonly int _count;

        public AddItemCommand(IInventorySystem inventorySystem, string itemId, int count)
        {
            _inventorySystem = inventorySystem;
            _itemId = itemId;
            _count = count;
        }

        public async Task<bool> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            await Task.Delay(50, cancellationToken); // 模拟异步操作
            _inventorySystem.AddItem(_itemId, _count);
            return true;
        }
    }

    /// <summary>
    /// 获取 Buff 值的查询
    /// </summary>
    public class GetBuffValueQuery : IQuery<int>
    {
        private readonly IBuffSystem _buffSystem;
        private readonly string _buffId;

        public GetBuffValueQuery(IBuffSystem buffSystem, string buffId)
        {
            _buffSystem = buffSystem;
            _buffId = buffId;
        }

        public int Execute()
        {
            return _buffSystem.GetBuffValue(_buffId);
        }
    }

    /// <summary>
    /// 获取物品数量的查询
    /// </summary>
    public class GetItemCountQuery : IQuery<int>
    {
        private readonly IInventorySystem _inventorySystem;
        private readonly string _itemId;

        public GetItemCountQuery(IInventorySystem inventorySystem, string itemId)
        {
            _inventorySystem = inventorySystem;
            _itemId = itemId;
        }

        public int Execute()
        {
            return _inventorySystem.GetItemCount(_itemId);
        }
    }

    /// <summary>
    /// Buff 应用成功事件
    /// </summary>
    public class BuffAppliedEvent : IEvent
    {
        public DateTime Timestamp { get; }
        public string BuffId { get; set; }
        public int Value { get; set; }

        public BuffAppliedEvent(string buffId, int value)
        {
            Timestamp = DateTime.UtcNow;
            BuffId = buffId;
            Value = value;
        }
    }

    /// <summary>
    /// 物品添加成功事件
    /// </summary>
    public class ItemAddedEvent : IEvent
    {
        public DateTime Timestamp { get; }
        public string ItemId { get; set; }
        public int Count { get; set; }

        public ItemAddedEvent(string itemId, int count)
        {
            Timestamp = DateTime.UtcNow;
            ItemId = itemId;
            Count = count;
        }
    }

    // ============================================================
    // Test Architecture (用于单元测试 - 不自动注册服务)
    // ============================================================

    public class TestArchitecture : ENekoArchitecture<TestArchitecture>
    {
        protected override void OnInit()
        {
            // 不自动注册服务，让测试用例自己决定需要注册什么
            // 这样可以保持测试的独立性和可控性
        }

        /// <summary>
        /// 获取所有已注册的服务（用于测试）
        /// </summary>
        public IEnumerable<ServiceDescriptor> GetAllServices()
        {
            return Container.GetAllServices();
        }

        /// <summary>
        /// 获取特定服务的描述符（用于测试）
        /// </summary>
        public ServiceDescriptor GetServiceDescriptor<TService>() where TService : class, IService
        {
            return Container.GetServiceDescriptor<TService>();
        }
    }

    // ============================================================
    // Integration Test Architecture (用于集成测试 - 自动注册服务)
    // ============================================================

    public class IntegrationTestArchitecture : ENekoArchitecture<IntegrationTestArchitecture>
    {
        protected override void OnInit()
        {
            // 注册 Mock 服务用于集成测试
            RegisterService<IBuffSystem, MockBuffSystem>();
            RegisterService<IInventorySystem, MockInventorySystem>();
        }

        /// <summary>
        /// 获取所有已注册的服务（用于测试）
        /// </summary>
        public IEnumerable<ServiceDescriptor> GetAllServices()
        {
            return Container.GetAllServices();
        }

        /// <summary>
        /// 获取特定服务的描述符（用于测试）
        /// </summary>
        public ServiceDescriptor GetServiceDescriptor<TService>() where TService : class, IService
        {
            return Container.GetServiceDescriptor<TService>();
        }
    }
}