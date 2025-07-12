using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace EasyPack
{
    public interface IItem
    {
        string ID { get; }
        string Name { get; }
        string Type { get; } 
        string Description { get; }
        float Weight { get; }
        bool IsStackable { get; }
        int MaxStackCount { get; }
        bool IsMultiSlot { get; }
        Vector2Int Size { get; }
        Dictionary<string, object> Attributes { get; set; }
        IItem Clone();
    }
}