using System.Collections;
using System.Collections.Generic;


namespace EasyPack
{
    public interface IItem
    {
        string ID { get; }
        string Name { get; }
        string Description { get; }
        float Weight { get; }
        bool IsStackable { get; }
        int MaxStackCount { get; }
        Dictionary<string, object> CustomAttributes { get; set; }
    }
}