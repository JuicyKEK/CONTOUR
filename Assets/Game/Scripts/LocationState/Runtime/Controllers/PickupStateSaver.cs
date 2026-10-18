using System;
using Game.Scripts.Inventory;
using JuicyDI.Attributes;
using UnityEngine;

namespace Game.Scripts.LocationState.Runtime.Controllers
{
    /// <summary>
    /// Сохраняет, подобран ли объект (кассета, предмет инвентаря). Вешается на объект с <see cref="IPickupObject"/>
    /// (AudioTapePickup, InventoryInteraction) - обычно прямо в префаб предмета.
    ///
    /// В момент подбора объект сообщает об этом (IPickupObject.PickedUp) до того, как удалит или выключит себя, -
    /// состояние запоминается сразу. После загрузки локации подобранный объект удаляется со сцены: именно
    /// удаляется, а не выключается, чтобы его снова не включил кто-то другой (например, InfectionRangeObject).
    /// </summary>
    [JDIMonoController]
    public class PickupStateSaver : LocationStateSaver
    {
        [Serializable]
        private class State
        {
            public bool IsPickedUp;
        }

        private IPickupObject m_Pickup;
        private bool m_IsPickedUp;

        public bool IsPickedUp => m_IsPickedUp;

        private void Awake()
        {
            m_Pickup = GetComponent<IPickupObject>();

            if (m_Pickup == null)
            {
                Debug.LogError($"[LocationState] PickupStateSaver '{name}': на объекте нет подбираемого компонента " +
                               "(IPickupObject: AudioTapePickup, InventoryInteraction).", this);
                return;
            }

            m_Pickup.PickedUp += OnPickedUp;
        }

        protected override void OnDestroy()
        {
            if (m_Pickup != null)
            {
                m_Pickup.PickedUp -= OnPickedUp;
            }

            base.OnDestroy();
        }

        public override string CaptureState()
        {
            return m_IsPickedUp ? JsonUtility.ToJson(new State { IsPickedUp = true }) : null;
        }

        public override void ApplySavedState(string state)
        {
            if (!JsonUtility.FromJson<State>(state).IsPickedUp)
            {
                return;
            }

            m_IsPickedUp = true;

            // Выключаем сразу: удаление происходит только в конце кадра.
            gameObject.SetActive(false);
            Destroy(gameObject);
        }

        private void OnPickedUp()
        {
            m_IsPickedUp = true;
            StoreStateNow();
        }
    }
}
