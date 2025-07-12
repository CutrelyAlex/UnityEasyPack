using System;
using System.Collections.Generic;

namespace EasyPack
{
    public enum BuffCallBackType
    {
        OnCreate,
        OnRemove,
        OnAddStack,
        OnReduceStack,
        OnUpdate,
        OnTick,

        Custom
    }


    public abstract class BuffModule
    {
        // 用于存储不同回调类型对应的处理方法
        private readonly Dictionary<BuffCallBackType, Action<Buff, object[]>> _callbackHandlers = new Dictionary<BuffCallBackType, Action<Buff, object[]>>();

        // 自定义回调名称到处理方法的映射
        private readonly Dictionary<string, Action<Buff, object[]>> _customCallbackHandlers = new Dictionary<string, Action<Buff, object[]>>();

        public int Priority { get; set; } = 0; // 控制多个模块的执行顺序，数字越大越先执行

        /// <summary>
        /// 为指定的回调类型注册处理方法
        /// </summary>
        /// <param name="callbackType">回调类型</param>
        /// <param name="handler">处理方法</param>
        public void RegisterCallback(BuffCallBackType callbackType, Action<Buff, object[]> handler)
        {
            if (callbackType == BuffCallBackType.Custom)
                throw new ArgumentException("使用RegisterCustomCallback方法注册自定义回调");

            _callbackHandlers[callbackType] = handler;
        }

        /// <summary>
        /// 为自定义回调类型注册处理方法
        /// </summary>
        /// <param name="customCallbackName">自定义回调名称</param>
        /// <param name="handler">处理方法</param>
        public void RegisterCustomCallback(string customCallbackName, Action<Buff, object[]> handler)
        {
            if (string.IsNullOrEmpty(customCallbackName))
                throw new ArgumentException("自定义回调名称不能为空");

            _customCallbackHandlers[customCallbackName] = handler;
        }

        /// <summary>
        /// 检查是否应该执行特定回调
        /// </summary>
        public virtual bool ShouldExecute(BuffCallBackType callbackType, string customCallbackName = "")
        {
            if (callbackType == BuffCallBackType.Custom)
            {
                return !string.IsNullOrEmpty(customCallbackName) &&
                       _customCallbackHandlers.ContainsKey(customCallbackName);
            }

            return _callbackHandlers.ContainsKey(callbackType);
        }

        /// <summary>
        /// 执行对应的回调处理方法
        /// </summary>
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
    }
}