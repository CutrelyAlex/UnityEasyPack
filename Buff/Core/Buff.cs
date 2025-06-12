using System;
using System.Collections.Generic;

namespace RPGPack
{
    /// <summary>
    /// ��ʾһ��Buff������/����Ч������
    /// �ɸ��ӵ������ϣ�֧�ֳ���ʱ�䡢���ӵȻ��ơ�
    /// ΪʲôֻЯ��һ��Modifier?
    /// ��϶��Ǿۺϣ�����Ч������ͨ����϶��Buffʵ�֣�������һ��Buff�а������Modifier��
    /// </summary>
    public class Buff : IRPGBuff
    {
        // Buff�����ơ�
        public string BuffID { get; }

        // Buff��Я������������
        public IModifier Modifier { get; }

        // Buff����ʱ�䣨�룩��-1Ϊ����Buff��
        public float Duration { get; } 
        // Buff�Ѵ��ڵ�ʱ�䣨�룩��
        public float Elapsed { get; set; }

        // Buff�Ƿ��ѹ��ڣ�������ʱ����Buff����
        public bool IsExpired => Duration > 0 && Elapsed >= Duration;

        // �Ƿ��������
        public bool CanStack { get; }

        // ��ǰ���Ӳ�����
        public int StackCount { get; set; } = 1;

        // Buff����Դ����������ͬID��ͬ��Դ��Buff���Ƽ�ʹ��string��GameObject�����͡�
        public object Source { get; set; }

        // �����Ӳ�����0Ϊ���޵��ӡ�
        public int MaxStackCount { get; set; } = 0;

        // �����Ƿ���ÿ�γ���ʱ�����ʱ�Ƴ�һ����ӣ�����ֱ���Ƴ�����Buff
        public bool RemoveOneStackEachDuration { get; set; } = false;

        public List<string> Tags { get; }
        public int Priority { get; }

        public string Layer { get; }

        public Buff(
            string buffID,
            IModifier modifier,
            float duration = -1,
            bool canStack = false,
            object source = null,
            List<string> tags = null,
            string layer = null,
            int priority = 0
        )
            : base()
        {
            BuffID = buffID;
            Modifier = modifier;
            Duration = duration;
            CanStack = canStack;
            Source = source;
            Tags = tags;
            Layer = layer;
            Priority = priority;

            BuffManager.GetAllBuffs();
        }

        public virtual Buff Clone()
        {
            var modifierClone = Modifier.Clone();
            var clone = new Buff(
                BuffID,
                modifierClone,
                Duration,
                CanStack,
                Source,
                Tags,
                Layer,
                Priority
            )
            {
                MaxStackCount = MaxStackCount,
                RemoveOneStackEachDuration = RemoveOneStackEachDuration
            };
            return clone;
        }
    }

    public class TriggerableBuff : Buff
    {
        public TriggerableBuff(string buffID, IModifier modifier, float duration = -1, bool canStack = false, object source = null) : base(buffID, modifier, duration, canStack, source)
        {
        }
        /// <summary>
        /// ��������
        /// </summary>
        public Func<bool> TriggerCondition { get; }

        /// <summary>
        /// ����ʱ�Ļص���
        /// </summary>
        public Action OnTriggered { get; }

        /// <summary>
        /// �Ƿ��Ѵ���
        /// </summary>
        public bool HasTriggered { get; private set; } = false;

        public TriggerableBuff(
               string buffID,
               IModifier modifier,
               Func<bool> triggerCondition,
               Action onTriggered = null,
               float duration = -1,
               bool canStack = false,
               object source = null,
               List<string> tags = null,
               string layer = null,
               int priority = 0
        )
       : base(buffID, modifier, duration, canStack, source, tags, layer, priority)
        {
            TriggerCondition = triggerCondition ?? throw new ArgumentNullException(nameof(triggerCondition));
            OnTriggered = onTriggered;
        }

        public override Buff Clone()
        {
            var modifierClone = Modifier.Clone();
            var clone = new TriggerableBuff(
                BuffID,
                modifierClone,
                TriggerCondition,
                OnTriggered,
                Duration,
                CanStack,
                Source,
                Tags,
                Layer,
                Priority
            )
            {
                MaxStackCount = MaxStackCount,
                RemoveOneStackEachDuration = RemoveOneStackEachDuration
            };
            return clone;
        }

        /// <summary>
        /// ��鲢���Դ���Buff��
        /// </summary>
        public void TryTrigger()
        {
            if (!HasTriggered && TriggerCondition())
            {
                HasTriggered = true;
                OnTriggered?.Invoke();
            }
        }
    }

}
