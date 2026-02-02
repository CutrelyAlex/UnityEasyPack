using System.Linq;
using NUnit.Framework;
using EasyPack.Architecture;
using EasyPack.GamePropertySystem;

namespace EasyPack.GamepropertyTests
{
    /// <summary>
    ///     测试GamePropertyManager的分类和标签查询功能
    /// </summary>
    [TestFixture]
    public class TestCategoryQuery
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

        #region 分类查询测试

        [Test]
        public void Test_按分类查询_精确匹配()
        {
            _manager.Register(new("hp", 100), "Character.Vital");
            _manager.Register(new("mp", 50), "Character.Vital");
            _manager.Register(new("strength", 10), "Character.Base");

            var vitalProps = _manager.GetByCategory("Character.Vital").ToList();
            Assert.AreEqual(2, vitalProps.Count);
            Assert.IsTrue(vitalProps.Any(p => p.ID == "hp"));
            Assert.IsTrue(vitalProps.Any(p => p.ID == "mp"));
        }

        [Test]
        public void Test_按分类查询_包含子分类()
        {
            _manager.Register(new("hp", 100), "Character.Vital");
            _manager.Register(new("mp", 50), "Character.Vital.Magic");
            _manager.Register(new("strength", 10), "Character.Base");

            var characterProps = _manager.GetByCategory("Character", true).ToList();
            Assert.GreaterOrEqual(characterProps.Count, 3);
            Assert.IsTrue(characterProps.Any(p => p.ID == "hp"));
            Assert.IsTrue(characterProps.Any(p => p.ID == "mp"));
            Assert.IsTrue(characterProps.Any(p => p.ID == "strength"));
        }

        [Test]
        public void Test_按分类查询_不包含子分类()
        {
            _manager.Register(new("hp", 100), "Character");
            _manager.Register(new("mp", 50), "Character.Vital");

            var props = _manager.GetByCategory("Character", false).ToList();
            Assert.AreEqual(1, props.Count);
            Assert.AreEqual("hp", props[0].ID);
        }

        [Test]
        public void Test_按分类查询_通配符模式()
        {
            _manager.Register(new("hp", 100), "Character.Vital.HP");
            _manager.Register(new("mp", 50), "Character.Vital.MP");
            _manager.Register(new("strength", 10), "Character.Base");

            var vitalProps = _manager.GetByCategory("Character.Vital.*", true).ToList();
            Assert.GreaterOrEqual(vitalProps.Count, 2);
        }

        [Test]
        public void Test_按分类查询_不存在的分类返回空集合()
        {
            var props = _manager.GetByCategory("NonExistent").ToList();
            Assert.AreEqual(0, props.Count);
        }

        [Test]
        public void Test_按分类查询_空分类名返回空集合()
        {
            var props1 = _manager.GetByCategory(null).ToList();
            var props2 = _manager.GetByCategory("").ToList();
            Assert.AreEqual(0, props1.Count);
            Assert.AreEqual(0, props2.Count);
        }

        #endregion

        #region 标签查询测试

        [Test]
        public void Test_按标签查询_单个标签()
        {
            var hp = new GameProperty("hp", 100);
            var mp = new GameProperty("mp", 50);
            var strength = new GameProperty("strength", 10);

            _manager.Register(hp, "Character", null, new[] { "vital", "ui" });
            _manager.Register(mp, "Character", null, new[] { "vital", "ui" });
            _manager.Register(strength, "Character", null, new[] { "ui" });

            var vitalProps = _manager.GetByTag("vital").ToList();
            Assert.AreEqual(2, vitalProps.Count);
            Assert.IsTrue(vitalProps.Any(p => p.ID == "hp"));
            Assert.IsTrue(vitalProps.Any(p => p.ID == "mp"));
        }

        [Test]
        public void Test_按标签查询_不存在的标签返回空集合()
        {
            var props = _manager.GetByTag("nonexistent").ToList();
            Assert.AreEqual(0, props.Count);
        }

        [Test]
        public void Test_按标签查询_空标签返回空集合()
        {
            var props1 = _manager.GetByTag(null).ToList();
            var props2 = _manager.GetByTag("").ToList();
            Assert.AreEqual(0, props1.Count);
            Assert.AreEqual(0, props2.Count);
        }

        [Test]
        public void Test_按标签查询_无元数据的属性不出现在结果中()
        {
            _manager.Register(new("hp", 100), "Character");
            _manager.Register(new("mp", 50), "Character", null, new[] { "vital" });

            var vitalProps = _manager.GetByTag("vital").ToList();
            Assert.AreEqual(1, vitalProps.Count);
            Assert.AreEqual("mp", vitalProps[0].ID);
        }

        #endregion

        #region 组合查询测试

        [Test]
        public void Test_组合查询_分类和标签交集()
        {
            _manager.Register(new("hp", 100), "Character.Vital", null, new[] { "ui", "saveable" });
            _manager.Register(new("mp", 50), "Character.Vital", null, new[] { "ui" });
            _manager.Register(new("strength", 10), "Character.Base", null, new[] { "ui", "saveable" });

            var props = _manager.GetByCategoryAndTag("Character.Vital", "saveable").ToList();
            Assert.AreEqual(1, props.Count);
            Assert.AreEqual("hp", props[0].ID);
        }

        [Test]
        public void Test_组合查询_无交集返回空集合()
        {
            _manager.Register(new("hp", 100), "Character.Vital", null, new[] { "ui" });
            _manager.Register(new("strength", 10), "Character.Base", null, new[] { "combat" });

            var props = _manager.GetByCategoryAndTag("Character.Vital", "combat").ToList();
            Assert.AreEqual(0, props.Count);
        }

        #endregion

        #region 层级分类测试

        [Test]
        public void Test_层级分类_深度嵌套()
        {
            _manager.Register(new("crit", 0.1f), "Character.Combat.Offense.Critical");
            _manager.Register(new("dodge", 0.05f), "Character.Combat.Defense.Evasion");

            var offenseProps = _manager.GetByCategory("Character.Combat.Offense", true).ToList();
            Assert.AreEqual(1, offenseProps.Count);
            Assert.AreEqual("crit", offenseProps[0].ID);

            var combatProps = _manager.GetByCategory("Character.Combat", true).ToList();
            Assert.AreEqual(2, combatProps.Count);
        }

        [Test]
        public void Test_分类名自由格式_支持任意字符()
        {
            _manager.Register(new("test1", 1), "角色_战斗_攻击");
            _manager.Register(new("test2", 2), "Character-Combat-Attack");
            _manager.Register(new("test3", 3), "Character/Combat/Attack");

            Assert.IsNotNull(_manager.Get("test1"));
            Assert.IsNotNull(_manager.Get("test2"));
            Assert.IsNotNull(_manager.Get("test3"));

            var categories = _manager.GetAllCategories().ToList();
            Assert.Contains("角色_战斗_攻击", categories);
            Assert.Contains("Character-Combat-Attack", categories);
            Assert.Contains("Character/Combat/Attack", categories);
        }

        #endregion

        #region 索引一致性测试

        [Test]
        public void Test_删除属性后索引自动清理()
        {
            var hp = new GameProperty("hp", 100);
            _manager.Register(hp, "Character.Vital", null, new[] { "vital" });

            _manager.Unregister("hp");

            var categoryProps = _manager.GetByCategory("Character.Vital").ToList();
            var tagProps = _manager.GetByTag("vital").ToList();

            Assert.AreEqual(0, categoryProps.Count);
            Assert.AreEqual(0, tagProps.Count);
        }

        [Test]
        public void Test_删除分类后所有索引清理()
        {
            _manager.Register(new("hp", 100), "Character.Vital", null, new[] { "vital" });
            _manager.Register(new("mp", 50), "Character.Vital", null, new[] { "vital" });

            _manager.UnregisterCategory("Character.Vital");

            var categoryProps = _manager.GetByCategory("Character.Vital").ToList();
            var tagProps = _manager.GetByTag("vital").ToList();

            Assert.AreEqual(0, categoryProps.Count);
            Assert.AreEqual(0, tagProps.Count);
        }

        #endregion
    }
}