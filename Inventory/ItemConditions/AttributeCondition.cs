using System;

namespace EasyPack
{
    /// <summary>
    /// 属性比较类型枚举
    /// </summary>
    public enum AttributeComparisonType
    {
        /// <summary>
        /// 值相等比较
        /// </summary>
        Equal,

        /// <summary>
        /// 值不等比较
        /// </summary>
        NotEqual,

        /// <summary>
        /// 大于比较(仅对数值类型有效)
        /// </summary>
        GreaterThan,

        /// <summary>
        /// 小于比较(仅对数值类型有效)
        /// </summary>
        LessThan,

        /// <summary>
        /// 大于等于比较(仅对数值类型有效)
        /// </summary>
        GreaterThanOrEqual,

        /// <summary>
        /// 小于等于比较(仅对数值类型有效)
        /// </summary>
        LessThanOrEqual,

        /// <summary>
        /// 包含比较(对字符串类型及集合类型有效)
        /// </summary>
        Contains,

        /// <summary>
        /// 不包含比较(对字符串类型及集合类型有效)
        /// </summary>
        NotContains,

        /// <summary>
        /// 检查属性是否存在此属性
        /// </summary>
        Exists
    }

    /// <summary>
    /// 属性条件检查类，可根据物品属性进行条件判断
    /// </summary>
    public class AttributeCondition : IItemCondition
    {
        /// <summary>
        /// 要检查的属性名称
        /// </summary>
        public string AttributeName { get; set; }

        /// <summary>
        /// 属性比较的目标值
        /// </summary>
        public object AttributeValue { get; set; }

        /// <summary>
        /// 属性比较类型
        /// </summary>
        public AttributeComparisonType ComparisonType { get; set; }

        /// <summary>
        /// 创建一个属性条件，默认使用相等比较
        /// </summary>
        /// <param name="attributeName">属性名称</param>
        /// <param name="requiredValue">比较的目标值</param>
        public AttributeCondition(string attributeName, object requiredValue)
        {
            AttributeName = attributeName;
            AttributeValue = requiredValue;
            ComparisonType = AttributeComparisonType.Equal;
        }

        /// <summary>
        /// 创建一个属性条件，指定比较类型
        /// </summary>
        /// <param name="attributeName">属性名称</param>
        /// <param name="requiredValue">比较的目标值</param>
        /// <param name="comparisonType">比较类型</param>
        public AttributeCondition(string attributeName, object requiredValue, AttributeComparisonType comparisonType)
        {
            AttributeName = attributeName;
            AttributeValue = requiredValue;
            ComparisonType = comparisonType;
        }

        /// <summary>
        /// 设置属性条件
        /// </summary>
        /// <param name="attributeName">属性名称</param>
        /// <param name="requiredValue">比较的目标值</param>
        public void SetAttribute(string attributeName, object requiredValue)
        {
            AttributeName = attributeName;
            AttributeValue = requiredValue;
        }

        /// <summary>
        /// 设置属性条件和比较类型
        /// </summary>
        /// <param name="attributeName">属性名称</param>
        /// <param name="requiredValue">比较的目标值</param>
        /// <param name="comparisonType">比较类型</param>
        public void SetAttribute(string attributeName, object requiredValue, AttributeComparisonType comparisonType)
        {
            AttributeName = attributeName;
            AttributeValue = requiredValue;
            ComparisonType = comparisonType;
        }

        /// <summary>
        /// 判断物品是否满足属性条件
        /// </summary>
        /// <param name="item">要检查的物品</param>
        /// <returns>如果满足条件返回true，否则返回false</returns>
        public bool IsCondition(IItem item)
        {
            // 检查物品是否为空
            if (item == null || item.Attributes == null)
                return false;

            // 仅检查属性是否存在
            if (ComparisonType == AttributeComparisonType.Exists)
                return item.Attributes.ContainsKey(AttributeName);

            // 尝试获取属性值
            if (!item.Attributes.TryGetValue(AttributeName, out var actualValue))
                return false;

            // 如果属性值为空的比较值也为空
            if (actualValue == null)
                return AttributeValue == null;

            // 根据比较类型进行比较
            return ComparisonType switch
            {
                AttributeComparisonType.Equal => actualValue.Equals(AttributeValue),
                AttributeComparisonType.NotEqual => !actualValue.Equals(AttributeValue),
                AttributeComparisonType.GreaterThan => CompareNumeric(actualValue, AttributeValue) > 0,
                AttributeComparisonType.LessThan => CompareNumeric(actualValue, AttributeValue) < 0,
                AttributeComparisonType.GreaterThanOrEqual => CompareNumeric(actualValue, AttributeValue) >= 0,
                AttributeComparisonType.LessThanOrEqual => CompareNumeric(actualValue, AttributeValue) <= 0,
                AttributeComparisonType.Contains => CompareContains(actualValue, AttributeValue),
                AttributeComparisonType.NotContains => !CompareContains(actualValue, AttributeValue),
                _ => false,
            };
        }

        /// <summary>
        /// 比较两个数值
        /// </summary>
        private int CompareNumeric(object value1, object value2)
        {
            if (value1 == null || value2 == null)
                return 0;

            // 尝试将值转换为可比较的数值类型
            if (value1 is IComparable comparable1 && value2 is IComparable comparable2)
            {
                try
                {
                    // 如果类型相同则直接比较
                    if (value1.GetType() == value2.GetType())
                        return comparable1.CompareTo(comparable2);

                    // 尝试将值转换为decimal进行比较
                    if (decimal.TryParse(value1.ToString(), out decimal num1) &&
                        decimal.TryParse(value2.ToString(), out decimal num2))
                        return num1.CompareTo(num2);

                    // 尝试将值转换为double进行比较
                    if (double.TryParse(value1.ToString(), out double d1) &&
                        double.TryParse(value2.ToString(), out double d2))
                        return d1.CompareTo(d2);
                }
                catch
                {
                    // 无法比较，返回0
                    return 0;
                }
            }

            // 无法比较，返回0
            return 0;
        }

        /// <summary>
        /// 检查一个值是否包含另一个值
        /// </summary>
        private bool CompareContains(object container, object value)
        {
            // 如果任一值为null则无法执行包含比较
            if (container == null || value == null)
                return false;

            // 字符串包含比较
            if (container is string containerStr && value is string valueStr)
                return containerStr.Contains(valueStr);

            // 集合包含比较
            if (container is System.Collections.IEnumerable enumerable && container is not string)
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