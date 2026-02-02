using System.Linq;
using NUnit.Framework;
using UnityEngine;
using EasyPack.Architecture;
using EasyPack.GamePropertySystem;
using EasyPack.ENekoFramework;
using EasyPack.Modifiers;
using EasyPack.Serialization;

namespace EasyPack.GamepropertyTests
{
    /// <summary>
    ///     测试 GamePropertyManager 的序列化和反序列化功能
    ///     包括属性数据、元数据和索引重建
    /// </summary>
    [TestFixture]
    public class TestSerialization
    {
        private GamePropertyService _manager;
        private PropertyManagerSerializer _serializer;

        [SetUp]
        public void Setup()
        {
            EasyPackArchitecture.ResetInstance();
            _manager = new();
            _manager.InitializeAsync().GetAwaiter().GetResult();
            _serializer = new();
        }

        [TearDown]
        public void TearDown()
        {
            _manager?.Dispose();
            _manager = null;
        }

        #region 基础序列化测试

        [Test]
        public void Test_序列化_空Manager()
        {
            // Act
            string json = _serializer.SerializeToJson(_manager);

            // Assert
            Assert.IsNotNull(json);
            Assert.IsTrue(json.Contains("Properties"));
            Assert.IsTrue(json.Contains("Metadata"));
        }

        [Test]
        public void Test_序列化_单个属性无元数据()
        {
            // Arrange
            var hp = new GameProperty("hp", 100f);
            _manager.Register(hp, "Character");

            // Act
            string json = _serializer.SerializeToJson(_manager);

            // Assert
            Assert.IsNotNull(json);
            Assert.IsTrue(json.Contains("\"ID\":\"hp\""));
            Assert.IsTrue(json.Contains("\"Category\":\"Character\""));
        }

        [Test]
        public void Test_序列化_多个属性不同分类()
        {
            // Arrange
            _manager.Register(new("hp", 100f), "Character.Vital");
            _manager.Register(new("mp", 50f), "Character.Vital");
            _manager.Register(new("strength", 10f), "Character.Base");

            // Act
            string json = _serializer.SerializeToJson(_manager);

            // Assert
            Assert.IsNotNull(json);
            Assert.IsTrue(json.Contains("\"ID\":\"hp\""));
            Assert.IsTrue(json.Contains("\"ID\":\"mp\""));
            Assert.IsTrue(json.Contains("\"ID\":\"strength\""));
        }

        [Test]
        public void Test_序列化_包含元数据()
        {
            // Arrange
            var hp = new GameProperty("hp", 100f);
            var metadata =
                new PropertyDisplayInfo { DisplayName = "生命值", Description = "角色当前生命值", IconPath = "Icons/hp" };
            string[] tags = new[] { "vital", "displayInUI" };
            _manager.Register(hp, "Character", metadata, tags);

            // Act
            string json = _serializer.SerializeToJson(_manager);

            // Assert
            Assert.IsNotNull(json);
            Assert.IsTrue(json.Contains("\"DisplayName\":\"生命值\""));
            Assert.IsTrue(json.Contains("\"Tags\""));
        }

        [Test]
        public void Test_序列化_包含修饰符()
        {
            // Arrange
            var hp = new GameProperty("hp", 100f);
            hp.AddModifier(new FloatModifier(ModifierType.Add, 100, 50f));
            hp.AddModifier(new FloatModifier(ModifierType.Mul, 200, 1.5f));
            _manager.Register(hp, "Character");

            // Act
            string json = _serializer.SerializeToJson(_manager);

            // Assert
            Assert.IsNotNull(json);
            // 检查SerializedProperty字段中包含修饰符信息
            Debug.Log("序列化JSON: " + json);
            Assert.IsTrue(json.Contains("SerializedProperty") || json.Contains("Modifiers"),
                "JSON应该包含修饰符信息");
        }

        #endregion

        #region 反序列化测试

        [Test]
        public void Test_反序列化_空Manager()
        {
            // Arrange
            string json = _serializer.SerializeToJson(_manager);

            // Act
            GamePropertyService loaded = _serializer.DeserializeFromJson(json);

            // Assert
            Assert.IsNotNull(loaded);
            Assert.AreEqual(ServiceLifecycleState.Ready, loaded.State);
            Assert.AreEqual(0, loaded.GetAllPropertyIds().Count());

            // Cleanup
            loaded.Dispose();
        }

        [Test]
        public void Test_反序列化_单个属性()
        {
            // Arrange
            var hp = new GameProperty("hp", 100f);
            _manager.Register(hp, "Character");
            string json = _serializer.SerializeToJson(_manager);

            // Act
            GamePropertyService loaded = _serializer.DeserializeFromJson(json);

            // Assert
            Assert.IsNotNull(loaded);
            GameProperty loadedHp = loaded.Get("hp");
            Assert.IsNotNull(loadedHp);
            Assert.AreEqual("hp", loadedHp.ID);
            Assert.AreEqual(100f, loadedHp.GetBaseValue());

            // Cleanup
            loaded.Dispose();
        }

        [Test]
        public void Test_反序列化_多个属性()
        {
            // Arrange
            _manager.Register(new("hp", 100f), "Character.Vital");
            _manager.Register(new("mp", 50f), "Character.Vital");
            _manager.Register(new("strength", 10f), "Character.Base");
            string json = _serializer.SerializeToJson(_manager);

            // Act
            GamePropertyService loaded = _serializer.DeserializeFromJson(json);

            // Assert
            Assert.IsNotNull(loaded);
            Assert.AreEqual(3, loaded.GetAllPropertyIds().Count());
            Assert.IsNotNull(loaded.Get("hp"));
            Assert.IsNotNull(loaded.Get("mp"));
            Assert.IsNotNull(loaded.Get("strength"));

            // Cleanup
            loaded.Dispose();
        }

        [Test]
        public void Test_反序列化_元数据还原()
        {
            // Arrange
            var hp = new GameProperty("hp", 100f);
            var metadata =
                new PropertyDisplayInfo { DisplayName = "生命值", Description = "角色当前生命值", IconPath = "Icons/hp" };
            string[] tags = new[] { "vital", "displayInUI" };
            _manager.Register(hp, "Character", metadata, tags);
            string json = _serializer.SerializeToJson(_manager);

            // Act
            GamePropertyService loaded = _serializer.DeserializeFromJson(json);

            // Assert
            PropertyDisplayInfo loadedMetadata = loaded.GetPropertyDisplayInfo("hp");
            Assert.IsNotNull(loadedMetadata);
            Assert.AreEqual("生命值", loadedMetadata.DisplayName);
            Assert.AreEqual("角色当前生命值", loadedMetadata.Description);
            Assert.AreEqual("Icons/hp", loadedMetadata.IconPath);
            Assert.AreEqual(2, loaded.GetTags("hp").Count());
            Assert.IsTrue(loaded.HasTag("hp", "vital"));

            // Cleanup
            loaded.Dispose();
        }

        [Test]
        public void Test_反序列化_修饰符还原()
        {
            // Arrange
            var hp = new GameProperty("hp", 100f);
            hp.AddModifier(new FloatModifier(ModifierType.Add, 100, 50f));
            hp.AddModifier(new FloatModifier(ModifierType.Mul, 200, 1.5f));
            _manager.Register(hp, "Character");
            string json = _serializer.SerializeToJson(_manager);

            // Act
            GamePropertyService loaded = _serializer.DeserializeFromJson(json);

            // Assert
            GameProperty loadedHp = loaded.Get("hp");
            Assert.IsNotNull(loadedHp);
            Assert.AreEqual(2, loadedHp.Modifiers.Count);
            float actualValue = loadedHp.GetValue();
            Assert.AreEqual(225f, actualValue, 0.01f,
                $"期望值: 225 (计算: (100 + 50) × 1.5), 实际值: {actualValue}");

            // Cleanup
            loaded.Dispose();
        }

        #endregion

        #region 索引重建测试

        [Test]
        public void Test_索引重建_分类索引()
        {
            // Arrange
            _manager.Register(new("hp", 100f), "Character.Vital");
            _manager.Register(new("mp", 50f), "Character.Vital");
            _manager.Register(new("strength", 10f), "Character.Base");
            string json = _serializer.SerializeToJson(_manager);

            // Act
            GamePropertyService loaded = _serializer.DeserializeFromJson(json);

            // Assert
            var vitalProps = loaded.GetByCategory("Character.Vital").ToList();
            Assert.AreEqual(2, vitalProps.Count);
            Assert.IsTrue(vitalProps.Any(p => p.ID == "hp"));
            Assert.IsTrue(vitalProps.Any(p => p.ID == "mp"));

            var baseProps = loaded.GetByCategory("Character.Base").ToList();
            Assert.AreEqual(1, baseProps.Count);
            Assert.AreEqual("strength", baseProps[0].ID);

            // Cleanup
            loaded.Dispose();
        }

        [Test]
        public void Test_索引重建_标签索引()
        {
            // Arrange
            var hp = new GameProperty("hp", 100f);
            string[] hpTags = new[] { "vital", "displayInUI" };
            _manager.Register(hp, "Character", null, hpTags);

            var mp = new GameProperty("mp", 50f);
            string[] mpTags = new[] { "vital" };
            _manager.Register(mp, "Character", null, mpTags);

            var gold = new GameProperty("gold", 1000f);
            string[] goldTags = new[] { "displayInUI" };
            _manager.Register(gold, "Economy", null, goldTags);

            string json = _serializer.SerializeToJson(_manager);

            // Act
            GamePropertyService loaded = _serializer.DeserializeFromJson(json);

            // Assert
            var vitalProps = loaded.GetByTag("vital").ToList();
            Assert.AreEqual(2, vitalProps.Count);
            Assert.IsTrue(vitalProps.Any(p => p.ID == "hp"));
            Assert.IsTrue(vitalProps.Any(p => p.ID == "mp"));

            var uiProps = loaded.GetByTag("displayInUI").ToList();
            Assert.AreEqual(2, uiProps.Count);
            Assert.IsTrue(uiProps.Any(p => p.ID == "hp"));
            Assert.IsTrue(uiProps.Any(p => p.ID == "gold"));

            // Cleanup
            loaded.Dispose();
        }

        [Test]
        public void Test_索引重建_组合查询()
        {
            // Arrange
            var hp = new GameProperty("hp", 100f);
            string[] hpTags = new[] { "vital" };
            _manager.Register(hp, "Character", null, hpTags);

            var mp = new GameProperty("mp", 50f);
            string[] mpTags = new[] { "resource" };
            _manager.Register(mp, "Character", null, mpTags);

            var gold = new GameProperty("gold", 1000f);
            string[] goldTags = new[] { "vital" };
            _manager.Register(gold, "Economy", null, goldTags);

            string json = _serializer.SerializeToJson(_manager);

            // Act
            GamePropertyService loaded = _serializer.DeserializeFromJson(json);

            // Assert
            var result = loaded.GetByCategoryAndTag("Character", "vital").ToList();
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("hp", result[0].ID);

            // Cleanup
            loaded.Dispose();
        }

        #endregion

        #region 边界情况测试

        [Test]
        public void Test_序列化_null对象()
        {
            // Act & Assert
            string json = _serializer.SerializeToJson(null);
            Assert.IsNull(json);
        }

        [Test]
        public void Test_反序列化_null字符串()
        {
            // Act & Assert
            Assert.Throws<SerializationException>(() => { _serializer.DeserializeFromJson(null); });
        }

        [Test]
        public void Test_反序列化_空字符串()
        {
            // Act & Assert
            Assert.Throws<SerializationException>(() => { _serializer.DeserializeFromJson(""); });
        }

        [Test]
        public void Test_反序列化_无效JSON()
        {
            // Act & Assert
            Assert.Throws<SerializationException>(() => { _serializer.DeserializeFromJson("{invalid json}"); });
        }

        [Test]
        public void Test_序列化反序列化_完整往返()
        {
            // Arrange
            _manager.Register(new("hp", 100f), "Character.Vital", new() { DisplayName = "生命值" }, new[] { "vital" });
            _manager.Register(new("mp", 50f), "Character.Vital", new() { DisplayName = "魔法值" }, new[] { "vital" });
            _manager.Register(new("strength", 10f), "Character.Base", new() { DisplayName = "力量" });

            // Act
            string json = _serializer.SerializeToJson(_manager);
            GamePropertyService loaded = _serializer.DeserializeFromJson(json);

            // Assert
            Assert.AreEqual(3, loaded.GetAllPropertyIds().Count());
            Assert.AreEqual(100f, loaded.Get("hp").GetBaseValue());
            Assert.AreEqual(50f, loaded.Get("mp").GetBaseValue());
            Assert.AreEqual(10f, loaded.Get("strength").GetBaseValue());
            Assert.AreEqual("生命值", loaded.GetPropertyDisplayInfo("hp").DisplayName);
            Assert.AreEqual(2, loaded.GetByCategory("Character.Vital").Count());
            Assert.AreEqual(2, loaded.GetByTag("vital").Count());

            // Cleanup
            loaded.Dispose();
        }

        #endregion
    }
}