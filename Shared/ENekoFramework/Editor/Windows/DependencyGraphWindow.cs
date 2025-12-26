#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace EasyPack.ENekoFramework.Editor
{
    /// <summary>
    ///     依赖关系图窗口
    ///     使用 GraphView 可视化服务依赖关系树，并检测循环依赖
    /// </summary>
    public class DependencyGraphWindow : EditorWindow
    {
        private DependencyGraphView _graphView;
        private bool _autoRefresh = true;
        private double _lastRefreshTime;
        private const double RefreshInterval = 2.0;

        /// <summary>
        ///     显示依赖关系图窗口
        /// </summary>
        public static void ShowWindow()
        {
            var window = GetWindow<DependencyGraphWindow>("Dependency Graph");
            window.minSize = new(800, 600);
            window.Show();
        }

        private void OnEnable()
        {
            CreateGraphView();
            RefreshGraph();
        }

        private void OnDisable()
        {
            if (_graphView != null) rootVisualElement.Remove(_graphView);
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
            _graphView = new() { name = "Dependency Graph" };

            _graphView.StretchToParentSize();
            rootVisualElement.Add(_graphView);

            // 添加工具栏
            var toolbar = new IMGUIContainer(() =>
            {
                GUILayout.BeginHorizontal(EditorStyles.toolbar);

                if (GUILayout.Button("刷新", EditorStyles.toolbarButton, GUILayout.Width(60))) RefreshGraph();

                if (GUILayout.Button("自动布局", EditorStyles.toolbarButton, GUILayout.Width(80))) _graphView.AutoLayout();

                _autoRefresh = GUILayout.Toggle(_autoRefresh, "自动刷新", EditorStyles.toolbarButton, GUILayout.Width(80));

                GUILayout.FlexibleSpace();

                int circularCount = _graphView.GetCircularDependencyCount();
                if (circularCount > 0)
                {
                    Color prevColor = GUI.contentColor;
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
    ///     依赖关系图视图
    /// </summary>
    public class DependencyGraphView : GraphView
    {
        private readonly List<Type> _circularDependencies = new();

        public DependencyGraphView()
        {
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);

            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            var grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();
        }

        public void RefreshGraph()
        {
            // 清空现有图
            DeleteElements(graphElements.ToList());
            _circularDependencies.Clear();

            var services = ServiceInspector.GetAllServices();
            if (services == null || services.Count == 0)
            {
                return;
            }

            var serviceNodes = new Dictionary<Type, ServiceNode>();

            // 创建节点
            foreach (ServiceDescriptor service in services)
            {
                ServiceNode node = CreateServiceNode(service);
                AddElement(node);
                serviceNodes[service.ServiceType] = node;
            }

            // 创建连接
            foreach (ServiceDescriptor service in services)
            {
                var dependencies = ServiceInspector.GetServiceDependencies(service.ServiceType);
                if (dependencies == null)
                {
                    continue;
                }

                foreach (Edge edge in from depType in dependencies
                                      where serviceNodes.ContainsKey(service.ServiceType) &&
                                            serviceNodes.ContainsKey(depType)
                                      select CreateEdge(serviceNodes[service.ServiceType], serviceNodes[depType]))
                {
                    AddElement(edge);
                }

                // 检查循环依赖
                if (!ServiceInspector.HasCircularDependency(service.ServiceType)) continue;
                _circularDependencies.Add(service.ServiceType);
                serviceNodes[service.ServiceType].MarkAsCircular();
            }

            // 自动布局
            AutoLayout();
        }

        public void AutoLayout()
        {
            var serviceNodes = nodes.ToList().Cast<ServiceNode>().ToList();
            if (serviceNodes.Count == 0)
            {
                return;
            }

            // 简单的层次布局算法
            var layers = CalculateLayers(serviceNodes);

            const float xSpacing = 250f;
            const float ySpacing = 150f;
            const float startX = 100f;
            const float startY = 100f;

            for (int layer = 0; layer < layers.Count; layer++)
            {
                var nodesInLayer = layers[layer];
                float y = startY + layer * ySpacing;

                for (int i = 0; i < nodesInLayer.Count; i++)
                {
                    float x = startX + i * xSpacing;
                    nodesInLayer[i].SetPosition(new(x, y, 200, 100));
                }
            }
        }

        public int GetCircularDependencyCount() => _circularDependencies.Count;

        private ServiceNode CreateServiceNode(ServiceDescriptor service) => ServiceNode.Create(service);

        private static Edge CreateEdge(ServiceNode from, ServiceNode to)
        {
            var edge = new Edge { output = from.OutputPort, input = to.InputPort };

            edge.output.Connect(edge);
            edge.input.Connect(edge);

            return edge;
        }

        private List<List<ServiceNode>> CalculateLayers(List<ServiceNode> serviceNodes)
        {
            var layers = new List<List<ServiceNode>>();
            var processed = new HashSet<ServiceNode>();
            var nodesByType = new Dictionary<Type, ServiceNode>();
            foreach (ServiceNode node in serviceNodes)
            {
                nodesByType[node.ServiceType] = node;
            }

            // 第一层：没有依赖的节点
            var layer0 = new List<ServiceNode>();
            foreach (ServiceNode node in serviceNodes)
            {
                var deps = ServiceInspector.GetServiceDependencies(node.ServiceType);
                if (deps == null || deps.Count == 0) layer0.Add(node);
            }

            if (layer0.Count == 0)
                // 如果所有节点都有依赖（可能是循环依赖），全部放在第一层
            {
                layer0.AddRange(serviceNodes);
            }

            layers.Add(layer0);
            foreach (ServiceNode node in layer0)
            {
                processed.Add(node);
            }

            // 后续层
            const int maxIterations = 100;
            int iteration = 0;

            while (processed.Count < serviceNodes.Count && iteration < maxIterations)
            {
                iteration++;

                var nextLayer = new List<ServiceNode>();
                foreach (ServiceNode node in serviceNodes)
                {
                    if (processed.Contains(node))
                    {
                        continue;
                    }

                    var deps = ServiceInspector.GetServiceDependencies(node.ServiceType);
                    if (deps == null)
                    {
                        nextLayer.Add(node);
                        continue;
                    }

                    // 检查所有依赖是否都已处理
                    bool allDepsProcessed = true;
                    foreach (Type depType in deps)
                    {
                        if (nodesByType.ContainsKey(depType) && processed.Contains(nodesByType[depType])) continue;
                        allDepsProcessed = false;
                        break;
                    }

                    if (allDepsProcessed) nextLayer.Add(node);
                }

                if (nextLayer.Count == 0)
                    // 剩余的节点可能涉及循环依赖，全部放入下一层
                {
                    foreach (ServiceNode node in serviceNodes)
                    {
                        if (!processed.Contains(node))
                        {
                            nextLayer.Add(node);
                        }
                    }
                }

                if (nextLayer.Count <= 0)
                {
                    layers.Add(nextLayer);
                    foreach (ServiceNode node in nextLayer)
                    {
                        processed.Add(node);
                    }
                }
            }

            return layers;
        }
    }

    /// <summary>
    ///     服务节点
    /// </summary>
    public class ServiceNode : Node
    {
        public Type ServiceType { get; }
        public Port InputPort { get; private set; }
        public Port OutputPort { get; private set; }

        private ServiceDescriptor _serviceDescriptor;

        private ServiceNode(ServiceDescriptor descriptor)
        {
            ServiceType = descriptor.ServiceType;
            _serviceDescriptor = descriptor;
        }

        public static ServiceNode Create(ServiceDescriptor service)
        {
            var node = new ServiceNode(service);
            node.Initialize();
            return node;
        }

        private void Initialize()
        {
            ServiceDescriptor service = _serviceDescriptor;

            // 设置标题
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
            var statusLabel = new Label(service.State.ToString())
            {
                style =
                {
                    color = GetStateColor(service.State),
                    unityFontStyleAndWeight = FontStyle.Bold,
                    marginTop = 5,
                },
            };
            mainContainer.Add(statusLabel);

            // 实现类型标签
            var implLabel = new Label(service.ImplementationType.Name)
            {
                style = { fontSize = 10, color = new Color(0.7f, 0.7f, 0.7f), marginBottom = 5 },
            };
            mainContainer.Add(implLabel);

            RefreshExpandedState();

            _serviceDescriptor = null;
        }

        public void MarkAsCircular()
        {
            style.backgroundColor = new Color(1f, 0.3f, 0.3f, 0.3f);

            var warningLabel = new Label("⚠ 循环依赖")
            {
                style = { color = Color.red, unityFontStyleAndWeight = FontStyle.Bold, fontSize = 11 },
            };
            mainContainer.Add(warningLabel);
        }

        private static Color GetStateColor(ServiceLifecycleState state)
        {
            return state switch
            {
                ServiceLifecycleState.Uninitialized => new(0.7f, 0.7f, 0.7f),
                ServiceLifecycleState.Initializing => new(1f, 0.8f, 0.3f),
                ServiceLifecycleState.Ready => new(0.3f, 1f, 0.3f),
                ServiceLifecycleState.Paused => new(0.9f, 0.6f, 0.3f),
                ServiceLifecycleState.Disposed => new(1f, 0.3f, 0.3f),
                _ => Color.white,
            };
        }
    }
}
#endif