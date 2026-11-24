using System.Collections.Generic;

namespace Game.Scripts.Inventory.External.Controllers.Interfaces
{
    /// <summary>
    /// Содержимое инвентаря целиком - для сохранения и загрузки (InventorySaveController).
    /// </summary>
    public interface IInventoryContent
    {
        /// <summary>
        /// Ячейки инвентаря по порядку: предмет ячейки (ключ, ключ иконки) и сколько таких предметов в ячейке.
        /// </summary>
        void GetItems(List<KeyValuePair<IInventoryObject, int>> result);

        /// <summary>
        /// Убирает из инвентаря все предметы.
        /// </summary>
        void ClearItems();
    }
}
