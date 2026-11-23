using UnityEngine;

namespace Game.Scripts.Inventory.External.Data
{
    /// <summary>
    /// Предмет в инвентаре: ключ и иконка. Создаётся при подборе (InventoryPickup) и при загрузке сохранения
    /// (иконка - из каталога предметов). Объекта на сцене у него нет.
    /// </summary>
    public class InventoryItemData : IInventoryObject
    {
        public string ObjectKey { get; }
        public Sprite ObjectIcon { get; }

        public InventoryItemData(string objectKey, Sprite objectIcon)
        {
            ObjectKey = objectKey;
            ObjectIcon = objectIcon;
        }

        public void InventoryObjectAction()
        {
        }
    }
}
