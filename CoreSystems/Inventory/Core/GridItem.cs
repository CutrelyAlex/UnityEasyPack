using System.Collections.Generic;

namespace EasyPack
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
        /// 物品默认在网格中的宽度
        /// </summary>
        public int GridWidth { get; set; } = 1;

        /// <summary>
        /// 物品默认在网格中的高度
        /// </summary>
        public int GridHeight { get; set; } = 1;

        /// <summary>
        /// 是否可以旋转
        /// </summary>
        public bool CanRotate { get; set; } = false;

        /// <summary>
        /// 当前旋转角度
        /// </summary>
        public RotationAngle Rotation { get; set; } = RotationAngle.Rotate0;

        /// <summary>
        /// 获取当前实际占用的宽度（考虑旋转）
        /// </summary>
        public int ActualWidth
        {
            get
            {
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
                IsContanierItem = this.IsContanierItem,
                ContainerIds = this.ContainerIds != null ? new List<string>(this.ContainerIds) : null,
                GridWidth = this.GridWidth,
                GridHeight = this.GridHeight,
                CanRotate = this.CanRotate,
                Rotation = this.Rotation,
                Attributes = new Dictionary<string, object>(this.Attributes)
            };
            return clone;
        }

        IItem IItem.Clone() => Clone();
    }
}
