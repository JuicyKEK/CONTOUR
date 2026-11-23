using System;
using Game.Scripts.Pickup;
using JuicyDI.Attributes;
using R3;
using UnityEngine;

namespace Game.Scripts.LocationState.Runtime.Controllers
{
    /// <summary>
    /// Сохраняет, подобран ли объект (кассета, предмет инвентаря). Вешается на объект с <see cref="IPickupObject"/>
    /// (AudioTapePickup, InventoryPickup) - обычно прямо в префаб предмета.
    ///
    /// Объект подобран (IPickupObject.IsPickedUp стал true) - состояние запоминается сразу. После загрузки локации
    /// подобранный объект удаляется со сцены: именно удаляется, а не выключается, чтобы его снова не включил кто-то
    /// другой (например, InfectionRangeObject).
    /// </summary>
    [JDIMonoController]
    public class PickupStateSaver : LocationStateSaver
    {
        [Serializable]
        private class State
        {
            public bool IsPickedUp;
        }

        private readonly CompositeDisposable m_Disposables = new();
        private bool m_IsPickedUp;

        public bool IsPickedUp => m_IsPickedUp;

        private void Awake()
        {
            if (!TryGetComponent(out IPickupObject pickup))
            {
                Debug.LogError($"[LocationState] PickupStateSaver '{name}': на объекте нет подбираемого компонента " +
                               "(IPickupObject: AudioTapePickup, InventoryPickup).", this);
                return;
            }

            pickup.IsPickedUp
                .Where(isPickedUp => isPickedUp)
                .Take(1)
                .Subscribe(_ => OnPickedUp())
                .AddTo(m_Disposables);
        }

        protected override void OnDestroy()
        {
            m_Disposables.Dispose();
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
