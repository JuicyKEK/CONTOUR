using R3;

namespace Game.Scripts.InteractionObjects.Interfaces
{
    public interface IInteractionDoor
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
    }
}