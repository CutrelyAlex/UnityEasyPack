using NUnit.Framework;
using UnityEngine.TestTools;
using System.Collections;
using System.Threading.Tasks;
using TestScripts.ENekoFramework.Mocks;
using UnityEngine;
using EasyPack.ENekoFramework;

namespace EasyPack.ENekoFrameworkTest.Integration
{
    /// <summary>
    ///     架构集成测试 (Architecture Integration Tests)
    ///     集成测试是什么？
    ///     ===============
    ///     集成测试验证多个组件协同工作的完整场景，重点关注：
    ///     1. 组件间通信 - 服务间的交互和数据流
    ///     2. 端到端工作流 - 从输入到输出的完整业务流程
    ///     3. 生命周期管理 - 服务的初始化、运行、暂停、销毁
    ///     4. 事件驱动架构 - 事件发布、订阅和处理
    ///     测试范围：ENekoFramework 核心架构的完整集成
    ///     测试对象：ServiceContainer + CommandDispatcher + QueryExecutor + EventBus
    ///     测试方法：使用 MockBuffSystem 和 MockInventorySystem 模拟真实业务场景
    ///     关键测试场景：
    ///     ============
    ///     1. 完整工作流 - 注册服务 → 执行命令 → 查询数据 → 广播事件
    ///     2. 服务间通信 - Buff系统影响Inventory系统的业务逻辑
    ///     3. 生命周期管理 - 服务的状态转换和资源清理
    ///     Mock 系统说明：
    ///     =============
    ///     • MockBuffSystem: 模拟游戏Buff系统（增益/减益效果）
    ///     • MockInventorySystem: 模拟物品背包系统
    ///     • 两个系统通过事件进行解耦通信
    ///     测试价值：
    ///     ========
    ///     • 验证架构设计的正确性
    ///     • 确保组件间契约一致
    ///     • 发现集成层面的问题
    ///     • 为重构提供安全网
    /// </summary>
    public class ArchitectureIntegrationTests
    {
        private IntegrationTestArchitecture _architecture;

        [SetUp]
        public void Setup()
        {
            IntegrationTestArchitecture.ResetInstance();
            _architecture = IntegrationTestArchitecture.Instance;
        }

        [TearDown]
        public void TearDown()
        {
            IntegrationTestArchitecture.ResetInstance();
        }

        #region 完整工作流测试 - 测试端到端业务流程

        /// <summary>
        ///     测试：完整工作流 - 注册服务 → 执行命令 → 查询数据 → 广播事件
        ///     这是一个端到端的集成测试，验证ENekoFramework的核心功能：
        ///     1. 服务注册和异步初始化
        ///     2. 命令执行和状态变更
        ///     3. 查询数据的同步获取
        ///     4. 事件发布和订阅处理
        ///     业务场景：游戏中玩家获得Buff并获取物品的完整流程
        ///     =========
        ///     玩家触发技能 → Buff系统应用效果 → 物品系统给予奖励 → 通知UI更新
        ///     验证内容：
        ///     ========
        ///     ✓ 服务容器能正确注册和解析服务
        ///     ✓ 命令分派器能执行异步命令
        ///     ✓ 查询执行器能同步返回数据
        ///     ✓ 事件总线能发布和订阅事件
        ///     ✓ 所有组件能协同工作
        ///     技术细节：
        ///     ========
        ///     • 使用UnityTest处理异步操作
        ///     • 验证服务生命周期状态
        ///     • 测试数据一致性
        ///     • 确认事件传递的完整性
        /// </summary>
        [UnityTest]
        public IEnumerator FullWorkflow_RegisterServices_ExecuteCommands_QueryData_BroadcastEvents_ShouldSucceed()
        {
            // Arrange - 服务已在 TestArchitecture.OnInit() 中注册

            // Act & Assert - 获取并初始化服务
            var buffTask = _architecture.ResolveAsync<IBuffSystem>();
            yield return new WaitUntil(() => buffTask.IsCompleted);
            IBuffSystem buffSystem = buffTask.Result;
            Assert.IsNotNull(buffSystem);
            Assert.AreEqual(ServiceLifecycleState.Ready, buffSystem.State);

            var inventoryTask = _architecture.ResolveAsync<IInventorySystem>();
            yield return new WaitUntil(() => inventoryTask.IsCompleted);
            IInventorySystem inventorySystem = inventoryTask.Result;
            Assert.IsNotNull(inventorySystem);
            Assert.AreEqual(ServiceLifecycleState.Ready, inventorySystem.State);

            // Act & Assert - 执行命令
            var applyBuffCommand = new ApplyBuffCommand(buffSystem, "buff_101", 50);
            var applyBuffTask = _architecture.SendCommandAsync(applyBuffCommand);
            yield return new WaitUntil(() => applyBuffTask.IsCompleted);
            Assert.IsTrue(applyBuffTask.Result);
            Assert.AreEqual(50, buffSystem.GetBuffValue("buff_101"));

            var addItemCommand = new AddItemCommand(inventorySystem, "item_201", 5);
            var addItemTask = _architecture.SendCommandAsync(addItemCommand);
            yield return new WaitUntil(() => addItemTask.IsCompleted);
            Assert.IsTrue(addItemTask.Result);
            Assert.AreEqual(5, inventorySystem.GetItemCount("item_201"));

            // Act & Assert - 查询数据
            var buffValueQuery = new GetBuffValueQuery(buffSystem, "buff_101");
            int buffValue = _architecture.ExecuteQuery(buffValueQuery);
            Assert.AreEqual(50, buffValue);

            var itemCountQuery = new GetItemCountQuery(inventorySystem, "item_201");
            int itemCount = _architecture.ExecuteQuery(itemCountQuery);
            Assert.AreEqual(5, itemCount);

            // Act & Assert - 广播事件
            bool eventReceived = false;
            _architecture.SubscribeEvent<BuffAppliedEvent>((e) =>
            {
                eventReceived = true;
                Assert.AreEqual("buff_999", e.BuffId);
                Assert.AreEqual(100, e.Value);
            });

            _architecture.PublishEvent(new BuffAppliedEvent("buff_999", 100));
            Assert.IsTrue(eventReceived);
        }

        #endregion

        #region 服务间通信测试 - 测试解耦的组件交互

        /// <summary>
        ///     测试：服务间通信 - Buff效果触发物品奖励
        ///     这个测试验证组件间的解耦通信，通过事件驱动的方式：
        ///     Buff系统应用特殊Buff → 发布事件 → Inventory系统监听并给予奖励
        ///     业务场景：游戏中获得特殊Buff时自动获得物品奖励
        ///     =========
        ///     玩家完成任务获得"幸运Buff" → 系统自动给予稀有物品 → 通知玩家
        ///     架构优势：
        ///     ========
        ///     • BuffSystem 不需要知道 InventorySystem 的存在
        ///     • 通过事件解耦，系统更易维护和扩展
        ///     • 可以轻松添加新的奖励逻辑
        ///     验证内容：
        ///     ========
        ///     ✓ 事件能正确传递数据
        ///     ✓ 订阅者能收到并处理事件
        ///     ✓ 服务间能通过事件进行状态同步
        ///     ✓ 异步事件处理机制正常工作
        ///     技术实现：
        ///     ========
        ///     1. BuffSystem 应用Buff并发布事件
        ///     2. InventorySystem 订阅事件并响应
        ///     3. 事件处理是异步的，需要等待一帧
        ///     4. 验证最终状态的一致性
        /// </summary>
        [UnityTest]
        public IEnumerator ServiceCommunication_BuffAffectsInventory_ShouldWork()
        {
            // Arrange - 获取服务
            var buffTask = _architecture.ResolveAsync<IBuffSystem>();
            yield return new WaitUntil(() => buffTask.IsCompleted);
            IBuffSystem buffSystem = buffTask.Result;

            var inventoryTask = _architecture.ResolveAsync<IInventorySystem>();
            yield return new WaitUntil(() => inventoryTask.IsCompleted);
            IInventorySystem inventorySystem = inventoryTask.Result;

            // 订阅 Buff 事件，触发物品添加
            bool itemAdded = false;
            _architecture.SubscribeEvent<BuffAppliedEvent>((e) =>
            {
                if (e.BuffId == "special_buff") // 特殊 Buff 给予物品
                {
                    inventorySystem.AddItem("reward_item", 1);
                    itemAdded = true;
                }
            });

            // Act - 应用 Buff 并发布事件
            buffSystem.ApplyBuff("special_buff", 10);
            _architecture.PublishEvent(new BuffAppliedEvent("special_buff", 10));

            // Wait a frame for event processing
            yield return null;

            // Assert
            Assert.IsTrue(itemAdded);
            Assert.AreEqual(1, inventorySystem.GetItemCount("reward_item"));
        }

        #endregion

        #region 生命周期测试 - 测试服务的状态管理

        /// <summary>
        ///     测试：服务生命周期 - 暂停、恢复和销毁
        ///     这个测试验证服务的完整生命周期管理：
        ///     Ready → Paused → Ready → Disposed
        ///     业务场景：游戏暂停/恢复、场景切换、应用退出时的资源管理
        ///     =========
        ///     游戏暂停时停止Buff更新 → 恢复时继续生效 → 退出时清理资源
        ///     生命周期状态：
        ///     ============
        ///     • Ready: 服务正常运行
        ///     • Paused: 服务暂停，停止处理但保持状态
        ///     • Disposed: 服务销毁，释放所有资源
        ///     验证内容：
        ///     ========
        ///     ✓ 服务能正确转换状态
        ///     ✓ 暂停/恢复操作正常工作
        ///     ✓ 销毁时正确清理资源
        ///     ✓ 状态转换是可逆的（除了Disposed）
        ///     技术细节：
        ///     ========
        ///     • Pause(): 停止服务处理，保持内部状态
        ///     • Resume(): 恢复服务处理
        ///     • Dispose(): 释放资源，服务不可再用
        ///     • 异步销毁需要等待一帧确认完成
        /// </summary>
        [UnityTest]
        public IEnumerator ServiceLifecycle_PauseAndResume_ShouldWork()
        {
            // Arrange - 获取服务
            var buffTask = _architecture.ResolveAsync<IBuffSystem>();
            yield return new WaitUntil(() => buffTask.IsCompleted);
            IBuffSystem buffSystem = buffTask.Result;

            // Act & Assert - 初始状态
            Assert.AreEqual(ServiceLifecycleState.Ready, buffSystem.State);

            // Act - Pause
            buffSystem.Pause();
            Assert.AreEqual(ServiceLifecycleState.Paused, buffSystem.State);

            // Act - Resume
            buffSystem.Resume();
            Assert.AreEqual(ServiceLifecycleState.Ready, buffSystem.State);

            // Act - Dispose
            buffSystem.Dispose();
            yield return null; // Wait a frame
            Assert.AreEqual(ServiceLifecycleState.Disposed, buffSystem.State);
        }

        #endregion
    }
}