using System.Collections.Generic;

namespace Game.Scripts.Inventory
{
    /// <summary>
    /// Ключи предметов, которые игрок уже осматривал (<see cref="IItemInspection"/>). Сохраняется вместе
    /// с инвентарём (InventorySaveController), чтобы после загрузки повторный подбор того же предмета не
    /// показывал осмотр снова.
    /// </summary>
    public interface IItemInspectionRegistry
    {
        bool WasInspected(string itemKey);

        void MarkInspected(string itemKey);

        /// <summary>
        /// Заполняет список всеми осмотренными ключами (список предварительно очищается).
        /// </summary>
        void GetInspectedKeys(List<string> result);

        void ClearInspected();
    }
}
