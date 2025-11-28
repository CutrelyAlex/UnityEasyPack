using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace EasyPack.ENekoFramework
{
    /// <summary>
    ///     UI绑定扩展方法集合。
    ///     提供便捷的数据绑定方法，将BindableProperty自动绑定到Unity UI组件。
    /// </summary>
    public static class UIBindingExtensions
    {
        #region Text组件绑定

        /// <summary>
        ///     将Text组件绑定到字符串属性。
        ///     当属性变更时自动更新Text的text字段。
        /// </summary>
        /// <param name="text">Text组件</param>
        /// <param name="property">要绑定的属性</param>
        public static void BindTo(this Text text, BindableProperty<string> property)
        {
            if (text == null || property == null)
                return;

            // 立即设置初始值
            text.text = property.Value;

            // 注册变更监听
            Action<string> handler = value => text.text = value;
            property.OnValueChanged += handler;

            // 注册清理回调
            DataBindingEngine.Instance.RegisterBinding(text.gameObject, () => { property.OnValueChanged -= handler; });
        }

        /// <summary>
        ///     将Text组件绑定到整数属性，支持格式化。
        /// </summary>
        /// <param name="text">Text组件</param>
        /// <param name="property">要绑定的属性</param>
        /// <param name="formatter">格式化函数，可选</param>
        public static void BindTo(this Text text, BindableProperty<int> property, Func<int, string> formatter = null)
        {
            if (text == null || property == null)
                return;

            formatter = formatter ?? (value => value.ToString());

            // 立即设置初始值
            text.text = formatter(property.Value);

            // 注册变更监听
            Action<int> handler = value => text.text = formatter(value);
            property.OnValueChanged += handler;

            // 注册清理回调
            DataBindingEngine.Instance.RegisterBinding(text.gameObject, () => { property.OnValueChanged -= handler; });
        }

        /// <summary>
        ///     将Text组件绑定到浮点数属性，支持格式化。
        /// </summary>
        /// <param name="text">Text组件</param>
        /// <param name="property">要绑定的属性</param>
        /// <param name="formatter">格式化函数，可选</param>
        public static void BindTo(this Text text, BindableProperty<float> property,
                                  Func<float, string> formatter = null)
        {
            if (text == null || property == null)
                return;

            formatter = formatter ?? (value => value.ToString("F2"));

            text.text = formatter(property.Value);

            Action<float> handler = value => text.text = formatter(value);
            property.OnValueChanged += handler;

            DataBindingEngine.Instance.RegisterBinding(text.gameObject, () => { property.OnValueChanged -= handler; });
        }

        #endregion

        #region Slider组件绑定

        /// <summary>
        ///     将Slider组件绑定到整数属性（单向绑定）。
        /// </summary>
        /// <param name="slider">Slider组件</param>
        /// <param name="property">要绑定的属性</param>
        public static void BindTo(this Slider slider, BindableProperty<int> property)
        {
            if (slider == null || property == null)
                return;

            slider.value = property.Value;

            Action<int> handler = value => slider.value = value;
            property.OnValueChanged += handler;

            DataBindingEngine.Instance.RegisterBinding(slider.gameObject,
                () => { property.OnValueChanged -= handler; });
        }

        /// <summary>
        ///     将Slider组件绑定到浮点数属性（单向绑定）。
        /// </summary>
        /// <param name="slider">Slider组件</param>
        /// <param name="property">要绑定的属性</param>
        public static void BindTo(this Slider slider, BindableProperty<float> property)
        {
            if (slider == null || property == null)
                return;

            slider.value = property.Value;

            Action<float> handler = value => slider.value = value;
            property.OnValueChanged += handler;

            DataBindingEngine.Instance.RegisterBinding(slider.gameObject,
                () => { property.OnValueChanged -= handler; });
        }

        /// <summary>
        ///     将Slider组件与整数属性双向绑定。
        /// </summary>
        /// <param name="slider">Slider组件</param>
        /// <param name="property">要绑定的属性</param>
        public static void BindBidirectional(this Slider slider, BindableProperty<int> property)
        {
            if (slider == null || property == null)
                return;

            slider.value = property.Value;

            // 属性 -> UI
            Action<int> propertyHandler = value => slider.value = value;
            property.OnValueChanged += propertyHandler;

            // UI -> 属性
            UnityAction<float> sliderHandler = value => property.Value = (int)value;
            slider.onValueChanged.AddListener(sliderHandler);

            DataBindingEngine.Instance.RegisterBinding(slider.gameObject, () =>
            {
                property.OnValueChanged -= propertyHandler;
                slider.onValueChanged.RemoveListener(sliderHandler);
            });
        }

        #endregion

        #region Image组件绑定

        /// <summary>
        ///     将Image组件的颜色绑定到颜色属性。
        /// </summary>
        /// <param name="image">Image组件</param>
        /// <param name="property">要绑定的属性</param>
        public static void BindColorTo(this Image image, BindableProperty<Color> property)
        {
            if (image == null || property == null)
                return;

            image.color = property.Value;

            Action<Color> handler = value => image.color = value;
            property.OnValueChanged += handler;

            DataBindingEngine.Instance.RegisterBinding(image.gameObject, () => { property.OnValueChanged -= handler; });
        }

        /// <summary>
        ///     将Image组件的Sprite绑定到Sprite属性。
        /// </summary>
        /// <param name="image">Image组件</param>
        /// <param name="property">要绑定的属性</param>
        public static void BindSpriteTo(this Image image, BindableProperty<Sprite> property)
        {
            if (image == null || property == null)
                return;

            image.sprite = property.Value;

            Action<Sprite> handler = value => image.sprite = value;
            property.OnValueChanged += handler;

            DataBindingEngine.Instance.RegisterBinding(image.gameObject, () => { property.OnValueChanged -= handler; });
        }

        #endregion

        #region GameObject绑定

        /// <summary>
        ///     将GameObject的激活状态绑定到布尔属性。
        /// </summary>
        /// <param name="gameObject">GameObject</param>
        /// <param name="property">要绑定的属性</param>
        public static void BindActiveTo(this GameObject gameObject, BindableProperty<bool> property)
        {
            if (gameObject == null || property == null)
                return;

            gameObject.SetActive(property.Value);

            Action<bool> handler = value => gameObject.SetActive(value);
            property.OnValueChanged += handler;

            DataBindingEngine.Instance.RegisterBinding(gameObject, () => { property.OnValueChanged -= handler; });
        }

        #endregion

        #region Toggle组件绑定

        /// <summary>
        ///     将Toggle组件与布尔属性双向绑定。
        /// </summary>
        /// <param name="toggle">Toggle组件</param>
        /// <param name="property">要绑定的属性</param>
        public static void BindBidirectional(this Toggle toggle, BindableProperty<bool> property)
        {
            if (toggle == null || property == null)
                return;

            toggle.isOn = property.Value;

            // 属性 -> UI
            Action<bool> propertyHandler = value => toggle.isOn = value;
            property.OnValueChanged += propertyHandler;

            // UI -> 属性
            UnityAction<bool> toggleHandler = value => property.Value = value;
            toggle.onValueChanged.AddListener(toggleHandler);

            DataBindingEngine.Instance.RegisterBinding(toggle.gameObject, () =>
            {
                property.OnValueChanged -= propertyHandler;
                toggle.onValueChanged.RemoveListener(toggleHandler);
            });
        }

        #endregion

        #region InputField组件绑定

        /// <summary>
        ///     将InputField组件与字符串属性双向绑定。
        /// </summary>
        /// <param name="inputField">InputField组件</param>
        /// <param name="property">要绑定的属性</param>
        public static void BindBidirectional(this InputField inputField, BindableProperty<string> property)
        {
            if (inputField == null || property == null)
                return;

            inputField.text = property.Value;

            // 属性 -> UI
            Action<string> propertyHandler = value => inputField.text = value;
            property.OnValueChanged += propertyHandler;

            // UI -> 属性
            UnityAction<string> inputHandler = value => property.Value = value;
            inputField.onValueChanged.AddListener(inputHandler);

            DataBindingEngine.Instance.RegisterBinding(inputField.gameObject, () =>
            {
                property.OnValueChanged -= propertyHandler;
                inputField.onValueChanged.RemoveListener(inputHandler);
            });
        }

        #endregion
    }
}