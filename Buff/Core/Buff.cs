using System;
using System.Collections.Generic;

namespace RPGPack
{
    /// <summary>
    /// 表示一个Buff（增益/减益效果）。
    /// 可附加到属性上，支持持续时间、叠加等机制。
    /// 为什么只携带一个Modifier?
    /// 组合而非聚合：复合效果可以通过组合多个Buff实现，而非在一个Buff中包含多个Modifier。
    /// </summary>
    public class Buff : IRPGBuff
    {
        // Buff的名称。
        public string BuffID { get; }

        // Buff所携带的修饰器。
        public IModifier Modifier { get; }

        // Buff持续时间（秒）。-1为永久Buff。
        public float Duration { get; } 
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
