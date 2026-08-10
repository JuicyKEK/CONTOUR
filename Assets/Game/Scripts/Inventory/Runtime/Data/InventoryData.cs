using System.Collections.Generic;
using Game.Scripts.Inventory.Runtime.Data.Interfaces;

namespace Game.Scripts.Inventory.Runtime.Data
{
    public class InventoryData : IInventoryData
    {
        public List<Queue<IInventoryObject>> InventoryObjects { get; set; }
    }
}