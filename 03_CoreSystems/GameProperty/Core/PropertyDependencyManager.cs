using System;
using System.Collections.Generic;
using UnityEngine;

namespace EasyPack.GamePropertySystem
{
    /// <summary>
    /// 依赖管理器
    /// </summary>
    public class PropertyDependencyManager
    {
        private const float EPSILON = 0.0001f;
        private const int MAX_DEPENDENCY_DEPTH = 100;

        private readonly GameProperty _owner;
        private readonly HashSet<GameProperty> _dependencies = new();
        private readonly HashSet<GameProperty> _dependents = new();
        private readonly Dictionary<GameProperty, Func<GameProperty, float, float>> _dependencyCalculators = new();

        private int _dependencyDepth = 0;
        private bool _hasRandomDependency = false;

        /// <summary>
        /// 依赖深度
        /// </summary>
        public int DependencyDepth => _dependencyDepth;

        /// <summary>
        /// 是否有随机依赖
        /// </summary>
        public bool HasRandomDependency => _hasRandomDependency;

        /// <summary>
        /// 依赖数量
        /// </summary>
        public int DependencyCount => _dependencies.Count;

        /// <summary>
        /// 依赖者数量
        /// </summary>
        public int DependentCount => _dependents.Count;

        public PropertyDependencyManager(GameProperty owner)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        }

        /// <summary>
        /// 添加一个依赖项，当dependency的值改变时，会调用calculator来计算新值
        /// </summary>
        /// <param name="dependency">依赖的属性</param>
        /// <param name="calculator">计算函数(dependency, newDependencyValue) => newThisValue</param>
        public bool AddDependency(GameProperty dependency, Func<GameProperty, float, float> calculator = null)
        {
            if (dependency == null)
                throw new ArgumentNullException(nameof(dependency));

            if (!_dependencies.Add(dependency))
                return false;

            if (WouldCreateCyclicDependency(dependency))
            {
                _dependencies.Remove(dependency);
                Debug.LogWarning($"检测到循环依赖，取消添加依赖关系: {_owner.ID} -> {dependency.ID}");
                return false;
            }

            // 添加反向引用
            dependency.DependencyManager._dependents.Add(_owner);

            // 更新依赖深度
            UpdateDependencyDepth();

            // 如果有计算函数则保存
            if (calculator != null)
            {
                _dependencyCalculators[dependency] = calculator;
                // 立即应用计算结果
                var dependencyValue = dependency.GetValue();
                var newValue = calculator(dependency, dependencyValue);
                _owner.SetBaseValue(newValue);
            }

            UpdateRandomDependencyState();
            return true;
        }

        /// <summary>
        /// 移除依赖关系
        /// </summary>
        public bool RemoveDependency(GameProperty dependency)
        {
            if (!_dependencies.Remove(dependency))
                return false;

            dependency.DependencyManager._dependents.Remove(_owner);
            _dependencyCalculators.Remove(dependency);
            UpdateDependencyDepth();

            UpdateRandomDependencyState();
            return true;
        }

        /// <summary>
        /// 触发所有依赖此属性的其他属性更新
        /// </summary>
        public void TriggerDependentUpdates(float currentValue)
        {
            // 早返
            if (_dependents.Count == 0)
                return;

            foreach (var dependent in _dependents)
            {
                if (dependent.DependencyManager._dependencyCalculators.TryGetValue(_owner, out var calculator))
                {
                    var newValue = calculator(_owner, currentValue);
                    if (Math.Abs(dependent.GetBaseValue() - newValue) > EPSILON)
                    {
                        dependent.SetBaseValue(newValue);
                    }
                }
                else
                {
                    ((IDrityTackable)dependent).MakeDirty();
                    dependent.GetValue();
                }
            }
        }

        /// <summary>
        /// 更新所有依赖项的值
        /// </summary>
        public void UpdateDependencies()
        {
            if (_dependencies.Count == 0)
                return;

            foreach (var dep in _dependencies)
            {
                dep.GetValue();
            }
        }

        /// <summary>
        /// 更新随机依赖状态
        /// </summary>
        public void UpdateRandomDependencyState()
        {
            bool hasRandom = false;
            var visited = new HashSet<GameProperty>();
            var queue = new Queue<GameProperty>(_dependencies);

            while (queue.Count > 0)
            {
                var dep = queue.Dequeue();
                if (!visited.Add(dep)) continue;

                // 使用公开属性访问
                if (dep.HasNonClampRangeModifiers())
                {
                    hasRandom = true;
                    break;
                }

                foreach (var subDep in dep.DependencyManager._dependencies)
                    queue.Enqueue(subDep);
            }

            _hasRandomDependency = hasRandom;
        }

        /// <summary>
        /// 检查是否会创建循环依赖
        /// </summary>
        private bool WouldCreateCyclicDependency(GameProperty dependency)
        {
            // 自引用
            if (dependency == _owner) return true;

            // 深度限制检查
            if (dependency.DependencyManager._dependencyDepth >= MAX_DEPENDENCY_DEPTH)
            {
                Debug.LogWarning($"依赖深度超过限制 {MAX_DEPENDENCY_DEPTH}");
                return true;
            }

            var visited = new HashSet<GameProperty>();
            var stack = new Stack<GameProperty>();
            stack.Push(dependency);

            while (stack.Count > 0)
            {
                var current = stack.Pop();
                if (!visited.Add(current)) continue;
                if (current == _owner) return true;

                // 检查正向依赖
                foreach (var dep in current.DependencyManager._dependencies)
                    if (!visited.Contains(dep)) stack.Push(dep);

                // 检查反向依赖
                foreach (var dep in current.DependencyManager._dependents)
                    if (!visited.Contains(dep)) stack.Push(dep);
            }

            return false;
        }

        /// <summary>
        /// 更新依赖深度
        /// </summary>
        private void UpdateDependencyDepth()
        {
            int oldDepth = _dependencyDepth;

            if (_dependencies.Count == 0)
            {
                _dependencyDepth = 0;
            }
            else
            {
                int maxDepth = 0;
                foreach (var dep in _dependencies)
                {
                    if (dep.DependencyManager._dependencyDepth > maxDepth)
                        maxDepth = dep.DependencyManager._dependencyDepth;
                }
                _dependencyDepth = maxDepth + 1;
            }

            if (oldDepth != _dependencyDepth)
            {
                foreach (var dependent in _dependents)
                {
                    dependent.DependencyManager.UpdateDependencyDepth();
                }
            }
        }

        /// <summary>
        /// 清理所有依赖关系
        /// </summary>
        public void ClearAll()
        {
            // 清理正向依赖
            foreach (var dependency in _dependencies)
            {
                dependency.DependencyManager._dependents.Remove(_owner);
            }
            _dependencies.Clear();

            // 清理反向依赖
            foreach (var dependent in _dependents)
            {
                dependent.DependencyManager._dependencies.Remove(_owner);
            }
            _dependents.Clear();

            _dependencyCalculators.Clear();
            _dependencyDepth = 0;
            _hasRandomDependency = false;
        }
    }
}

