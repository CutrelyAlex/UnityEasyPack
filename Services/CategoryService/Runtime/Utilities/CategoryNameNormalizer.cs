using System;

namespace EasyPack.CategoryService
{
    /// <summary>
    /// 分类名称规范化工具
    /// 处理分类名称的验证和标准化
    /// </summary>
    public static class CategoryNameNormalizer
    {
        private const int MaxCategoryDepth = 5;
        private const char Separator = '.';

        /// <summary>
        /// 规范化分类名称
        /// </summary>
        /// <param name="categoryName">原始分类名称</param>
        /// <param name="comparisonMode">字符串比较模式</param>
        /// <returns>规范化后的分类名称</returns>
        public static string Normalize(string categoryName, StringComparison comparisonMode)
        {
            if (string.IsNullOrWhiteSpace(categoryName))
            {
                return string.Empty;
            }

            // 去除首尾空白
            categoryName = categoryName.Trim();

            // 根据比较模式处理大小写
            if (comparisonMode == StringComparison.OrdinalIgnoreCase ||
                comparisonMode == StringComparison.InvariantCultureIgnoreCase)
            {
                categoryName = categoryName.ToLowerInvariant();
            }

            return categoryName;
        }

        /// <summary>
        /// 验证分类名称是否有效
        /// </summary>
        /// <param name="categoryName">分类名称</param>
        /// <param name="errorMessage">错误消息（如果无效）</param>
        /// <returns>是否有效</returns>
        public static bool IsValid(string categoryName, out string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(categoryName))
            {
                errorMessage = "分类名称不能为空";
                return false;
            }

            // 检查深度
            var parts = categoryName.Split(Separator);
            if (parts.Length > MaxCategoryDepth)
            {
                errorMessage = $"分类深度不能超过 {MaxCategoryDepth} 层";
                return false;
            }

            // 检查每个部分是否有效
            foreach (var part in parts)
            {
                if (string.IsNullOrWhiteSpace(part))
                {
                    errorMessage = "分类名称部分不能为空";
                    return false;
                }
            }

            errorMessage = null;
            return true;
        }

        /// <summary>
        /// 获取父分类名称
        /// </summary>
        /// <param name="categoryName">分类名称</param>
        /// <returns>父分类名称，如果没有父分类则返回 null</returns>
        public static string GetParentCategory(string categoryName)
        {
            if (string.IsNullOrWhiteSpace(categoryName))
            {
                return null;
            }

            var lastDotIndex = categoryName.LastIndexOf(Separator);
            if (lastDotIndex < 0)
            {
                return null;
            }

            return categoryName.Substring(0, lastDotIndex);
        }

        /// <summary>
        /// 获取分类的层级深度
        /// </summary>
        /// <param name="categoryName">分类名称</param>
        /// <returns>层级深度</returns>
        public static int GetDepth(string categoryName)
        {
            if (string.IsNullOrWhiteSpace(categoryName))
            {
                return 0;
            }

            return categoryName.Split(Separator).Length;
        }
    }
}
