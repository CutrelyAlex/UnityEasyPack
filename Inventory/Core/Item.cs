using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;

namespace EasyPack
{
    public class Item : IItem
    {
        #region ��������
        public string ID { get; set; }

        public string Name { get; set; }

        public string Type { get; set; } = "Default";
        public string Description { get; set; } = "";

        public float Weight { get; set; } = 1;

        public bool IsStackable { get; set; } = true;

        public int MaxStackCount { get; set; } = -1; // -1��ʾ���޶ѵ�

        public Dictionary<string, object> Attributes { get; set; } = new Dictionary<string, object>();


        public bool isContanierItem = false;
        public List<IContainer> Containers { get; set; } // Ƕ�׵�����

        #endregion

        #region �����Ʒ

        public bool IsMultiSlot { get; set; } = false;
        public Vector2Int Size { get; set; } = new Vector2Int(1, 1);

        #endregion

        /// <summary>
        /// ������Ʒ�����
        /// </summary>
        /// <returns>��Ʒ�ĸ���</returns>
        public IItem Clone()
        {
            var clone = new Item
            {
                ID = this.ID,
                Name = this.Name,
                Type = this.Type,
                Description = this.Description,
                Weight = this.Weight,
                IsStackable = this.IsStackable,
                MaxStackCount = this.MaxStackCount,
                IsMultiSlot = this.IsMultiSlot,
                Size = this.Size,
                isContanierItem = this.isContanierItem
            };

            // ��ȸ��������ֵ�
            if (this.Attributes != null)
            {
                clone.Attributes = new Dictionary<string, object>();
                foreach (var kvp in this.Attributes)
                {
                    clone.Attributes[kvp.Key] = kvp.Value;
                }
            }

            // ��ȸ��������б�����еĻ���
            if (this.Containers != null && this.Containers.Count > 0)
            {
                clone.Containers = new List<IContainer>(this.Containers);
            }

            return clone;
        }
    }
}