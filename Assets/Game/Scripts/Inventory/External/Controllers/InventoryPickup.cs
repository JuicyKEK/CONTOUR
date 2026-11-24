using Game.Scripts.Inventory.Controllers.Interfaces;
using Game.Scripts.Inventory.External.Data;
using Game.Scripts.Pickup.Controllers;
using JuicyDI.Attributes;
using UnityEngine;

namespace Game.Scripts.Inventory.Controllers
{
    /// <summary>
    /// Предмет инвентаря на сцене (спрей, молоток). При подборе в инвентарь кладутся его данные - ключ предмета и ключ
    /// иконки в Addressables (InventoryItemData), а не сам объект. Убирает объект со сцены и звучит вид на нём
    /// (PickupHideView, PickupSoundView).
    /// </summary>
    [JDIMonoController]
    public class InventoryPickup : PickupObject
    {
        public string ObjectKey => m_ObjectName;
        public string IconAddress => m_IconAddress;

        [Inject] private IInventoryAdd m_Inventory;

        [Tooltip("Ключ предмета: по нему инвентарь складывает одинаковые предметы, а двери и зоны проверяют, что в руках.")]
        [SerializeField] private string m_ObjectName = "TestInteraction";
        [Tooltip("Ключ (адрес) иконки предмета в Addressables - группа InventoryIcons.")]
        [SerializeField] private string m_IconAddress;

        protected override bool TryReceive()
        {
            if (m_Inventory == null)
            {
                return false;
            }

            m_Inventory.AddItem(new InventoryItemData(m_ObjectName, m_IconAddress));
            return true;
        }
    }
}
