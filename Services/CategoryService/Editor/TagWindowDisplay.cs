#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace EasyPack.Category.Editor
{
    public class TagWindowDisplay : EditorWindow
    {
        private Vector2 _scrollPosition;
        private Vector2 _entityScrollPosition;
        private string _searchFilter = "";
        private string _selectedTag = "";
        private Dictionary<string, List<string>> _tagData = new();

        public static void ShowWindow()
        {
            var window = GetWindow<TagWindowDisplay>("Tags");
            window.minSize = new Vector2(300, 300);
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("标签系统", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("搜索:", GUILayout.Width(50));
            _searchFilter = EditorGUILayout.TextField(_searchFilter);
            if (GUILayout.Button("刷新", GUILayout.Width(50)))
            {
                RefreshData();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();
            
            EditorGUILayout.BeginVertical(GUILayout.Width(150));
            DrawTagList();
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.BeginVertical();
            DrawEntityList();
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void DrawTagList()
        {
            EditorGUILayout.LabelField("标签列表", EditorStyles.boldLabel);
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(400));
            
            if (_tagData.Count == 0)
            {
                EditorGUILayout.HelpBox("没有标签数据", MessageType.Info);
            }
            else
            {
                var sortedTags = _tagData.Keys.OrderBy(x => x).ToList();
                foreach (var tag in sortedTags)
                {
                    if (!string.IsNullOrEmpty(_searchFilter) && !tag.ToLower().Contains(_searchFilter.ToLower()))
                    {
                        continue;
                    }
                    
                    var isSelected = tag == _selectedTag;
                    var style = isSelected ? new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleLeft } : new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft };
                    var entityCount = _tagData[tag].Count;
                    
                    if (GUILayout.Button($"{tag} ({entityCount})", style, GUILayout.Height(25)))
                    {
                        _selectedTag = tag;
                    }
                }
            }
            
            EditorGUILayout.EndScrollView();
        }

        private void DrawEntityList()
        {
            EditorGUILayout.LabelField("关联实体", EditorStyles.boldLabel);
            
            if (string.IsNullOrEmpty(_selectedTag))
            {
                EditorGUILayout.HelpBox("请选择一个标签", MessageType.Info);
            }
            else
            {
                EditorGUILayout.LabelField($"标签: {_selectedTag}", EditorStyles.miniBoldLabel);
                _entityScrollPosition = EditorGUILayout.BeginScrollView(_entityScrollPosition, GUILayout.Height(400));
                
                if (_tagData.TryGetValue(_selectedTag, out var entities))
                {
                    if (entities.Count == 0)
                    {
                        EditorGUILayout.HelpBox("此标签没有关联实体", MessageType.Info);
                    }
                    else
                    {
                        foreach (var entityId in entities)
                        {
                            EditorGUILayout.BeginHorizontal("box");
                            EditorGUILayout.LabelField(entityId);
                            EditorGUILayout.EndHorizontal();
                        }
                    }
                }
                
                EditorGUILayout.EndScrollView();
            }
        }

        private void RefreshData()
        {
            // 尝试从服务获取真实数据
            var service = CategoryServiceEditor.GetCategoryService();
            
            if (service != null)
            {
                Debug.Log("[TagWindowDisplay] 已连接到 CategoryService");
                LoadRealData(service);
            }
            else
            {
                Debug.LogWarning("[TagWindowDisplay] CategoryService 未初始化");
            }
            Repaint();
        }

        private void LoadRealData(CategoryService service)
        {
            try
            {
                _tagData.Clear();
                
                var registeredTypes = service.GetRegisteredEntityTypes();
                
                if (registeredTypes.Count == 0)
                {
                    Debug.LogWarning("[TagWindowDisplay] 没有注册任何 Manager");
                    return;
                }
                
                // 遍历所有类型，获取标签数据
                foreach (var entityType in registeredTypes)
                {
                    try
                    {
                        var getManagerMethod = service.GetType().GetMethod("GetManager")?.MakeGenericMethod(entityType);
                        if (getManagerMethod == null) continue;
                        
                        var manager = getManagerMethod.Invoke(service, null);
                        if (manager == null) continue;
                        
                        // 获取标签索引 (使用内部访问器)
                        var getTagIndexMethod = manager.GetType().GetMethod("GetTagIndex", 
                            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                        
                        if (getTagIndexMethod != null)
                        {
                            var tagIndex = getTagIndexMethod.Invoke(manager, null) as IReadOnlyDictionary<string, HashSet<string>>;
                            if (tagIndex != null)
                            {
                                foreach (var kvp in tagIndex)
                                {
                                    if (!_tagData.ContainsKey(kvp.Key))
                                    {
                                        _tagData[kvp.Key] = new List<string>();
                                    }
                                    
                                    // 添加实体ID（带类型前缀）
                                    foreach (var entityId in kvp.Value)
                                    {
                                        _tagData[kvp.Key].Add($"[{entityType.Name}] {entityId}");
                                    }
                                }
                            }
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"[TagWindowDisplay] 处理类型 {entityType.Name} 失败: {ex.Message}");
                    }
                }
                
                if (_tagData.Count == 0)
                {
                    Debug.LogWarning("[TagWindowDisplay] 未能加载任何标签数据");
                }
                else
                {
                    Debug.Log($"[TagWindowDisplay] 成功加载 {_tagData.Count} 个标签");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[TagWindowDisplay] 加载数据失败: {ex.Message}");
            }
        }

        private void OnEnable()
        {
            RefreshData();
        }
    }
}
#endif
