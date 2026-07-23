using R3;

namespace Game.Scripts.InteractionObjects.Interfaces
{
    public interface IInteractionSimpleDoor
    {
        public ReadOnlyReactiveProperty<bool> IsOpen { get; }
    }
}