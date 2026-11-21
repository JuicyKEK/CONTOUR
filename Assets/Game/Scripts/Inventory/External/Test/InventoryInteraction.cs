using System;
using Game.Scripts.Inventory.Controllers.Interfaces;
using JuicyDI.Attributes;
using R3;
using UnityEngine;

namespace Game.Scripts.Inventory.External
{
    [JDIMonoController]
    public class InventoryInteraction : MonoBehaviour, IInteraction, IInventoryObject, IPickupObject
    {
        public event Action PickedUp;
        public Subject<Unit> IsPlaySound => m_IsPlaySound;
        public string ObjectKey => m_ObjectName;
        public Sprite ObjectIcon => m_ObjectIcon;
        /// <summary>
        /// Название предмета для игрока (подпись при осмотре). Не задано - ключ предмета.
        /// </summary>
        public string DisplayName => string.IsNullOrEmpty(m_DisplayName) ? m_ObjectName : m_DisplayName;

        [Inject] private IInventoryAdd m_Inventory; //?? надо придумать как нормально инжектить
        // Осмотр при первом подборе предмета этого типа (ItemInspectionController в префабе игрока).
        [Inject] private IItemInspection m_Inspection;

        [SerializeField] private string m_ObjectName = "TestInteraction";
        [SerializeField] private Sprite m_ObjectIcon;
        [Tooltip("Название предмета для игрока - подпись под предметом при первом осмотре. Пусто - показывается ключ предмета.")]
        [SerializeField] private string m_DisplayName;

        public Subject<Unit> m_IsPlaySound = new Subject<Unit>();

        public void InventoryObjectAction()
        {
            Debug.Log("TestInteraction");
        }

        public void Interact()
        {
            m_IsPlaySound.OnNext(Unit.Default);

            // Первый подбор предмета этого типа: предмет осматривают, в инвентарь он попадает, когда долетит до
            // панели инвентаря. Сам объект на сцене прячется сразу - на время осмотра его заменяет копия перед камерой.
            if (m_Inspection != null && m_Inspection.TryInspect(this, DisplayName, gameObject, OnInspectionFinished))
            {
                PickedUp?.Invoke();
                gameObject.SetActive(false);
                return;
            }

            m_Inventory.AddItem(this);
            PickedUp?.Invoke();
            Destroy(gameObject);
        }

        private void OnInspectionFinished()
        {
            m_Inventory.AddItem(this);

            if (this != null)
            {
                Destroy(gameObject);
            }
        }
    }
}