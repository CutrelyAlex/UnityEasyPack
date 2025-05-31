using System;

namespace RPGPack
{
    /// <summary>
    /// 表示一个Buff（增益/减益效果）。
    /// 可附加到属性上，支持持续时间、叠加等机制。
    /// </summary>
    public class Buff
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

        /// <summary>
        /// 构造一个Buff实例。
        /// </summary>
        /// <param name="buffID">Buff唯一ID</param>
        /// <param name="modifier">修饰器</param>
        /// <param name="duration">持续时间（-1为永久）</param>
        /// <param name="canStack">是否可叠加</param>
        /// <param name="source">Buff来源</param>
        public Buff(string buffID, IModifier modifier, float duration = -1, bool canStack = false, object source = null)
        {
            BuffID = buffID;
            Modifier = modifier;
            Duration = duration;
            CanStack = canStack;
            Source = source;
        }
    }
}