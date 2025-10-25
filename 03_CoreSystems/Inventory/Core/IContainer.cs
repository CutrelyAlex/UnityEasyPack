using System;
using System.Collections.Generic;

namespace EasyPack
{
    /// <summary>
    /// 容器接口，定义标准化的容器操作
    /// </summary>
    public interface IContainer
    {
        #region 基本信息
        /// <summary>
        /// 容器唯一标识符
        /// </summary>
        string ID { get; }

        /// <summary>
        /// 容器名称
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 容器类型
        /// </summary>
        string Type { get; }

        /// <summary>
        /// 容器容量（槽位数量），-1 表示无限容量
        /// </summary>
        int Capacity { get; }

        /// <summary>
        /// 已使用的槽位数量
        /// </summary>
        int UsedSlots { get; }

        /// <summary>
        /// 剩余空闲槽位数量
        /// </summary>
        int FreeSlots { get; }

        /// <summary>
        /// 是否为网格容器
        /// </summary>
        bool IsGrid { get; }

        /// <summary>
        /// 容器条件列表
        /// </summary>
        List<IItemCondition> ContainerCondition { get; }

        /// <summary>
        /// 所有槽位的只读视图
        /// </summary>
        IReadOnlyList<ISlot> Slots { get; }
        #endregion

        #region 物品操作
        /// <summary>
        /// 添加物品到容器
        /// </summary>
        /// <param name="item">要添加的物品</param>
        /// <param name="count">添加数量</param>
        /// <param name="slotIndex">指定槽位索引，-1 表示自动查找</param>
        /// <returns>操作结果和实际添加数量</returns>
        (AddItemResult result, int actualCount) AddItems(IItem item, int count = 1, int slotIndex = -1);

        /// <summary>
        /// 从容器移除物品
        /// </summary>
        /// <param name="itemId">物品ID</param>
        /// <param name="count">移除数量</param>
        /// <returns>操作结果和实际移除数量</returns>
        (RemoveItemResult result, int actualCount) RemoveItems(string itemId, int count = 1);

        /// <summary>
        /// 检查容器是否包含指定物品
        /// </summary>
        /// <param name="itemId">物品ID</param>
        /// <returns>是否包含</returns>
        bool HasItem(string itemId);

        /// <summary>
        /// 获取指定物品的总数量
        /// </summary>
        /// <param name="itemId">物品ID</param>
        /// <returns>物品总数量</returns>
        int GetItemTotalCount(string itemId);

        /// <summary>
        /// 获取指定物品的所有槽位
        /// </summary>
        /// <param name="itemId">物品ID</param>
        /// <returns>包含该物品的槽位列表</returns>
        List<ISlot> GetItemSlots(string itemId);
        #endregion

        #region 槽位操作
        /// <summary>
        /// 获取指定索引的槽位
        /// </summary>
        /// <param name="index">槽位索引</param>
        /// <returns>槽位对象，索引无效返回 null</returns>
        ISlot GetSlot(int index);

        /// <summary>
        /// 获取所有被占用的槽位
        /// </summary>
        /// <returns>被占用的槽位列表</returns>
        List<ISlot> GetOccupiedSlots();

        /// <summary>
        /// 获取所有空闲槽位
        /// </summary>
        /// <returns>空闲槽位列表</returns>
        List<ISlot> GetFreeSlots();

        /// <summary>
        /// 清空指定槽位
        /// </summary>
        /// <param name="slotIndex">槽位索引</param>
        /// <returns>是否成功清空</returns>
        bool ClearSlot(int slotIndex);

        /// <summary>
        /// 清空所有槽位
        /// </summary>
        void ClearAllSlots();
        #endregion

        #region 条件过滤
        /// <summary>
        /// 根据条件获取符合的槽位
        /// </summary>
        /// <param name="condition">过滤条件</param>
        /// <returns>符合条件的槽位列表</returns>
        List<ISlot> GetSlotsByCondition(IItemCondition condition);

        /// <summary>
        /// 检查物品是否满足容器条件
        /// </summary>
        /// <param name="item">要检查的物品</param>
        /// <returns>是否满足条件</returns>
        bool CheckContainerCondition(IItem item);
        #endregion

        #region 容器管理
        /// <summary>
        /// 验证容器缓存一致性
        /// </summary>
        /// <returns>是否一致</returns>
        bool ValidateCaches();

        /// <summary>
        /// 重建容器缓存
        /// </summary>
        void RebuildCaches();
        #endregion

        #region 事件
        /// <summary>
        /// 添加物品操作结果事件
        /// </summary>
        event Action<IItem, int, int, AddItemResult, List<int>> OnItemAddResult;

        /// <summary>
        /// 移除物品操作结果事件
        /// </summary>
        event Action<string, int, int, RemoveItemResult, List<int>> OnItemRemoveResult;

        /// <summary>
        /// 槽位数量变更事件
        /// </summary>
        event Action<int, IItem, int, int> OnSlotCountChanged;
        #endregion
    }
}
