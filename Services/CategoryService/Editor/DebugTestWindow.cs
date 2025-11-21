#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using EasyPack.Architecture;

namespace EasyPack.Category.Editor
{
    public class DebugTestWindow : EditorWindow
    {
        private Vector2 _scrollPosition;
        private string _testEntityId = "TestEntity_001";
        private string _testCategory = "Combat.Physical";
        private string _testTag = "Debuff";
        private string _newEntityId = "";
        private List<string> _testResults = new();
        private CategoryService _categoryService;
        private object _currentManager;
        private System.Type _currentManagerType;
        private int _selectedTypeIndex = -1;
        private string[] _typeNames = System.Array.Empty<string>();
        

        [MenuItem("EasyPack/CategoryService/Debug Test")]
        public static void ShowWindow()
        {
            var window = GetWindow<DebugTestWindow>("Debug Test");
            window.minSize = new Vector2(500, 600);
        }

        private void OnEnable()
        {
            RefreshService();
        }

        private void RefreshService()
        {
            _categoryService = EasyPackArchitecture.Instance.Resolve<ICategoryService>() as CategoryService;
            AddTestResult(_categoryService == null ? "⚠️ CategoryService 未初始化或未注册" : "✓ CategoryService 已连接");
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical();

            EditorGUILayout.LabelField("CategoryService 调试测试", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            DrawParametersPanel();
            EditorGUILayout.Space(5);
            DrawServiceStatusPanel();
            EditorGUILayout.Space(5);
            DrawOperationsPanel();
            EditorGUILayout.Space(5);
            DrawResultsPanel();

            EditorGUILayout.EndVertical();
        }

        private void DrawParametersPanel()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("测试参数", EditorStyles.boldLabel);
            _testEntityId = EditorGUILayout.TextField("实体 ID:", _testEntityId);
            _testCategory = EditorGUILayout.TextField("分类:", _testCategory);
            _testTag = EditorGUILayout.TextField("标签:", _testTag);
            EditorGUILayout.EndVertical();
        }

        private void DrawServiceStatusPanel()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("服务状态", EditorStyles.boldLabel);
            
            if (_categoryService != null)
            {
                var registeredTypes = _categoryService.GetRegisteredEntityTypes();
                EditorGUILayout.HelpBox($"✓ 已连接\n已注册类型: {registeredTypes.Count}", MessageType.Info);
                
                // 类型选择下拉栏
                if (registeredTypes.Count > 0)
                {
                    // 更新类型名称数组
                    if (_typeNames.Length != registeredTypes.Count)
                    {
                        _typeNames = new string[registeredTypes.Count];
                        for (int i = 0; i < registeredTypes.Count; i++)
                        {
                            _typeNames[i] = registeredTypes[i].Name;
                        }
                        _selectedTypeIndex = -1;
                    }
                    
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("选择类型:", GUILayout.Width(60));
                    
                    int newIndex = EditorGUILayout.Popup(_selectedTypeIndex, _typeNames);
                    if (newIndex != _selectedTypeIndex)
                    {
                        _selectedTypeIndex = newIndex;
                        if (_selectedTypeIndex >= 0 && _selectedTypeIndex < registeredTypes.Count)
                        {
                            InitializeManagerForType(registeredTypes[_selectedTypeIndex]);
                        }
                    }
                    
                    EditorGUILayout.EndHorizontal();
                    
                    // 添加实体部分
                    EditorGUILayout.Space(5);
                    EditorGUILayout.LabelField("添加实体", EditorStyles.boldLabel);
                    EditorGUILayout.BeginHorizontal();
                    _newEntityId = EditorGUILayout.TextField("实体 ID:", _newEntityId);
                    if (GUILayout.Button("添加", GUILayout.Width(50)))
                    {
                        if (!string.IsNullOrEmpty(_newEntityId))
                        {
                            ExecuteCreateEntity(_newEntityId);
                            _newEntityId = "";
                        }
                        else
                        {
                            AddTestResult("⚠️ 请输入实体 ID");
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("✗ 未连接到 CategoryService", MessageType.Error);
            }
            
            EditorGUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("重新连接", GUILayout.Height(25)))
            {
                RefreshService();
            }
            if (GUILayout.Button("手动初始化测试", GUILayout.Height(25)))
            {
                ExecuteManualInitialization();
            }
            EditorGUILayout.EndHorizontal();
            
            if (_currentManager != null && _currentManagerType != null)
            {
                EditorGUILayout.HelpBox($"✓ 当前 Manager: {_currentManagerType.Name}", MessageType.Info);
            }
            
            EditorGUILayout.EndVertical();
        }

        private void DrawOperationsPanel()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("测试操作", EditorStyles.boldLabel);

            if (_categoryService != null && _currentManager != null)
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("删除实体", GUILayout.Height(30)))
                {
                    ExecuteDeleteEntity();
                }
                if (GUILayout.Button("按分类查询", GUILayout.Height(30)))
                {
                    ExecuteGetByCategory();
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("按标签查询", GUILayout.Height(30)))
                {
                    ExecuteGetByTag();
                }
                if (GUILayout.Button("获取统计信息", GUILayout.Height(30)))
                {
                    ExecuteGetStatistics();
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("移动实体", GUILayout.Height(30)))
                {
                    ExecuteMoveEntity();
                }
                if (GUILayout.Button("获取所有分类", GUILayout.Height(30)))
                {
                    ExecuteGetAllCategories();
                }
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.HelpBox("请先选择一个实体类型", MessageType.Warning);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawResultsPanel()
        {
            EditorGUILayout.BeginVertical("box");
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("测试结果", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("清空", GUILayout.Width(50)))
            {
                _testResults.Clear();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(3);

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            if (_testResults.Count == 0)
            {
                EditorGUILayout.HelpBox("执行操作查看结果", MessageType.Info);
            }
            else
            {
                // 创建一个统一的富文本显示区域
                var resultText = string.Join("\n", _testResults);
                
                // 使用 TextArea 而不是多个 Label，支持更好的文本选择和显示
                EditorGUILayout.TextArea(resultText, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.EndVertical();
        }

        private void InitializeManagerForType(System.Type entityType)
        {
            try
            {
                var getManagerMethod = _categoryService.GetType()
                    .GetMethod("GetManager", BindingFlags.Public | BindingFlags.Instance)
                    ?.MakeGenericMethod(entityType);

                if (getManagerMethod != null)
                {
                    _currentManager = getManagerMethod.Invoke(_categoryService, null);
                    _currentManagerType = entityType;
                    AddTestResult($"✓ 已初始化 Manager<{entityType.Name}>");
                }
            }
            catch (System.Exception ex)
            {
                AddTestResult($"✗ 初始化失败: {ex.Message}");
            }
        }

        private void ExecuteManualInitialization()
        {
            // 在后台线程执行异步初始化，确保 ResolveAsync 能正确执行 InitializeAsync
            _ = ExecuteManualInitializationAsync();
        }

        private async Task ExecuteManualInitializationAsync()
        {
            try
            {
                AddTestResult("开始手动初始化测试...");
                
                var service = await EasyPackArchitecture.Instance.ResolveAsync<ICategoryService>() as CategoryService;
                
                if (service == null)
                {
                    AddTestResult("✗ 无法获取 CategoryService");
                    AddTestResult("  原因: ICategoryService 未在架构中注册或未初始化");
                    return;
                }

                AddTestResult("✓ 通过 EasyPackArchitecture 获取 CategoryService 成功");
                AddTestResult("✓ CategoryService 已异步初始化");

                // 创建一个测试 Manager
                var testManager = service.GetOrCreateManager<string>(
                    idExtractor: s => s,
                    cacheStrategy: CacheStrategy.Balanced
                );

                if (testManager != null)
                {
                    _currentManager = testManager;
                    _currentManagerType = typeof(string);
                    AddTestResult($"✓ 成功创建测试 Manager<String>");
                    AddTestResult("✓ 手动初始化测试完成");
                    
                    // 刷新服务引用
                    _categoryService = service;
                }
                else
                {
                    AddTestResult("✗ 创建 Manager 失败");
                }
            }
            catch (System.Exception ex)
            {
                AddTestResult($"✗ 手动初始化失败: {ex.Message}");
                AddTestResult($"  堆栈: {ex.StackTrace}");
                AddTestResult($"  建议: 确保 EasyPackArchitecture 已初始化并注册了 ICategoryService");
            }
        }

        private void ExecuteDeleteEntity()
        {
            try
            {
                if (_currentManager == null) return;

                var deleteMethod = _currentManager.GetType()
                    .GetMethod("DeleteEntity", BindingFlags.Public | BindingFlags.Instance);

                if (deleteMethod == null) return;
                
                var result = deleteMethod.Invoke(_currentManager, new object[] { _testEntityId });
                AddTestResult($"✓ DeleteEntity('{_testEntityId}')");

                if (result is OperationResult operationResult)
                {
                    AddTestResult($"  IsSuccess: {operationResult.IsSuccess}");
                    if (!operationResult.IsSuccess)
                        AddTestResult($"  Error: {operationResult.ErrorMessage}");
                }
            }
            catch (System.Exception ex)
            {
                AddTestResult($"✗ 执行失败: {ex.Message}");
            }
        }

        private void ExecuteGetByCategory()
        {
            try
            {
                if (_currentManager == null) return;

                var getMethod = _currentManager.GetType()
                    .GetMethod("GetByCategory", BindingFlags.Public | BindingFlags.Instance);

                if (getMethod != null)
                {
                    var result = getMethod.Invoke(_currentManager, new object[] { _testCategory, false });
                    
                    int count = 0;
                    if (result is System.Collections.ICollection collection)
                    {
                        count = collection.Count;
                    }

                    AddTestResult($"✓ GetByCategory('{_testCategory}')");
                    AddTestResult($"  返回 {count} 个实体");
                }
            }
            catch (System.Exception ex)
            {
                AddTestResult($"✗ 查询失败: {ex.Message}");
            }
        }

        private void ExecuteGetByTag()
        {
            try
            {
                if (_currentManager == null) return;

                var getMethod = _currentManager.GetType()
                    .GetMethod("GetByTag", BindingFlags.Public | BindingFlags.Instance);

                if (getMethod != null)
                {
                    var result = getMethod.Invoke(_currentManager, new object[] { _testTag });
                    
                    int count = 0;
                    if (result is System.Collections.ICollection collection)
                    {
                        count = collection.Count;
                    }

                    AddTestResult($"✓ GetByTag('{_testTag}')");
                    AddTestResult($"  返回 {count} 个实体");
                }
            }
            catch (System.Exception ex)
            {
                AddTestResult($"✗ 查询失败: {ex.Message}");
            }
        }

        private void ExecuteMoveEntity()
        {
            try
            {
                if (_currentManager == null) return;

                var moveMethod = _currentManager.GetType()
                    .GetMethod("MoveEntityToCategory", BindingFlags.Public | BindingFlags.Instance);

                if (moveMethod != null)
                {
                    var result = moveMethod.Invoke(_currentManager, new object[] { _testEntityId, _testCategory });
                    AddTestResult($"✓ MoveEntityToCategory('{_testEntityId}', '{_testCategory}')");

                    if (result is OperationResult operationResult)
                    {
                        AddTestResult($"  IsSuccess: {operationResult.IsSuccess}");
                    }
                }
            }
            catch (System.Exception ex)
            {
                AddTestResult($"✗ 移动失败: {ex.Message}");
            }
        }

        private void ExecuteGetStatistics()
        {
            try
            {
                if (_currentManager == null) return;

                var statsMethod = _currentManager.GetType()
                    .GetMethod("GetStatistics", BindingFlags.Public | BindingFlags.Instance);

                if (statsMethod == null) return;
                var stats = statsMethod.Invoke(_currentManager, null) as Statistics;
                    
                AddTestResult($"✓ GetStatistics()");
                if (stats != null)
                {
                    AddTestResult($"  实体数: {stats.TotalEntities}");
                    AddTestResult($"  分类数: {stats.TotalCategories}");
                    AddTestResult($"  标签数: {stats.TotalTags}");
                    AddTestResult($"  缓存命中率: {stats.CacheHitRate:P2}");
                    AddTestResult($"  内存使用: {stats.MemoryUsageMB:F2} MB");
                }
            }
            catch (System.Exception ex)
            {
                AddTestResult($"✗ 获取统计失败: {ex.Message}");
            }
        }

        private void ExecuteGetAllCategories()
        {
            try
            {
                if (_currentManager == null) return;

                var getAllMethod = _currentManager.GetType()
                    .GetMethod("GetAllCategories", BindingFlags.Public | BindingFlags.Instance);

                if (getAllMethod != null)
                {
                    var result = getAllMethod.Invoke(_currentManager, null) as System.Collections.ICollection;
                    
                    AddTestResult($"✓ GetAllCategories()");
                    if (result != null)
                    {
                        AddTestResult($"  返回 {result.Count} 个分类");
                    }
                }
            }
            catch (System.Exception ex)
            {
                AddTestResult($"✗ 查询失败: {ex.Message}");
            }
        }
        
        private void ExecuteCreateEntity(string entityId)
        {
            try
            {
                if (_currentManager == null)
                {
                    AddTestResult("⚠️ 请先选择一个实体类型");
                }
            }
            catch (System.Exception ex)
            {
                AddTestResult($"✗{entityId} 创建失败: {ex.Message}");
            }
        }



        private void AddTestResult(string result)
        {
            _testResults.Insert(0, $"[{System.DateTime.Now:HH:mm:ss}] {result}");
            if (_testResults.Count > 100)
            {
                _testResults.RemoveAt(_testResults.Count - 1);
            }
            Repaint();
        }
    }
}
#endif
