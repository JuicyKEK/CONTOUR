using Game.Scripts.Audio.Interfaces;
using Game.Scripts.InfectionZone.Runtime.Interfaces;
using Game.Scripts.Inventory;
using Game.Scripts.Inventory.External.Controllers.Interfaces;
using JuicyDI.Attributes;
using R3;
using UnityEngine;

namespace Game.Scripts.InfectionZone.External.Controllers
{
    /// <summary>
    /// Базовый интерактивный объект, меняющий степень заражения зоны предметом из инвентаря
    /// (по аналогии с UseInventoryObject). Срабатывает, только если у игрока выбран предмет
    /// с ключом <see cref="RequiredItemKey"/>: предмет расходуется, заражение зоны меняется
    /// (как именно - решает наследник), HUD показывает переход уровня, проигрывается звук.
    /// Если нужного предмета нет - ничего не происходит.
    ///
    /// Наследники: <see cref="InfectionCleanupObject"/> (очистка), <see cref="InfectionSpreadObject"/> (заражение).
    /// </summary>
    public abstract class InfectionItemInteraction : MonoBehaviour, IInteraction, ISoundPlay
    {
        public Subject<Unit> IsPlaySound => m_IsPlaySound;

        [Header("Ссылка на зону")]
        [Tooltip("Зона, степень заражения которой меняется при взаимодействии.")]
        [SerializeField] private MonoBehaviour m_ZoneSource; // должен реализовывать IInfectionZone

        [Inject] private IInventoryGetObject m_Inventory;
        [Inject] private IInfectionLevelHud m_Hud;

        private readonly Subject<Unit> m_IsPlaySound = new();
        private IInfectionZone m_Zone;

        /// <summary>
        /// Ключ предмета инвентаря, необходимого для взаимодействия.
        /// </summary>
        protected abstract string RequiredItemKey { get; }

        /// <summary>
        /// Меняет заражение зоны (вызывается после того, как предмет израсходован).
        /// </summary>
        protected abstract void ChangeInfection(IInfectionZone zone);

        /// <summary>
        /// Что происходит с самим объектом после успешного использования.
        /// </summary>
        protected abstract void OnUsed();

        protected virtual void Awake()
        {
            m_Zone = m_ZoneSource as IInfectionZone;

            if (m_Zone == null)
            {
                Debug.LogError($"[{GetType().Name}] {name}: m_ZoneSource должен реализовывать IInfectionZone.", this);
            }

            if (string.IsNullOrEmpty(RequiredItemKey))
            {
                Debug.LogError($"[{GetType().Name}] {name}: не задан ключ нужного предмета инвентаря.", this);
            }
        }

        public void Interact()
        {
            if (m_Zone == null)
            {
                return;
            }

            var selectedObject = m_Inventory.GetInventorySelectedObject();

            // Срабатывает, только если у игрока выбран нужный предмет.
            if (selectedObject == null || selectedObject.ObjectKey != RequiredItemKey)
            {
                return;
            }

            m_Inventory.DeleteSelectedInventoryObject();

            // Игрок меняет заражение зоны, в которой он сейчас физически находится (объект стоит
            // в этой же зоне) - поэтому сразу показываем HUD с анимированным переходом от старого
            // значения к новому, не полагаясь на триггер-волюм зоны.
            float levelBefore = m_Zone.InfectionLevel.CurrentValue;
            ChangeInfection(m_Zone);
            float levelAfter = m_Zone.InfectionLevel.CurrentValue;
            m_Hud?.ShowLevelChange(m_Zone.DisplayName, levelBefore, levelAfter);
            m_IsPlaySound.OnNext(Unit.Default);

            OnUsed();
        }
    }
}
