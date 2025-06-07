using System;

namespace RPGPack
{
    public enum BuffStackType
    {
        Stackable,      // �ɵ���
        Override,       // ����
        IgnoreIfExists  // �Ѵ��������
    }
    /// <summary>
    /// ��ʾһ��Buff������/����Ч������
    /// �ɸ��ӵ������ϣ�֧�ֳ���ʱ�䡢���ӵȻ��ơ�
    /// </summary>
    public class Buff : IRPGBuff
    {
        // Buff��Ψһ��ʶ����
        public string BuffID { get; }

        // Buff��Я������������Ӱ�����Եľ��巽ʽ����
        public IModifier Modifier { get; }

        // Buff����ʱ�䣨�룩��-1Ϊ����Buff��
        public float Duration { get; } // -1Ϊ����

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

        public string Group { get; }
        public int Priority { get; }
        public BuffStackType StackType { get; }

        public Buff(
            string buffID,
            IModifier modifier,
            float duration = -1,
            bool canStack = false,
            object source = null,
            string group = null,
            int priority = 0,
            BuffStackType stackType = BuffStackType.Stackable)
            : base()
        {
            BuffID = buffID;
            Modifier = modifier;
            Duration = duration;
            CanStack = canStack;
            Source = source;
            Group = group;
            Priority = priority;
            StackType = stackType;
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
                Group,
                Priority,
                StackType
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
               string group = null,
               int priority = 0,
               BuffStackType stackType = BuffStackType.Stackable)
       : base(buffID, modifier, duration, canStack, source, group, priority, stackType)
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
                Group,
                Priority,
                StackType
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
