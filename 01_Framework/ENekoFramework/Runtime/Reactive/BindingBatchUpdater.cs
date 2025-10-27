using System.Collections.Generic;
using UnityEngine;

namespace EasyPack.ENekoFramework
{
    /// <summary>
    /// 帧末批处理管理器 - 在 LateUpdate 时统一刷新所有脏绑定
    /// 性能目标: 1000个绑定批处理 < 16ms
    /// </summary>
    public sealed class BindingBatchUpdater : MonoBehaviour
    {
        private static BindingBatchUpdater _instance;
        private readonly HashSet<IBindable> _dirtyBindings = new HashSet<IBindable>();
        private bool _isUpdateScheduled = false;
        
        /// <summary>
        /// 获取单例实例
        /// </summary>
        public static BindingBatchUpdater Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("[BindingBatchUpdater]");
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<BindingBatchUpdater>();
                }
                return _instance;
            }
        }
        
        /// <summary>
        /// 是否有待处理的更新
        /// </summary>
        public bool IsUpdateScheduled => _isUpdateScheduled;
        
        /// <summary>
        /// 脏绑定数量（仅用于测试）
        /// </summary>
        public int DirtyBindingsCount => _dirtyBindings.Count;
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
        }
        
        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
        
        /// <summary>
        /// 标记绑定为脏状态，等待帧末批处理
        /// </summary>
        /// <param name="bindable">需要更新的绑定对象</param>
        public void MarkDirty(IBindable bindable)
        {
            if (bindable == null)
            {
                Debug.LogWarning("[BindingBatchUpdater] Attempted to mark null bindable as dirty");
                return;
            }
            
            _dirtyBindings.Add(bindable);
            _isUpdateScheduled = true;
        }
        
        /// <summary>
        /// 在 LateUpdate 时刷新所有脏绑定
        /// </summary>
        private void LateUpdate()
        {
            if (!_isUpdateScheduled)
            {
                return;
            }
            
            FlushAllUpdates();
        }
        
        /// <summary>
        /// 刷新所有待处理的绑定更新
        /// </summary>
        private void FlushAllUpdates()
        {
            if (_dirtyBindings.Count == 0)
            {
                _isUpdateScheduled = false;
                return;
            }
            
            // 性能监控（仅在开发模式）
            #if UNITY_EDITOR
            var startTime = System.Diagnostics.Stopwatch.StartNew();
            #endif
            
            // 批处理刷新所有脏绑定
            foreach (var bindable in _dirtyBindings)
            {
                try
                {
                    bindable?.FlushUpdates();
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[BindingBatchUpdater] Exception during FlushUpdates: {ex}");
                }
            }
            
            _dirtyBindings.Clear();
            _isUpdateScheduled = false;
            
            #if UNITY_EDITOR
            startTime.Stop();
            if (startTime.ElapsedMilliseconds > 16)
            {
                Debug.LogWarning($"[BindingBatchUpdater] Batch update took {startTime.ElapsedMilliseconds}ms " +
                               $"(target: <16ms). Consider optimizing bindings.");
            }
            #endif
        }
    }
}
