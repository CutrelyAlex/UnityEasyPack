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

        public string Description { get; set; } = "";

        public float Weight { get; set; } = 1;

        public bool IsStackable { get; set; } = true;

        public int MaxStackCount { get; set; } = -1; // -1��ʾ���޶ѵ�

        public Dictionary<string, object> CustomAttributes { get; set; } = new Dictionary<string, object>();
        

        public bool isContanierItem = false;
        public List<IContainer> Containers { get; set; } // Ƕ�׵�����

        #endregion

        #region �����Ʒ

        public bool isMultiSlot = false;
        public Vector2Int Size = new Vector2Int(1, 1);

        #endregion
    }
}
