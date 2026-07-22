using System;
using System.Collections.Generic;
using Game.Scripts.Inventory.Runtime.Data.Interfaces;
using UnityEditor;

namespace Game.Scripts.Inventory.Controllers
{
    public interface IInventoryDataController
    {
        void AddItemToInventory(IInventoryObject item);
        IInventoryData GetInventoryDataObjects();
        IInventoryObject PeekInventoryObject(int indexObject);
        void DeleteInventoryObject(int indexObject);
    }
}