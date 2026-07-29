using R3;

namespace Game.Scripts.InteractionObjects.Interfaces
{
    public interface IInteractionDoorTryOpenEvent
    {
        public Subject<Unit> IsTryOpenDoorStart { get; }
        public Subject<Unit> IsTryOpenDoorEnd { get; }
    }
}