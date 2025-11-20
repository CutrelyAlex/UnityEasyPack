#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;
using EasyPack.Modifiers;
using EasyPack.Architecture;

namespace EasyPack.GamePropertySystem.Editor
{
    /// <summary>
    /// GamePropertyManager 可视化管理窗口
    /// 通过 EasyPack 架构自动解析 Manager 服务
    /// </summary>
    public class GamePropertyManagerWindow : EditorWindow
    {
        private GamePropertyService _manager;
        private string _searchText = "";
        private string _selectedCategory = "All";
        private string _selectedTag = "All";
        private bool _showMetadata = true;
        private bool _showModifiers = false;
        private Vector2 _scrollPosition;
        private bool _initialized = false;

        private void OnEnable()
        {
            TryResolveManager();
        }

        [MenuItem("EasyPack/CoreSystems/游戏属性(GameProperty)/管理器窗口")]
        public static void OpenGamePropertyManagerWindow()
        {
            var window = GetWindow<GamePropertyManagerWindow>("GameProperty Manager");
            window.Show();
        }

        private async void TryResolveManager()
        {
            try
            {
                if (_initialized && _manager != null) return;
                
                // 先检查服务是否已注册和初始化
                if (!EasyPackArchitecture.Instance.IsServiceRegistered<IGamePropertyService>())
                {
                    _initialized = false;
                    return;
                }

                // 检查服务是否已实例化
                if (!EasyPackArchitecture.Instance.HasInstance<IGamePropertyService>())
                {
                    _initialized = false;
                    return;
                }

                // 解析服务
                var service = await EasyPackArchitecture.Instance.ResolveAsync<IGamePropertyService>();

                // 检查服务状态
                if (service.State != ENekoFramework.ServiceLifecycleState.Ready)
                {
                    _initialized = false;
                    return;
                }

                _manager = service as GamePropertyService;
                _initialized = true;
                Repaint();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[GamePropertyManagerWindow] 解析 Manager 失败: {e.Message}");
            }
        }

        private void OnGUI()
        {
            if (!_initialized || _manager == null)
            {
                if (!EasyPackArchitecture.Instance.IsServiceRegistered<IGamePropertyService>())
                {
                    EditorGUILayout.HelpBox("GamePropertyManager 服务未注册到架构。", MessageType.Warning);
                }
                else if (!EasyPackArchitecture.Instance.HasInstance<IGamePropertyService>())
                {
                    EditorGUILayout.HelpBox("GamePropertyManager 服务未实例化。请先初始化架构。", MessageType.Warning);
                }
                else
                {
                    EditorGUILayout.HelpBox("GamePropertyManager 服务未就绪。", MessageType.Warning);
                }

                if (GUILayout.Button("重试解析"))
                {
                    TryResolveManager();
                }
                return;
            }

            DrawHeader();
            DrawSearchAndFilters();
            DrawPropertyList();
        }

        private void DrawHeader()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("GameProperty Manager", EditorStyles.boldLabel);

            var allProperties = _manager.GetAllPropertyIds().ToList();
            var allCategories = _manager.GetAllCategories().ToList();

            EditorGUILayout.LabelField($"总属性数: {allProperties.Count}");
            EditorGUILayout.LabelField($"分类数: {allCategories.Count}");
            EditorGUILayout.LabelField($"服务状态: {_manager.State}");

            EditorGUILayout.Space();
        }

        private void DrawSearchAndFilters()
        {
            EditorGUILayout.LabelField("搜索和过滤", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("搜索:", GUILayout.Width(60));
            _searchText = EditorGUILayout.TextField(_searchText);
            if (GUILayout.Button("清除", GUILayout.Width(60)))
            {
                _searchText = "";
                GUI.FocusControl(null);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("分类:", GUILayout.Width(60));

            var categories = new List<string> { "All" };
            categories.AddRange(_manager.GetAllCategories());

            int selectedIndex = categories.IndexOf(_selectedCategory);
            if (selectedIndex < 0) selectedIndex = 0;

            selectedIndex = EditorGUILayout.Popup(selectedIndex, categories.ToArray());
            _selectedCategory = categories[selectedIndex];
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("标签:", GUILayout.Width(60));

            var tags = new List<string> { "All" };
            var allTags = new HashSet<string>();
            foreach (var propId in _manager.GetAllPropertyIds())
            {
                var metadata = _manager.GetMetadata(propId);
                if (metadata?.Tags != null)
                {
                    foreach (var tag in metadata.Tags)
                        allTags.Add(tag);
                }
            }
            tags.AddRange(allTags.OrderBy(t => t));

            int tagIndex = tags.IndexOf(_selectedTag);
            if (tagIndex < 0) tagIndex = 0;

            tagIndex = EditorGUILayout.Popup(tagIndex, tags.ToArray());
            _selectedTag = tags[tagIndex];
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            _showMetadata = EditorGUILayout.Toggle("显示元数据", _showMetadata);
            _showModifiers = EditorGUILayout.Toggle("显示修饰符", _showModifiers);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
        }

        private void DrawPropertyList()
        {
            EditorGUILayout.LabelField("属性列表", EditorStyles.boldLabel);

            var properties = GetFilteredProperties();

            if (properties.Count == 0)
            {
                EditorGUILayout.HelpBox("没有找到匹配的属性", MessageType.Info);
                return;
            }

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            foreach (var property in properties)
            {
                DrawPropertyItem(property);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawPropertyItem(GameProperty property)
        {
            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"ID: {property.ID}", EditorStyles.boldLabel, GUILayout.Width(200));
            EditorGUILayout.LabelField($"基础值: {property.GetBaseValue():F2}", GUILayout.Width(120));
            EditorGUILayout.LabelField($"最终值: {property.GetValue():F2}", GUILayout.Width(120));
            EditorGUILayout.EndHorizontal();

            if (_showMetadata)
            {
                var metadata = _manager.GetMetadata(property.ID);
                if (metadata != null)
                {
                    EditorGUI.indentLevel++;

                    if (!string.IsNullOrEmpty(metadata.DisplayName))
                        EditorGUILayout.LabelField($"显示名: {metadata.DisplayName}");

                    if (!string.IsNullOrEmpty(metadata.Description))
                        EditorGUILayout.LabelField($"描述: {metadata.Description}");

                    if (metadata.Tags != null && metadata.Tags.Length > 0)
                        EditorGUILayout.LabelField($"标签: {string.Join(", ", metadata.Tags)}");

                    EditorGUI.indentLevel--;
                }
            }

            if (_showModifiers && property.Modifiers != null && property.Modifiers.Count > 0)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField($"修饰符数量: {property.Modifiers.Count}");

                foreach (var modifier in property.Modifiers)
                {
                    string modifierInfo = "";
                    if (modifier is FloatModifier fm)
                    {
                        modifierInfo = $"{fm.Type}: {fm.Value:F2} (优先级: {fm.Priority})";
                    }
                    else if (modifier is RangeModifier rm)
                    {
                        modifierInfo = $"{rm.Type}: [{rm.Value.x:F2}, {rm.Value.y:F2}] (优先级: {rm.Priority})";
                    }
                    EditorGUILayout.LabelField($"  - {modifierInfo}");
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }

        private List<GameProperty> GetFilteredProperties()
        {
            IEnumerable<GameProperty> properties;

            if (_selectedCategory == "All")
            {
                properties = _manager.GetAllPropertyIds().Select(id => _manager.Get(id));
            }
            else
            {
                properties = _manager.GetByCategory(_selectedCategory);
            }

            if (_selectedTag != "All")
            {
                var tagProperties = _manager.GetByTag(_selectedTag).Select(p => p.ID).ToHashSet();
                properties = properties.Where(p => tagProperties.Contains(p.ID));
            }

            if (!string.IsNullOrEmpty(_searchText))
            {
                properties = properties.Where(p =>
                {
                    if (p.ID.IndexOf(_searchText, System.StringComparison.OrdinalIgnoreCase) >= 0)
                        return true;

                    var metadata = _manager.GetMetadata(p.ID);
                    if (metadata != null)
                    {
                        if (!string.IsNullOrEmpty(metadata.DisplayName) &&
                            metadata.DisplayName.IndexOf(_searchText, System.StringComparison.OrdinalIgnoreCase) >= 0)
                            return true;

                        if (!string.IsNullOrEmpty(metadata.Description) &&
                            metadata.Description.IndexOf(_searchText, System.StringComparison.OrdinalIgnoreCase) >= 0)
                            return true;
                    }

                    return false;
                });
            }

            return properties.OrderBy(p => p.ID).ToList();
        }
    }
}
#endif
