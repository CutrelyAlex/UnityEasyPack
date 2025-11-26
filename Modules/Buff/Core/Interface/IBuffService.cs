using System.Collections.Generic;
using UnityEngine;
using EasyPack.ENekoFramework;

namespace EasyPack.BuffSystem
{
    /// <summary>
    ///     Buff系统服务接口
    ///     继承IService以集成到EasyPack架构中
    ///     定义Buff系统的核心操作契约
    /// </summary>
    public interface IBuffService : IService
    {
        #region Buff创建与管理

        /// <summary>
        ///     创建并添加新的 Buff
        /// </summary>
        /// <param name="buffData">Buff 配置数据</param>
        /// <param name="creator">创建 Buff 的游戏对象</param>
        /// <param name="target">Buff 应用的目标对象</param>
        /// <returns>创建或更新的 Buff 实例</returns>
        Buff CreateBuff(BuffData buffData, GameObject creator, GameObject target);

        /// <summary>
        ///     更新所有 Buff（需要在游戏循环中调用）
        /// </summary>
        /// <param name="deltaTime">时间增量</param>
        void Update(float deltaTime);

        #endregion

        #region Buff移除操作

        /// <summary>
        ///     移除单个Buff
        /// </summary>
        /// <param name="buff">要移除的 Buff 实例</param>
        void RemoveBuff(Buff buff);

        /// <summary>
        ///     根据 ID 移除目标对象上的 Buff
        /// </summary>
        /// <param name="target">目标对象</param>
        /// <param name="buffID">Buff 的 ID</param>
        void RemoveBuffByID(object target, string buffID);

        /// <summary>
        ///     移除目标对象上的所有 Buff
        /// </summary>
        /// <param name="target">目标对象</param>
        void RemoveAllBuffs(object target);

        /// <summary>
        ///     根据标签移除 Buff
        /// </summary>
        /// <param name="target">目标对象</param>
        /// <param name="tag">标签名</param>
        void RemoveBuffsByTag(object target, string tag);

        /// <summary>
        ///     根据层级移除 Buff
        /// </summary>
        /// <param name="target">目标对象</param>
        /// <param name="layer">层级名</param>
        void RemoveBuffsByLayer(object target, string layer);

        #endregion

        #region Buff查询操作

        /// <summary>
        ///     检查目标对象是否拥有指定 ID 的 Buff
        /// </summary>
        /// <param name="target">目标对象</param>
        /// <param name="buffID">Buff 的 ID</param>
        /// <returns>拥有返回 true，否则返回 false</returns>
        bool ContainsBuff(object target, string buffID);

        /// <summary>
        ///     获取目标对象上指定 ID 的 Buff
        /// </summary>
        /// <param name="target">目标对象</param>
        /// <param name="buffID">Buff 的 ID</param>
        /// <returns>Buff 实例，不存在返回 null</returns>
        Buff GetBuff(object target, string buffID);

        /// <summary>
        ///     获取目标对象上的所有 Buff
        /// </summary>
        /// <param name="target">目标对象</param>
        /// <returns>Buff 列表</returns>
        List<Buff> GetTargetBuffs(object target);

        /// <summary>
        ///     根据标签获取 Buff 列表
        /// </summary>
        /// <param name="target">目标对象</param>
        /// <param name="tag">标签名</param>
        /// <returns>匹配的 Buff 列表</returns>
        List<Buff> GetBuffsByTag(object target, string tag);

        /// <summary>
        ///     根据层级获取 Buff 列表
        /// </summary>
        /// <param name="target">目标对象</param>
        /// <param name="layer">层级名</param>
        /// <returns>匹配的 Buff 列表</returns>
        List<Buff> GetBuffsByLayer(object target, string layer);

        #endregion
    }
}