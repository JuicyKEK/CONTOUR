using UnityEngine;

namespace Game.Scripts.Inventory.External.Data
{
    /// <summary>
    /// Предмет инвентаря, восстановленный из сохранения. Объекта на сцене у него нет (подобранный
    /// предмет уничтожается), поэтому это просто ключ и иконка из каталога предметов.
    /// </summary>
    public class InventorySavedObject : IInventoryObject
    {
        public string ObjectKey { get; }
        public Sprite ObjectIcon { get; }

        public InventorySavedObject(string objectKey, Sprite objectIcon)
        {
            ObjectKey = objectKey;
            ObjectIcon = objectIcon;
        }

        public void InventoryObjectAction()
        {
        }
    }
}
