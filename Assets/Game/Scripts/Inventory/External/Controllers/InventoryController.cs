using Game.Scripts.InputController;
using Game.Scripts.Inventory.Controllers.Interfaces;
using Game.Scripts.Inventory.External.Controllers.Interfaces;
using Game.Scripts.Inventory.Runtime.Data;
using JuicyDI;
using JuicyDI.Attributes;
using UnityEngine;

namespace Game.Scripts.Inventory.Controllers
{
    [JDIMonoController]
    [SequenceParticipant(100)]
    public class InventoryController : MonoBehaviour, IInventoryAdd, IInventoryGetObject, ISequence
    {
        [Inject] private IInputSelectionActions m_inputSelectionActions;
        
        [SerializeField] private InventoryViewController m_InventoryViewController;
        
        private IInventoryDataController m_InventoryDataController = new InventoryDataController();
        private int m_CurrentSellectObject;
        
        public void MethodInit()
        {
            
        }

        public void MethodStart()
        {
            m_inputSelectionActions.AddPressKeyboardNumbersAction(TrySelectedInventoryObject);
            m_inputSelectionActions.AddScrollMouseAction(TryScrollInventoryObjects);
        }
        
        public void AddItem(IInventoryObject item)
        {
            m_InventoryDataController.AddItemToInventory(item);
            m_InventoryViewController.UpdateInventoryView(m_InventoryDataController.GetInventoryDataObjects());

            if (m_InventoryDataController.GetInventoryDataObjects().InventoryObjects.Count == 1)
            {
                TrySelectedInventoryObject(0);
            }
        }

        public void SelectedInventoryObject(int selectedIndex)
        {    
            if (selectedIndex > m_InventoryDataController.GetInventoryDataObjects().InventoryObjects.Count)
            {
                return;
            }

            m_InventoryViewController.UpdateInventorySelectedView(selectedIndex);
        }

        public IInventoryObject GetInventoryObject(int itemId)
        {
            return m_InventoryDataController.PeekInventoryObject(itemId);
        }

        public IInventoryObject GetInventorySelectedObject()
        {
            if (m_InventoryDataController.GetInventoryDataObjects().InventoryObjects == null ||
                m_InventoryDataController.GetInventoryDataObjects().InventoryObjects.Count == 0)
            {
                return null;
            }
            
            return GetInventoryObject(m_CurrentSellectObject);
        }

        public void DeleteSelectedInventoryObject()
        {
            m_InventoryDataController.DeleteInventoryObject(m_CurrentSellectObject);
            
            if (m_InventoryDataController.GetInventoryDataObjects().InventoryObjects.Count > 0)
            {
                TrySelectedInventoryObject(1);
            }
            
            m_InventoryViewController.UpdateInventoryView(m_InventoryDataController.GetInventoryDataObjects());
        }

        private void TrySelectedInventoryObject(int selectedIndex)
        {
            if (selectedIndex < m_InventoryDataController.GetInventoryDataObjects().InventoryObjects.Count &&
                m_InventoryDataController.GetInventoryDataObjects().InventoryObjects.Count > 0)
            {
                m_CurrentSellectObject = selectedIndex;
                SelectedInventoryObject(m_CurrentSellectObject);
            }
        }
        
        private void TryScrollInventoryObjects(float scroll)
        {
            if (m_InventoryDataController.GetInventoryDataObjects() == null ||
                m_InventoryDataController.GetInventoryDataObjects().InventoryObjects == null)
            {
                return;
            }
            
            if (scroll != 0 && m_InventoryDataController.GetInventoryDataObjects().InventoryObjects.Count > 0)
            {
                ChangeCurrentSelectObject(scroll < 0 ? -1 : 1);
            }
        }

        private void ChangeCurrentSelectObject(int newSelectObject)
        {
            m_CurrentSellectObject += newSelectObject;

            if (m_CurrentSellectObject < 0)
            {
                m_CurrentSellectObject = m_InventoryDataController.GetInventoryDataObjects().InventoryObjects.Count - 1;
            }
            
            if (m_CurrentSellectObject >= m_InventoryDataController.GetInventoryDataObjects().InventoryObjects.Count)
            {
                m_CurrentSellectObject = 0;
            }
            
            SelectedInventoryObject(m_CurrentSellectObject);
        }
    }
}