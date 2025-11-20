using UnityEditor;
using UnityEngine;

namespace EasyPack.CategoryService.Editor
{
    /// <summary>
    /// CategoryService 编辑器扩展
    /// 提供 Inspector 面板中的自定义编辑器功能
    /// </summary>
    public static class CategoryServiceEditor
    {
        /// <summary>
        /// 添加菜单项：创建 CategoryService 测试场景
        /// </summary>
        [MenuItem("EasyPack/CategoryService/Create Test Scene")]
        public static void CreateTestScene()
        {
            Debug.Log("CategoryService: 测试场景创建功能待实现");
        }

        /// <summary>
        /// 添加菜单项：验证 CategoryService 配置
        /// </summary>
        [MenuItem("EasyPack/CategoryService/Validate Configuration")]
        public static void ValidateConfiguration()
        {
            Debug.Log("CategoryService: 配置验证功能待实现");
        }

        /// <summary>
        /// 添加菜单项：生成 CategoryService 文档
        /// </summary>
        [MenuItem("EasyPack/CategoryService/Generate Documentation")]
        public static void GenerateDocumentation()
        {
            Debug.Log("CategoryService: 文档生成功能待实现");
        }
    }
}
