using System;

namespace EasyPack
{
    /// <summary>
    /// ���ԱȽ�����ö��
    /// </summary>
    public enum AttributeComparisonType
    {
        /// <summary>
        /// ֵ��ȱȽ�
        /// </summary>
        Equal,

        /// <summary>
        /// ֵ���ȱȽ�
        /// </summary>
        NotEqual,

        /// <summary>
        /// ���ڱȽ�(������������Ч)
        /// </summary>
        GreaterThan,

        /// <summary>
        /// С�ڱȽ�(������������Ч)
        /// </summary>
        LessThan,

        /// <summary>
        /// ���ڵ��ڱȽ�(������������Ч)
        /// </summary>
        GreaterThanOrEqual,

        /// <summary>
        /// С�ڵ��ڱȽ�(������������Ч)
        /// </summary>
        LessThanOrEqual,

        /// <summary>
        /// �����Ƚ�(���ַ����ͼ���������Ч)
        /// </summary>
        Contains,

        /// <summary>
        /// �������Ƚ�(���ַ����ͼ���������Ч)
        /// </summary>
        NotContains,

        /// <summary>
        /// ���ж��Ƿ���ڴ�����
        /// </summary>
        Exists
    }

    /// <summary>
    /// ������������࣬�ɸ�����Ʒ�����Խ��������ж�
    /// </summary>
    public class AttributeCondition : IItemCondition
    {
        /// <summary>
        /// Ҫ������������
        /// </summary>
        public string AttributeName { get; set; }

        /// <summary>
        /// ���ԱȽϵ�Ŀ��ֵ
        /// </summary>
        public object AttributeValue { get; set; }

        /// <summary>
        /// ���ԱȽ�����
        /// </summary>
        public AttributeComparisonType ComparisonType { get; set; }

        /// <summary>
        /// ����һ������������Ĭ��ʹ����ȱȽ�
        /// </summary>
        /// <param name="attributeName">��������</param>
        /// <param name="requiredValue">�Ƚϵ�Ŀ��ֵ</param>
        public AttributeCondition(string attributeName, object requiredValue)
        {
            AttributeName = attributeName;
            AttributeValue = requiredValue;
            ComparisonType = AttributeComparisonType.Equal;
        }

        /// <summary>
        /// ����һ������������ָ���Ƚ�����
        /// </summary>
        /// <param name="attributeName">��������</param>
        /// <param name="requiredValue">�Ƚϵ�Ŀ��ֵ</param>
        /// <param name="comparisonType">�Ƚ�����</param>
        public AttributeCondition(string attributeName, object requiredValue, AttributeComparisonType comparisonType)
        {
            AttributeName = attributeName;
            AttributeValue = requiredValue;
            ComparisonType = comparisonType;
        }

        /// <summary>
        /// ������������
        /// </summary>
        /// <param name="attributeName">��������</param>
        /// <param name="requiredValue">�Ƚϵ�Ŀ��ֵ</param>
        public void SetAttribute(string attributeName, object requiredValue)
        {
            AttributeName = attributeName;
            AttributeValue = requiredValue;
        }

        /// <summary>
        /// �������������ͱȽ�����
        /// </summary>
        /// <param name="attributeName">��������</param>
        /// <param name="requiredValue">�Ƚϵ�Ŀ��ֵ</param>
        /// <param name="comparisonType">�Ƚ�����</param>
        public void SetAttribute(string attributeName, object requiredValue, AttributeComparisonType comparisonType)
        {
            AttributeName = attributeName;
            AttributeValue = requiredValue;
            ComparisonType = comparisonType;
        }

        /// <summary>
        /// �ж���Ʒ�Ƿ�������������
        /// </summary>
        /// <param name="item">Ҫ������Ʒ</param>
        /// <returns>���������������true�����򷵻�false</returns>
        public bool IsCondition(IItem item)
        {
            // �����Ʒ�Ƿ�Ϊ��
            if (item == null || item.Attributes == null)
                return false;

            // �����������Դ�����
            if (ComparisonType == AttributeComparisonType.Exists)
                return item.Attributes.ContainsKey(AttributeName);

            // ���Ի�ȡ����ֵ
            if (!item.Attributes.TryGetValue(AttributeName, out var actualValue))
                return false;

            // �������ֵΪ�յ��Ƚ�ֵ��Ϊ��
            if (actualValue == null)
                return AttributeValue == null;

            // ���ݱȽ����ͽ��бȽ�
            switch (ComparisonType)
            {
                case AttributeComparisonType.Equal:
                    return actualValue.Equals(AttributeValue);

                case AttributeComparisonType.NotEqual:
                    return !actualValue.Equals(AttributeValue);

                case AttributeComparisonType.GreaterThan:
                    return CompareNumeric(actualValue, AttributeValue) > 0;

                case AttributeComparisonType.LessThan:
                    return CompareNumeric(actualValue, AttributeValue) < 0;

                case AttributeComparisonType.GreaterThanOrEqual:
                    return CompareNumeric(actualValue, AttributeValue) >= 0;

                case AttributeComparisonType.LessThanOrEqual:
                    return CompareNumeric(actualValue, AttributeValue) <= 0;

                case AttributeComparisonType.Contains:
                    return CompareContains(actualValue, AttributeValue);

                case AttributeComparisonType.NotContains:
                    return !CompareContains(actualValue, AttributeValue);

                default:
                    return false;
            }
        }

        /// <summary>
        /// �Ƚ�������ֵ
        /// </summary>
        private int CompareNumeric(object value1, object value2)
        {
            if (value1 == null || value2 == null)
                return 0;

            // ���Խ�ֵת��Ϊ�ɱȽϵ���ֵ����
            if (value1 is IComparable comparable1 && value2 is IComparable comparable2)
            {
                try
                {
                    // ���������ͬ��ֱ�ӱȽ�
                    if (value1.GetType() == value2.GetType())
                        return comparable1.CompareTo(comparable2);

                    // ���Խ�ֵת��Ϊdecimal���бȽ�
                    if (decimal.TryParse(value1.ToString(), out decimal num1) &&
                        decimal.TryParse(value2.ToString(), out decimal num2))
                        return num1.CompareTo(num2);

                    // ���Խ�ֵת��Ϊdouble���бȽ�
                    if (double.TryParse(value1.ToString(), out double d1) &&
                        double.TryParse(value2.ToString(), out double d2))
                        return d1.CompareTo(d2);
                }
                catch
                {
                    // �޷��Ƚϣ�����0
                    return 0;
                }
            }

            // �޷��Ƚϣ�����0
            return 0;
        }

        /// <summary>
        /// ���һ��ֵ�Ƿ������һ��ֵ
        /// </summary>
        private bool CompareContains(object container, object value)
        {
            // �����һֵΪnull���޷�ִ�а����Ƚ�
            if (container == null || value == null)
                return false;

            // �ַ��������Ƚ�
            if (container is string containerStr && value is string valueStr)
                return containerStr.Contains(valueStr);

            // ���ϰ����Ƚ�
            if (container is System.Collections.IEnumerable enumerable && !(container is string))
            {
                foreach (var item in enumerable)
                {
                    if (item.Equals(value))
                        return true;
                }
            }

            return false;
        }
    }
}