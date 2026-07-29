using R3;

namespace Game.Scripts.InteractionObjects.Interfaces
{
    public interface IInteractionDoorOpenEvent
    {
        public Subject<Unit> IsOpenDoorStart { get; }
        public Subject<Unit> IsOpenDoorEnd { get; }
    }
}