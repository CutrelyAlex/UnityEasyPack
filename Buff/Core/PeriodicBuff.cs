using System;
using System.Collections.Generic;

namespace RPGPack
{
    /// <summary>
    /// ��ʾһ��������ִ�лص���Buff��
    /// ���Զ���ִ��ָ���Ĳ������綨ʱ����˺����ָ������ȡ�
    /// </summary>
    public class PeriodicBuff : Buff
    {
        /// <summary>
        /// ���ڼ�����룩
        /// </summary>
        public float Interval { get; }

        /// <summary>
        /// ����ִ�еĻص�����
        /// </summary>
        public Action<PeriodicBuff> OnTick { get; }

        /// <summary>
        /// �����´δ�����ʱ�䣨�룩
        /// </summary>
        public float TimeUntilNextTick { get; private set; }

        /// <summary>
        /// ��ִ�е����ڴ���
        /// </summary>
        public int TickCount { get; private set; } = 0;

        /// <summary>
        /// ���ִ�д�����0��ʾ���޴���
        /// </summary>
        public int MaxTickCount { get; set; } = 0;

        /// <summary>
        /// �Ƿ������Buffʱ����ִ��һ��
        /// </summary>
        public bool TickOnStart { get; }

        public PeriodicBuff(
            string buffID,
            IModifier modifier,
            float interval,
            Action<PeriodicBuff> onTick,
            bool tickOnStart = false,
            float duration = -1,
            bool canStack = false,
            object source = null,
            List<string> tags = null,
            string layer = null,
            int priority = 0
        ) : base(buffID, modifier, duration, canStack, source, tags, layer, priority)
        {
            if (interval <= 0)
                throw new ArgumentException("����Ӧ������0��Interval must be greater than zero", nameof(interval));

            Interval = interval;
            OnTick = onTick ?? throw new ArgumentNullException(nameof(onTick));
            TickOnStart = tickOnStart;
            TimeUntilNextTick = tickOnStart ? 0 : interval;
        }

        public override Buff Clone()
        {
            var modifierClone = Modifier.Clone();
            var clone = new PeriodicBuff(
                BuffID,
                modifierClone,
                Interval,
                OnTick,
                TickOnStart,
                Duration,
                CanStack,
                Source,
                Tags,
                Layer,
                Priority
            )
            {
                MaxStackCount = MaxStackCount,
                RemoveOneStackEachDuration = RemoveOneStackEachDuration,
                MaxTickCount = MaxTickCount,
                TickCount = TickCount,
                TimeUntilNextTick = TimeUntilNextTick
            };
            return clone;
        }

        /// <summary>
        /// �������ڼ�ʱ���ڵ�����ʱ�����ص�
        /// </summary>
        /// <param name="deltaTime">ʱ������</param>
        /// <returns>���ִ���˻ص�����true�����򷵻�false</returns>
        public bool UpdateTick(float deltaTime)
        {
            // ����ﵽ���ִ�д��������ٴ���
            if (MaxTickCount > 0 && TickCount >= MaxTickCount)
                return false;

            TimeUntilNextTick -= deltaTime;
            if (TimeUntilNextTick <= 0)
            {
                ExecuteTick();
                TimeUntilNextTick += Interval; // ���ü�ʱ��������������
                return true;
            }
            return false;
        }

        /// <summary>
        /// ִ�����ڻص�
        /// </summary>
        public void ExecuteTick()
        {
            TickCount++;
            OnTick?.Invoke(this);
        }

        /// <summary>
        /// ǿ����������һ�λص�����Ӱ��ԭ������
        /// </summary>
        public void ForceTick()
        {
            ExecuteTick();
        }

        /// <summary>
        /// �������ڼ�ʱ��
        /// </summary>
        public void ResetTick()
        {
            TimeUntilNextTick = Interval;
        }
    }
}