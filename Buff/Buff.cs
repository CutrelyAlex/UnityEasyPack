using System;

namespace RPGPack
{
    public enum BuffStackType
    {
        Stackable,      // 可叠加
        Override,       // 覆盖
        IgnoreIfExists  // 已存在则忽略
    }
    /// <summary>
    /// 表示一个Buff（增益/减益效果）。
    /// 可附加到属性上，支持持续时间、叠加等机制。
    /// </summary>
    public class Buff : IRPGBuff
    {
        // Buff的唯一标识符。
        public string BuffID { get; }

        // Buff所携带的修饰器（影响属性的具体方式）。
        public IModifier Modifier { get; }

        // Buff持续时间（秒）。-1为永久Buff。
        public float Duration { get; } // -1为永久

        // Buff已存在的时间（秒）。
        public float Elapsed { get; set; }

        // Buff是否已过期（仅限有时长的Buff）。
        public bool IsExpired => Duration > 0 && Elapsed >= Duration;

        // 是否允许叠加
        public bool CanStack { get; }

        // 当前叠加层数。
        public int StackCount { get; set; } = 1;

        // Buff的来源，用于区分同ID不同来源的Buff，推荐使用string或GameObject等类型。
        public object Source { get; set; }

        // 最大叠加层数。0为无限叠加。
        public int MaxStackCount { get; set; } = 0;

        // 决定是否在每次持续时间结束时移除一层叠加，否则直接移除整个Buff
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
        /// 触发条件
        /// </summary>
        public Func<bool> TriggerCondition { get; }

        /// <summary>
        /// 触发时的回调。
        /// </summary>
        public Action OnTriggered { get; }

        /// <summary>
        /// 是否已触发
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
        /// 检查并尝试触发Buff。
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
