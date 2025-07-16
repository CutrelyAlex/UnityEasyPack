/// <summary>
/// CombinePropertySingle 实现了 ICombineGameProperty，
/// 仅包含单一 GameProperty，直接返回该属性的值作为最终结果。
/// 适用于无需属性组合，仅需单属性表现的简单场景。
/// </summary>

using System;
namespace EasyPack
{
    public class CombinePropertySingle : ICombineGameProperty
    {
        public string ID { get; }
        public Func<ICombineGameProperty, float> Calculater { get; }

        private readonly GameProperty _resultHolder;
        public GameProperty ResultHolder => _resultHolder;

        private bool _isDisposed = false;
        private readonly float _baseCombineValue;

        public float GetValue()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(CombinePropertySingle));

            return Calculater(this);
        }

        public CombinePropertySingle(string id, float baseValue = 0)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("ID cannot be null or empty", nameof(id));

            ID = id;
            _baseCombineValue = baseValue;
            _resultHolder = new GameProperty(id + "@ResultHolder", baseValue);
            Calculater = e => ResultHolder.GetValue();
        }

        /// <summary>
        /// 获取基础组合值
        /// </summary>
        public float GetBaseValue() => _baseCombineValue;

        /// <summary>
        /// 获取内部属性，Single类型只返回ResultHolder
        /// </summary>
        public GameProperty GetProperty(string id = "")
        {
            if (_isDisposed) return null;
            return ResultHolder;
        }

        /// <summary>
        /// 检查对象是否有效
        /// </summary>
        public bool IsValid() => !_isDisposed && _resultHolder != null;

        /// <summary>
        /// 获取ResultHolder的引用，用于外部操作
        /// </summary>
        public GameProperty GetResultHolder() => _isDisposed ? null : _resultHolder;

        /// <summary>
        /// 设置ResultHolder的基础值
        /// </summary>
        public void SetBaseValue(float value)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(CombinePropertySingle));

            _resultHolder.SetBaseValue(value);
        }
    
        /// <summary>
        /// 资源清理
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed) return;

            // 清理ResultHolder的修饰器和依赖
            _resultHolder?.ClearModifiers();

            // 如果GameProperty实现了IDisposable，也应该调用
            if (_resultHolder is IDisposable disposableProperty)
            {
                disposableProperty.Dispose();
            }

            _isDisposed = true;
        }

        /// <summary>
        /// 添加修饰器到ResultHolder
        /// </summary>
        public void AddModifier(IModifier modifier)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(CombinePropertySingle));

            if (modifier == null)
                throw new ArgumentNullException(nameof(modifier));

            _resultHolder.AddModifier(modifier);
        }

        /// <summary>
        /// 从ResultHolder移除修饰器
        /// </summary>
        public void RemoveModifier(IModifier modifier)
        {
            if (_isDisposed || modifier == null) return;
            _resultHolder.RemoveModifier(modifier);
        }

        /// <summary>
        /// 清空ResultHolder的所有修饰器
        /// </summary>
        public void ClearModifiers()
        {
            if (_isDisposed) return;
            _resultHolder.ClearModifiers();
        }
    }
}