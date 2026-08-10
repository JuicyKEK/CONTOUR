using Game.Scripts.Audio.Interfaces;
using Game.Scripts.Inventory.Controllers.Interfaces;
using JuicyDI.Attributes;
using R3;
using UnityEngine;

namespace Game.Scripts.Inventory.External
{
    [JDIMonoController]
    public class InventoryInteraction : MonoBehaviour, IInteraction, IInventoryObject, ISoundPlay
    {
        public Subject<Unit> IsPlaySound => m_IsPlaySound;
        public string ObjectKey => m_ObjectName;
        public Sprite ObjectIcon => m_ObjectIcon;
        
        [Inject] private IInventoryAdd m_Inventory; //? надо придумать как нормально инжектить
        
        [SerializeField] private string m_ObjectName = "TestInteraction";
        [SerializeField] private Sprite m_ObjectIcon;

        public Subject<Unit> m_IsPlaySound = new Subject<Unit>(); 
        
        public void InventoryObjectAction()
        {
            Debug.Log("TestInteraction");
        }

        public void Interact()
        {
            m_IsPlaySound.OnNext(Unit.Default);
            m_Inventory.AddItem(this);
            Destroy(gameObject);
        }
    }
}