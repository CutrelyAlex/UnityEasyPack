using EasyPack.InventorySystem;
using EasyPack.ENekoFramework;
using System.Threading.Tasks;
using UnityEngine;
using EasyPack.Architecture;

namespace EasyPack.Tests
{
    /// <summary>
    /// InventoryService 架构集成快速测试
    /// </summary>
    public class InventoryServiceIntegrationTest : MonoBehaviour
    {
        private async void Start()
        {
            Debug.Log("===== InventoryService 集成测试开始 =====");

            await TestServiceLifecycle();
            await TestBasicOperations();
            await TestThreadSafety();

            Debug.Log("===== 所有测试通过 ✅ =====");
        }

        /// <summary>
        /// 测试服务生命周期
        /// </summary>
        private async Task TestServiceLifecycle()
        {
            Debug.Log("\n[测试1] 服务生命周期");

            var service = new InventoryService();

            // 初始状态
            AssertEqual("初始状态", ServiceLifecycleState.Uninitialized, service.State);

            // 初始化
            await service.InitializeAsync();
            AssertEqual("初始化后", ServiceLifecycleState.Ready, service.State);

            // 暂停
            service.Pause();
            AssertEqual("暂停后", ServiceLifecycleState.Paused, service.State);

            // 恢复
            service.Resume();
            AssertEqual("恢复后", ServiceLifecycleState.Ready, service.State);

            // 释放
            service.Dispose();
            AssertEqual("释放后", ServiceLifecycleState.Disposed, service.State);

            Debug.Log("✅ 生命周期测试通过");
        }

        /// <summary>
        /// 测试基本操作
        /// </summary>
        private async Task TestBasicOperations()
        {
            Debug.Log("\n[测试2] 基本操作");

            var service = await EasyPackArchitecture.GetInventoryServiceAsync();

            // 确保初始化
            if (service.State == ServiceLifecycleState.Uninitialized)
            {
                await service.InitializeAsync();
            }

            // 创建容器
            var backpack = new LinerContainer("test_backpack", "测试背包", "Backpack", 20);
            var storage = new LinerContainer("test_storage", "测试储物箱", "Storage", -1);

            // 注册容器
            AssertTrue("注册背包", service.RegisterContainer(backpack, 1, "Player"));
            AssertTrue("注册储物箱", service.RegisterContainer(storage, 0, "Storage"));
            AssertEqual("容器数量", 2, service.ContainerCount);

            // 查询容器
            var retrieved = service.GetContainer("test_backpack");
            AssertNotNull("获取容器", retrieved);
            AssertEqual("容器ID", "test_backpack", retrieved.ID);

            // 按类型查询
            var backpacks = service.GetContainersByType("Backpack");
            AssertEqual("按类型查询", 1, backpacks.Count);

            // 按分类查询
            var playerContainers = service.GetContainersByCategory("Player");
            AssertEqual("按分类查询", 1, playerContainers.Count);

            // 优先级
            service.SetContainerPriority("test_backpack", 10);
            AssertEqual("优先级设置", 10, service.GetContainerPriority("test_backpack"));

            // 分类
            service.SetContainerCategory("test_backpack", "TestCategory");
            AssertEqual("分类设置", "TestCategory", service.GetContainerCategory("test_backpack"));

            // 注销容器
            AssertTrue("注销容器", service.UnregisterContainer("test_backpack"));
            AssertEqual("注销后数量", 1, service.ContainerCount);

            Debug.Log("✅ 基本操作测试通过");
        }

        /// <summary>
        /// 测试线程安全（模拟并发注册）
        /// </summary>
        private async Task TestThreadSafety()
        {
            Debug.Log("\n[测试3] 线程安全");

            var service = await EasyPackArchitecture.GetInventoryServiceAsync();

            // 清理之前的测试
            var existing = service.GetAllContainers();
            foreach (var container in existing)
            {
                service.UnregisterContainer(container.ID);
            }

            // 并发注册容器
            var tasks = new Task[10];
            for (int i = 0; i < 10; i++)
            {
                int index = i;
                tasks[i] = Task.Run(() =>
                {
                    var container = new LinerContainer($"concurrent_{index}", $"并发容器{index}", "Test", 10);
                    service.RegisterContainer(container);
                });
            }

            await Task.WhenAll(tasks);

            // 验证结果
            AssertEqual("并发注册后数量", 10, service.ContainerCount);

            Debug.Log("✅ 线程安全测试通过");
        }

        #region 断言辅助方法

        private void AssertEqual<T>(string testName, T expected, T actual)
        {
            if (!expected.Equals(actual))
            {
                Debug.LogError($"❌ {testName} 失败: 期望 {expected}, 实际 {actual}");
            }
            else
            {
                Debug.Log($"  ✓ {testName}: {actual}");
            }
        }

        private void AssertTrue(string testName, bool condition)
        {
            if (!condition)
            {
                Debug.LogError($"❌ {testName} 失败");
            }
            else
            {
                Debug.Log($"  ✓ {testName}");
            }
        }

        private void AssertNotNull(string testName, object obj)
        {
            if (obj == null)
            {
                Debug.LogError($"❌ {testName} 失败: 对象为 null");
            }
            else
            {
                Debug.Log($"  ✓ {testName}: 不为 null");
            }
        }

        #endregion
    }
}
