using System;

namespace Game.Scripts.Inventory
{
    /// <summary>
    /// Объект, который игрок подбирает (кассета, предмет инвентаря). Сообщает о подборе до того, как уберёт
    /// себя со сцены, - по этому событию запоминается, что объект подобран (PickupStateSaver), и после
    /// загрузки локации он больше не появляется.
    /// </summary>
    public interface IPickupObject
    {
        /// <summary>
        /// Игрок подобрал объект. Вызывается до того, как объект удалит или выключит себя.
        /// </summary>
        event Action PickedUp;
    }
}
