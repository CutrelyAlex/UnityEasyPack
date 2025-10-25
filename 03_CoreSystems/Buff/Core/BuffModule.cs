using System;
using System.Collections.Generic;

namespace EasyPack
{
    /// <summary>
    /// Buff 回调类型，定义 Buff 生命周期中的各个事件点
    /// </summary>
    public enum BuffCallBackType
    {
        /// <summary>
        /// Buff 创建时触发
        /// </summary>
        OnCreate,

        /// <summary>
        /// Buff 移除时触发
        /// </summary>
        OnRemove,

        /// <summary>
        /// Buff 堆叠层数增加时触发
        /// </summary>
        OnAddStack,

        /// <summary>
        /// Buff 堆叠层数减少时触发
        /// </summary>
        OnReduceStack,

        /// <summary>
        /// Buff 每帧更新时触发
        /// </summary>
        OnUpdate,

        /// <summary>
        /// Buff 定时触发时触发
        /// </summary>
        OnTick,

        /// <summary>
        /// 条件触发
        /// </summary>
        Condition,

        /// <summary>
        /// 自定义回调类型
        /// </summary>
        Custom
    }

    /// <summary>
    /// Buff 模块抽象基类，定义 Buff 的具体行为和逻辑
    /// 通过注册回调函数响应 Buff 生命周期事件
    /// </summary>
    public abstract class BuffModule
    {
        /// <summary>
        /// 存储不同回调类型对应的处理方法
        /// </summary>
        private readonly Dictionary<BuffCallBackType, Action<Buff, object[]>> _callbackHandlers = new Dictionary<BuffCallBackType, Action<Buff, object[]>>();

        /// <summary>
        /// 自定义回调名称到处理方法的映射
        /// </summary>
        private readonly Dictionary<string, Action<Buff, object[]>> _customCallbackHandlers = new Dictionary<string, Action<Buff, object[]>>();

        /// <summary>
        /// 获取或设置条件触发逻辑，决定是否执行回调
        /// </summary>
        public Func<Buff, bool> TriggerCondition { get; set; }

        /// <summary>
        /// 获取或设置模块执行优先级，数字越大越先执行
        /// </summary>
        public int Priority { get; set; } = 0;

        /// <summary>
        /// 获取父级 Buff 的引用
        /// </summary>
        public Buff ParentBuff { get; private set; }

        /// <summary>
        /// 为指定的回调类型注册处理方法
        /// </summary>
        /// <param name="callbackType">回调类型</param>
        /// <param name="handler">处理方法</param>
        /// <exception cref="ArgumentException">当使用 Custom 类型时抛出，应使用重载方法</exception>
        public void RegisterCallback(BuffCallBackType callbackType, Action<Buff, object[]> handler)
        {
            if (callbackType == BuffCallBackType.Custom)
                throw new ArgumentException("使用RegisterCallback的重载方法注册自定义回调");

            _callbackHandlers[callbackType] = handler;
        }

        /// <summary>
        /// 为自定义回调类型注册处理方法
        /// </summary>
        /// <param name="customCallbackName">自定义回调名称</param>
        /// <param name="handler">处理方法</param>
        /// <exception cref="ArgumentException">当回调名称为空时抛出</exception>
        public void RegisterCallback(string customCallbackName, Action<Buff, object[]> handler)
        {
            if (string.IsNullOrEmpty(customCallbackName))
                throw new ArgumentException("自定义回调名称不能为空");

            _customCallbackHandlers[customCallbackName] = handler;
        }

        /// <summary>
        /// 检查是否应该执行特定回调
        /// </summary>
        /// <param name="callbackType">回调类型</param>
        /// <param name="customCallbackName">自定义回调名称（可选）</param>
        /// <returns>应该执行返回 true，否则返回 false</returns>
        public virtual bool ShouldExecute(BuffCallBackType callbackType, string customCallbackName = "")
        {
            if (callbackType == BuffCallBackType.Custom)
            {
                return !string.IsNullOrEmpty(customCallbackName) &&
                       _customCallbackHandlers.ContainsKey(customCallbackName);
            }

            if (TriggerCondition != null)
            {
                return TriggerCondition(ParentBuff);
            }

            return _callbackHandlers.ContainsKey(callbackType);
        }

        /// <summary>
        /// 执行对应的回调处理方法
        /// </summary>
        /// <param name="buff">执行回调的 Buff 实例</param>
        /// <param name="callbackType">回调类型</param>
        /// <param name="customCallbackName">自定义回调名称（可选）</param>
        /// <param name="parameters">传递给回调的参数</param>
        public virtual void Execute(Buff buff, BuffCallBackType callbackType, string customCallbackName = "", object[] parameters = null)
        {
            parameters ??= Array.Empty<object>();

            if (callbackType == BuffCallBackType.Custom)
            {
                if (!string.IsNullOrEmpty(customCallbackName) &&
                    _customCallbackHandlers.TryGetValue(customCallbackName, out var customHandler))
                {
                    customHandler(buff, parameters);
                }
                return;
            }

            if (_callbackHandlers.TryGetValue(callbackType, out var handler))
            {
                handler(buff, parameters);
            }
        }

        /// <summary>
        /// 设置父级 Buff 引用
        /// </summary>
        /// <param name="parentBuff">父级 Buff 实例</param>
        public void SetParentBuff(Buff parentBuff)
        {
            ParentBuff = parentBuff;
        }
    }
}