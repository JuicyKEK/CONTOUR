using Game.Scripts.Inventory;
using R3;
using UnityEngine;

namespace Game.Scripts.Pickup.Controllers
{
    /// <summary>
    /// Подбираемый предмет: игрок взаимодействует - предмет попадает туда, куда решает наследник (TryReceive: инвентарь,
    /// кассеты), и IsPickedUp становится true. Как предмет исчезает и звучит, решают виды на том же объекте
    /// (PickupHideView, PickupSoundView), подписанные на IsPickedUp; что он подобран, запоминает PickupStateSaver.
    /// </summary>
    public abstract class PickupObject : MonoBehaviour, IInteraction, IPickupObject
    {
        public ReadOnlyReactiveProperty<bool> IsPickedUp => m_IsPickedUp;

        private readonly ReactiveProperty<bool> m_IsPickedUp = new(false);

        public virtual void Interact()
        {
            if (m_IsPickedUp.Value || !TryReceive())
            {
                return;
            }

            m_IsPickedUp.Value = true;
        }

        /// <summary>
        /// Отдать предмет туда, куда он подбирается. false - подобрать не получилось, предмет остаётся на месте.
        /// </summary>
        protected abstract bool TryReceive();
    }
}
