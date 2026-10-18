using R3;

namespace Game.Scripts.InteractionObjects.Interfaces
{
    public interface IInteractionSimpleDoor : IRestorableDoor
    {
        public ReadOnlyReactiveProperty<bool> IsOpen { get; }
    }
}