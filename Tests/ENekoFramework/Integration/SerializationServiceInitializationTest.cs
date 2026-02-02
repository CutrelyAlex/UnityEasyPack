using NUnit.Framework;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.TestTools;
using EasyPack.Architecture;
using EasyPack.Serialization;
using EasyPack.GamePropertySystem;

namespace EasyPack.ENekoFrameworkTest
{
    /// <summary>
    /// 序列化服务初始化集成测试
    /// 
    /// 测试目标：
    /// =========
    /// 验证 SerializationService 能够正确集成到 EasyPackArchitecture 中，包括：
    /// 1. 服务注册 - 通过 Container.ResolveAsync 获取服务实例
    /// 2. 生命周期 - 服务的初始化和序列化器注册
    /// 3. 基本功能 - 验证注册的序列化器可用
    /// 
    /// 测试场景：
    /// ========
    /// • 架构初始化后能够解析 ISerializationService
    /// • 服务初始化后已注册所有系统的序列化器
    /// • 基本的序列化/反序列化功能可用
    /// </summary>
    [TestFixture]
    public class SerializationServiceInitializationTest
    {
        private EasyPackArchitecture _architecture;

        [SetUp]
        public void Setup()
        {
            // 重置架构实例以确保测试隔离
            EasyPackArchitecture.ResetInstance();
            _architecture = EasyPackArchitecture.Instance;
        }

        [TearDown]
        public void TearDown()
        {
            // 清理架构实例
            EasyPackArchitecture.ResetInstance();
        }

        /// <summary>
        /// 测试：架构初始化后可以解析序列化服务
        /// </summary>
        [Test]
        public async Task Test_CanResolveSerializationService_AfterArchitectureInit()
        {
            // Act: 从容器解析序列化服务
            var service = await _architecture.ResolveAsync<ISerializationService>();

            // Assert: 服务应该成功解析
            Assert.IsNotNull(service, "序列化服务应该成功从容器中解析");
            Assert.IsInstanceOf<SerializationService>(service, "解析的服务应该是 SerializationService 类型");
        }

        /// <summary>
        /// 测试：序列化服务初始化后已注册序列化器
        /// </summary>
        [Test]
        public async Task Test_SerializationService_HasRegisteredSerializers_AfterInit()
        {
            // Arrange: 解析服务
            var service = await _architecture.ResolveAsync<ISerializationService>();

            // Act: 获取已注册的序列化器类型列表
            var registeredTypes = service.GetRegisteredTypes();

            // Assert: 应该有序列化器被注册
            Assert.IsNotNull(registeredTypes, "已注册序列化器类型列表不应为 null");
            Assert.Greater(registeredTypes.Count, 0, "应该至少注册了一些序列化器");

            Debug.Log($"[SerializationServiceInitializationTest] 已注册 {registeredTypes.Count} 个序列化器");
            foreach (var type in registeredTypes)
            {
                Debug.Log($"  - {type.Name}");
            }
        }

        /// <summary>
        /// 测试：验证 GameProperty 系统的序列化器已注册
        /// </summary>
        [Test]
        public async Task Test_GamePropertySerializers_AreRegistered()
        {
            // Arrange: 先解析 GamePropertyManager 以触发序列化器注册
            var managerTask = _architecture.ResolveAsync<IGamePropertyService>();
            await managerTask;

            // 然后解析 SerializationService
            var service = await _architecture.ResolveAsync<ISerializationService>();

            // Assert: 验证 GameProperty 相关的序列化器
            Assert.IsTrue(
                service.HasSerializer<GamePropertySystem.GameProperty>(),
                "应该注册了 GameProperty 序列化器"
            );

            Debug.Log("[SerializationServiceInitializationTest] GameProperty 序列化器已正确注册");
        }

        /// <summary>
        /// 测试：验证 Inventory 系统的序列化器已注册
        /// </summary>
        [Test]
        public async Task Test_InventorySerializers_AreRegistered()
        {
            // Arrange: 初始化 InventoryService（会注册序列化器）
            var inventoryService = await EasyPackArchitecture.GetInventoryServiceAsync();
            var inventoryInitTask = inventoryService.InitializeAsync();
            await inventoryInitTask;

            // 检查初始化是否成功
            if (inventoryInitTask.IsFaulted)
            {
                Debug.LogError($"InventoryService 初始化失败: {inventoryInitTask.Exception}");
                Assert.Fail("InventoryService 初始化失败");
                return;
            }

            // 解析序列化服务
            var service = await _architecture.ResolveAsync<ISerializationService>();

            // 检查解析是否成功
            if (service == null)
            {
                Debug.LogError("序列化服务解析失败");
                Assert.Fail("序列化服务解析失败");
                return;
            }

            Assert.IsNotNull(service, "序列化服务不应为 null");

            // Assert: 验证 Inventory 相关的序列化器
            Assert.IsTrue(
                service.HasSerializer<InventorySystem.Item>(),
                "应该注册了 Item 序列化器"
            );

            Assert.IsTrue(
                service.HasSerializer<InventorySystem.Container>(),
                "应该注册了 Container 序列化器"
            );

            Debug.Log("[SerializationServiceInitializationTest] Inventory 序列化器已正确注册");
        }

        /// <summary>
        /// 测试：验证 Card 系统的序列化器已注册（新接口实现）
        /// </summary>
        [Test]
        public async Task Test_CardSerializer_IsRegistered_WithNewInterface()
        {
            // Arrange: 解析服务
            var service = await _architecture.ResolveAsync<ISerializationService>();

            // Assert: 验证 Card 序列化器已注册
            Assert.IsTrue(
                service.HasSerializer<EmeCardSystem.Card>(),
                "应该注册了 Card 序列化器（新的双泛型接口实现）"
            );

            Debug.Log("[SerializationServiceInitializationTest] Card 序列化器（新接口）已正确注册");
        }

        /// <summary>
        /// 测试：验证序列化服务的基本序列化功能
        /// </summary>
        [Test]
        public async Task Test_BasicSerialization_WorksCorrectly()
        {
            // Arrange: 解析服务和创建测试对象
            var service = await _architecture.ResolveAsync<ISerializationService>();

            // 创建一个简单的 Card 对象用于测试
            var cardData = new EmeCardSystem.CardData(
                id: "test_card",
                name: "测试卡牌",
                desc: "这是一个测试卡牌",
                category: "Category.Object",
                defaultTags: new[] { "test" },
                sprite: null
            );
            var testCard = new EmeCardSystem.Card(cardData);

            // Act: 序列化
            string json = service.SerializeToJson(testCard);

            // Assert: 验证序列化结果
            Assert.IsNotNull(json, "序列化结果不应为 null");
            Assert.IsNotEmpty(json, "序列化结果不应为空字符串");
            Assert.IsTrue(json.Contains("test_card"), "JSON 应包含卡牌 ID");
            Assert.IsTrue(json.Contains("测试卡牌"), "JSON 应包含卡牌名称");

            Debug.Log($"[SerializationServiceInitializationTest] 序列化成功，JSON 长度: {json.Length}");
            Debug.Log($"JSON 内容: {json}");
        }

        /// <summary>
        /// 测试：验证序列化服务的基本反序列化功能
        /// </summary>
        [Test]
        public async Task Test_BasicDeserialization_WorksCorrectly()
        {
            // Arrange: 解析服务和准备测试数据
            var service = await _architecture.ResolveAsync<ISerializationService>();

            // 创建测试卡牌并序列化
            var cardData = new EmeCardSystem.CardData(
                id: "test_card_deser",
                name: "反序列化测试卡牌",
                desc: "用于测试反序列化",
                category: "Category.Object",
                defaultTags: new[] { "test", "deser" },
                sprite: null
            );
            var originalCard = new EmeCardSystem.Card(cardData);
            string json = service.SerializeToJson(originalCard);

            // Act: 反序列化
            var deserializedCard = service.DeserializeFromJson<EmeCardSystem.Card>(json);

            // Assert: 验证反序列化结果
            Assert.IsNotNull(deserializedCard, "反序列化结果不应为 null");
            Assert.AreEqual(originalCard.Id, deserializedCard.Id, "卡牌 ID 应该相同");
            Assert.AreEqual(originalCard.Name, deserializedCard.Name, "卡牌名称应该相同");
            Assert.AreEqual(originalCard.Description, deserializedCard.Description, "卡牌描述应该相同");
            Assert.AreEqual(originalCard.Category, deserializedCard.Category, "卡牌分类应该相同");

            Debug.Log($"[SerializationServiceInitializationTest] 反序列化成功，卡牌: {deserializedCard.Name}");
        }
    }
}
