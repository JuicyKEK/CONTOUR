using System.Collections.Generic;
using Game.Scripts.Inventory.Runtime.Data;
using Game.Scripts.Inventory.Runtime.Data.Interfaces;
using UnityEngine;

namespace Game.Scripts.Inventory.Controllers
{
    public class InventoryDataController : IInventoryDataController
    {
        private IInventoryData m_InventoryDataObjects = new InventoryData();
        
        public void AddItemToInventory(IInventoryObject item)
        {
            if (m_InventoryDataObjects.InventoryObjects == null)
            {
                m_InventoryDataObjects.InventoryObjects = new List<Queue<IInventoryObject>>();
            }
            
            for (int i = 0; i < m_InventoryDataObjects.InventoryObjects.Count; i++)
            {
                if (m_InventoryDataObjects.InventoryObjects[i].Peek().ObjectKey == item.ObjectKey)
                {
                    AddExistingItem(item, i);
                    return;
                }
            }

            AddNewItem(item);
        }
        
        public IInventoryData GetInventoryDataObjects()
        {
            return m_InventoryDataObjects;
        }

        public IInventoryObject PeekInventoryObject(int indexObject)
        {
            if (indexObject > m_InventoryDataObjects.InventoryObjects.Count)
            {
                return null;
            }
            
            return m_InventoryDataObjects.InventoryObjects[indexObject].Peek();
        }

        public void DeleteInventoryObject(int indexObject)
        {
            if (indexObject > m_InventoryDataObjects.InventoryObjects.Count)
            {
                return;
            }

            m_InventoryDataObjects.InventoryObjects[indexObject].Dequeue();

            if (m_InventoryDataObjects.InventoryObjects[indexObject].Count == 0)
            {
                m_InventoryDataObjects.InventoryObjects.RemoveAt(indexObject);
            }
            
            //LogDataObjects();
        }

        private void AddNewItem(IInventoryObject item)
        {
            var queue = new Queue<IInventoryObject>();
            queue.Enqueue(item);
            
            m_InventoryDataObjects.InventoryObjects.Add(queue);
        }
        
        private void AddExistingItem(IInventoryObject item, int index)
        {
            m_InventoryDataObjects.InventoryObjects[index].Enqueue(item);
        }
        
        
        private void LogDataObjects()
        {
            for (int i = 0; i < m_InventoryDataObjects.InventoryObjects.Count; i++)
            {
                Debug.Log($"{m_InventoryDataObjects.InventoryObjects[i].Peek().ObjectKey} - {m_InventoryDataObjects.InventoryObjects[i].Count}");
                foreach (var item in m_InventoryDataObjects.InventoryObjects[i])
                {
                    Debug.Log(item);
                }
            }
        }
    }
}