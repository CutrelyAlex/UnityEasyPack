using NUnit.Framework;
using EasyPack.ENekoFramework;

namespace EasyPack.ENekoFrameworkTest.Unit
{
    /// <summary>
    /// 测试BindableProperty的批量更新功能。
    /// 验证BeginBatch/EndBatch是否正确合并多次属性变更并只触发一次通知。
    /// </summary>
    [TestFixture]
    public class BatchUpdateTest
    {
        private BindableProperty<int> _intProperty;
        private int _changeCount;

        [SetUp]
        public void Setup()
        {
            _intProperty = new BindableProperty<int>(0);
            _changeCount = 0;
            _intProperty.OnValueChanged += _ => _changeCount++;
        }

        [TearDown]
        public void TearDown()
        {
            _intProperty = null;
        }

        [Test]
        public void BeginBatch_MultipleChanges_OnlyOneNotification()
        {
            // Arrange
            _intProperty.BeginBatch();

            // Act
            _intProperty.Value = 1;
            _intProperty.Value = 2;
            _intProperty.Value = 3;
            _intProperty.EndBatch();

            // Assert
            Assert.AreEqual(1, _changeCount, "批量更新应该只触发一次通知");
            Assert.AreEqual(3, _intProperty.Value, "最终值应该是最后一次设置的值");
        }

        [Test]
        public void BeginBatch_NoChange_NoNotification()
        {
            // Arrange
            _intProperty.BeginBatch();

            // Act
            _intProperty.EndBatch();

            // Assert
            Assert.AreEqual(0, _changeCount, "没有实际变更时不应触发通知");
        }

        [Test]
        public void BeginBatch_SameValue_NoNotification()
        {
            // Arrange
            _intProperty.Value = 5;
            _changeCount = 0; // 重置计数

            _intProperty.BeginBatch();

            // Act
            _intProperty.Value = 5;
            _intProperty.Value = 5;
            _intProperty.EndBatch();

            // Assert
            Assert.AreEqual(0, _changeCount, "设置相同值时不应触发通知");
        }

        [Test]
        public void NestedBatch_OnlyOuterBatchTriggersNotification()
        {
            // Arrange
            _intProperty.BeginBatch();
            _intProperty.BeginBatch(); // 嵌套批量更新

            // Act
            _intProperty.Value = 10;
            _intProperty.EndBatch(); // 内层结束
            Assert.AreEqual(0, _changeCount, "内层批量结束时不应触发通知");

            _intProperty.EndBatch(); // 外层结束

            // Assert
            Assert.AreEqual(1, _changeCount, "外层批量结束时应触发通知");
            Assert.AreEqual(10, _intProperty.Value);
        }

        [Test]
        public void BatchUpdate_WithMultipleListeners_AllReceiveNotification()
        {
            // Arrange
            int listener1Count = 0;
            int listener2Count = 0;

            _intProperty.OnValueChanged += _ => listener1Count++;
            _intProperty.OnValueChanged += _ => listener2Count++;

            _intProperty.BeginBatch();

            // Act
            _intProperty.Value = 1;
            _intProperty.Value = 2;
            _intProperty.EndBatch();

            // Assert
            Assert.AreEqual(1, listener1Count, "监听器1应该收到一次通知");
            Assert.AreEqual(1, listener2Count, "监听器2应该收到一次通知");
            Assert.AreEqual(1, _changeCount, "原始监听器也应该收到一次通知");
        }

        [Test]
        public void NormalUpdate_AfterBatch_TriggersNotification()
        {
            // Arrange
            _intProperty.BeginBatch();
            _intProperty.Value = 5;
            _intProperty.EndBatch();
            _changeCount = 0; // 重置计数

            // Act
            _intProperty.Value = 10;

            // Assert
            Assert.AreEqual(1, _changeCount, "批量更新后的普通更新应该正常触发通知");
        }

        [Test]
        public void EndBatch_WithoutBeginBatch_DoesNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => _intProperty.EndBatch(),
                "EndBatch without BeginBatch should not throw");
        }
    }
}
