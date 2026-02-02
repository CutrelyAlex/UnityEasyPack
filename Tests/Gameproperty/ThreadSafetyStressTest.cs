using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using NUnit.Framework;
using EasyPack.Architecture;
using EasyPack.GamePropertySystem;

namespace EasyPack.GamepropertyTests
{
    /// <summary>
    ///     线程安全压力测试
    /// </summary>
    [TestFixture]
    public class ThreadSafetyStressTest
    {
        private GamePropertyService _manager;

        [SetUp]
        public void Setup()
        {
            EasyPackArchitecture.ResetInstance();
            _manager = new();
            _manager.InitializeAsync().GetAwaiter().GetResult();
        }

        [TearDown]
        public void TearDown()
        {
            _manager?.Dispose();
            _manager = null;
        }

        [Test]
        public void Test_并发注册100个属性_无数据丢失()
        {
            var tasks = new List<Task>();
            const int totalProps = 100;

            for (int i = 0; i < totalProps; i++)
            {
                int index = i;
                tasks.Add(Task.Run(() =>
                {
                    var property = new GameProperty($"prop_{index}", index);
                    _manager.Register(property, "Concurrent");
                }));
            }

            Task.WaitAll(tasks.ToArray());

            var props = _manager.GetByCategory("Concurrent");
            Assert.AreEqual(totalProps, props.Count(),
                $"期望注册 {totalProps} 个属性，实际 {props.Count()} 个");
        }

        [Test]
        public void Test_并发读写_读取不崩溃()
        {
            // 先注册10个属性
            for (int i = 0; i < 10; i++)
            {
                var property = new GameProperty($"prop_{i}", i);
                _manager.Register(property, "ReadWrite");
            }

            var tasks = new List<Task>();

            // 10个读线程
            for (int i = 0; i < 10; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    for (int j = 0; j < 100; j++)
                    {
                        var props = _manager.GetByCategory("ReadWrite");
                        Assert.IsNotNull(props);
                    }
                }));
            }

            // 5个写线程
            for (int i = 0; i < 5; i++)
            {
                int index = i + 100;
                tasks.Add(Task.Run(() =>
                {
                    for (int j = 0; j < 20; j++)
                    {
                        var property = new GameProperty($"prop_{index}_{j}", index + j);
                        _manager.Register(property, "ReadWrite");
                    }
                }));
            }

            Assert.DoesNotThrow(() => Task.WaitAll(tasks.ToArray()),
                "并发读写不应抛出异常");
        }

        [Test]
        public void Test_并发注册到多个分类_数据一致()
        {
            var tasks = new List<Task>();
            const int categoriesCount = 5;
            const int propsPerCategory = 20;

            for (int cat = 0; cat < categoriesCount; cat++)
            {
                int categoryIndex = cat;
                for (int i = 0; i < propsPerCategory; i++)
                {
                    int propIndex = i;
                    tasks.Add(Task.Run(() =>
                    {
                        var property = new GameProperty($"cat{categoryIndex}_prop{propIndex}", propIndex);
                        _manager.Register(property, $"Category{categoryIndex}");
                    }));
                }
            }

            Task.WaitAll(tasks.ToArray());

            // 验证每个分类都有正确数量的属性
            for (int cat = 0; cat < categoriesCount; cat++)
            {
                var props = _manager.GetByCategory($"Category{cat}");
                Assert.AreEqual(propsPerCategory, props.Count(),
                    $"Category{cat} 应有 {propsPerCategory} 个属性，实际 {props.Count()} 个");
            }
        }

        [Test]
        public void Test_并发带标签注册_标签索引正确()
        {
            var tasks = new List<Task>();
            const int totalProps = 50;
            const int tagsCount = 5;

            for (int i = 0; i < totalProps; i++)
            {
                int index = i;
                tasks.Add(Task.Run(() =>
                {
                    var property = new GameProperty($"prop_{index}", index);
                    string[] tags = new[] { $"Tag{index % tagsCount}" };
                    _manager.Register(property, "Tagged", null, tags);
                }));
            }

            Task.WaitAll(tasks.ToArray());

            // 验证每个标签的属性数量
            for (int tag = 0; tag < tagsCount; tag++)
            {
                var props = _manager.GetByTag($"Tag{tag}");
                Assert.AreEqual(totalProps / tagsCount, props.Count(),
                    $"Tag{tag} 应有 {totalProps / tagsCount} 个属性，实际 {props.Count()} 个");
            }
        }
    }
}