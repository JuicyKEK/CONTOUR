using UnityEngine;

namespace Game.Scripts.Inventory
{
    public interface IInventoryObject
    {
        string ObjectKey { get; }
        Sprite ObjectIcon { get; }
        void InventoryObjectAction();
    }
}