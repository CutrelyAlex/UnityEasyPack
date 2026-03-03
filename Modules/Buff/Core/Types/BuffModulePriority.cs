namespace EasyPack.BuffSystem
{
    /// <summary>
    ///     BuffModule 执行优先级枚举
    ///     数值越大越先执行
    /// </summary>
    public enum BuffModulePriority
    {
        /// <summary>
        ///     适合日志记录、数据统计等
        /// </summary>
        Lowest = -10000,

        /// <summary>
        ///     基础属性层：对Property的加减修正通常应最早完成
        /// </summary>
        BaseProperty = -5000,

        /// <summary>
        ///     适合持续效果层，随时间触发的效果（中毒、燃烧、流血、持续回血等 DoT / HoT）
        /// </summary>
        DotHot = -100,

        /// <summary>
        ///     默认优先级
        /// </summary>
        Normal = 0,

        /// <summary>
        ///     增益层：正面状态效果，适合攻击力倍率提升、移动加速、护甲强化等
        /// </summary>
        Buff = 1000,

        /// <summary>
        ///     减益层：负面状态效果，适合攻击力降低、移动减速、防御削弱等
        /// </summary>
        Debuff = 5000,

        /// <summary>
        ///     元素状态层，需要在控制效果判断前完成属性附加
        /// </summary>
        ElementalStatus = 10000,

        /// <summary>
        ///     光环层：持续影响自身或周围单位的被动效果，例如团队增益等
        /// </summary>
        Aura = 15000,

        /// <summary>
        ///     护盾层：适合伤害吸收、屏障、格挡类效果
        /// </summary>
        Shield = 20000,

        /// <summary>
        ///     控制层：适合硬控制效果
        /// </summary>
        CrowdControl = 30000,

        /// <summary>
        ///     绝对优先，保留给必须最先执行的特殊覆盖逻辑（如无敌帧、免疫判断等）
        /// </summary>
        Critical = 100000,
    }
}
