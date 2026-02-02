using System;
using System.Linq;
using NUnit.Framework;
using EasyPack.Architecture;
using EasyPack.GamePropertySystem;
using EasyPack.ENekoFramework;

namespace EasyPack.GamepropertyTests
{
    /// <summary>
    /// 测试GamePropertyManager的CRUD功能
    /// 包括注册、查询、删除等基础操作
    /// </summary>
    [TestFixture]
    public class TestManagerCRUD
    {
        private GamePropertyService _manager;

        [SetUp]
        public void Setup()
        {
            EasyPackArchitecture.ResetInstance();
            _manager = new GamePropertyService();
            _manager.InitializeAsync().GetAwaiter().GetResult();
        }

        [TearDown]
        public void TearDown()
        {
            _manager?.Dispose();
            _manager = null;
        }

        #region 生命周期测试

        [Test]
        public void Test_服务生命周期_正常流程()
        {
            var manager = new GamePropertyService();
            Assert.AreEqual(ServiceLifecycleState.Uninitialized, manager.State);

            manager.InitializeAsync().GetAwaiter().GetResult();
            Assert.AreEqual(ServiceLifecycleState.Ready, manager.State);

            manager.Pause();
            Assert.AreEqual(ServiceLifecycleState.Paused, manager.State);

            manager.Resume();
            Assert.AreEqual(ServiceLifecycleState.Ready, manager.State);

            manager.Dispose();
            Assert.AreEqual(ServiceLifecycleState.Disposed, manager.State);
        }

        [Test]
        public void Test_服务未就绪时操作抛出异常()
        {
            var manager = new GamePropertyService();
            var property = new GameProperty("test", 100);

            Assert.Throws<InvalidOperationException>(() => manager.Register(property));
        }

        #endregion

        #region 注册测试

        [Test]
        public void Test_注册单个属性_成功()
        {
            var property = new GameProperty("hp", 100);
            _manager.Register(property, "Character");

            var retrieved = _manager.Get("hp");
            Assert.IsNotNull(retrieved);
            Assert.AreEqual("hp", retrieved.ID);
            Assert.AreEqual(100, retrieved.GetValue());
        }

        [Test]
        public void Test_注册属性带元数据_成功()
        {
            var property = new GameProperty("hp", 100);
            var metadata = new PropertyDisplayInfo
            {
                DisplayName = "生命值",
                Description = "角色当前生命值"
            };
            var tags = new[] { "vital", "ui" };

            _manager.Register(property, "Character", metadata, tags);

            var retrievedMeta = _manager.GetPropertyDisplayInfo("hp");
            Assert.IsNotNull(retrievedMeta);
            Assert.AreEqual("生命值", retrievedMeta.DisplayName);
            Assert.AreEqual("角色当前生命值", retrievedMeta.Description);
            Assert.IsTrue(_manager.HasTag("hp", "vital"));
            Assert.IsTrue(_manager.HasTag("hp", "ui"));
        }

        [Test]
        public void Test_注册重复ID抛出异常()
        {
            var property1 = new GameProperty("hp", 100);
            var property2 = new GameProperty("hp", 200);

            _manager.Register(property1);
            Assert.Throws<ArgumentException>(() => _manager.Register(property2));
        }

        [Test]
        public void Test_注册空属性抛出异常()
        {
            Assert.Throws<ArgumentNullException>(() => _manager.Register(null));
        }

        [Test]
        public void Test_批量注册属性_成功()
        {
            var properties = new[]
            {
                new GameProperty("hp", 100),
                new GameProperty("mp", 50),
                new GameProperty("strength", 10)
            };

            _manager.RegisterRange(properties, "Character");

            Assert.IsNotNull(_manager.Get("hp"));
            Assert.IsNotNull(_manager.Get("mp"));
            Assert.IsNotNull(_manager.Get("strength"));
        }

        #endregion

        #region 查询测试

        [Test]
        public void Test_按ID查询_存在时返回属性()
        {
            var property = new GameProperty("hp", 100);
            _manager.Register(property);

            var result = _manager.Get("hp");
            Assert.IsNotNull(result);
            Assert.AreEqual("hp", result.ID);
        }

        [Test]
        public void Test_按ID查询_不存在时返回null()
        {
            var result = _manager.Get("nonexistent");
            Assert.IsNull(result);
        }

        [Test]
        public void Test_按ID查询_空ID返回null()
        {
            Assert.IsNull(_manager.Get(null));
            Assert.IsNull(_manager.Get(""));
        }

        [Test]
        public void Test_获取所有属性ID()
        {
            _manager.Register(new GameProperty("hp", 100));
            _manager.Register(new GameProperty("mp", 50));
            _manager.Register(new GameProperty("strength", 10));

            var ids = _manager.GetAllPropertyIds().ToList();
            Assert.AreEqual(3, ids.Count);
            Assert.Contains("hp", ids);
            Assert.Contains("mp", ids);
            Assert.Contains("strength", ids);
        }

        [Test]
        public void Test_获取所有分类()
        {
            _manager.Register(new GameProperty("hp", 100), "Character.Vital");
            _manager.Register(new GameProperty("mp", 50), "Character.Vital");
            _manager.Register(new GameProperty("strength", 10), "Character.Base");

            var categories = _manager.GetAllCategories().ToList();
            Assert.Contains("Character.Vital", categories);
            Assert.Contains("Character.Base", categories);
        }

        #endregion

        #region 删除测试

        [Test]
        public void Test_删除存在的属性_返回true()
        {
            var property = new GameProperty("hp", 100);
            _manager.Register(property);

            var result = _manager.Unregister("hp");
            Assert.IsTrue(result);
            Assert.IsNull(_manager.Get("hp"));
        }

        [Test]
        public void Test_删除不存在的属性_返回false()
        {
            var result = _manager.Unregister("nonexistent");
            Assert.IsFalse(result);
        }

        [Test]
        public void Test_删除属性后元数据也被删除()
        {
            var property = new GameProperty("hp", 100);
            var metadata = new PropertyDisplayInfo { DisplayName = "生命值" };
            _manager.Register(property, "Character", metadata);

            _manager.Unregister("hp");
            Assert.IsNull(_manager.GetPropertyDisplayInfo("hp"));
        }

        [Test]
        public void Test_UID查询与删除_基础流程()
        {
            var property = new GameProperty("hp", 100);
            _manager.Register(property, "Character");

            long uid = property.UID;
            Assert.Greater(uid, 0);

            var byUid = _manager.GetByUid(uid);
            Assert.IsNotNull(byUid);
            Assert.AreEqual("hp", byUid.ID);

            Assert.IsTrue(_manager.UnregisterByUid(uid));
            Assert.IsNull(_manager.Get("hp"));
            Assert.IsNull(_manager.GetByUid(uid));
        }

        [Test]
        public void Test_UID移动分类_成功()
        {
            var property = new GameProperty("hp", 100);
            _manager.Register(property, "Character.Vital");

            long uid = property.UID;
            Assert.Greater(uid, 0);

            Assert.IsTrue(_manager.MoveToCategoryByUid(uid, "Character.Base"));

            Assert.AreEqual(0, _manager.GetByCategory("Character.Vital", includeChildren: false).Count());
            Assert.AreEqual(1, _manager.GetByCategory("Character.Base", includeChildren: false).Count());
        }

        [Test]
        public void Test_删除整个分类()
        {
            _manager.Register(new GameProperty("hp", 100), "Character.Vital");
            _manager.Register(new GameProperty("mp", 50), "Character.Vital");
            _manager.Register(new GameProperty("strength", 10), "Character.Base");

            _manager.UnregisterCategory("Character.Vital");

            Assert.IsNull(_manager.Get("hp"));
            Assert.IsNull(_manager.Get("mp"));
            Assert.IsNotNull(_manager.Get("strength"));
        }

        #endregion

        #region 元数据测试

        [Test]
        public void Test_元数据标签自动去重()
        {
            var property = new GameProperty("hp", 100);
            var tags = new[] { "vital", "ui", "vital", "ui" };

            _manager.Register(property, "Character", null, tags);

            var retrievedTags = _manager.GetTags("hp").ToList();
            Assert.AreEqual(2, retrievedTags.Count);
            Assert.Contains("vital", retrievedTags);
            Assert.Contains("ui", retrievedTags);
        }

        [Test]
        public void Test_获取不存在属性的元数据返回null()
        {
            var meta = _manager.GetPropertyDisplayInfo("nonexistent");
            Assert.IsNull(meta);
        }

        #endregion
    }
}
