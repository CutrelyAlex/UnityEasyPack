using System;
using System.Collections.Generic;

namespace RPGPack
{
    /// <summary>
    /// Buff接口，定义Buff的核心属性。
    /// </summary>
    public interface IRPGBuff
    {
        string BuffID { get; }
        public List<string> Tags { get; }
        public string Layer { get; }
        public int Priority { get; }
        IModifier Modifier { get; }
        float Duration { get; }
        float Elapsed { get; set; }
        bool IsExpired { get; }
        bool CanStack { get; }
        int StackCount { get; set; }
        object Source { get; set; }
        int MaxStackCount { get; set; }
        bool RemoveOneStackEachDuration { get; set; }
    }
}