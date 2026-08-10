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
    /// Интерактивный объект заражения (например: гнездо плесени, зараженный предмет).
    /// Очищается только если у игрока в инвентаре выбран предмет с ключом <see cref="m_RequiredItemKey"/>
    /// (например "Spray with vinegar") - по аналогии с UseInventoryObject.
    /// При успешном взаимодействии предмет расходуется, степень заражения зоны уменьшается,
    /// а сам объект деактивируется (исчезает со сцены).
    /// Если нужного предмета нет - ничего не происходит, аномалия остаётся на месте.
    /// </summary>
    [JDIMonoController]
    public class InfectionCleanupObject : MonoBehaviour, IInteraction, ISoundPlay
    {
        public Subject<Unit> IsPlaySound => m_IsPlaySound;
        
        [Header("Ссылка на зону")]
        [Tooltip("Зона, степень заражения которой будет уменьшена при взаимодействии.")]
        [SerializeField] private MonoBehaviour m_ZoneSource; // должен реализовывать IInfectionZone

        [Header("Настройки очистки")]
        [Tooltip("Ключ предмета инвентаря, необходимого для очистки этой аномалии.")]
        [SerializeField] private string m_RequiredItemKey = "Spray with vinegar";

        [Tooltip("На сколько процентов уменьшится заражение зоны при успешной очистке.")]
        [SerializeField] private float m_DecreaseAmount = 10f;

        [Inject] private IInventoryGetObject m_Inventory;
        [Inject] private IInfectionLevelHud m_Hud;

        private Subject<Unit> m_IsPlaySound = new Subject<Unit>();
        private IInfectionZone m_Zone;

        private void Awake()
        {
            m_Zone = m_ZoneSource as IInfectionZone;

            if (m_Zone == null)
            {
                Debug.LogError($"[InfectionCleanupObject] {name}: m_ZoneSource должен реализовывать IInfectionZone.", this);
            }
        }

        public void Interact()
        {
            if (m_Zone == null)
            {
                return;
            }

            var selectedObject = m_Inventory.GetInventorySelectedObject();

            // Очистка происходит, только если у игрока выбран нужный предмет.
            // Если предмета нет или выбран не тот - аномалия не очищается.
            if (selectedObject == null || selectedObject.ObjectKey != m_RequiredItemKey)
            {
                return;
            }

            m_Inventory.DeleteSelectedInventoryObject();

            // Игрок своим действием (очисткой) уменьшает заражение зоны, в которой он
            // сейчас физически находится (аномалия расположена в этой же зоне) - поэтому
            // сразу показываем HUD с анимированным переходом от старого значения к новому,
            // не полагаясь на отдельный триггер-волюм зоны на сцене.
            float levelBefore = m_Zone.InfectionLevel.CurrentValue;
            m_Zone.DecreaseInfection(m_DecreaseAmount);
            float levelAfter = m_Zone.InfectionLevel.CurrentValue;
            m_Hud?.ShowLevelChange(m_Zone.DisplayName, levelBefore, levelAfter);
            m_IsPlaySound.OnNext(Unit.Default);
            
            gameObject.SetActive(false);
        }
    }
}
