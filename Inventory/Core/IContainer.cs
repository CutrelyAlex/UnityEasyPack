using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;


namespace EasyPack
{
    public interface IContainer
    {
        string ID { get; }
        string Name { get; }
        string Type { get; }
        // IReadOnlyList<ISlot> Slots { get; }
        bool AddItem(IItem item, int slotIndex = -1);
        bool RemoveItem(string itemId, int count = 1);
        bool ContainsItem(string itemId);

    }
}