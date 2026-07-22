using System.Collections.Generic;

namespace Game.Scripts.Inventory.Runtime.Data.Interfaces
{
    public interface IInventoryData
    {
        List<Queue<IInventoryObject>> InventoryObjects { get; set; }
    }
}