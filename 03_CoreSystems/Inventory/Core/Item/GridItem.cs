using System.Collections.Generic;
using System.Linq;

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
        /// 物品默认在网格中的宽度（用于矩形形状）
        /// </summary>
        public int GridWidth { get; set; } = 1;

        /// <summary>
        /// 物品默认在网格中的高度（用于矩形形状）
        /// </summary>
        public int GridHeight { get; set; } = 1;

        /// <summary>
        /// 自定义形状的单元格坐标列表（相对于左上角原点）
        /// 如果为 null 或空，则使用 GridWidth × GridHeight 的矩形形状
        /// </summary>
        public List<(int x, int y)> CustomShape { get; set; } = null;

        /// <summary>
        /// 是否可以旋转
        /// </summary>
        public bool CanRotate { get; set; } = false;

        /// <summary>
        /// 当前旋转角度
        /// </summary>
        public RotationAngle Rotation { get; set; } = RotationAngle.Rotate0;

        /// <summary>
        /// 获取实际占用的单元格坐标列表（考虑旋转）
        /// </summary>
        public List<(int x, int y)> GetOccupiedCells()
        {
            // 如果有自定义形状，使用自定义形状
            if (CustomShape != null && CustomShape.Count > 0)
            {
                return RotateShape(CustomShape, Rotation);
            }

            // 否则使用矩形形状
            var cells = new List<(int x, int y)>();
            int width = ActualWidth;
            int height = ActualHeight;

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
        /// 旋转形状坐标
        /// </summary>
        private List<(int x, int y)> RotateShape(List<(int x, int y)> shape, RotationAngle angle)
        {
            if (angle == RotationAngle.Rotate0)
                return new List<(int x, int y)>(shape);

            // 计算形状的边界
            int minX = shape.Min(p => p.x);
            int maxX = shape.Max(p => p.x);
            int minY = shape.Min(p => p.y);
            int maxY = shape.Max(p => p.y);
            int shapeWidth = maxX - minX + 1;
            int shapeHeight = maxY - minY + 1;

            var rotated = new List<(int x, int y)>();

            foreach (var (x, y) in shape)
            {
                // 先归一化到原点
                int normX = x - minX;
                int normY = y - minY;

                // 旋转
                int rotX, rotY;
                switch (angle)
                {
                    case RotationAngle.Rotate90:
                        rotX = shapeHeight - 1 - normY;
                        rotY = normX;
                        break;
                    case RotationAngle.Rotate180:
                        rotX = shapeWidth - 1 - normX;
                        rotY = shapeHeight - 1 - normY;
                        break;
                    case RotationAngle.Rotate270:
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
                if (CustomShape != null && CustomShape.Count > 0)
                {
                    var cells = GetOccupiedCells();
                    return cells.Max(c => c.x) + 1;
                }

                return Rotation switch
                {
                    RotationAngle.Rotate0 => GridWidth,
                    RotationAngle.Rotate90 => GridHeight,
                    RotationAngle.Rotate180 => GridWidth,
                    RotationAngle.Rotate270 => GridHeight,
                    _ => GridWidth
                };
            }
        }

        /// <summary>
        /// 获取当前实际占用的高度（考虑旋转）
        /// </summary>
        public int ActualHeight
        {
            get
            {
                if (CustomShape != null && CustomShape.Count > 0)
                {
                    var cells = GetOccupiedCells();
                    return cells.Max(c => c.y) + 1;
                }

                return Rotation switch
                {
                    RotationAngle.Rotate0 => GridHeight,
                    RotationAngle.Rotate90 => GridWidth,
                    RotationAngle.Rotate180 => GridHeight,
                    RotationAngle.Rotate270 => GridWidth,
                    _ => GridHeight
                };
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
        /// 旋转物品（顺时针旋转90度）
        /// </summary>
        /// <returns>是否成功旋转</returns>
        public bool Rotate()
        {
            if (!CanRotate) return false;

            Rotation = Rotation switch
            {
                RotationAngle.Rotate0 => RotationAngle.Rotate90,
                RotationAngle.Rotate90 => RotationAngle.Rotate180,
                RotationAngle.Rotate180 => RotationAngle.Rotate270,
                RotationAngle.Rotate270 => RotationAngle.Rotate0,
                _ => RotationAngle.Rotate0
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
            if (!CanRotate && angle != RotationAngle.Rotate0) return false;
            Rotation = angle;
            return true;
        }

        /// <summary>
        /// 克隆网格物品
        /// </summary>
        public new GridItem Clone()
        {
            var clone = new GridItem
            {
                ID = this.ID,
                Name = this.Name,
                Type = this.Type,
                Description = this.Description,
                Weight = this.Weight,
                IsStackable = this.IsStackable,
                MaxStackCount = this.MaxStackCount,
                IsContainerItem = this.IsContainerItem,
                ContainerIds = this.ContainerIds != null ? new List<string>(this.ContainerIds) : null,
                GridWidth = this.GridWidth,
                GridHeight = this.GridHeight,
                CustomShape = this.CustomShape != null ? new List<(int x, int y)>(this.CustomShape) : null,
                CanRotate = this.CanRotate,
                Rotation = this.Rotation,
                Attributes = new Dictionary<string, object>(this.Attributes)
            };
            return clone;
        }

        IItem IItem.Clone() => Clone();
    }
}
