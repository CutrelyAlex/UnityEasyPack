using System;
using System.Collections.Generic;
using UnityEngine;

namespace EasyPack
{
    /// <summary>
    /// Buff 实例，表示一个应用到目标对象的增益或减益效果
    /// 包含生命周期管理、堆叠控制和事件回调机制
    /// </summary>
    public class Buff
    {
        /// <summary>
        /// 获取或设置 Buff 的配置数据
        /// </summary>
        public BuffData BuffData { get; set; }

        /// <summary>
        /// 获取或设置创建此 Buff 的游戏对象
        /// </summary>
        public GameObject Creator;

        /// <summary>
        /// 获取或设置此 Buff 应用的目标游戏对象
        /// </summary>
        public GameObject Target;

        /// <summary>
        /// 获取或设置持续时间计时器（秒）
        /// -1 表示永久效果
        /// </summary>
        public float DurationTimer;

        /// <summary>
        /// 获取或设置触发间隔计时器（秒）
        /// </summary>
        public float TriggerTimer;

        /// <summary>
        /// 获取或设置当前堆叠层数
        /// </summary>
        public int CurrentStacks { get; set; } = 1;

        /// <summary>
        /// Buff 创建时的回调事件
        /// </summary>
        public Action<Buff> OnCreate { get; set; }

        /// <summary>
        /// Buff 移除时的回调事件
        /// </summary>
        public Action<Buff> OnRemove { get; set; }

        /// <summary>
        /// Buff 堆叠层数增加时的回调事件
        /// </summary>
        public Action<Buff> OnAddStack { get; set; }

        /// <summary>
        /// Buff 堆叠层数减少时的回调事件
        /// </summary>
        public Action<Buff> OnReduceStack { get; set; }

        /// <summary>
        /// Buff 每帧更新时的回调事件
        /// </summary>
        public Action<Buff> OnUpdate { get; set; }

        /// <summary>
        /// Buff 定时触发时的回调事件
        /// </summary>
        public Action<Buff> OnTrigger { get; set; }
    }
}