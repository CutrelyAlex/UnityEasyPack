using System;
using System.Collections.Generic;
using UnityEngine;

namespace EasyPack
{
    /// <summary>
    /// Buff 配置数据，定义 Buff 的属性、行为和叠加策略
    /// </summary>
    public class BuffData
    {
        /// <summary>
        /// Buff 的唯一标识符
        /// </summary>
        public string ID;

        /// <summary>
        /// Buff 的显示名称
        /// </summary>
        public string Name;

        /// <summary>
        /// Buff 的描述文本
        /// </summary>
        public string Description;

        /// <summary>
        /// Buff 的图标精灵
        /// </summary>
        public Sprite Sprite;

        /// <summary>
        /// 自定义数据条目列表
        /// </summary>
        public List<CustomDataEntry> CustomData;

        /// <summary>
        /// 最大堆叠层数，默认为 1
        /// </summary>
        public int MaxStacks = 1;

        /// <summary>
        /// 持续时间（秒），-1 表示永久效果
        /// </summary>
        public float Duration = -1f;

        /// <summary>
        /// 触发间隔时间（秒），定义 Buff 每次触发的间隔
        /// </summary>
        public float TriggerInterval = 1f;

        /// <summary>
        /// Buff 持续时间叠加策略
        /// </summary>
        public BuffSuperpositionDurationType BuffSuperpositionStrategy = BuffSuperpositionDurationType.Add;

        /// <summary>
        /// Buff 堆叠层数叠加策略
        /// </summary>
        public BuffSuperpositionStacksType BuffSuperpositionStacksStrategy = BuffSuperpositionStacksType.Add;

        /// <summary>
        /// Buff 移除策略
        /// </summary>
        public BuffRemoveType BuffRemoveStrategy = BuffRemoveType.All;

        /// <summary>
        /// 是否在创建时立即触发一次
        /// </summary>
        public bool TriggerOnCreate = false;

        /// <summary>
        /// Buff 模块列表，定义 Buff 的具体行为
        /// </summary>
        public List<BuffModule> BuffModules = new List<BuffModule>();

        /// <summary>
        /// 标签列表，用于分类和查询 Buff
        /// </summary>
        public List<string> Tags = new List<string>();

        /// <summary>
        /// 层级列表，用于 Buff 的层级管理
        /// </summary>
        public List<string> Layers = new List<string>();

        /// <summary>
        /// 检查是否包含指定标签
        /// </summary>
        /// <param name="tag">要检查的标签</param>
        /// <returns>包含返回 true，否则返回 false</returns>
        public bool HasTag(string tag) => Tags.Contains(tag);

        /// <summary>
        /// 检查是否在指定层级
        /// </summary>
        /// <param name="layer">要检查的层级</param>
        /// <returns>在该层级返回 true，否则返回 false</returns>
        public bool InLayer(string layer) => Layers.Contains(layer);
    }

    /// <summary>
    /// Buff 持续时间叠加策略
    /// </summary>
    public enum BuffSuperpositionDurationType
    {
        /// <summary>
        /// 叠加持续时间
        /// </summary>
        Add,

        /// <summary>
        /// 重置持续时间后再叠加
        /// </summary>
        ResetThenAdd,

        /// <summary>
        /// 重置持续时间
        /// </summary>
        Reset,

        /// <summary>
        /// 保持原有持续时间不变
        /// </summary>
        Keep
    }

    /// <summary>
    /// Buff 堆叠层数叠加策略
    /// </summary>
    public enum BuffSuperpositionStacksType
    {
        /// <summary>
        /// 叠加堆叠层数
        /// </summary>
        Add,

        /// <summary>
        /// 重置堆叠层数后再叠加
        /// </summary>
        ResetThenAdd,

        /// <summary>
        /// 重置堆叠层数为 1
        /// </summary>
        Reset,

        /// <summary>
        /// 保持原有堆叠层数不变
        /// </summary>
        Keep
    }

    /// <summary>
    /// Buff 移除策略
    /// </summary>
    public enum BuffRemoveType
    {
        /// <summary>
        /// 移除所有堆叠层
        /// </summary>
        All,

        /// <summary>
        /// 仅移除一层堆叠
        /// </summary>
        OneStack,

        /// <summary>
        /// 手动移除（不自动移除）
        /// </summary>
        Manual,
    }
}
