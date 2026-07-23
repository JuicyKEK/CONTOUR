using R3;

namespace Game.Scripts.InteractionObjects.Interfaces
{
    public interface IInteractionDoor
    {
        public void Lock();
        public ReadOnlyReactiveProperty<bool> IsOpen { get; }
        public ReadOnlyReactiveProperty<bool> IsLocked { get; }
        public Subject<Unit> IsTryingOpenLockedDoor { get; }
    }
}