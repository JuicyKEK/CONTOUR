using R3;

namespace Game.Scripts.Pickup
{
    /// <summary>
    /// Объект, который игрок подбирает (предмет инвентаря, кассета). IsPickedUp становится true в момент подбора - на это
    /// подписаны виды на том же объекте (PickupHideView, PickupSoundView) и сохранение (PickupStateSaver).
    /// </summary>
    public interface IPickupObject
    {
        ReadOnlyReactiveProperty<bool> IsPickedUp { get; }
    }
}
