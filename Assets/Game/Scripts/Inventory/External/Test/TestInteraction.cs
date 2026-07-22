using Game.Scripts.Inventory.Controllers.Interfaces;
using JuicyDI.Attributes;
using UnityEngine;

namespace Game.Scripts.Inventory.External
{
    [JDIMonoController]
    public class TestInteraction : MonoBehaviour, IInteraction, IInventoryObject
    {
        [Inject] private IInventoryAdd m_Inventory; //? надо придумать как нормально инжектить
        
        [SerializeField] private string m_ObjectName = "TestInteraction";
        [SerializeField] private Sprite m_ObjectIcon;
        
        public string ObjectKey => m_ObjectName;
        public Sprite ObjectIcon => m_ObjectIcon;
        
        public void InventoryObjectAction()
        {
            Debug.Log("TestInteraction");
        }

        public void Interact()
        {
            m_Inventory.AddItem(this);
            Destroy(gameObject);
        }
    }
}