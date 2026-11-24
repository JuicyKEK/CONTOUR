using R3;
using UnityEngine;

namespace Game.Scripts.InteractionObjects.Interfaces
{
    public interface IInteractionDoor : IRestorableDoor
    {
        public void Lock();
        public ReadOnlyReactiveProperty<bool> IsOpen { get; }
        public ReadOnlyReactiveProperty<bool> IsLocked { get; }
        public ReadOnlyReactiveProperty<DoorOpenerKind> LastOpener { get; }
        public Subject<Unit> IsTryingOpenLockedDoor { get; }

        /// <summary>
        /// Принудительно пытается ОТКРЫТЬ дверь (в отличие от Interact() у игрока -
        /// никогда не закрывает уже открытую дверь). Используется NPC: если дверь
        /// заперта - возвращает false и поднимает IsTryingOpenLockedDoor (визуальная
        /// "дверь не поддаётся"), если не заперта - открывает и возвращает true.
        /// </summary>
        public bool TryOpenBy(DoorOpenerKind opener);

        /// <summary>
        /// Распахнуть дверь с разбега: в неё вбегает игрок или монстр в погоне - нажимать не нужно. Створка
        /// распахивается быстрее обычного (настройки "С разбега" у вида двери) и уходит от того, кто вбегает:
        /// если он с той стороны, куда дверь обычно открывается, - открывается в обратную сторону.
        /// Заперта - как <see cref="TryOpenBy"/>: дверь дёргается, возвращается false. Уже открытую не трогает.
        /// </summary>
        /// <param name="openerPosition">Где сейчас тот, кто вбегает.</param>
        public bool TryRushOpen(DoorOpenerKind opener, Vector3 openerPosition);

        /// <summary>
        /// Дверь открыта с разбега - вид двери берёт для этого открытия быстрые настройки "С разбега".
        /// </summary>
        public bool IsRushOpen { get; }

        /// <summary>
        /// Створка открыта в обратную сторону (её распахнули с разбега с той стороны, куда она обычно открывается).
        /// </summary>
        public bool IsOpenReversed { get; }

        /// <summary>
        /// Как <see cref="IRestorableDoor.RestoreState"/>, но ещё и в какую сторону открыта створка.
        /// </summary>
        public void RestoreState(bool isOpen, bool isLocked, bool isOpenReversed);
    }
}