using System.Collections.Generic;
using Game.Scripts.Inventory.Runtime.Data.Interfaces;
using Game.Scripts.Inventory.Runtime.View;
using UnityEngine;

namespace Game.Scripts.Inventory.Controllers
{
    public class InventoryViewController : MonoBehaviour
    {
        [SerializeField] private InventoryItemView m_InventoryItemViewPrefab;
        [SerializeField] private Transform m_InventoryItemTransform;
        
        private List<InventoryItemView> m_InventoryItemViews = new List<InventoryItemView>();
        
        public void UpdateInventoryView(IInventoryData inventoryData)
        {
            DisableAllInventoryView();

            for (int i = 0; i < inventoryData.InventoryObjects.Count; i++)
            {
                var view = GetItemView(i);
                view.Init(inventoryData.InventoryObjects[i].Peek().ObjectIcon, 
                    inventoryData.InventoryObjects[i].Count);
            }
        }
        
        public void UpdateInventorySelectedView(int selectedIndex)
        {
            DisableSelectedAllInventoryView();
            
            GetItemView(selectedIndex).SetSelected(true);
        }

        private void DisableAllInventoryView()
        {
            for (int i = 0; i < m_InventoryItemViews.Count; i++)
            {
                m_InventoryItemViews[i].gameObject.SetActive(false);
            }
        }
        
        private void DisableSelectedAllInventoryView()
        {
            for (int i = 0; i < m_InventoryItemViews.Count; i++)
            {
                m_InventoryItemViews[i].SetSelected(false);
            }
        }

        private InventoryItemView GetItemView(int index)
        {
            if (index + 1 > m_InventoryItemViews.Count)
            {
                m_InventoryItemViews.Add(Instantiate(m_InventoryItemViewPrefab, m_InventoryItemTransform));
            }
            
            return m_InventoryItemViews[index];
        }
    }
}