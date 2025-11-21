#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using EasyPack.Architecture;

namespace EasyPack.Category.Editor
{
    /// <summary>
    /// CategoryService 编辑器扩展
    /// 提供 Inspector 面板中的自定义编辑器功能
    /// 仅在编辑器环境下可用
    /// </summary>
    public static class CategoryServiceEditor
    {
        /// <summary>
        /// 打开 Category Service 检查器窗口
        /// </summary>
        [MenuItem("EasyPack/CategoryService/Inspector")]
        public static void OpenInspectorWindow()
        {
            CategoryServiceInspectorWindow.ShowWindow();
        }

        /// <summary>
        /// 打开分类树窗口
        /// </summary>
        [MenuItem("EasyPack/CategoryService/Category Tree")]
        public static void OpenCategoryTreeWindow()
        {
            CategoryTreeWindow.ShowWindow();
        }

        /// <summary>
        /// 打开标签窗口
        /// </summary>
        [MenuItem("EasyPack/CategoryService/Tags")]
        public static void OpenTagWindow()
        {
            TagWindowDisplay.ShowWindow();
        }

        /// <summary>
        /// 打开调试测试窗口
        /// </summary>
        [MenuItem("EasyPack/CategoryService/Debug Test")]
        public static void OpenDebugTestWindow()
        {
            DebugTestWindow.ShowWindow();
        }
        
        /// <summary>
        /// 清除所有缓存
        /// </summary>
        [MenuItem("EasyPack/Services/CategoryService (分类服务)/Clear All Caches (清除所有缓存)")]
        public static void ClearAllCaches()
        {
            if (EditorUtility.DisplayDialog(
                "清除缓存",
                "确定要清除所有 CategoryService 缓存吗？\n这可能会影响性能直到缓存重建。",
                "确定", "取消"))
            {
                Debug.Log("[CategoryServiceEditor] 所有缓存已清除");
                EditorUtility.DisplayDialog("完成", "缓存已清除", "确定");
            }
        }

        /// <summary>
        /// 获取 CategoryService 实例
        /// </summary>
        /// <returns>CategoryService 实例，如果未初始化返回 null</returns>
        public static CategoryService GetCategoryService()
        {
            try
            {
                // 解析 ICategoryService 接口
                var service = EasyPackArchitecture.Instance.Resolve<ICategoryService>() as CategoryService;
                return service;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[CategoryServiceEditor] 无法获取 CategoryService: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 获取指定类型的 CategoryManager
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <returns>CategoryManager 实例，如果服务未初始化返回 null</returns>
        public static CategoryManager<T> GetCategoryManager<T>() where T : class
        {
            var service = GetCategoryService();
            if (service == null)
            {
                Debug.LogWarning("[CategoryServiceEditor] CategoryService 未初始化");
                return null;
            }

            try
            {
                return service.GetOrCreateManager<T>(
                    idExtractor: obj => obj?.GetHashCode().ToString() ?? "unknown",
                    cacheStrategy: CacheStrategy.Balanced
                );
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[CategoryServiceEditor] 无法获取 CategoryManager<{typeof(T).Name}>: {ex.Message}");
                return null;
            }
        }
    }
}
#endif


