using System;
using EasyPack.CustomData;
using System.Collections.Generic;
using System.Linq;
using static EasyPack.InventorySystem.RotationAngle;

namespace EasyPack.InventorySystem
{
    /// <summary>
    /// 旋转角度枚举
    /// </summary>
    public enum RotationAngle
    {
        Rotate0 = 0,
        Rotate90 = 1,
        Rotate180 = 2,
        Rotate270 = 3
    }

    /// <summary>
    /// 网格物品 - 在网格容器中占用多个格子的物品
    /// </summary>
    public class GridItem : Item, IItem
    {
        /// <summary>
        /// 形状的单元格坐标列表（相对于左上角原点）
        /// 默认为 1x1 单格物品
        /// </summary>
        public List<(int x, int y)> Shape { get; set; } = new List<(int x, int y)> { (0, 0) };

        /// <summary>
        /// 是否可以旋转
        /// </summary>
        public bool CanRotate { get; set; } = false;

        /// <summary>
        /// 当前旋转角度
        /// </summary>
        public RotationAngle Rotation { get; set; } = Rotate0;

        /// <summary>
        /// 获取实际占用的单元格坐标列表（考虑旋转）
        /// </summary>
        public List<(int x, int y)> GetOccupiedCells()
        {
            if (Shape == null || Shape.Count == 0)
                return new List<(int x, int y)> { (0, 0) };

            return RotateShape(Shape, Rotation);
        }

        /// <summary>
        /// 旋转形状坐标
        /// </summary>
        private List<(int x, int y)> RotateShape(List<(int x, int y)> shape, RotationAngle angle)
        {
            if (angle == Rotate0)
                return new List<(int x, int y)>(shape);

            // 计算形状的边界
            int minX = shape.Min(p => p.x);
            int maxX = shape.Max(p => p.x);
            int minY = shape.Min(p => p.y);
            int maxY = shape.Max(p => p.y);
            int shapeWidth = maxX - minX + 1;
            int shapeHeight = maxY - minY + 1;

            var rotated = new List<(int x, int y)>();

            foreach ((int x, int y) in shape)
            {
                // 先归一化到原点
                int normX = x - minX;
                int normY = y - minY;

                // 旋转
                int rotX, rotY;
                switch (angle)
                {
                    case Rotate90:
                        rotX = shapeHeight - 1 - normY;
                        rotY = normX;
                        break;
                    case Rotate180:
                        rotX = shapeWidth - 1 - normX;
                        rotY = shapeHeight - 1 - normY;
                        break;
                    case Rotate270:
                        rotX = normY;
                        rotY = shapeWidth - 1 - normX;
                        break;
                    default:
                        rotX = normX;
                        rotY = normY;
                        break;
                }

                rotated.Add((rotX, rotY));
            }

            return rotated;
        }

        /// <summary>
        /// 获取当前实际占用的宽度（考虑旋转）
        /// </summary>
        public int ActualWidth
        {
            get
            {
                var cells = GetOccupiedCells();
                if (cells.Count == 0) return 1;
                return cells.Max(c => c.x) + 1;
            }
        }

        /// <summary>
        /// 获取当前实际占用的高度（考虑旋转）
        /// </summary>
        public int ActualHeight
        {
            get
            {
                var cells = GetOccupiedCells();
                if (cells.Count == 0) return 1;
                return cells.Max(c => c.y) + 1;
            }
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        public GridItem()
        {
            // 网格物品通常不可堆叠
            IsStackable = false;
            MaxStackCount = 1;
        }

        /// <summary>
        /// 创建矩形形状的单元格列表
        /// </summary>
        /// <param name="width">宽度</param>
        /// <param name="height">高度</param>
        /// <returns>矩形形状的单元格坐标列表</returns>
        public static List<(int x, int y)> CreateRectangleShape(int width, int height)
        {
            var cells = new List<(int x, int y)>();
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    cells.Add((x, y));
                }
            }
            return cells;
        }

        /// <summary>
        /// 旋转物品（顺时针旋转90度）
        /// </summary>
        /// <returns>是否成功旋转</returns>
        public bool Rotate()
        {
            if (!CanRotate) return false;

            Rotation = Rotation switch
            {
                Rotate0 => Rotate90,
                Rotate90 => Rotate180,
                Rotate180 => Rotate270,
                Rotate270 => Rotate0,
                _ => throw new ArgumentOutOfRangeException(nameof(Rotation), Rotation, null)
            };

            return true;
        }

        /// <summary>
        /// 设置旋转角度
        /// </summary>
        /// <param name="angle">目标旋转角度</param>
        /// <returns>是否成功设置</returns>
        public bool SetRotation(RotationAngle angle)
        {
            if (!CanRotate && angle != Rotate0) return false;
            Rotation = angle;
            return true;
        }

        /// <summary>
        /// 克隆网格物品
        /// </summary>
        private new GridItem Clone()
        {
            var clone = new GridItem
            {
                ID = ID,
                Name = Name,
                Type = Type,
                Description = Description,
                Weight = Weight,
                IsStackable = IsStackable,
                MaxStackCount = MaxStackCount,
                IsContainerItem = IsContainerItem,
                ContainerIds = ContainerIds != null ? new List<string>(ContainerIds) : null,
                CustomData = CustomDataUtility.Clone(CustomData),
                Shape = Shape != null ? new List<(int x, int y)>(Shape) : new List<(int x, int y)> { (0, 0) },
                CanRotate = CanRotate,
                Rotation = Rotation
            };
            return clone;
        }

        IItem IItem.Clone() => Clone();
    }
}