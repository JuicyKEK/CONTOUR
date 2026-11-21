using UnityEngine;

namespace Game.Scripts.ItemInspection
{
    /// <summary>
    /// Компонент на том же объекте, что и InspectOnFirstPickup, который говорит, куда улетает предмет после осмотра
    /// (например, AudioTapeInspectionInfo: кассета улетает в кассетный плеер). Нет такого компонента - предмет улетает
    /// туда, куда по умолчанию (Fly Target у ItemInspectionController - панель инвентаря).
    /// </summary>
    public interface IItemInspectionFlyTargetSource
    {
        /// <summary>
        /// Куда улетает предмет: объект на сцене (летит к его позиции) или элемент интерфейса (RectTransform).
        /// null - туда, куда по умолчанию.
        /// </summary>
        Transform GetFlyTarget();
    }
}
