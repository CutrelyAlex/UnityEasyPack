using System;
using System.Collections.Generic;
using EasyPack.ENekoFramework;
using EasyPack.Modifiers;

namespace EasyPack.GamePropertySystem
{
    /// <summary>
    ///     游戏属性管理器接口
    ///     提供属性注册、查询、分类管理和批量操作功能
    /// </summary>
    public interface IGamePropertyService : IService
    {
        #region 注册API

        /// <summary>
        ///     注册单个属性到指定分类
        /// </summary>
        /// <param name="property">要注册的属性</param>
        /// <param name="category">分类名（支持层级，如"Character.Base"）</param>
        /// <param name="metadata">可选的元数据（显示名、描述、标签等）</param>
        /// <exception cref="ArgumentException">属性ID已存在时抛出</exception>
        /// <exception cref="InvalidOperationException">服务未就绪时抛出</exception>
        void Register(GameProperty property, string category = "Default", PropertyData metadata = null);

        /// <summary>
        ///     批量注册属性到指定分类
        /// </summary>
        /// <param name="properties">属性集合</param>
        /// <param name="category">分类名</param>
        void RegisterRange(IEnumerable<GameProperty> properties, string category = "Default");

        #endregion

        #region 查询API

        /// <summary>
        ///     通过ID获取属性
        /// </summary>
        /// <param name="id">属性ID</param>
        /// <returns>属性实例，不存在则返回null</returns>
        GameProperty Get(string id);

        /// <summary>
        ///     通过 UID 获取属性
        /// </summary>
        /// <param name="uid">属性 UID</param>
        /// <returns>属性实例，不存在则返回 null</returns>
        GameProperty GetByUid(long uid);

        /// <summary>
        ///     获取指定分类的所有属性
        /// </summary>
        /// <param name="category">分类名</param>
        /// <param name="includeChildren">是否包含子分类（支持通配符"Category.*"）</param>
        /// <returns>属性集合</returns>
        IEnumerable<GameProperty> GetByCategory(string category, bool includeChildren = false);

        /// <summary>
        ///     获取包含指定标签的所有属性
        /// </summary>
        /// <param name="tag">标签名</param>
        /// <returns>属性集合</returns>
        IEnumerable<GameProperty> GetByTag(string tag);

        /// <summary>
        ///     组合查询：获取同时满足分类和标签条件的属性（交集）
        /// </summary>
        /// <param name="category">分类名</param>
        /// <param name="tag">标签名</param>
        /// <returns>属性集合</returns>
        IEnumerable<GameProperty> GetByCategoryAndTag(string category, string tag);

        /// <summary>
        ///     获取属性的元数据
        /// </summary>
        /// <param name="id">属性ID</param>
        /// <returns>元数据实例，不存在则返回null</returns>
        PropertyData GetMetadata(string id);

        /// <summary>
        ///     获取所有已注册的属性ID
        /// </summary>
        /// <returns>属性ID集合</returns>
        IEnumerable<string> GetAllPropertyIds();

        /// <summary>
        ///     获取所有分类名
        /// </summary>
        /// <returns>分类名集合</returns>
        IEnumerable<string> GetAllCategories();

        #endregion

        #region 移除API

        /// <summary>
        ///     移除指定属性
        /// </summary>
        /// <param name="id">属性ID</param>
        /// <returns>是否成功移除（false表示属性不存在）</returns>
        bool Unregister(string id);

        /// <summary>
        ///     通过 UID 移除指定属性
        /// </summary>
        /// <param name="uid">属性 UID</param>
        /// <returns>是否成功移除（false表示属性不存在）</returns>
        bool UnregisterByUid(long uid);

        /// <summary>
        ///     通过 UID 将属性移动到新的分类
        /// </summary>
        /// <param name="uid">属性 UID</param>
        /// <param name="newCategory">新的分类名</param>
        /// <returns>是否成功移动</returns>
        bool MoveToCategoryByUid(long uid, string newCategory);

        /// <summary>
        ///     移除整个分类及其所有属性
        /// </summary>
        /// <param name="category">分类名</param>
        void UnregisterCategory(string category);

        #endregion

        #region 批量操作API

        /// <summary>
        ///     设置分类中所有属性的激活状态
        /// </summary>
        /// <param name="category">分类名</param>
        /// <param name="active">激活状态</param>
        /// <returns>操作结果（包含成功数量和失败详情）</returns>
        OperationResult<List<string>> SetCategoryActive(string category, bool active);

        /// <summary>
        ///     为分类中所有属性应用修饰符
        /// </summary>
        /// <param name="category">分类名</param>
        /// <param name="modifier">修饰符实例</param>
        /// <returns>操作结果（包含成功应用的属性ID列表）</returns>
        OperationResult<List<string>> ApplyModifierToCategory(string category, IModifier modifier);

        #endregion
    }
}