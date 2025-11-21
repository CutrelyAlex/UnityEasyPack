#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace EasyPack.Category.Editor
{
    /// <summary>
    /// CategoryService 检查器窗口
    /// 显示统计信息、分类树和实体列表
    /// 通过服务获取真实数据
    /// </summary>
    public class CategoryServiceInspectorWindow : EditorWindow
    {
        private Vector2 _scrollPosition;
        private Vector2 _categoryScrollPosition;
        private Vector2 _entityScrollPosition;
        
        private string _searchFilter = "";
        private string _selectedCategory = "";
        private bool _showStatistics = true;
        private bool _showCategoryTree = true;
        private bool _showEntityList = true;

        private Dictionary<string, List<string>> _categoryData = new();
        private Statistics _statistics;

        [MenuItem("EasyPack/CategoryService/Inspector")]
        public static void ShowWindow()
        {
            var window = GetWindow<CategoryServiceInspectorWindow>("Category Service");
            window.minSize = new Vector2(600, 400);
        }

        private void OnEnable()
        {
            RefreshData();
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical();
            
            DrawHeader();
            
            EditorGUILayout.Space(5);
            
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            
            if (_showStatistics)
            {
                DrawStatisticsPanel();
                EditorGUILayout.Space(10);
            }
            
            EditorGUILayout.BeginHorizontal();
            
            if (_showCategoryTree)
            {
                EditorGUILayout.BeginVertical(GUILayout.Width(250));
                DrawCategoryTree();
                EditorGUILayout.EndVertical();
            }
            
            if (_showEntityList)
            {
                EditorGUILayout.BeginVertical();
                DrawEntityList();
                EditorGUILayout.EndVertical();
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndScrollView();
            
            EditorGUILayout.EndVertical();
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            GUILayout.Label("Category Service Inspector", EditorStyles.boldLabel);
            
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("刷新", EditorStyles.toolbarButton, GUILayout.Width(50)))
            {
                RefreshData();
            }
            
            _showStatistics = GUILayout.Toggle(_showStatistics, "统计", EditorStyles.toolbarButton, GUILayout.Width(50));
            _showCategoryTree = GUILayout.Toggle(_showCategoryTree, "分类树", EditorStyles.toolbarButton, GUILayout.Width(60));
            _showEntityList = GUILayout.Toggle(_showEntityList, "实体列表", EditorStyles.toolbarButton, GUILayout.Width(70));
            
            EditorGUILayout.EndHorizontal();
        }

        private void DrawStatisticsPanel()
        {
            EditorGUILayout.BeginVertical("box");
            
            GUILayout.Label("统计信息", EditorStyles.boldLabel);
            
            if (_statistics == null)
            {
                EditorGUILayout.HelpBox("统计数据未加载", MessageType.Info);
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                
                EditorGUILayout.BeginVertical(GUILayout.Width(200));
                DrawStatField("实体总数", _statistics.TotalEntities.ToString());
                DrawStatField("分类总数", _statistics.TotalCategories.ToString());
                DrawStatField("标签总数", _statistics.TotalTags.ToString());
                EditorGUILayout.EndVertical();
                
                EditorGUILayout.BeginVertical(GUILayout.Width(200));
                DrawStatField("最大分类深度", _statistics.MaxCategoryDepth.ToString());
                DrawStatField("平均实体/分类", _statistics.AverageEntitiesPerCategory.ToString("F2"));
                DrawStatField("平均标签/实体", _statistics.AverageTagsPerEntity.ToString("F2"));
                EditorGUILayout.EndVertical();
                
                EditorGUILayout.BeginVertical(GUILayout.Width(200));
                DrawStatField("缓存命中率", _statistics.CacheHitRate.ToString("P2"));
                DrawStatField("内存使用", $"{_statistics.MemoryUsageMB:F2} MB");
                DrawStatField("平均内存/实体", $"{_statistics.AverageMemoryPerEntity:F0} bytes");
                EditorGUILayout.EndVertical();
                
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.Space(5);
                EditorGUILayout.BeginHorizontal();
                DrawStatField("缓存查询总数", _statistics.TotalCacheQueries.ToString());
                DrawStatField("命中次数", _statistics.CacheHits.ToString());
                DrawStatField("未命中次数", _statistics.CacheMisses.ToString());
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndVertical();
        }

        private void DrawStatField(string label, string value)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(120));
            EditorGUILayout.LabelField(value, EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawCategoryTree()
        {
            EditorGUILayout.BeginVertical("box");
            
            GUILayout.Label("分类树", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("搜索:", GUILayout.Width(50));
            _searchFilter = EditorGUILayout.TextField(_searchFilter);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            _categoryScrollPosition = EditorGUILayout.BeginScrollView(_categoryScrollPosition, GUILayout.Height(400));
            
            if (_categoryData.Count == 0)
            {
                EditorGUILayout.HelpBox("没有分类数据", MessageType.Info);
            }
            else
            {
                var sortedCategories = _categoryData.Keys.OrderBy(k => k).ToList();
                foreach (var category in sortedCategories)
                {
                    if (!string.IsNullOrEmpty(_searchFilter) && 
                        !category.ToLower().Contains(_searchFilter.ToLower()))
                    {
                        continue;
                    }
                    
                    DrawCategoryNode(category, 0);
                }
            }
            
            EditorGUILayout.EndScrollView();
            
            EditorGUILayout.EndVertical();
        }

        private void DrawCategoryNode(string category, int depth)
        {
            EditorGUILayout.BeginHorizontal();
            
            GUILayout.Space(depth * 15);
            
            var parts = category.Split('.');
            var displayName = parts.Length > 0 ? parts[^1] : category;
            var entityCount = _categoryData.TryGetValue(category, out var value) ? value.Count : 0;
            
            var isSelected = category == _selectedCategory;
            var style = isSelected ? new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleLeft } : new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft };
            
            if (GUILayout.Button($"{displayName} ({entityCount})", style))
            {
                _selectedCategory = category;
            }
            
            EditorGUILayout.EndHorizontal();
        }

        private void DrawEntityList()
        {
            EditorGUILayout.BeginVertical("box");
            
            GUILayout.Label("实体列表", EditorStyles.boldLabel);
            
            if (string.IsNullOrEmpty(_selectedCategory))
            {
                EditorGUILayout.HelpBox("请在左侧选择一个分类", MessageType.Info);
            }
            else
            {
                GUILayout.Label($"分类: {_selectedCategory}", EditorStyles.miniBoldLabel);
                
                EditorGUILayout.Space(5);
                
                _entityScrollPosition = EditorGUILayout.BeginScrollView(_entityScrollPosition, GUILayout.Height(400));
                
                if (_categoryData.TryGetValue(_selectedCategory, out var entities))
                {
                    if (entities.Count == 0)
                    {
                        EditorGUILayout.HelpBox("此分类没有实体", MessageType.Info);
                    }
                    else
                    {
                        foreach (var entityId in entities)
                        {
                            DrawEntityItem(entityId);
                        }
                    }
                }
                
                EditorGUILayout.EndScrollView();
            }
            
            EditorGUILayout.EndVertical();
        }

        private void DrawEntityItem(string entityId)
        {
            EditorGUILayout.BeginHorizontal("box");
            
            EditorGUILayout.LabelField(entityId);
            
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("删除", GUILayout.Width(50)))
            {
                if (EditorUtility.DisplayDialog("确认删除", $"确定要删除实体 '{entityId}' 吗？", "确定", "取消"))
                {
                    Debug.Log($"删除实体: {entityId}");
                    RefreshData();
                }
            }
            
            EditorGUILayout.EndHorizontal();
        }

        private void RefreshData()
        {
            // 尝试从服务获取真实数据
            var service = CategoryServiceEditor.GetCategoryService();
            
            if (service != null)
            {
                Debug.Log("[CategoryServiceInspectorWindow] 已连接到 CategoryService");
                LoadRealData(service);
            }
            else
            {
                Debug.LogWarning("[CategoryServiceInspectorWindow] CategoryService 未初始化，使用示例数据");
            }
            
            Repaint();
        }

        private void LoadRealData(CategoryService service)
        {
            try
            {
                _categoryData.Clear();
                
                var registeredTypes = service.GetRegisteredEntityTypes();
                
                if (registeredTypes.Count == 0)
                {
                    Debug.LogWarning("[CategoryServiceInspectorWindow] 没有注册任何 Manager");
                    return;
                }
                
                // 收集统计信息
                int totalEntities = 0;
                int totalCategories = 0;
                int totalTags = 0;
                
                // 遍历所有类型
                foreach (var entityType in registeredTypes)
                {
                    try
                    {
                        var getManagerMethod = service.GetType().GetMethod("GetManager")?.MakeGenericMethod(entityType);
                        if (getManagerMethod == null) continue;
                        
                        var manager = getManagerMethod.Invoke(service, null);
                        if (manager == null) continue;
                        
                        // 获取统计信息
                        var getStatsMethod = manager.GetType().GetMethod("GetStatistics");
                        if (getStatsMethod != null)
                        {
                            if (getStatsMethod.Invoke(manager, null) is Statistics stats)
                            {
                                totalEntities += stats.TotalEntities;
                                totalCategories += stats.TotalCategories;
                                totalTags += stats.TotalTags;
                                
                                // 使用第一个 Manager 的统计作为总体统计
                                _statistics ??= stats;
                            }
                        }
                        
                        // 获取分类数据
                        var getAllCategoriesMethod = manager.GetType().GetMethod("GetAllCategories");
                        if (getAllCategoriesMethod == null) continue;

                        if (getAllCategoriesMethod.Invoke(manager, null) is not IReadOnlyList<string> categories)
                            continue;
                        
                        var getByCategoryMethod = manager.GetType().GetMethod("GetByCategory");
                        foreach (var category in categories)
                        {
                            var entities = getByCategoryMethod?.Invoke(manager, new object[] { category, false });
                            var entitiesCollection = entities as System.Collections.ICollection;
                                    
                            if (!_categoryData.ContainsKey(category))
                            {
                                _categoryData[category] = new List<string>();
                            }
                                    
                            if (entitiesCollection != null)
                            {
                                _categoryData[category].Add($"{entityType.Name}:{entitiesCollection.Count}");
                            }
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"[CategoryServiceInspectorWindow] 处理类型 {entityType.Name} 失败: {ex.Message}");
                    }
                }
                
                // 如果有数据，更新统计的总计
                if (_statistics != null && totalCategories > 0)
                {
                    _statistics.TotalEntities = totalEntities;
                    _statistics.TotalCategories = totalCategories;
                    _statistics.TotalTags = totalTags;
                }
                
                if (_categoryData.Count == 0)
                {
                    Debug.LogWarning("[CategoryServiceInspectorWindow] 未能加载任何分类数据");
                }
                else
                {
                    Debug.Log($"[CategoryServiceInspectorWindow] 成功加载 {_categoryData.Count} 个分类");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[CategoryServiceInspectorWindow] 加载真实数据失败: {ex.Message}");
            }
        }
    }
}
#endif

