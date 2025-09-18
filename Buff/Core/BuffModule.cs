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
        Condition,

        Custom
    }


    public abstract class BuffModule
    {
        // 用于存储不同回调类型对应的处理方法
        private readonly Dictionary<BuffCallBackType, Action<Buff, object[]>> _callbackHandlers = new Dictionary<BuffCallBackType, Action<Buff, object[]>>();

        // 自定义回调名称到处理方法的映射
        private readonly Dictionary<string, Action<Buff, object[]>> _customCallbackHandlers = new Dictionary<string, Action<Buff, object[]>>();

        // 条件触发逻辑
        public Func<Buff, bool> TriggerCondition { get; set; }

        public int Priority { get; set; } = 0; // 控制多个模块的执行顺序，数字越大越先执行

        public Buff ParentBuff { get; private set; } // 父级 Buff 的引用

        /// <summary>
        /// 为指定的回调类型注册处理方法
        /// </summary>
        /// <param name="callbackType">回调类型</param>
        /// <param name="handler">处理方法</param>
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
        public void RegisterCallback(string customCallbackName, Action<Buff, object[]> handler)
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

            if (TriggerCondition != null)
            {
                return TriggerCondition(ParentBuff);
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
        /// <summary>
        /// 设置父级 Buff 引用
        /// </summary>
        /// <param name="parentBuff"></param>
        public void SetParentBuff(Buff parentBuff)
        {
            ParentBuff = parentBuff;
        }
    }
}