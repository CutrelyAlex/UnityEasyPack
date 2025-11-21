#if UNITY_EDITOR
using System.Collections;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace EasyPack.Category.Editor
{
    /// <summary>
    /// 分类树窗口
    /// 根据 Category 的层级自动生成连接树结构
    /// 类似 ENekoFramework 对依赖的可视化设计
    /// </summary>
    public class CategoryTreeWindow : EditorWindow
    {
        private Vector2 _scrollPosition;
        private string _searchFilter = "";
        private Dictionary<string, List<string>> _categoryData = new();
        private Dictionary<string, bool> _expandedNodes = new();

        public static void ShowWindow()
        {
            var window = GetWindow<CategoryTreeWindow>("Category Tree");
            window.minSize = new Vector2(300, 300);
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical();

            EditorGUILayout.LabelField("分类树结构", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("搜索:", GUILayout.Width(50));
            _searchFilter = EditorGUILayout.TextField(_searchFilter);
            if (GUILayout.Button("刷新", GUILayout.Width(50)))
            {
                RefreshData();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            if (_categoryData.Count == 0)
            {
                EditorGUILayout.HelpBox("没有分类数据", MessageType.Info);
            }
            else
            {
                var rootCategories = GetRootCategories();
                foreach (var root in rootCategories)
                {
                    if (string.IsNullOrEmpty(_searchFilter) || root.ToLower().Contains(_searchFilter.ToLower()))
                    {
                        DrawCategoryTreeNode(root, 0);
                    }
                }
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private List<string> GetRootCategories()
        {
            var roots = new List<string>();
            foreach (var cat in _categoryData.Keys)
            {
                if (!cat.Contains("."))
                {
                    roots.Add(cat);
                }
            }
            return roots.OrderBy(x => x).ToList();
        }

        private List<string> GetChildCategories(string parent)
        {
            var children = new List<string>();
            var prefix = parent + ".";
            foreach (var cat in _categoryData.Keys)
            {
                if (!cat.StartsWith(prefix)) continue;
                var remainder = cat[prefix.Length..];
                if (!remainder.Contains("."))
                {
                    children.Add(cat);
                }
            }
            return children.OrderBy(x => x).ToList();
        }

        private void DrawCategoryTreeNode(string category, int depth)
        {
            EditorGUILayout.BeginHorizontal();

            GUILayout.Space(depth * 20);

            var children = GetChildCategories(category);
            var hasChildren = children.Count > 0;

            if (hasChildren)
            {
                _expandedNodes.TryAdd(category, false);

                _expandedNodes[category] = EditorGUILayout.Foldout(_expandedNodes[category], "");
            }
            else
            {
                GUILayout.Space(15);
            }

            var parts = category.Split('.');
            var displayName = parts.Length > 0 ? parts[^1] : category;
            var entityCount = _categoryData.TryGetValue(category, out var value) ? value.Count : 0;

            EditorGUILayout.LabelField($"{displayName} ({entityCount})", GUILayout.MinWidth(200));

            EditorGUILayout.EndHorizontal();

            if (hasChildren && _expandedNodes[category])
            {
                foreach (var child in children)
                {
                    DrawCategoryTreeNode(child, depth + 1);
                }
            }
        }
        private void RefreshData()
        {
            // 尝试从服务获取真实数据
            var service = CategoryServiceEditor.GetCategoryService();
            
            if (service != null)
            {
                Debug.Log("[CategoryTreeWindow] 已连接到 CategoryService");
                LoadRealData(service);
            }
            else
            {
                Debug.LogWarning("[CategoryTreeWindow] CategoryService 未初始化，使用示例数据");
            }

            Repaint();
        }

        private void LoadRealData(CategoryService service)
        {
            try
            {
                _categoryData.Clear();
                
                // 获取所有已注册的实体类型
                var registeredTypes = service.GetRegisteredEntityTypes();
                
                if (registeredTypes.Count == 0)
                {
                    Debug.LogWarning("[CategoryTreeWindow] CategoryService 中没有注册任何 Manager");
                    return;
                }
                
                Debug.Log($"[CategoryTreeWindow] 找到 {registeredTypes.Count} 个已注册的实体类型");
                
                // 遍历所有类型，获取数据
                foreach (var entityType in registeredTypes)
                {
                    try
                    {
                        // 使用反射调用 GetManager<T>()
                        var getManagerMethod = service.GetType().GetMethod("GetManager")
                            ?.MakeGenericMethod(entityType);
                        
                        if (getManagerMethod == null) continue;
                        
                        var manager = getManagerMethod.Invoke(service, null);
                        if (manager == null) continue;
                        
                        // 获取 GetAllCategories 方法
                        var getAllCategoriesMethod = manager.GetType().GetMethod("GetAllCategories");
                        if (getAllCategoriesMethod == null) continue;
                        
                        var categories = getAllCategoriesMethod.Invoke(manager, null) as IReadOnlyList<string>;
                        if (categories == null) continue;
                        
                        // 获取 GetByCategory 方法
                        var getByCategoryMethod = manager.GetType().GetMethod("GetByCategory");
                        if (getByCategoryMethod == null) continue;
                        
                        foreach (var category in categories)
                        {
                            var entities = getByCategoryMethod.Invoke(manager, new object[] { category, false });
                            if (entities == null) continue;
                            
                            // 使用反射获取实体列表的 Count
                            if (entities is not ICollection entitiesCollection) continue;
                            
                            // 创建或累加分类数据
                            if (!_categoryData.ContainsKey(category))
                            {
                                _categoryData[category] = new List<string>();
                            }
                            
                            // 添加实体信息（显示类型名 + 数量）
                            _categoryData[category].Add($"{entityType.Name}:{entitiesCollection.Count}");
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"[CategoryTreeWindow] 处理类型 {entityType.Name} 失败: {ex.Message}");
                    }
                }
                
                if (_categoryData.Count == 0)
                {
                    Debug.LogWarning("[CategoryTreeWindow] 未能从 CategoryService 加载任何分类数据");
                }
                else
                {
                    Debug.Log($"[CategoryTreeWindow] 成功加载 {_categoryData.Count} 个分类");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[CategoryTreeWindow] 加载真实数据失败: {ex.Message}");
            }
        }

        private void OnEnable()
        {
            RefreshData();
        }
    }
}
#endif

