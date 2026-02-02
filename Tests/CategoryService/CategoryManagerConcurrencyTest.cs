using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EasyPack.Category;
using NUnit.Framework;

namespace EasyPack.CategoryTests
{
    /// <summary>
    /// 并发/线程安全回归测试（偏“不会炸 + 不留脏索引”）。
    ///
    /// 说明：这些测试更关注
    /// - 并发读写不抛异常（尤其是集合在枚举期间被修改）
    /// - DeleteEntity / DeleteCategoryRecursive 后，不会遗留标签反向索引，导致同 key 重新注册后“继承旧标签”
    /// </summary>
    [TestFixture]
    public class CategoryManagerConcurrencyTests : CategoryTestBase
    {
        [Test]
        public void Concurrent_TagAndCategoryOperations_DoesNotThrow_And_NoOrphanTagsAfterReregister()
        {
            // Arrange
            var entities = TestDataGenerator.GenerateEntities(120, "cc_");
            foreach (var e in entities)
            {
                TestAssertions.AssertSuccess(RegisterEntity(e, "Root.Level1"));
            }

            var deletedSuccessfully = new ConcurrentDictionary<string, byte>(StringComparer.Ordinal);
            var exceptions = new ConcurrentQueue<Exception>();

            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromSeconds(1.5));
            CancellationToken token = cts.Token;

            // Act
            Task tagWriter = Task.Run(() =>
            {
                int i = 0;
                while (!token.IsCancellationRequested)
                {
                    string id = entities[i % entities.Count].Id;
                    try
                    {
                        // 允许 NotFound（可能刚好被删除）；关键是不要抛异常、不要留下脏索引。
                        Manager.AddTag(id, "t" + (i % 5));
                    }
                    catch (Exception ex)
                    {
                        exceptions.Enqueue(ex);
                    }
                    i++;
                }
            }, token);

            Task categoryMover = Task.Run(() =>
            {
                int i = 0;
                while (!token.IsCancellationRequested)
                {
                    string id = entities[i % entities.Count].Id;
                    try
                    {
                        Manager.MoveEntityToCategory(id, (i % 2 == 0) ? "Root.Level2" : "Root.Level1");
                    }
                    catch (Exception ex)
                    {
                        exceptions.Enqueue(ex);
                    }
                    i++;
                }
            }, token);

            Task entityDeleter = Task.Run(() =>
            {
                int i = 0;
                while (!token.IsCancellationRequested)
                {
                    string id = entities[i % entities.Count].Id;
                    try
                    {
                        var r = Manager.DeleteEntity(id);
                        if (r.IsSuccess)
                        {
                            deletedSuccessfully.TryAdd(id, 0);
                        }
                    }
                    catch (Exception ex)
                    {
                        exceptions.Enqueue(ex);
                    }
                    i++;
                }
            }, token);

            Task reader = Task.Run(() =>
            {
                int i = 0;
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        // 并发读：分类查询 + 标签查询 + 元数据读取
                        Manager.GetByCategory("Root.*", includeChildren: true);
                        Manager.GetByTag("t" + (i % 5));
                        Manager.GetEntityTags(entities[i % entities.Count].Id);
                    }
                    catch (Exception ex)
                    {
                        exceptions.Enqueue(ex);
                    }
                    i++;
                }
            }, token);

            try
            {
                Task.WaitAll(tagWriter, categoryMover, entityDeleter, reader);
            }
            finally
            {
                cts.Dispose();
            }

            // Assert
            if (!exceptions.IsEmpty)
            {
                // 聚合展示前几个异常，便于定位。
                var first = exceptions.Take(3).ToArray();
                Assert.Fail("Concurrency test threw exception(s):\n" + string.Join("\n---\n", first.Select(e => e.ToString())));
            }

            // 关键一致性断言：对“曾经被成功删除过”的 key，重新注册后不应该继承旧标签。
            foreach (string id in deletedSuccessfully.Keys)
            {
                var re = new TestEntity(id, "Re-" + id);
                TestAssertions.AssertSuccess(Manager.RegisterEntity(re, "ReRoot").Complete(), "重新注册应成功");

                var tags = Manager.GetEntityTags(id);
                Assert.AreEqual(0, tags.Count, $"重新注册的实体 '{id}' 不应继承旧标签");

                string path = Manager.GetReadableCategoryPath(id);
                Assert.AreEqual("ReRoot", path, $"重新注册的实体 '{id}' 应在新分类中");
            }
        }

        [Test]
        public void DeadlockFree_TreeAndTagLockOrder_IsPreserved()
        {
            var stableEntity = new TestEntity("deadlock_stable", "Stable");
            RegisterEntity(stableEntity, "LockRoot.Stable");

            var exceptions = new ConcurrentQueue<Exception>();
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            CancellationToken token = cts.Token;

            int moveCounter = 0;

            Task deleteEntityTask = Task.Run(() =>
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        string tempId = $"deadlock_temp_{Guid.NewGuid():N}";
                        RegisterEntity(new TestEntity(tempId, "Temp"), "LockRoot.Temp");
                        Manager.DeleteEntity(tempId);
                    }
                    catch (Exception ex)
                    {
                        exceptions.Enqueue(ex);
                    }
                }
            }, token);

            Task addTagTask = Task.Run(() =>
            {
                int tagIndex = 0;
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        Manager.AddTag(stableEntity.Id, $"deadlock_tag_{tagIndex++ % 4}");
                    }
                    catch (Exception ex)
                    {
                        exceptions.Enqueue(ex);
                    }
                }
            }, token);

            Task moveTask = Task.Run(() =>
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        string newCategory = (Interlocked.Increment(ref moveCounter) % 2 == 0)
                            ? "LockRoot.Stable"
                            : "LockRoot.Stable.Moved";
                        Manager.MoveEntityToCategory(stableEntity.Id, newCategory);
                    }
                    catch (Exception ex)
                    {
                        exceptions.Enqueue(ex);
                    }
                }
            }, token);

            bool completed;
            try
            {
                completed = Task.WaitAll(new[] { deleteEntityTask, addTagTask, moveTask }, TimeSpan.FromSeconds(5));
            }
            finally
            {
                cts.Dispose();
            }

            Assert.IsTrue(completed, "并行锁持有任务未及时退出——可能的锁顺序死锁");
            Assert.IsEmpty(exceptions, "死锁顺序测试期间出现意外异常");
        }

        [Test]
        public void DeleteCategoryRecursive_WithAddTagAndMove_RespectsTagCleanup()
        {
            var stableEntity = new TestEntity("delete_recursive_stable", "Stable");
            RegisterEntity(stableEntity, "DeleteRecursive.Stable");

            var deletedIds = new ConcurrentBag<string>();
            var exceptions = new ConcurrentQueue<Exception>();
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            CancellationToken token = cts.Token;

            int moveCounter = 0;
            int deleteIteration = 0;

            Task deleteCategoryTask = Task.Run(() =>
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        int iter = Interlocked.Increment(ref deleteIteration);
                        string category = $"DeleteRecursive.Temp.Group{iter % 3}";
                        string id = $"delrec_entity_{iter}";
                        RegisterEntity(new TestEntity(id, "Temp"), category);
                        var result = Manager.DeleteCategoryRecursive(category);
                        if (result.IsSuccess)
                        {
                            deletedIds.Add(id);
                        }
                        else if (result.ErrorCode != ErrorCode.NotFound)
                        {
                            exceptions.Enqueue(new InvalidOperationException(result.ErrorMessage));
                        }
                    }
                    catch (Exception ex)
                    {
                        exceptions.Enqueue(ex);
                    }
                }
            }, token);

            Task addTagTask = Task.Run(() =>
            {
                int tagIndex = 0;
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        Manager.AddTag(stableEntity.Id, $"delrec_tag_{tagIndex++ % 3}");
                    }
                    catch (Exception ex)
                    {
                        exceptions.Enqueue(ex);
                    }
                }
            }, token);

            Task moveTask = Task.Run(() =>
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        string target = (Interlocked.Increment(ref moveCounter) % 2 == 0)
                            ? "DeleteRecursive.Stable"
                            : "DeleteRecursive.Stable.Moved";
                        Manager.MoveEntityToCategory(stableEntity.Id, target);
                    }
                    catch (Exception ex)
                    {
                        exceptions.Enqueue(ex);
                    }
                }
            }, token);

            bool completed;
            try
            {
                completed = Task.WaitAll(new[] { deleteCategoryTask, addTagTask, moveTask }, TimeSpan.FromSeconds(5));
            }
            finally
            {
                cts.Dispose();
            }

            Assert.IsTrue(completed, "DeleteCategoryRecursive 压力测试未完成——可能的死锁");
            Assert.IsEmpty(exceptions, "DeleteCategoryRecursive 压力测试期间出现意外异常");

            foreach (string id in deletedIds)
            {
                var reentity = new TestEntity(id, "Recovered");
                TestAssertions.AssertSuccess(Manager.RegisterEntity(reentity, "DeleteRecursive.Recovered").Complete(),
                    "重新注册应成功");

                var tags = Manager.GetEntityTags(id);
                Assert.AreEqual(0, tags.Count, $"重新注册的实体 '{id}' 不应有残留标签");
            }
        }
    }
}
