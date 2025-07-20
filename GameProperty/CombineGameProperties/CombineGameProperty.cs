using System;
using System.Collections.Generic;

namespace EasyPack
{
    /// <summary>
    /// ������ԵĻ���
    /// </summary>
    public abstract class CombineGameProperty : ICombineGameProperty, IDisposable
    {
        #region ��������

        /// <summary>
        /// ���Ե�Ψһ��ʶ��
        /// </summary>
        public string ID { get; protected set; }

        /// <summary>
        /// ���������������ڼ���������Ե�ֵ
        /// </summary>
        public Func<ICombineGameProperty, float> Calculater { get; set; }

        /// <summary>
        /// ��������ߣ��������ռ�����
        /// </summary>
        public GameProperty ResultHolder { get; protected set; }

        /// <summary>
        /// �������ֵ
        /// </summary>
        protected readonly float _baseCombineValue;

        /// <summary>
        /// �Ƿ����ͷ���Դ
        /// </summary>
        protected bool _isDisposed = false;

        #endregion

        #region ���캯��

        /// <summary>
        /// ��ʼ��������Ի���
        /// </summary>
        /// <param name="id">����ID</param>
        /// <param name="baseValue">����ֵ</param>
        protected CombineGameProperty(string id, float baseValue = 0)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("ID cannot be null or empty", nameof(id));

            ID = id;
            _baseCombineValue = baseValue;
            ResultHolder = new GameProperty(id , baseValue);

            // ����Ĭ�ϼ�����
            Calculater = e => _baseCombineValue;
        }

        protected CombineGameProperty(GameProperty gameProperty, float baseValue = 0)
        {
            if (gameProperty == null) return;
            ID = gameProperty.ID;
            _baseCombineValue = baseValue;
            ResultHolder = gameProperty;

            Calculater = e => _baseCombineValue;
        }

        #endregion

        #region ��ȡ

        /// <summary>
        /// ��ȡ�������ֵ
        /// </summary>
        public virtual float GetBaseValue() => _baseCombineValue;

        /// <summary>
        /// ��ȡ����������ֵ
        /// </summary>
        public virtual float GetValue()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().Name);

            return GetCalculatedValue();
        }

        /// <summary>
        /// ��ȡ�ڲ����ԣ�������ʵ�־����߼�
        /// </summary>
        public abstract GameProperty GetProperty(string id);

        /// <summary>
        /// �������Ƿ���Ч
        /// </summary>
        public virtual bool IsValid() => !_isDisposed && ResultHolder != null;

        public virtual Func<ICombineGameProperty, float> GetCalculater()
        {
            return Calculater;
        }

        #endregion

        #region ��������

        /// <summary>
        /// ��ȡ������ֵ����������д��ʵ�ֲ�ͬ�ļ����߼�
        /// </summary>
        protected virtual float GetCalculatedValue()
        {
            var calculatedValue = Calculater?.Invoke(this) ?? _baseCombineValue;
            ResultHolder.SetBaseValue(calculatedValue);
            return ResultHolder.GetValue();
        }

        /// <summary>
        /// ����Ƿ����ͷţ���������׳��쳣
        /// </summary>
        protected void ThrowIfDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().Name);
        }

        #endregion

        #region �¼�֧��

        /// <summary>
        /// ������ֵ�����仯ʱ����
        /// </summary>
        public event Action<float, float> OnValueChanged
        {
            add => ResultHolder.OnValueChanged += value;
            remove => ResultHolder.OnValueChanged -= value;
        }

        #endregion

        #region ������֧��

        /// <summary>
        /// �����������ResultHolder
        /// </summary>
        public virtual void AddModifierToHolder(IModifier modifier)
        {
            ThrowIfDisposed();

            if (modifier == null)
                throw new ArgumentNullException(nameof(modifier));

            ResultHolder.AddModifier(modifier);
        }

        /// <summary>
        /// ��ResultHolder�Ƴ�������
        /// </summary>
        public virtual void RemoveModifierFromHolder(IModifier modifier)
        {
            if (_isDisposed || modifier == null) return;
            ResultHolder.RemoveModifier(modifier);
        }

        /// <summary>
        /// ���ResultHolder������������
        /// </summary>
        public virtual void ClearModifiersFromHolder()
        {
            if (_isDisposed) return;
            ResultHolder.ClearModifiers();
        }

        #endregion

        #region IDisposable ʵ��

        /// <summary>
        /// �ͷ���Դ
        /// </summary>
        public virtual void Dispose()
        {
            if (_isDisposed) return;

            // ��������������߼�
            DisposeCore();

            // ����ResultHolder
            ResultHolder?.ClearModifiers();

            _isDisposed = true;
        }

        /// <summary>
        /// �����߼�
        /// </summary>
        protected virtual void DisposeCore()
        {
            // ���಻��Ҫ����������߼�
        }

        #endregion
    }
}