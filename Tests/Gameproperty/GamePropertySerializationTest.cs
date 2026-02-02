using NUnit.Framework;
using UnityEngine;
using EasyPack.GamePropertySystem;
using EasyPack.Architecture;
using EasyPack.Modifiers;
using EasyPack.Serialization;

namespace EasyPack.GamepropertyTests
{
    /// <summary>
    ///     GameProperty 序列化系统测试（EasyPack 架构）
    ///     使用 GamePropertyManager 和异步初始化
    /// </summary>
    [TestFixture]
    public class GamePropertySerializationTests
    {
        private GamePropertyService _manager;

        /// <summary>
        ///     测试前设置：初始化 GamePropertyManager
        /// </summary>
        [SetUp]
        public void Setup()
        {
            EasyPackArchitecture.ResetInstance();
            _manager = new();
            _manager.InitializeAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        ///     测试后清理：释放资源
        /// </summary>
        [TearDown]
        public void Teardown()
        {
            _manager?.Dispose();
        }

        /// <summary>
        ///     测试 GameProperty 的 JSON 序列化和反序列化
        /// </summary>
        [Test]
        public void Test_GameProperty_Serialization()
        {
            // 创建一个带有修饰器的 GameProperty
            var originalProperty = new GameProperty("TestProp", 100f);
            originalProperty.AddModifier(new FloatModifier(ModifierType.Add, 1, 20f));
            originalProperty.AddModifier(new FloatModifier(ModifierType.Mul, 2, 1.5f));
            originalProperty.AddModifier(new RangeModifier(ModifierType.Clamp, 3, new(0f, 200f)));

            // 注册属性到管理器
            _manager.Register(originalProperty, "Test.Serialization");

            Assert.GreaterOrEqual(originalProperty.UID, 0, "注册后应为属性分配 UID");

            // 获取原始值
            float originalValue = originalProperty.GetValue();

            // 获取序列化服务
            ISerializationService serializationService = EasyPackArchitecture.Instance
                .ResolveAsync<ISerializationService>().GetAwaiter().GetResult();

            // 序列化
            string json = serializationService.SerializeToJson(originalProperty);
            Assert.IsNotNull(json, "序列化结果不应为空");
            Assert.IsNotEmpty(json, "序列化结果不应为空");

            // 反序列化
            var deserializedProperty = serializationService.DeserializeFromJson<GameProperty>(json);
            Assert.IsNotNull(deserializedProperty, "反序列化结果不应为空");
            Assert.AreEqual(originalProperty.ID, deserializedProperty.ID, "ID应该一致");
            Assert.AreEqual(originalProperty.UID, deserializedProperty.UID, "UID 应该一致");
            Assert.AreEqual(originalProperty.GetBaseValue(), deserializedProperty.GetBaseValue(),
                "基础值应该一致");

            // 验证修饰器
            Assert.AreEqual(originalProperty.ModifierCount, deserializedProperty.ModifierCount,
                "修饰器数量应该一致");

            // 验证最终值（应用修饰器后）
            float deserializedValue = deserializedProperty.GetValue();
            Assert.AreEqual(originalValue, deserializedValue,
                $"最终值应该一致: 原始={originalValue}, 反序列化={deserializedValue}");
        }

        /// <summary>
        ///     测试空修饰器列表的序列化
        /// </summary>
        [Test]
        public void Test_GameProperty_EmptyModifiers_Serialization()
        {
            var emptyProperty = new GameProperty("EmptyProp", 50f);
            _manager.Register(emptyProperty, "Test.Serialization");

            Assert.GreaterOrEqual(emptyProperty.UID, 0, "注册后应为属性分配 UID");

            // 获取序列化服务
            ISerializationService serializationService = EasyPackArchitecture.Instance
                .ResolveAsync<ISerializationService>().GetAwaiter().GetResult();

            // 序列化和反序列化
            string json = serializationService.SerializeToJson(emptyProperty);
            var deserializedEmpty = serializationService.DeserializeFromJson<GameProperty>(json);

            Assert.AreEqual(0, deserializedEmpty.ModifierCount, "空修饰器列表应该保持为空");
            Assert.AreEqual(50f, deserializedEmpty.GetValue(), "空修饰器的值应该等于基础值");
        }

        /// <summary>
        ///     测试带依赖关系的 GameProperty 序列化
        /// </summary>
        [Test]
        public void Test_GameProperty_WithDependencies_Serialization()
        {
            // 创建源属性和依赖属性
            var sourceProperty = new GameProperty("Source", 100f);
            var dependentProperty = new GameProperty("Dependent", 0f);

            // 添加依赖关系：Dependent = Source * 2
            dependentProperty.AddDependency(sourceProperty, (dep, newVal) => { return newVal * 2f; });

            // 注册到管理器
            _manager.Register(sourceProperty, "Test.Dependencies");
            _manager.Register(dependentProperty, "Test.Dependencies");

            Assert.GreaterOrEqual(sourceProperty.UID, 0, "注册后应为属性分配 UID");
            Assert.GreaterOrEqual(dependentProperty.UID, 0, "注册后应为属性分配 UID");

            // 获取序列化服务
            ISerializationService serializationService = EasyPackArchitecture.Instance
                .ResolveAsync<ISerializationService>().GetAwaiter().GetResult();

            // 分别序列化两个属性
            string sourceJson = serializationService.SerializeToJson(sourceProperty);
            string dependentJson = serializationService.SerializeToJson(dependentProperty);

            // 反序列化
            var deserializedSource = serializationService.DeserializeFromJson<GameProperty>(sourceJson);
            var deserializedDependent = serializationService.DeserializeFromJson<GameProperty>(dependentJson);

            Assert.AreEqual(sourceProperty.ID, deserializedSource.ID, "源属性ID应该一致");
            Assert.AreEqual(dependentProperty.ID, deserializedDependent.ID, "依赖属性ID应该一致");
            Assert.AreEqual(100f, deserializedSource.GetValue(), "源属性值应该一致");
        }

        /// <summary>
        ///     测试多个修饰器的复杂序列化
        /// </summary>
        [Test]
        public void Test_GameProperty_ComplexModifiers_Serialization()
        {
            var complexProperty = new GameProperty("Complex", 100f);

            // 添加多种类型的修饰器
            complexProperty.AddModifier(new FloatModifier(ModifierType.Add, 1, 10f));
            complexProperty.AddModifier(new FloatModifier(ModifierType.Add, 1, 20f));
            complexProperty.AddModifier(new FloatModifier(ModifierType.Mul, 2, 1.5f));
            complexProperty.AddModifier(new FloatModifier(ModifierType.PriorityAdd, 3, 5f));
            complexProperty.AddModifier(new RangeModifier(ModifierType.Clamp, 999, new(50f, 300f)));

            _manager.Register(complexProperty, "Test.Complex");

            // 获取序列化服务
            ISerializationService serializationService = EasyPackArchitecture.Instance
                .ResolveAsync<ISerializationService>().GetAwaiter().GetResult();

            // 计算原始值
            float originalValue = complexProperty.GetValue();

            // 序列化和反序列化
            string json = serializationService.SerializeToJson(complexProperty);
            var deserializedProperty = serializationService.DeserializeFromJson<GameProperty>(json);

            // 验证所有修饰器都被正确反序列化
            Assert.AreEqual(5, deserializedProperty.ModifierCount, "应该有5个修饰器");
            Assert.AreEqual(originalValue, deserializedProperty.GetValue(), "复杂修饰器计算结果应该一致");
        }

        /// <summary>
        ///     测试通过元数据序列化属性
        /// </summary>
        [Test]
        public void Test_GameProperty_WithMetadata_Serialization()
        {
            var property = new GameProperty("WithMetadata", 75f);
            var metadata = new PropertyDisplayInfo { DisplayName = "测试属性" };

            string[] tags = new[] { "test", "serialization", "metadata" };
            var customData = new CustomData.CustomDataCollection();
            customData.Set("testKey", "testValue");
            customData.Set("testNumber", 42);

            _manager.Register(property, "Test.Metadata", metadata, tags, customData);

            // 获取元数据并验证
            PropertyDisplayInfo retrievedMetadata = _manager.GetPropertyDisplayInfo(property.ID);
            Assert.IsNotNull(retrievedMetadata, "元数据应该存在");
            Assert.AreEqual("测试属性", retrievedMetadata.DisplayName, "DisplayName应该一致");

            // 验证标签和自定义数据
            Assert.IsTrue(_manager.HasTag(property.ID, "test"));
            Assert.AreEqual("testValue", _manager.GetCustomData(property.ID).Get<string>("testKey", null),
                "自定义字符串数据应该一致");
            Assert.AreEqual(42, _manager.GetCustomData(property.ID).Get<int>("testNumber", 0), "自定义数字数据应该一致");
        }

        /// <summary>
        ///     测试使用 ISerializationService 的正确性
        /// </summary>
        [Test]
        public void Test_SerializationService_Resolution()
        {
            // 验证序列化服务可以通过架构正确解析
            ISerializationService serializationService = EasyPackArchitecture.Instance
                .ResolveAsync<ISerializationService>().GetAwaiter().GetResult();
            Assert.IsNotNull(serializationService, "序列化服务应该能够正确解析");

            // 创建并序列化一个简单属性
            var simpleProperty = new GameProperty("Simple", 42f);
            string json = serializationService.SerializeToJson(simpleProperty);
            var deserialized = serializationService.DeserializeFromJson<GameProperty>(json);

            Assert.AreEqual(42f, deserialized.GetValue(), "通过序列化服务的基本操作应该正确");
        }
    }
}