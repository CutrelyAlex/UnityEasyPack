using NUnit.Framework;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using EasyPack.Architecture;
using EasyPack.ObjectPool;

namespace EasyPack.ObjectPoolTests
{
    /// <summary>
    ///     ObjectPoolService 服务集成测试
    /// </summary>
    [TestFixture]
    public class ObjectPoolServiceTests
    {
        private IObjectPoolService _poolService;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            Task.Run(async () =>
            {
                Debug.Log("[ObjectPoolServiceTests] 开始初始化...");

                EasyPackArchitecture architecture = EasyPackArchitecture.Instance;
                Assert.IsNotNull(architecture, "EasyPackArchitecture.Instance 不能为 null");

                _poolService = await EasyPackArchitecture.GetObjectPoolServiceAsync();
                Assert.IsNotNull(_poolService, "ObjectPoolService 不能为 null");

                if (_poolService.State == ENekoFramework.ServiceLifecycleState.Uninitialized)
                    await _poolService.InitializeAsync();

                Debug.Log($"[ObjectPoolServiceTests] 初始化完成，状态: {_poolService.State}");
            }).Wait();
        }

        #region ObjectPoolService 服务测试

        /// <summary>
        ///     测试服务创建
        /// </summary>
        [Test]
        public void Test_ServiceCreation()
        {
            // Assert
            Assert.IsNotNull(_poolService, "服务应成功创建");
            Assert.AreEqual(ENekoFramework.ServiceLifecycleState.Ready, _poolService.State, "服务应处于 Ready 状态");
        }

        /// <summary>
        ///     测试创建池
        /// </summary>
        [Test]
        public void Test_CreatePool()
        {
            // Act
            var pool = _poolService.CreatePool(() => new TestData(), maxCapacity: 32);

            // Assert
            Assert.IsNotNull(pool, "池应成功创建");
            Assert.AreEqual(32, pool.MaxCapacity, "池容量应为 32");
        }

        /// <summary>
        ///     测试获取池
        /// </summary>
        [Test]
        public void Test_GetPool()
        {
            // Arrange
            var pool1 = _poolService.CreatePool(() => new TestData2(), maxCapacity: 16);

            // Act
            var pool2 = _poolService.GetPool<TestData2>();

            // Assert
            Assert.IsNotNull(pool2, "应能获取已创建的池");
            Assert.AreSame(pool1, pool2, "应返回同一个池实例");
        }

        /// <summary>
        ///     测试获取不存在的池
        /// </summary>
        [Test]
        public void Test_GetNonExistentPool_ReturnsNull()
        {
            // Act
            var pool = _poolService.GetPool<NonExistentType>();

            // Assert
            Assert.IsNull(pool, "获取不存在的池应返回 null");
        }

        /// <summary>
        ///     测试获取或创建池
        /// </summary>
        [Test]
        public void Test_GetOrCreatePool()
        {
            // Act - 第一次应创建
            var pool1 = _poolService.GetOrCreatePool(() => new TestData3(), maxCapacity: 48);
            // 第二次应获取现有池
            var pool2 = _poolService.GetOrCreatePool(() => new TestData3(), maxCapacity: 99);

            // Assert
            Assert.IsNotNull(pool1, "第一次应创建池");
            Assert.AreSame(pool1, pool2, "第二次应返回同一个池");
            Assert.AreEqual(48, pool2.MaxCapacity, "容量应为第一次设置的值");
        }

        /// <summary>
        ///     测试销毁池
        /// </summary>
        [Test]
        public void Test_DestroyPool()
        {
            // Arrange
            var pool = _poolService.CreatePool(() => new TestData4(), maxCapacity: 32);

            // Act
            bool destroyed = _poolService.DestroyPool<TestData4>();

            // Assert
            Assert.IsTrue(destroyed, "应成功销毁池");
            Assert.IsNull(_poolService.GetPool<TestData4>(), "销毁后应无法获取池");
        }

        /// <summary>
        ///     测试销毁不存在的池
        /// </summary>
        [Test]
        public void Test_DestroyNonExistentPool_ReturnsFalse()
        {
            // Act
            bool destroyed = _poolService.DestroyPool<NonExistentType>();

            // Assert
            Assert.IsFalse(destroyed, "销毁不存在的池应返回 false");
        }

        /// <summary>
        ///     测试通过服务租用对象
        /// </summary>
        [Test]
        public void Test_RentThroughService()
        {
            // Arrange
            _poolService.CreatePool(() => new TestData5(), maxCapacity: 32);

            // Act
            var obj = _poolService.Rent<TestData5>();

            // Assert
            Assert.IsNotNull(obj, "应成功租用对象");
            Assert.IsInstanceOf<TestData5>(obj, "对象类型应正确");

            // Cleanup
            _poolService.Return(obj);
        }

        /// <summary>
        ///     测试租用不存在的池抛出异常
        /// </summary>
        [Test]
        public void Test_RentNonExistentPool_ThrowsException()
        {
            // Act & Assert
            Assert.Throws<System.InvalidOperationException>(() => { _poolService.Rent<NonExistentType2>(); },
                "租用不存在的池应抛出异常");
        }

        /// <summary>
        ///     测试通过服务归还对象
        /// </summary>
        [Test]
        public void Test_ReturnThroughService()
        {
            // Arrange
            var pool = _poolService.CreatePool(() => new TestData6(), maxCapacity: 32);
            var obj = _poolService.Rent<TestData6>();

            // Act
            _poolService.Return(obj);

            // Assert
            Assert.AreEqual(1, pool.CountInactive, "归还后池中应有 1 个对象");
        }

        /// <summary>
        ///     测试生命周期 - 暂停和恢复
        /// </summary>
        [Test]
        public void Test_Lifecycle_PauseResume()
        {
            // Act
            _poolService.Pause();
            Assert.AreEqual(ENekoFramework.ServiceLifecycleState.Paused, _poolService.State, "应处于暂停状态");

            _poolService.Resume();
            Assert.AreEqual(ENekoFramework.ServiceLifecycleState.Ready, _poolService.State, "应恢复到 Ready 状态");
        }

        #endregion

        #region ObjectPool<T> 基础测试

        /// <summary>
        ///     测试对象池基础功能
        /// </summary>
        [Test]
        public void Test_ObjectPool_RentAndReturn()
        {
            // Arrange
            var pool = new ObjectPool<SimpleObject>(() => new SimpleObject(), maxCapacity: 10);

            // Act
            var obj1 = pool.Rent();
            var obj2 = pool.Rent();

            // Assert
            Assert.IsNotNull(obj1, "应成功租用对象");
            Assert.IsNotNull(obj2, "应成功租用第二个对象");
            Assert.AreNotSame(obj1, obj2, "应是不同的对象实例");
            Assert.AreEqual(0, pool.CountInactive, "池中应无对象");

            // 归还对象
            pool.Return(obj1);
            Assert.AreEqual(1, pool.CountInactive, "归还后池中应有 1 个对象");

            pool.Return(obj2);
            Assert.AreEqual(2, pool.CountInactive, "归还后池中应有 2 个对象");
        }

        /// <summary>
        ///     测试对象池复用
        /// </summary>
        [Test]
        public void Test_ObjectPool_Reuse()
        {
            // Arrange
            var pool = new ObjectPool<SimpleObject>(() => new SimpleObject(), maxCapacity: 10);
            var obj1 = pool.Rent();
            pool.Return(obj1);

            // Act
            var obj2 = pool.Rent();

            // Assert
            Assert.AreSame(obj1, obj2, "应复用同一对象");
        }

        /// <summary>
        ///     测试对象池清理回调
        /// </summary>
        [Test]
        public void Test_ObjectPool_CleanupCallback()
        {
            // Arrange
            bool cleanupCalled = false;
            var pool = new ObjectPool<SimpleObject>(
                () => new SimpleObject { Value = 100 },
                obj =>
                {
                    cleanupCalled = true;
                    obj.Value = 0;
                },
                maxCapacity: 10
            );

            var obj = pool.Rent();
            obj.Value = 999;

            // Act
            pool.Return(obj);

            // Assert
            Assert.IsTrue(cleanupCalled, "清理回调应被调用");
            Assert.AreEqual(0, obj.Value, "对象应被清理");
        }

        /// <summary>
        ///     测试对象池容量限制
        /// </summary>
        [Test]
        public void Test_ObjectPool_MaxCapacity()
        {
            // Arrange
            var pool = new ObjectPool<SimpleObject>(() => new SimpleObject(), maxCapacity: 2);

            var obj1 = pool.Rent();
            var obj2 = pool.Rent();
            var obj3 = pool.Rent();

            // Act - 归还超过容量的对象
            pool.Return(obj1);
            pool.Return(obj2);
            pool.Return(obj3); // 超出容量，应被丢弃

            // Assert
            Assert.AreEqual(2, pool.CountInactive, "池中对象数量应为最大容量");
        }

        /// <summary>
        ///     测试 Clear 功能
        /// </summary>
        [Test]
        public void Test_ObjectPool_Clear()
        {
            // Arrange
            var pool = new ObjectPool<SimpleObject>(() => new SimpleObject(), maxCapacity: 10);
            var obj1 = pool.Rent();
            var obj2 = pool.Rent();
            pool.Return(obj1);
            pool.Return(obj2);

            // Act
            pool.Clear();

            // Assert
            Assert.AreEqual(0, pool.CountInactive, "清空后池中应无对象");
        }

        /// <summary>
        ///     测试归还 null 对象
        /// </summary>
        [Test]
        public void Test_ObjectPool_ReturnNull()
        {
            // Arrange
            var pool = new ObjectPool<SimpleObject>(() => new SimpleObject(), maxCapacity: 10);

            // Act & Assert - 不应抛出异常
            Assert.DoesNotThrow(() => pool.Return(null), "归还 null 不应抛出异常");
            Assert.AreEqual(0, pool.CountInactive, "池中应无对象");
        }

        #endregion

        #region IPoolable 回调测试

        /// <summary>
        ///     测试 IPoolable.OnAllocate 回调
        /// </summary>
        [Test]
        public void Test_IPoolable_OnAllocate()
        {
            // Arrange
            var pool = new ObjectPool<PoolableObject>(() => new PoolableObject(), maxCapacity: 10);

            // Act
            var obj = pool.Rent();

            // Assert
            Assert.IsTrue(obj.AllocateCalled, "OnAllocate 应被调用");
            Assert.IsFalse(obj.IsRecycled, "IsRecycled 应为 false");

            pool.Return(obj);
        }

        /// <summary>
        ///     测试 IPoolable.OnRecycle 回调
        /// </summary>
        [Test]
        public void Test_IPoolable_OnRecycle()
        {
            // Arrange
            var pool = new ObjectPool<PoolableObject>(() => new PoolableObject(), maxCapacity: 10);
            var obj = pool.Rent();
            obj.AllocateCalled = false; // 重置

            // Act
            pool.Return(obj);

            // Assert
            Assert.IsTrue(obj.RecycleCalled, "OnRecycle 应被调用");
            Assert.IsTrue(obj.IsRecycled, "IsRecycled 应为 true");
        }

        /// <summary>
        ///     测试重复回收防护
        /// </summary>
        [Test]
        public void Test_IPoolable_PreventDoubleRecycle()
        {
            // Arrange
            var pool = new ObjectPool<PoolableObject>(() => new PoolableObject(), maxCapacity: 10);
            var obj = pool.Rent();
            pool.Return(obj);
            int countAfterFirst = pool.CountInactive;
            obj.RecycleCalled = false; // 重置

            // Act - 尝试重复回收
            pool.Return(obj);

            // Assert
            Assert.AreEqual(countAfterFirst, pool.CountInactive, "重复回收不应增加池中对象");
            Assert.IsFalse(obj.RecycleCalled, "OnRecycle 不应被再次调用");
        }

        /// <summary>
        ///     测试 IPoolable 完整生命周期
        /// </summary>
        [Test]
        public void Test_IPoolable_FullLifecycle()
        {
            // Arrange
            var pool = new ObjectPool<PoolableObject>(() => new PoolableObject(), maxCapacity: 10);

            // Act & Assert - 第一次分配
            var obj = pool.Rent();
            Assert.IsTrue(obj.AllocateCalled, "第一次分配：OnAllocate 应被调用");
            Assert.IsFalse(obj.IsRecycled, "第一次分配：IsRecycled 应为 false");

            // 回收
            pool.Return(obj);
            Assert.IsTrue(obj.RecycleCalled, "回收：OnRecycle 应被调用");
            Assert.IsTrue(obj.IsRecycled, "回收：IsRecycled 应为 true");

            // 重置计数器
            obj.AllocateCalled = false;
            obj.RecycleCalled = false;

            // 第二次分配（复用）
            var obj2 = pool.Rent();
            Assert.AreSame(obj, obj2, "应复用同一对象");
            Assert.IsTrue(obj2.AllocateCalled, "第二次分配：OnAllocate 应被调用");
            Assert.IsFalse(obj2.IsRecycled, "第二次分配：IsRecycled 应为 false");

            pool.Return(obj2);
        }

        #endregion

        #region ListPool 测试

        /// <summary>
        ///     测试 ListPool 基础功能
        /// </summary>
        [Test]
        public void Test_ListPool_GetAndRelease()
        {
            // Arrange
            ListPool<int>.Clear();

            // Act
            var list = ListPool<int>.Get();
            list.Add(1);
            list.Add(2);
            list.Add(3);

            // Assert
            Assert.IsNotNull(list, "应成功获取列表");
            Assert.AreEqual(3, list.Count, "列表应有 3 个元素");

            // Release
            ListPool<int>.Release(list);
            Assert.AreEqual(0, list.Count, "归还后列表应被清空");
            Assert.AreEqual(1, ListPool<int>.Count, "池中应有 1 个列表");
        }

        /// <summary>
        ///     测试 ListPool 复用
        /// </summary>
        [Test]
        public void Test_ListPool_Reuse()
        {
            // Arrange
            ListPool<string>.Clear();
            var list1 = ListPool<string>.Get();
            ListPool<string>.Release(list1);

            // Act
            var list2 = ListPool<string>.Get();

            // Assert
            Assert.AreSame(list1, list2, "应复用同一列表");

            ListPool<string>.Release(list2);
        }

        /// <summary>
        ///     测试 ListPool 指定容量
        /// </summary>
        [Test]
        public void Test_ListPool_GetWithCapacity()
        {
            // Arrange
            ListPool<double>.Clear();

            // Act
            var list = ListPool<double>.Get(100);

            // Assert
            Assert.GreaterOrEqual(list.Capacity, 100, "容量应大于等于指定值");

            ListPool<double>.Release(list);
        }

        /// <summary>
        ///     测试 ListPool 扩展方法
        /// </summary>
        [Test]
        public void Test_ListPool_ExtensionMethod()
        {
            // Arrange
            ListPool<float>.Clear();
            var list = ListPool<float>.Get();
            list.Add(1.0f);

            // Act
            list.Release2Pool();

            // Assert
            Assert.AreEqual(0, list.Count, "列表应被清空");
            Assert.AreEqual(1, ListPool<float>.Count, "池中应有 1 个列表");
        }

        /// <summary>
        ///     测试 ListPool Clear
        /// </summary>
        [Test]
        public void Test_ListPool_Clear()
        {
            // Arrange
            ListPool<long>.Clear();
            var list1 = ListPool<long>.Get();
            var list2 = ListPool<long>.Get();
            ListPool<long>.Release(list1);
            ListPool<long>.Release(list2);

            // Act
            ListPool<long>.Clear();

            // Assert
            Assert.AreEqual(0, ListPool<long>.Count, "清空后池中应无列表");
        }

        #endregion

        #region HashSetPool 测试

        /// <summary>
        ///     测试 HashSetPool 基础功能
        /// </summary>
        [Test]
        public void Test_HashSetPool_GetAndRelease()
        {
            // Arrange
            HashSetPool<int>.Clear();

            // Act
            var set = HashSetPool<int>.Get();
            set.Add(1);
            set.Add(2);
            set.Add(3);

            // Assert
            Assert.IsNotNull(set, "应成功获取集合");
            Assert.AreEqual(3, set.Count, "集合应有 3 个元素");

            // Release
            HashSetPool<int>.Release(set);
            Assert.AreEqual(0, set.Count, "归还后集合应被清空");
            Assert.AreEqual(1, HashSetPool<int>.Count, "池中应有 1 个集合");
        }

        /// <summary>
        ///     测试 HashSetPool 复用
        /// </summary>
        [Test]
        public void Test_HashSetPool_Reuse()
        {
            // Arrange
            HashSetPool<string>.Clear();
            var set1 = HashSetPool<string>.Get();
            HashSetPool<string>.Release(set1);

            // Act
            var set2 = HashSetPool<string>.Get();

            // Assert
            Assert.AreSame(set1, set2, "应复用同一集合");

            HashSetPool<string>.Release(set2);
        }

        /// <summary>
        ///     测试 HashSetPool 扩展方法
        /// </summary>
        [Test]
        public void Test_HashSetPool_ExtensionMethod()
        {
            // Arrange
            HashSetPool<float>.Clear();
            var set = HashSetPool<float>.Get();
            set.Add(1.0f);

            // Act
            set.Release2Pool();

            // Assert
            Assert.AreEqual(0, set.Count, "集合应被清空");
            Assert.AreEqual(1, HashSetPool<float>.Count, "池中应有 1 个集合");
        }

        #endregion

        #region StackPool 测试

        /// <summary>
        ///     测试 StackPool 基础功能
        /// </summary>
        [Test]
        public void Test_StackPool_GetAndRelease()
        {
            // Arrange
            StackPool<int>.Clear();

            // Act
            var stack = StackPool<int>.Get();
            stack.Push(1);
            stack.Push(2);
            stack.Push(3);

            // Assert
            Assert.IsNotNull(stack, "应成功获取栈");
            Assert.AreEqual(3, stack.Count, "栈应有 3 个元素");

            // Release
            StackPool<int>.Release(stack);
            Assert.AreEqual(0, stack.Count, "归还后栈应被清空");
            Assert.AreEqual(1, StackPool<int>.Count, "池中应有 1 个栈");
        }

        /// <summary>
        ///     测试 StackPool 复用
        /// </summary>
        [Test]
        public void Test_StackPool_Reuse()
        {
            // Arrange
            StackPool<string>.Clear();
            var stack1 = StackPool<string>.Get();
            StackPool<string>.Release(stack1);

            // Act
            var stack2 = StackPool<string>.Get();

            // Assert
            Assert.AreSame(stack1, stack2, "应复用同一栈");

            StackPool<string>.Release(stack2);
        }

        /// <summary>
        ///     测试 StackPool 扩展方法
        /// </summary>
        [Test]
        public void Test_StackPool_ExtensionMethod()
        {
            // Arrange
            StackPool<float>.Clear();
            var stack = StackPool<float>.Get();
            stack.Push(1.0f);

            // Act
            stack.Release2Pool();

            // Assert
            Assert.AreEqual(0, stack.Count, "栈应被清空");
            Assert.AreEqual(1, StackPool<float>.Count, "池中应有 1 个栈");
        }

        #endregion

        #region 测试数据类型

        private class TestData { }

        private class TestData2 { }

        private class TestData3 { }

        private class TestData4 { }

        private class TestData5 { }

        private class TestData6 { }

        private class NonExistentType { }

        private class NonExistentType2 { }

        private class SimpleObject
        {
            public int Value { get; set; }
        }

        private class PoolableObject : IPoolable
        {
            public bool IsRecycled { get; set; }
            public bool AllocateCalled { get; set; }
            public bool RecycleCalled { get; set; }

            public void OnAllocate()
            {
                AllocateCalled = true;
            }

            public void OnRecycle()
            {
                RecycleCalled = true;
            }
        }

        #endregion
    }
}