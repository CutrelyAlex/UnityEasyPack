using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using EasyPack.ENekoFramework;

namespace EasyPack.ENekoFrameworkTest.Integration
{
    /// <summary>
    ///     测试UI元素与BindableProperty的数据绑定功能。
    ///     验证当绑定属性变更时，UI元素是否自动更新。
    /// </summary>
    [TestFixture]
    public class UIBindingIntegrationTests
    {
        private GameObject _testObject;
        private BindableProperty<string> _textProperty;
        private BindableProperty<int> _healthProperty;

        [SetUp]
        public void Setup()
        {
            _testObject = new("TestObject");
            _textProperty = new("Initial");
            _healthProperty = new(100);
        }

        [TearDown]
        public void TearDown()
        {
            if (_testObject != null) Object.DestroyImmediate(_testObject);
            _textProperty = null;
            _healthProperty = null;
        }

        [Test]
        public void BindToText_PropertyChanged_TextUpdated()
        {
            // Arrange
            var textComponent = _testObject.AddComponent<Text>();
            textComponent.BindTo(_textProperty);

            // Act
            _textProperty.Value = "Updated Text";

            // Assert
            Assert.AreEqual("Updated Text", textComponent.text, "Text组件应该自动更新");
        }

        [Test]
        public void BindToSlider_PropertyChanged_SliderValueUpdated()
        {
            // Arrange
            var slider = _testObject.AddComponent<Slider>();
            slider.minValue = 0;
            slider.maxValue = 100;
            slider.BindTo(_healthProperty);

            // Act
            _healthProperty.Value = 75;

            // Assert
            Assert.AreEqual(75, slider.value, "Slider值应该自动更新");
        }

        [Test]
        public void BindToImage_ColorProperty_ImageColorUpdated()
        {
            // Arrange
            var image = _testObject.AddComponent<Image>();
            var colorProperty = new BindableProperty<Color>(Color.white);
            image.BindColorTo(colorProperty);

            // Act
            colorProperty.Value = Color.red;

            // Assert
            Assert.AreEqual(Color.red, image.color, "Image颜色应该自动更新");
        }

        [Test]
        public void BindToGameObject_ActiveProperty_GameObjectActiveUpdated()
        {
            // Arrange
            var activeProperty = new BindableProperty<bool>(true);
            _testObject.BindActiveTo(activeProperty);

            // Act
            activeProperty.Value = false;

            // Assert
            Assert.IsFalse(_testObject.activeSelf, "GameObject激活状态应该自动更新");

            // Act
            activeProperty.Value = true;

            // Assert
            Assert.IsTrue(_testObject.activeSelf, "GameObject激活状态应该自动更新");
        }

        [Test]
        public void UnbindOnDestroy_PropertyChanged_NoException()
        {
            // Arrange
            var textComponent = _testObject.AddComponent<Text>();
            textComponent.BindTo(_textProperty);

            // Act
            Object.DestroyImmediate(_testObject);
            _testObject = null;

            // Assert - 不应该抛出异常
            Assert.DoesNotThrow(() => _textProperty.Value = "After Destroy",
                "GameObject销毁后属性变更不应抛出异常");
        }

        [Test]
        public void MultipleBindings_DifferentComponents_AllUpdate()
        {
            // Arrange
            var text1 = _testObject.AddComponent<Text>();
            var text2Object = new GameObject("Text2");
            var text2 = text2Object.AddComponent<Text>();

            text1.BindTo(_textProperty);
            text2.BindTo(_textProperty);

            // Act
            _textProperty.Value = "Shared Value";

            // Assert
            Assert.AreEqual("Shared Value", text1.text, "Text1应该更新");
            Assert.AreEqual("Shared Value", text2.text, "Text2应该更新");

            // Cleanup
            Object.DestroyImmediate(text2Object);
        }

        [Test]
        public void BindWithFormatter_PropertyChanged_FormattedTextDisplayed()
        {
            // Arrange
            var textComponent = _testObject.AddComponent<Text>();
            textComponent.BindTo(_healthProperty, value => $"HP: {value}/100");

            // Act
            _healthProperty.Value = 50;

            // Assert
            Assert.AreEqual("HP: 50/100", textComponent.text, "应该显示格式化后的文本");
        }

        [Test]
        public void BindBidirectional_SliderChanged_PropertyUpdated()
        {
            // Arrange
            var slider = _testObject.AddComponent<Slider>();
            slider.minValue = 0;
            slider.maxValue = 100;
            slider.BindBidirectional(_healthProperty);

            // Act
            slider.value = 80;

            // Assert
            Assert.AreEqual(80, _healthProperty.Value, "属性应该反向更新");
        }

        [Test]
        public void BatchUpdate_MultipleBindings_OnlyOneUIUpdate()
        {
            // Arrange
            var textComponent = _testObject.AddComponent<Text>();
            int updateCount = 0;

            // 使用自定义绑定追踪更新次数
            _textProperty.OnValueChanged += _ =>
            {
                updateCount++;
                textComponent.text = _textProperty.Value;
            };

            _textProperty.BeginBatch();

            // Act
            _textProperty.Value = "Update1";
            _textProperty.Value = "Update2";
            _textProperty.Value = "Final";
            _textProperty.EndBatch();

            // Assert
            Assert.AreEqual(1, updateCount, "批量更新应该只触发一次UI更新");
            Assert.AreEqual("Final", textComponent.text, "显示最终值");
        }
    }
}