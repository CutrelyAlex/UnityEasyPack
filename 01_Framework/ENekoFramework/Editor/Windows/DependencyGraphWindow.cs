using UnityEditor;
using UnityEngine;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EasyPack.ENekoFramework.Editor.Windows
{
    /// <summary>
    /// 依赖关系图窗口
    /// 使用 GraphView 可视化服务依赖关系树，并检测循环依赖
    /// </summary>
    public class DependencyGraphWindow : EditorWindow
    {
        private DependencyGraphView _graphView;
        private bool _autoRefresh = true;
        private double _lastRefreshTime;
        private const double RefreshInterval = 2.0;

        /// <summary>
        /// 显示依赖关系图窗口
        /// </summary>
        public static void ShowWindow()
        {
            var window = GetWindow<DependencyGraphWindow>("Dependency Graph");
            window.minSize = new Vector2(800, 600);
            window.Show();
        }

        private void OnEnable()
        {
            CreateGraphView();
            RefreshGraph();
        }

        private void OnDisable()
        {
            if (_graphView != null)
            {
                rootVisualElement.Remove(_graphView);
            }
        }

        private void Update()
        {
            if (_autoRefresh && EditorApplication.timeSinceStartup - _lastRefreshTime > RefreshInterval)
            {
                RefreshGraph();
                _lastRefreshTime = EditorApplication.timeSinceStartup;
            }
        }

        private void CreateGraphView()
        {
            _graphView = new DependencyGraphView
            {
                name = "Dependency Graph"
            };
            
            _graphView.StretchToParentSize();
            rootVisualElement.Add(_graphView);
            
            // 添加工具栏
            var toolbar = new IMGUIContainer(() =>
            {
                GUILayout.BeginHorizontal(EditorStyles.toolbar);
                
                if (GUILayout.Button("刷新", EditorStyles.toolbarButton, GUILayout.Width(60)))
                {
                    RefreshGraph();
                }
                
                if (GUILayout.Button("自动布局", EditorStyles.toolbarButton, GUILayout.Width(80)))
                {
                    _graphView.AutoLayout();
                }
                
                _autoRefresh = GUILayout.Toggle(_autoRefresh, "自动刷新", EditorStyles.toolbarButton, GUILayout.Width(80));
                
                GUILayout.FlexibleSpace();
                
                var circularCount = _graphView.GetCircularDependencyCount();
                if (circularCount > 0)
                {
                    var prevColor = GUI.contentColor;
                    GUI.contentColor = Color.red;
                    GUILayout.Label($"⚠ 发现 {circularCount} 个循环依赖", EditorStyles.toolbarButton);
                    GUI.contentColor = prevColor;
                }
                else
                {
                    GUILayout.Label("✓ 无循环依赖", EditorStyles.toolbarButton);
                }
                
                GUILayout.EndHorizontal();
            });
            
            rootVisualElement.Add(toolbar);
        }

        private void RefreshGraph()
        {
            _graphView?.RefreshGraph();
        }
    }

    /// <summary>
    /// 依赖关系图视图
    /// </summary>
    public class DependencyGraphView : GraphView
    {
        private List<Type> _circularDependencies = new List<Type>();

        public DependencyGraphView()
        {
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());
            
            var grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();
            
            var styleSheet = Resources.Load<StyleSheet>("DependencyGraphStyle");
            if (styleSheet != null)
            {
                styleSheets.Add(styleSheet);
            }
        }

        public void RefreshGraph()
        {
            // 清空现有图
            DeleteElements(graphElements.ToList());
            _circularDependencies.Clear();
            
            var services = ServiceInspector.GetAllServices();
            if (services == null || services.Count == 0)
                return;
            
            var nodes = new Dictionary<Type, ServiceNode>();
            
            // 创建节点
            foreach (var service in services)
            {
                var node = CreateServiceNode(service);
                AddElement(node);
                nodes[service.ServiceType] = node;
            }
            
            // 创建连接
            foreach (var service in services)
            {
                var dependencies = ServiceInspector.GetServiceDependencies(service.ServiceType);
                if (dependencies == null)
                    continue;
                
                foreach (var depType in dependencies)
                {
                    if (nodes.ContainsKey(service.ServiceType) && nodes.ContainsKey(depType))
                    {
                        var edge = CreateEdge(nodes[service.ServiceType], nodes[depType]);
                        AddElement(edge);
                    }
                }
                
                // 检查循环依赖
                if (ServiceInspector.HasCircularDependency(service.ServiceType))
                {
                    _circularDependencies.Add(service.ServiceType);
                    nodes[service.ServiceType].MarkAsCircular();
                }
            }
            
            // 自动布局
            AutoLayout();
        }

        public void AutoLayout()
        {
            var nodes = this.nodes.ToList().Cast<ServiceNode>().ToList();
            if (nodes.Count == 0)
                return;
            
            // 简单的层次布局算法
            var layers = CalculateLayers(nodes);
            
            float xSpacing = 250f;
            float ySpacing = 150f;
            float startX = 100f;
            float startY = 100f;
            
            for (int layer = 0; layer < layers.Count; layer++)
            {
                var nodesInLayer = layers[layer];
                float y = startY + layer * ySpacing;
                
                for (int i = 0; i < nodesInLayer.Count; i++)
                {
                    float x = startX + i * xSpacing;
                    nodesInLayer[i].SetPosition(new Rect(x, y, 200, 100));
                }
            }
        }

        public int GetCircularDependencyCount()
        {
            return _circularDependencies.Count;
        }

        private ServiceNode CreateServiceNode(ServiceDescriptor service)
        {
            return new ServiceNode(service);
        }

        private Edge CreateEdge(ServiceNode from, ServiceNode to)
        {
            var edge = new Edge
            {
                output = from.OutputPort,
                input = to.InputPort
            };
            
            edge.output.Connect(edge);
            edge.input.Connect(edge);
            
            return edge;
        }

        private List<List<ServiceNode>> CalculateLayers(List<ServiceNode> nodes)
        {
            var layers = new List<List<ServiceNode>>();
            var processed = new HashSet<ServiceNode>();
            var nodesByType = nodes.ToDictionary(n => n.ServiceType);
            
            // 第一层：没有依赖的节点
            var layer0 = nodes.Where(n =>
            {
                var deps = ServiceInspector.GetServiceDependencies(n.ServiceType);
                return deps == null || deps.Count == 0;
            }).ToList();
            
            if (layer0.Count == 0)
            {
                // 如果所有节点都有依赖（可能是循环依赖），全部放在第一层
                layer0 = nodes.ToList();
            }
            
            layers.Add(layer0);
            foreach (var node in layer0)
            {
                processed.Add(node);
            }
            
            // 后续层
            int maxIterations = 100;
            int iteration = 0;
            
            while (processed.Count < nodes.Count && iteration < maxIterations)
            {
                iteration++;
                
                var nextLayer = nodes.Where(n =>
                {
                    if (processed.Contains(n))
                        return false;
                    
                    var deps = ServiceInspector.GetServiceDependencies(n.ServiceType);
                    if (deps == null)
                        return true;
                    
                    // 检查所有依赖是否都已处理
                    return deps.All(depType =>
                        !nodesByType.ContainsKey(depType) ||
                        processed.Contains(nodesByType[depType])
                    );
                }).ToList();
                
                if (nextLayer.Count == 0)
                {
                    // 剩余的节点可能涉及循环依赖，全部放入下一层
                    nextLayer = nodes.Where(n => !processed.Contains(n)).ToList();
                }
                
                if (nextLayer.Count > 0)
                {
                    layers.Add(nextLayer);
                    foreach (var node in nextLayer)
                    {
                        processed.Add(node);
                    }
                }
            }
            
            return layers;
        }
    }

    /// <summary>
    /// 服务节点
    /// </summary>
    public class ServiceNode : Node
    {
        public Type ServiceType { get; }
        public Port InputPort { get; }
        public Port OutputPort { get; }
        
        private Label _statusLabel;

        public ServiceNode(ServiceDescriptor service)
        {
            ServiceType = service.ServiceType;
            title = service.ServiceType.Name;
            
            // 输入端口（被依赖）
            InputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(object));
            InputPort.portName = "Dependents";
            inputContainer.Add(InputPort);
            
            // 输出端口（依赖其他）
            OutputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(object));
            OutputPort.portName = "Dependencies";
            outputContainer.Add(OutputPort);
            
            // 状态标签
            _statusLabel = new Label(service.State.ToString())
            {
                style =
                {
                    color = GetStateColor(service.State),
                    unityFontStyleAndWeight = FontStyle.Bold,
                    marginTop = 5
                }
            };
            mainContainer.Add(_statusLabel);
            
            // 实现类型标签
            var implLabel = new Label(service.ImplementationType.Name)
            {
                style =
                {
                    fontSize = 10,
                    color = new Color(0.7f, 0.7f, 0.7f),
                    marginBottom = 5
                }
            };
            mainContainer.Add(implLabel);
            
            RefreshExpandedState();
        }

        public void MarkAsCircular()
        {
            style.backgroundColor = new Color(1f, 0.3f, 0.3f, 0.3f);
            
            var warningLabel = new Label("⚠ 循环依赖")
            {
                style =
                {
                    color = Color.red,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    fontSize = 11
                }
            };
            mainContainer.Add(warningLabel);
        }

        private Color GetStateColor(ServiceLifecycleState state)
        {
            return state switch
            {
                ServiceLifecycleState.Uninitialized => new Color(0.7f, 0.7f, 0.7f),
                ServiceLifecycleState.Initializing => new Color(1f, 0.8f, 0.3f),
                ServiceLifecycleState.Ready => new Color(0.3f, 1f, 0.3f),
                ServiceLifecycleState.Paused => new Color(0.9f, 0.6f, 0.3f),
                ServiceLifecycleState.Disposed => new Color(1f, 0.3f, 0.3f),
                _ => Color.white
            };
        }
    }
}
