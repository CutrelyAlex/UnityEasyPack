using System;
using System.Collections.Generic;

namespace RPGPack
{
    /// <summary>
    /// 表示一个周期性执行回调的Buff。
    /// 可以定期执行指定的操作，如定时造成伤害、恢复生命等。
    /// </summary>
    public class PeriodicBuff : Buff
    {
        /// <summary>
        /// 周期间隔（秒）
        /// </summary>
        public float Interval { get; }

        /// <summary>
        /// 周期执行的回调函数
        /// </summary>
        public Action<PeriodicBuff> OnTick { get; }

        /// <summary>
        /// 距离下次触发的时间（秒）
        /// </summary>
        public float TimeUntilNextTick { get; private set; }

        /// <summary>
        /// 已执行的周期次数
        /// </summary>
        public int TickCount { get; private set; } = 0;

        /// <summary>
        /// 最大执行次数，0表示无限次数
        /// </summary>
        public int MaxTickCount { get; set; } = 0;

        /// <summary>
        /// 是否在添加Buff时立即执行一次
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
                throw new ArgumentException("周期应当大于0。Interval must be greater than zero", nameof(interval));

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
        /// 更新周期计时并在到达间隔时触发回调
        /// </summary>
        /// <param name="deltaTime">时间增量</param>
        /// <returns>如果执行了回调返回true，否则返回false</returns>
        public bool UpdateTick(float deltaTime)
        {
            // 如果达到最大执行次数，不再触发
            if (MaxTickCount > 0 && TickCount >= MaxTickCount)
                return false;

            TimeUntilNextTick -= deltaTime;
            if (TimeUntilNextTick <= 0)
            {
                ExecuteTick();
                TimeUntilNextTick += Interval; // 重置计时器，保持周期性
                return true;
            }
            return false;
        }

        /// <summary>
        /// 执行周期回调
        /// </summary>
        public void ExecuteTick()
        {
            TickCount++;
            OnTick?.Invoke(this);
        }

        /// <summary>
        /// 强制立即触发一次回调，不影响原有周期
        /// </summary>
        public void ForceTick()
        {
            ExecuteTick();
        }

        /// <summary>
        /// 重置周期计时器
        /// </summary>
        public void ResetTick()
        {
            TimeUntilNextTick = Interval;
        }
    }
}