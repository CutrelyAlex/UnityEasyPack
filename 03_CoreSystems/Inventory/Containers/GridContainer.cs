using System;
using UnityEngine;

namespace EasyPack.InventorySystem
{
    /// <summary>
    /// 网格容器 - 支持二维网格布局和网格物品放置
    /// </summary>
    public class GridContainer : Container
    {
        #region 基本属性

        /// <summary>
        /// 标识该容器为网格容器
        /// </summary>
        public override bool IsGrid => true;

        /// <summary>
        /// 返回网格的尺寸（宽, 高）
        /// </summary>
        public override Vector2 Grid => new(GridWidth, GridHeight);

        /// <summary>
        /// 网格宽度（列数）
        /// </summary>
        public int GridWidth { get; private set; }

        /// <summary>
        /// 网格高度（行数）
        /// </summary>
        public int GridHeight { get; private set; }

        #endregion

        #region 内部类

        /// <summary>
        /// 占位符物品 - 用于标记网格物品占用的额外格子
        /// </summary>
        private class GridOccupiedMarker : Item
        {
            public int MainSlotIndex { get; set; } // 指向主物品所在槽位

            public GridOccupiedMarker()
            {
                ID = "__GRID_OCCUPIED__";
                Name = "Occupied";
                IsStackable = false;
            }

            public new virtual IItem Clone()
            {
                return new GridOccupiedMarker { MainSlotIndex = this.MainSlotIndex };
            }
        }

        #endregion

        #region 构造函数

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="id">容器ID</param>
        /// <param name="name">容器名称</param>
        /// <param name="type">容器类型</param>
        /// <param name="gridWidth">网格宽度</param>
        /// <param name="gridHeight">网格高度</param>
        public GridContainer(string id, string name, string type, int gridWidth, int gridHeight)
            : base(id, name, type, gridWidth * gridHeight) // 总容量 = 宽 * 高
        {
            if (gridWidth <= 0) throw new ArgumentException("Grid width must be positive", nameof(gridWidth));
            if (gridHeight <= 0) throw new ArgumentException("Grid height must be positive", nameof(gridHeight));

            GridWidth = gridWidth;
            GridHeight = gridHeight;

            // 初始化所有槽位
            InitializeSlots();
            RebuildCaches();
        }

        /// <summary>
        /// 初始化网格槽位
        /// </summary>
        private void InitializeSlots()
        {
            for (int i = 0; i < Capacity; i++)
            {
                _slots.Add(new Slot { Index = i, Container = this });
            }
        }

        #endregion

        #region 坐标转换

        /// <summary>
        /// 将二维坐标转换为一维索引
        /// </summary>
        /// <param name="x">X坐标（列）</param>
        /// <param name="y">Y坐标（行）</param>
        /// <returns>一维索引</returns>
        public int CoordToIndex(int x, int y)
        {
            if (x < 0 || x >= GridWidth || y < 0 || y >= GridHeight)
                return -1;
            return y * GridWidth + x;
        }

        /// <summary>
        /// 将一维索引转换为二维坐标
        /// </summary>
        /// <param name="index">一维索引</param>
        /// <returns>(x, y) 坐标</returns>
        public (int x, int y) IndexToCoord(int index)
        {
            if (index < 0 || index >= Capacity)
                return (-1, -1);
            return (index % GridWidth, index / GridWidth);
        }

        #endregion

        #region 碰撞检测

        /// <summary>
        /// 检查指定区域是否可以放置物品
        /// </summary>
        /// <param name="x">起始X坐标</param>
        /// <param name="y">起始Y坐标</param>
        /// <param name="width">物品宽度</param>
        /// <param name="height">物品高度</param>
        /// <param name="excludeIndex">排除的槽位索引（用于移动物品时）</param>
        /// <returns>是否可以放置</returns>
        public bool CanPlaceAt(int x, int y, int width, int height, int excludeIndex = -1)
        {
            // 检查是否超出边界
            if (x < 0 || y < 0 || x + width > GridWidth || y + height > GridHeight)
                return false;

            // 检查区域内所有槽位是否可用
            for (int dy = 0; dy < height; dy++)
            {
                for (int dx = 0; dx < width; dx++)
                {
                    int checkIndex = CoordToIndex(x + dx, y + dy);
                    if (checkIndex == excludeIndex) continue;

                    var slot = Slots[checkIndex];
                    if (slot.IsOccupied)
                        return false;
                }
            }

            return true;
        }

        #endregion

        #region 添加物品

        /// <summary>
        /// 在指定网格位置添加物品
        /// </summary>
        /// <param name="item">物品</param>
        /// <param name="x">X坐标</param>
        /// <param name="y">Y坐标</param>
        /// <param name="count">数量（对于网格物品通常为1）</param>
        /// <returns>添加结果和实际添加数量</returns>
        public (AddItemResult result, int actualCount) AddItemAt(IItem item, int x, int y, int count = 1)
        {
            if (item == null)
                return (AddItemResult.ItemIsNull, 0);

            int slotIndex = CoordToIndex(x, y);
            if (slotIndex < 0)
                return (AddItemResult.SlotNotFound, 0);

            return AddItems(item, count, slotIndex);
        }

        /// <summary>
        /// 重写添加物品方法以支持网格物品
        /// </summary>
        public override (AddItemResult result, int actualCount) AddItems(IItem item, int count = 1, int slotIndex = -1)
        {
            if (item == null)
                return (AddItemResult.ItemIsNull, 0);

            if (count <= 0)
                return (AddItemResult.AddNothingLOL, 0);

            // 检查容器条件
            if (!ValidateItemCondition(item))
                return (AddItemResult.ItemConditionNotMet, 0);

            // 如果是网格物品，使用特殊逻辑
            if (item is GridItem gridItem)
            {
                return AddGridItem(gridItem, count, slotIndex);
            }

            // 普通物品使用基类逻辑
            return base.AddItems(item, count, slotIndex);
        }

        /// <summary>
        /// 添加网格物品的专用方法
        /// </summary>
        private (AddItemResult result, int actualCount) AddGridItem(GridItem gridItem, int count, int slotIndex)
        {
            // 网格物品通常不可堆叠
            if (count != 1)
                return (AddItemResult.AddNothingLOL, 0);

            // 如果指定了槽位，检查该位置是否可以放置
            if (slotIndex >= 0)
            {
                var (x, y) = IndexToCoord(slotIndex);
                if (x < 0 || !CanPlaceAt(x, y, gridItem.ActualWidth, gridItem.ActualHeight))
                    return (AddItemResult.NoSuitableSlotFound, 0);

                // 放置物品并占用空间
                PlaceGridItem(gridItem, slotIndex);
                return (AddItemResult.Success, 1);
            }

            // 自动寻找可放置位置
            int targetIndex = FindPlacementPosition(gridItem.ActualWidth, gridItem.ActualHeight);
            if (targetIndex < 0)
                return (AddItemResult.ContainerIsFull, 0);

            PlaceGridItem(gridItem, targetIndex);
            return (AddItemResult.Success, 1);
        }

        /// <summary>
        /// 寻找可以放置指定尺寸物品的位置
        /// </summary>
        /// <param name="width">物品宽度</param>
        /// <param name="height">物品高度</param>
        /// <returns>可放置的槽位索引，如果没有则返回-1</returns>
        private int FindPlacementPosition(int width, int height)
        {
            // 从左上到右下扫描
            for (int y = 0; y <= GridHeight - height; y++)
            {
                for (int x = 0; x <= GridWidth - width; x++)
                {
                    if (CanPlaceAt(x, y, width, height))
                    {
                        return CoordToIndex(x, y);
                    }
                }
            }
            return -1;
        }

        /// <summary>
        /// 在指定位置放置网格物品
        /// </summary>
        private void PlaceGridItem(GridItem gridItem, int slotIndex)
        {
            var (startX, startY) = IndexToCoord(slotIndex);

            // 在主槽位放置实际物品
            var mainSlot = _slots[slotIndex];
            mainSlot.SetItem(gridItem, 1);

            // 更新主槽位缓存
            _cacheService.UpdateEmptySlotCache(slotIndex, false);
            _cacheService.UpdateItemSlotIndexCache(gridItem.ID, slotIndex, true);
            _cacheService.UpdateItemTypeCache(gridItem.Type, slotIndex, true);
            _cacheService.UpdateItemCountCache(gridItem.ID, 1);

            // 在其他占用的槽位放置占位符
            for (int dy = 0; dy < gridItem.ActualHeight; dy++)
            {
                for (int dx = 0; dx < gridItem.ActualWidth; dx++)
                {
                    if (dx == 0 && dy == 0) continue; // 跳过主槽位

                    int occupiedIndex = CoordToIndex(startX + dx, startY + dy);
                    var occupiedSlot = _slots[occupiedIndex];
                    var marker = new GridOccupiedMarker { MainSlotIndex = slotIndex };
                    occupiedSlot.SetItem(marker, 1);

                    // 更新空槽位缓存（占位符槽位也被占用）
                    _cacheService.UpdateEmptySlotCache(occupiedIndex, false);
                }
            }

            // 触发槽位变更事件
            OnSlotQuantityChanged(slotIndex, gridItem, 0, 1);

            // 触发总数变更事件
            TriggerItemTotalCountChanged(gridItem.ID, gridItem);
        }

        #endregion

        #region 移除物品

        /// <summary>
        /// 重写移除物品方法以支持网格物品
        /// </summary>
        public override RemoveItemResult RemoveItem(string itemId, int count = 1)
        {
            // 先找到物品所在的槽位
            var slotIndices = FindSlotIndices(itemId);
            if (slotIndices == null || slotIndices.Count == 0)
                return RemoveItemResult.ItemNotFound;

            int slotIndex = slotIndices[0];
            var slot = _slots[slotIndex];

            // 如果是网格物品，清理所有占用的槽位
            if (slot.Item is GridItem gridItem)
            {
                var (result, _) = RemoveGridItem(gridItem, slotIndex, count);
                return result;
            }

            // 普通物品使用基类逻辑
            return base.RemoveItem(itemId, count);
        }

        /// <summary>
        /// 移除网格物品的专用方法
        /// </summary>
        private (RemoveItemResult result, int removedCount) RemoveGridItem(GridItem gridItem, int slotIndex, int count)
        {
            if (count != 1)
                return (RemoveItemResult.InsufficientQuantity, 0);

            var (startX, startY) = IndexToCoord(slotIndex);
            string itemId = gridItem.ID;
            string itemType = gridItem.Type;

            // 清理所有占用的槽位
            for (int dy = 0; dy < gridItem.ActualHeight; dy++)
            {
                for (int dx = 0; dx < gridItem.ActualWidth; dx++)
                {
                    int occupiedIndex = CoordToIndex(startX + dx, startY + dy);
                    var occupiedSlot = _slots[occupiedIndex];
                    occupiedSlot.ClearSlot();

                    // 更新空槽位缓存
                    _cacheService.UpdateEmptySlotCache(occupiedIndex, true);
                }
            }

            // 更新缓存
            _cacheService.UpdateItemSlotIndexCache(itemId, slotIndex, false);
            _cacheService.UpdateItemTypeCache(itemType, slotIndex, false);
            _cacheService.UpdateItemCountCache(itemId, -1);

            // 触发槽位变更事件
            OnSlotQuantityChanged(slotIndex, gridItem, 1, 0);

            // 触发总数变更事件
            TriggerItemTotalCountChanged(itemId, null);

            return (RemoveItemResult.Success, 1);
        }

        #endregion

        #region 查询操作

        /// <summary>
        /// 获取指定位置的主物品（如果是占位符则返回主物品）
        /// </summary>
        /// <param name="x">X坐标</param>
        /// <param name="y">Y坐标</param>
        /// <returns>主物品，如果槽位为空则返回null</returns>
        public IItem GetItemAt(int x, int y)
        {
            int index = CoordToIndex(x, y);
            if (index < 0) return null;

            var slot = Slots[index];
            if (!slot.IsOccupied) return null;

            // 如果是占位符，返回主物品
            if (slot.Item is GridOccupiedMarker marker)
            {
                return Slots[marker.MainSlotIndex].Item;
            }

            return slot.Item;
        }

        #endregion

        #region 物品操作

        /// <summary>
        /// 尝试旋转指定位置的物品
        /// </summary>
        /// <param name="x">X坐标</param>
        /// <param name="y">Y坐标</param>
        /// <returns>是否成功旋转</returns>
        public bool TryRotateItemAt(int x, int y)
        {
            int index = CoordToIndex(x, y);
            if (index < 0) return false;

            var slot = Slots[index];
            if (!slot.IsOccupied) return false;

            // 如果是占位符，找到主物品
            if (slot.Item is GridOccupiedMarker marker)
            {
                index = marker.MainSlotIndex;
                slot = Slots[index];
            }

            if (slot.Item is not GridItem gridItem || !gridItem.CanRotate)
                return false;

            var (startX, startY) = IndexToCoord(index);

            // 检查旋转后是否还能放置
            int newWidth = gridItem.ActualHeight;  // 旋转后宽高互换
            int newHeight = gridItem.ActualWidth;

            if (!CanPlaceAt(startX, startY, newWidth, newHeight, index))
                return false;

            // 先清理当前占用
            RemoveGridItem(gridItem, index, 1);

            // 旋转
            gridItem.Rotate();

            // 重新放置
            PlaceGridItem(gridItem, index);

            return true;
        }

        #endregion

        #region 调试工具

        /// <summary>
        /// 获取网格的可视化表示（用于调试）
        /// </summary>
        /// <returns>网格状态字符串</returns>
        public string GetGridVisualization()
        {
            var lines = new System.Text.StringBuilder();
            lines.AppendLine($"GridContainer [{GridWidth}x{GridHeight}]:");

            for (int y = 0; y < GridHeight; y++)
            {
                for (int x = 0; x < GridWidth; x++)
                {
                    int index = CoordToIndex(x, y);
                    var slot = Slots[index];

                    if (!slot.IsOccupied)
                    {
                        lines.Append("[ ]");
                    }
                    else if (slot.Item is GridOccupiedMarker)
                    {
                        lines.Append("[X]");
                    }
                    else
                    {
                        lines.Append($"[{slot.Item.ID[0]}]");
                    }
                }
                lines.AppendLine();
            }

            return lines.ToString();
        }

        #endregion
    }
}

