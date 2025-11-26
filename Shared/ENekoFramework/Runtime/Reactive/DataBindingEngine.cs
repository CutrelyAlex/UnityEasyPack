using System;
using System.Collections.Generic;
using UnityEngine;

namespace EasyPack.ENekoFramework
{
    /// <summary>
    ///     数据绑定引擎，管理UI到属性的绑定关系。
    ///     负责在GameObject销毁时自动清理绑定，防止内存泄漏。
    /// </summary>
    public class DataBindingEngine : MonoBehaviour
    {
        private static DataBindingEngine _instance;
        private readonly Dictionary<GameObject, List<Action>> _bindings = new();

        /// <summary>
        ///     获取数据绑定引擎的单例实例。
        ///     如果不存在则自动创建。
        /// </summary>
        public static DataBindingEngine Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("[DataBindingEngine]");
                    _instance = go.AddComponent<DataBindingEngine>();
                    DontDestroyOnLoad(go);
                }

                return _instance;
            }
        }

        /// <summary>
        ///     注册UI组件到属性的绑定。
        ///     当GameObject销毁时会自动清理绑定。
        /// </summary>
        /// <param name="target">绑定的目标GameObject</param>
        /// <param name="cleanup">清理回调，用于取消订阅</param>
        public void RegisterBinding(GameObject target, Action cleanup)
        {
            if (target == null || cleanup == null)
                return;

            if (!_bindings.ContainsKey(target))
            {
                _bindings[target] = new();

                // 添加销毁监听
                var destroyer = target.GetComponent<BindingDestroyer>();
                if (destroyer == null)
                {
                    destroyer = target.AddComponent<BindingDestroyer>();
                    destroyer.OnDestroyed += () => CleanupBindings(target);
                }
            }

            _bindings[target].Add(cleanup);
        }

        /// <summary>
        ///     清理指定GameObject的所有绑定。
        /// </summary>
        /// <param name="target">要清理绑定的GameObject</param>
        public void CleanupBindings(GameObject target)
        {
            if (!target || !_bindings.TryGetValue(target, out var cleanups))
                return;

            foreach (Action cleanup in cleanups) cleanup?.Invoke();

            _bindings.Remove(target);
        }

        /// <summary>
        ///     获取当前绑定数量（用于诊断）。
        /// </summary>
        /// <returns>绑定数量</returns>
        public int GetBindingCount()
        {
            int total = 0;
            foreach (var list in _bindings.Values) total += list.Count;

            return total;
        }

        private void OnDestroy()
        {
            // 清理所有绑定
            foreach (var kvp in _bindings)
            foreach (Action cleanup in kvp.Value)
                cleanup?.Invoke();

            _bindings.Clear();
        }
    }

    /// <summary>
    ///     用于检测GameObject销毁的辅助组件。
    ///     当GameObject销毁时触发OnDestroyed事件。
    /// </summary>
    internal class BindingDestroyer : MonoBehaviour
    {
        /// <summary>
        ///     当GameObject被销毁时触发。
        /// </summary>
        public event Action OnDestroyed;

        private void OnDestroy() { OnDestroyed?.Invoke(); }
    }
}