using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;
using EasyPack.ENekoFramework;

namespace EasyPack.ENekoFrameworkTest.Unit
{
    /// <summary>
    /// 测试 BindingBatchUpdater 的帧末批处理功能
    /// </summary>
    [TestFixture]
    public class BindingBatchUpdaterTest
    {
        private GameObject _updaterObject;
        private BindingBatchUpdater _updater;

        [SetUp]
        public void Setup()
        {
            // 清理之前的实例
            var existing = GameObject.Find("[BindingBatchUpdater]");
            if (existing != null)
            {
                Object.DestroyImmediate(existing);
            }

            // 创建新的 BindingBatchUpdater 实例
            _updaterObject = new GameObject("[BindingBatchUpdater]");
            _updater = _updaterObject.AddComponent<BindingBatchUpdater>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_updaterObject != null)
            {
                Object.DestroyImmediate(_updaterObject);
            }
        }

        [Test]
        public void Instance_ShouldReturnSingleton()
        {
            // Arrange & Act
            var instance1 = BindingBatchUpdater.Instance;
            var instance2 = BindingBatchUpdater.Instance;

            // Assert
            Assert.IsNotNull(instance1);
            Assert.AreSame(instance1, instance2, "Instance should be a singleton");
        }

        [Test]
        public void MarkDirty_ShouldScheduleUpdate()
        {
            // Arrange
            var mockBinding = new MockBindable();

            // Act
            BindingBatchUpdater.Instance.MarkDirty(mockBinding);

            // Assert
            Assert.IsTrue(BindingBatchUpdater.Instance.IsUpdateScheduled,
                "Update should be scheduled after marking dirty");
        }

        [Test]
        public void MarkDirty_MultipleTimes_ShouldOnlyScheduleOnce()
        {
            // Arrange
            var mockBinding1 = new MockBindable();
            var mockBinding2 = new MockBindable();

            // Act
            BindingBatchUpdater.Instance.MarkDirty(mockBinding1);
            BindingBatchUpdater.Instance.MarkDirty(mockBinding2);
            BindingBatchUpdater.Instance.MarkDirty(mockBinding1); // 重复标记

            // Assert
            Assert.AreEqual(2, BindingBatchUpdater.Instance.DirtyBindingsCount,
                "Should have 2 unique dirty bindings");
        }

        [UnityTest]
        public IEnumerator LateUpdate_ShouldFlushDirtyBindings()
        {
            // Arrange
            var mockBinding = new MockBindable();
            BindingBatchUpdater.Instance.MarkDirty(mockBinding);

            // Act - 等待 LateUpdate 执行
            yield return null;

            // Assert
            Assert.IsTrue(mockBinding.WasFlushed, "Binding should be flushed in LateUpdate");
            Assert.IsFalse(BindingBatchUpdater.Instance.IsUpdateScheduled,
                "Update should not be scheduled after flush");
            Assert.AreEqual(0, BindingBatchUpdater.Instance.DirtyBindingsCount,
                "Dirty bindings should be cleared");
        }

        [UnityTest]
        public IEnumerator BatchUpdate_ShouldFlushAllBindingsOnce()
        {
            // Arrange
            var bindings = new MockBindable[10];
            for (int i = 0; i < bindings.Length; i++)
            {
                bindings[i] = new MockBindable();
                BindingBatchUpdater.Instance.MarkDirty(bindings[i]);
            }

            // Act - 等待 LateUpdate 执行
            yield return null;

            // Assert
            foreach (var binding in bindings)
            {
                Assert.IsTrue(binding.WasFlushed, "All bindings should be flushed");
                Assert.AreEqual(1, binding.FlushCount, "Each binding should be flushed exactly once");
            }
        }

        [UnityTest]
        public IEnumerator FlushUpdates_WithException_ShouldContinueFlushingOthers()
        {
            // Arrange
            var goodBinding = new MockBindable();
            var badBinding = new MockBindable { ThrowOnFlush = true };
            var anotherGoodBinding = new MockBindable();

            BindingBatchUpdater.Instance.MarkDirty(goodBinding);
            BindingBatchUpdater.Instance.MarkDirty(badBinding);
            BindingBatchUpdater.Instance.MarkDirty(anotherGoodBinding);

            // 在异常发生前设置期望的日志
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex(".*Exception.*"));

            // Act - 等待 LateUpdate 执行
            yield return null;

            // Assert
            Assert.IsTrue(goodBinding.WasFlushed, "Good binding should be flushed");
            Assert.IsTrue(anotherGoodBinding.WasFlushed, "Another good binding should be flushed");
        }

        /// <summary>
        /// 用于测试的 Mock Bindable 实现
        /// </summary>
        private class MockBindable : IBindable
        {
            public bool WasFlushed { get; private set; }
            public int FlushCount { get; private set; }
            public bool ThrowOnFlush { get; set; }

            public void FlushUpdates()
            {
                if (ThrowOnFlush)
                {
                    throw new System.Exception("Test exception");
                }

                WasFlushed = true;
                FlushCount++;
            }
        }
    }
}
