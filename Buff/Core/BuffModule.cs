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
        // ���ڴ洢��ͬ�ص����Ͷ�Ӧ�Ĵ�����
        private readonly Dictionary<BuffCallBackType, Action<Buff, object[]>> _callbackHandlers = new Dictionary<BuffCallBackType, Action<Buff, object[]>>();

        // �Զ���ص����Ƶ���������ӳ��
        private readonly Dictionary<string, Action<Buff, object[]>> _customCallbackHandlers = new Dictionary<string, Action<Buff, object[]>>();

        public int Priority { get; set; } = 0; // ���ƶ��ģ���ִ��˳������Խ��Խ��ִ��

        /// <summary>
        /// Ϊָ���Ļص�����ע�ᴦ����
        /// </summary>
        /// <param name="callbackType">�ص�����</param>
        /// <param name="handler">������</param>
        public void RegisterCallback(BuffCallBackType callbackType, Action<Buff, object[]> handler)
        {
            if (callbackType == BuffCallBackType.Custom)
                throw new ArgumentException("ʹ��RegisterCustomCallback����ע���Զ���ص�");

            _callbackHandlers[callbackType] = handler;
        }

        /// <summary>
        /// Ϊ�Զ���ص�����ע�ᴦ����
        /// </summary>
        /// <param name="customCallbackName">�Զ���ص�����</param>
        /// <param name="handler">������</param>
        public void RegisterCustomCallback(string customCallbackName, Action<Buff, object[]> handler)
        {
            if (string.IsNullOrEmpty(customCallbackName))
                throw new ArgumentException("�Զ���ص����Ʋ���Ϊ��");

            _customCallbackHandlers[customCallbackName] = handler;
        }

        /// <summary>
        /// ����Ƿ�Ӧ��ִ���ض��ص�
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
        /// ִ�ж�Ӧ�Ļص�������
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