using Game.Scripts.Inventory.External.Controllers.Interfaces;
using JuicyDI.Attributes;
using UnityEngine;

namespace Game.Scripts.Inventory.External
{
    [JDIMonoController]
    public class TestDoor : MonoBehaviour, IInteraction
    {
        [SerializeField] private string m_ObjectKeyKey = "TestInteraction2";
        
        [Inject] private IInventoryGetObject m_Inventory;

        public void Interact()
        {
            if (m_Inventory.GetInventorySelectedObject() != null && m_Inventory.GetInventorySelectedObject().ObjectKey == m_ObjectKeyKey)
            {
                m_Inventory.DeleteSelectedInventoryObject();
                OpenDoor();
            }
        }

        private void OpenDoor()
        {
            Debug.Log("Open door");
        }
    }
}