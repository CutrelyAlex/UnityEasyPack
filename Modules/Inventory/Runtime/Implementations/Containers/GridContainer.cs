using System;
using System.Linq;
using System.Text;
using UnityEngine;

namespace EasyPack.InventorySystem
{
    /// <summary>
    ///     网格容器 - 支持二维网格布局和网格物品放置
    /// </summary>
    public class GridContainer : Container
    {
        #region 基本属性

        /// <summary>
        ///     标识该容器为网格容器
        /// </summary>
        public override bool IsGrid => true;

        /// <summary>
        ///     返回网格的尺寸（宽, 高）
        /// </summary>
        public override Vector2 Grid => new(GridWidth, GridHeight);

        /// <summary>
        ///     网格宽度（列数）
        /// </summary>
        public int GridWidth { get; private set; }

        /// <summary>
        ///     网格高度（行数）
        /// </summary>
        public int GridHeight { get; private set; }

        #endregion

        #region 内部类

        /// <summary>
        ///     占位符物品 - 用于标记网格物品占用的额外格子
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

            public new virtual IItem Clone() => new GridOccupiedMarker { MainSlotIndex = MainSlotIndex };
        }

        #endregion

        #region 构造函数

        /// <summary>
        ///     构造函数
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
        ///     初始化网格槽位
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
        ///     将二维坐标转换为一维索引
        /// </summary>
        /// <param name="x">X坐标（列）</param>
        /// <param name="y">Y坐标（行）</param>
        /// <returns>一维索引</returns>
        public int CoordToIndex(int x, int y)
        {
            if (x < 0 || x >= GridWidth || y < 0 || y >= GridHeight)
            {
                return -1;
            }

            return y * GridWidth + x;
        }

        /// <summary>
        ///     将一维索引转换为二维坐标
        /// </summary>
        /// <param name="index">一维索引</param>
        /// <returns>(x, y) 坐标</returns>
        public (int x, int y) IndexToCoord(int index)
        {
            if (index < 0 || index >= Capacity)
            {
                return (-1, -1);
            }

            return (index % GridWidth, index / GridWidth);
        }

        #endregion

        #region 碰撞检测

        /// <summary>
        ///     检查指定区域是否可以放置物品
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
            {
                return false;
            }

            // 检查区域内所有槽位是否可用
            for (int dy = 0; dy < height; dy++)
            for (int dx = 0; dx < width; dx++)
            {
                int checkIndex = CoordToIndex(x + dx, y + dy);
                if (checkIndex == excludeIndex) continue;

                ISlot slot = Slots[checkIndex];
                if (slot.IsOccupied)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        ///     检查指定位置是否可以放置网格物品（支持任意形状）
        /// </summary>
        /// <param name="gridItem">要放置的网格物品</param>
        /// <param name="slotIndex">放置的起始槽位索引</param>
        /// <param name="excludeIndex">排除的槽位索引（用于移动物品时）</param>
        /// <returns>是否可以放置</returns>
        public bool CanPlaceGridItem(GridItem gridItem, int slotIndex, int excludeIndex = -1)
        {
            (int startX, int startY) = IndexToCoord(slotIndex);
            var occupiedCells = gridItem.GetOccupiedCells();

            foreach ((int dx, int dy) in occupiedCells)
            {
                int x = startX + dx;
                int y = startY + dy;

                // 检查是否超出边界
                if (x < 0 || x >= GridWidth || y < 0 || y >= GridHeight)
                {
                    return false;
                }

                int checkIndex = CoordToIndex(x, y);
                if (checkIndex == excludeIndex) continue;

                ISlot slot = _slots[checkIndex];

                // 检查槽位是否已被占用
                if (slot.IsOccupied)
                {
                    return false;
                }
            }

            return true;
        }

        #endregion

        #region 添加物品

        /// <summary>
        ///     在指定网格位置添加物品
        /// </summary>
        /// <param name="item">物品</param>
        /// <param name="x">X坐标</param>
        /// <param name="y">Y坐标</param>
        /// <returns>添加结果和实际添加数量</returns>
        public (AddItemResult result, int actualCount) AddItemAt(IItem item, int x, int y)
        {
            if (item == null)
            {
                return (AddItemResult.ItemIsNull, 0);
            }

            int slotIndex = CoordToIndex(x, y);
            return slotIndex < 0 ? (AddItemResult.SlotNotFound, 0) : AddItems(item, slotIndex: slotIndex);
        }

        /// <summary>
        ///     重写添加物品方法以支持网格物品
        /// </summary>
        public override (AddItemResult result, int actualCount) AddItems(IItem item, int slotIndex = -1, bool autoStack = true)
        {
            if (item == null)
            {
                return (AddItemResult.ItemIsNull, 0);
            }

            if (item.Count <= 0)
            {
                return (AddItemResult.InvalidCount, 0);
            }

            // 检查容器条件
            if (!ValidateItemCondition(item))
            {
                return (AddItemResult.ItemConditionNotMet, 0);
            }

            // 如果是网格物品，使用特殊逻辑
            if (item is GridItem gridItem) return AddGridItem(gridItem, slotIndex);

            // 普通物品使用基类逻辑
            return base.AddItems(item, slotIndex, autoStack);
        }

        /// <summary>
        ///     添加网格物品的专用方法
        /// </summary>
        private (AddItemResult result, int actualCount) AddGridItem(GridItem gridItem, int slotIndex)
        {
            // 如果是可堆叠的网格物品，尝试堆叠
            if (gridItem.IsStackable && slotIndex < 0)
            {
                // 尝试找到相同的可堆叠网格物品
                var existingSlots = FindSlotIndices(gridItem.ID);
                foreach (var existingSlotIndex in existingSlots)
                {
                    var existingSlot = _slots[existingSlotIndex];
                    if (existingSlot.Item is GridItem existingGridItem && 
                        gridItem.CanStack(existingGridItem))
                    {
                        // 堆叠到现有槽位
                        int oldCount = existingGridItem.Count;
                        int newCount = oldCount + gridItem.Count;
                        
                        // 检查最大堆叠数量
                        if (gridItem.MaxStackCount > 0 && newCount > gridItem.MaxStackCount)
                        {
                            // 如果超过最大堆叠数，填满当前槽位，剩余部分继续寻找或创建新槽位
                            int canAdd = gridItem.MaxStackCount - oldCount;
                            if (canAdd > 0)
                            {
                                existingGridItem.Count = gridItem.MaxStackCount;
                                // 更新数量缓存
                                _cacheService.UpdateItemCountCache(gridItem.ID, canAdd);
                                OnSlotQuantityChanged(existingSlotIndex, existingGridItem, oldCount, gridItem.MaxStackCount);
                                
                                int remaining = gridItem.Count - canAdd;
                                if (remaining > 0)
                                {
                                    // 创建新的物品实例处理剩余部分
                                    var remainingItem = ((IItem)gridItem).Clone() as GridItem;
                                    if (remainingItem == null)
                                    {
                                        Debug.LogError("GridItem.Clone() 发生未知错误");
                                        return (AddItemResult.ItemConditionNotMet, canAdd);
                                    }
                                    remainingItem.Count = remaining;
                                    var (remainResult, remainAdded) = AddGridItem(remainingItem, -1);
                                    
                                    TriggerItemTotalCountChanged(gridItem.ID, gridItem);
                                    return (remainResult, canAdd + remainAdded);
                                }
                                
                                TriggerItemTotalCountChanged(gridItem.ID, gridItem);
                                return (AddItemResult.Success, canAdd);
                            }
                            continue; // 当前槽位已满，继续查找
                        }
                        
                        // 没有超过最大堆叠数，直接合并
                        existingGridItem.Count = newCount;
                        // 更新数量缓存
                        _cacheService.UpdateItemCountCache(gridItem.ID, gridItem.Count);
                        OnSlotQuantityChanged(existingSlotIndex, existingGridItem, oldCount, newCount);
                        TriggerItemTotalCountChanged(gridItem.ID, gridItem);
                        return (AddItemResult.Success, gridItem.Count);
                    }
                }
            }

            // 无法堆叠或不可堆叠，创建新槽位
            // 如果指定了槽位，检查该位置是否可以放置
            if (slotIndex >= 0)
            {
                if (!CanPlaceGridItem(gridItem, slotIndex))
                {
                    return (AddItemResult.NoSuitableSlotFound, 0);
                }

                // 放置物品并占用空间
                PlaceGridItem(gridItem, slotIndex);
                return (AddItemResult.Success, gridItem.Count);
            }

            // 自动寻找可放置位置
            int targetIndex = FindPlacementPosition(gridItem);
            if (targetIndex < 0)
            {
                return (AddItemResult.ContainerIsFull, 0);
            }

            PlaceGridItem(gridItem, targetIndex);
            return (AddItemResult.Success, gridItem.Count);
        }

        /// <summary>
        ///     寻找可以放置网格物品的位置（支持任意形状）
        /// </summary>
        private int FindPlacementPosition(GridItem gridItem)
        {
            var occupiedCells = gridItem.GetOccupiedCells();
            if (occupiedCells.Count == 0)
            {
                return -1;
            }

            // 计算物品的边界框
            int maxDx = occupiedCells.Max(c => c.x);
            int maxDy = occupiedCells.Max(c => c.y);

            // 从左上到右下扫描
            for (int y = 0; y <= GridHeight - maxDy - 1; y++)
            for (int x = 0; x <= GridWidth - maxDx - 1; x++)
            {
                int slotIndex = CoordToIndex(x, y);
                if (CanPlaceGridItem(gridItem, slotIndex)) return slotIndex;
            }

            return -1;
        }

        /// <summary>
        ///     在指定位置放置网格物品
        /// </summary>
        private void PlaceGridItem(GridItem gridItem, int slotIndex)
        {
            (int startX, int startY) = IndexToCoord(slotIndex);
            var occupiedCells = gridItem.GetOccupiedCells();

            // 获取主槽位索引（第一个占用单元格的索引）
            (int firstDx, int firstDy) = occupiedCells[0];
            int mainSlotIndex = CoordToIndex(startX + firstDx, startY + firstDy);

            // 遍历所有占用的单元格
            bool isFirst = true;
            foreach ((int dx, int dy) in occupiedCells)
            {
                int currentIndex = CoordToIndex(startX + dx, startY + dy);
                ISlot currentSlot = _slots[currentIndex];

                if (isFirst)
                {
                    // 第一个单元格放置实际物品
                    currentSlot.SetItem(gridItem);

                    // 更新主槽位缓存
                    _cacheService.UpdateEmptySlotCache(currentIndex, false);
                    _cacheService.UpdateItemSlotIndexCache(gridItem.ID, currentIndex, true);
                    _cacheService.UpdateItemTypeCache(gridItem.Type, currentIndex, true);
                    _cacheService.UpdateItemCountCache(gridItem.ID, gridItem.Count);

                    isFirst = false;
                }
                else
                {
                    // 其他单元格放置占位符，指向主槽位
                    var marker = new GridOccupiedMarker { MainSlotIndex = mainSlotIndex };
                    currentSlot.SetItem(marker);

                    // 更新空槽位缓存（占位符槽位也被占用）
                    _cacheService.UpdateEmptySlotCache(currentIndex, false);
                }
            }

            // 触发槽位变更事件
            OnSlotQuantityChanged(mainSlotIndex, gridItem, 0, gridItem.Count);

            // 触发总数变更事件
            TriggerItemTotalCountChanged(gridItem.ID, gridItem);
        }

        #endregion

        #region 移除物品

        /// <summary>
        ///     重写移除物品方法以支持网格物品
        /// </summary>
        public override RemoveItemResult RemoveItem(string itemId, int count = 1)
        {
            // 先找到物品所在的槽位
            var slotIndices = FindSlotIndices(itemId);
            if (slotIndices == null || slotIndices.Count == 0)
            {
                return RemoveItemResult.ItemNotFound;
            }

            int slotIndex = slotIndices[0];
            ISlot slot = _slots[slotIndex];

            // 如果是网格物品，清理所有占用的槽位
            if (slot.Item is GridItem gridItem)
            {
                (RemoveItemResult result, _) = RemoveGridItem(gridItem, slotIndex, count);
                return result;
            }

            // 普通物品使用基类逻辑
            return base.RemoveItem(itemId, count);
        }

        /// <summary>
        ///     移除网格物品的专用方法
        /// </summary>
        private (RemoveItemResult result, int removedCount) RemoveGridItem(GridItem gridItem, int mainSlotIndex,
                                                                           int count)
        {
            if (count != 1)
            {
                return (RemoveItemResult.InsufficientQuantity, 0);
            }

            string itemId = gridItem.ID;
            string itemType = gridItem.Type;
            var occupiedCells = gridItem.GetOccupiedCells();

            // 计算起始位置（锚点）
            (int mainX, int mainY) = IndexToCoord(mainSlotIndex);
            (int firstDx, int firstDy) = occupiedCells[0];
            int startX = mainX - firstDx;
            int startY = mainY - firstDy;

            // 清理所有占用的槽位
            foreach ((int dx, int dy) in occupiedCells)
            {
                int occupiedIndex = CoordToIndex(startX + dx, startY + dy);
                ISlot occupiedSlot = _slots[occupiedIndex];
                occupiedSlot.ClearSlot();

                // 更新空槽位缓存
                _cacheService.UpdateEmptySlotCache(occupiedIndex, true);
            }

            // 更新缓存（使用主槽位索引）
            _cacheService.UpdateItemSlotIndexCache(itemId, mainSlotIndex, false);
            _cacheService.UpdateItemTypeCache(itemType, mainSlotIndex, false);
            _cacheService.UpdateItemCountCache(itemId, -1);

            // 触发槽位变更事件
            OnSlotQuantityChanged(mainSlotIndex, gridItem, 1, 0);

            // 触发总数变更事件
            TriggerItemTotalCountChanged(itemId);

            return (RemoveItemResult.Success, 1);
        }

        #endregion

        #region 查询操作

        /// <summary>
        ///     获取指定位置的主物品（如果是占位符则返回主物品）
        /// </summary>
        /// <param name="x">X坐标</param>
        /// <param name="y">Y坐标</param>
        /// <returns>主物品，如果槽位为空则返回null</returns>
        public IItem GetItemAt(int x, int y)
        {
            int index = CoordToIndex(x, y);
            if (index < 0) return null;

            ISlot slot = Slots[index];
            if (!slot.IsOccupied) return null;

            // 如果是占位符，返回主物品
            if (slot.Item is GridOccupiedMarker marker) return Slots[marker.MainSlotIndex].Item;

            return slot.Item;
        }

        #endregion

        #region 物品操作

        /// <summary>
        ///     尝试旋转指定位置的物品
        /// </summary>
        /// <param name="x">X坐标</param>
        /// <param name="y">Y坐标</param>
        /// <returns>是否成功旋转</returns>
        public bool TryRotateItemAt(int x, int y)
        {
            int index = CoordToIndex(x, y);
            if (index < 0) return false;

            ISlot slot = Slots[index];
            if (!slot.IsOccupied) return false;

            // 如果是占位符，找到主物品
            int mainSlotIndex = index;
            if (slot.Item is GridOccupiedMarker marker)
            {
                mainSlotIndex = marker.MainSlotIndex;
                slot = Slots[mainSlotIndex];
            }

            if (slot.Item is not GridItem { CanRotate: true } gridItem)
            {
                return false;
            }

            // 计算当前的起始位置（锚点）
            (int mainX, int mainY) = IndexToCoord(mainSlotIndex);
            var occupiedCellsBefore = gridItem.GetOccupiedCells();
            (int firstDx, int firstDy) = occupiedCellsBefore[0];
            int startX = mainX - firstDx;
            int startY = mainY - firstDy;
            int startIndex = CoordToIndex(startX, startY);

            // 保存原始旋转状态
            RotationAngle originalRotation = gridItem.Rotation;

            // 先清理当前占用（在旋转之前）
            RemoveGridItem(gridItem, mainSlotIndex, 1);

            // 旋转物品
            gridItem.Rotate();

            // 检查旋转后是否还能放置
            bool canPlace = CanPlaceGridItem(gridItem, startIndex);

            if (canPlace)
            {
                // 可以放置，重新放置旋转后的物品
                PlaceGridItem(gridItem, startIndex);
                return true;
            }

            // 不能放置，还原旋转并放回原位
            gridItem.Rotation = originalRotation;
            PlaceGridItem(gridItem, startIndex);
            return false;
        }

        #endregion

        #region 调试工具

        /// <summary>
        ///     获取网格的可视化表示（用于调试）
        /// </summary>
        /// <returns>网格状态字符串</returns>
        public string GetGridVisualization()
        {
            var lines = new StringBuilder();
            lines.AppendLine($"GridContainer [{GridWidth}x{GridHeight}]:");

            for (int y = 0; y < GridHeight; y++)
            {
                for (int x = 0; x < GridWidth; x++)
                {
                    int index = CoordToIndex(x, y);
                    ISlot slot = Slots[index];

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