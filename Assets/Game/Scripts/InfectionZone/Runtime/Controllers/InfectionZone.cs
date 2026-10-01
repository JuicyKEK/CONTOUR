using Game.Scripts.InfectionZone.Runtime.Interfaces;
using JuicyDI;
using JuicyDI.Attributes;
using R3;
using UnityEngine;

namespace Game.Scripts.InfectionZone.Runtime.Controllers
{
    /// <summary>
    /// Зона заражения локации (например: зона общежитий, зона входа, зона столовой).
    /// Хранит степень заражения (0..100%) и уведомляет подписчиков (аномалии, вью) об изменениях.
    /// Регистрируется в глобальном реестре <see cref="InfectionZoneRegistry"/>, чтобы сюжетные
    /// события могли найти нужную зону по её <see cref="ZoneId"/>.
    /// </summary>
    [JDIMonoController]
    [SequenceParticipant(10)]
    public class InfectionZone : MonoBehaviour, IInfectionZone, ISequence
    {
        [Header("Настройки зоны")]
        [Tooltip("Уникальный идентификатор зоны. Используется сюжетными событиями для поиска зоны через реестр.")]
        [SerializeField] private string m_ZoneId;

        [Tooltip("Отображаемое имя зоны для UI (например 'Общежития'). Если не заполнено - в UI используется ZoneId.")]
        [SerializeField] private string m_DisplayName;

        [Tooltip("Стартовая степень заражения зоны (0..100%).")]
        [Range(0f, 100f)]
        [SerializeField] private float m_StartInfectionLevel;

        [Inject] private IInfectionZoneRegistry m_Registry;

        private readonly ReactiveProperty<float> m_InfectionLevel = new(0f);
        private bool m_IsLevelRestored;

        public string ZoneId => m_ZoneId;
        public string DisplayName => string.IsNullOrEmpty(m_DisplayName) ? m_ZoneId : m_DisplayName;
        public ReadOnlyReactiveProperty<float> InfectionLevel => m_InfectionLevel;

        public void MethodInit()
        {
            // Регистрируемся уже в MethodInit: InfectionZoneSaveController в своём MethodInit (он идёт позже)
            // восстанавливает уровень зоны из сохранения до того, как в MethodStart применится стартовый.
            m_Registry?.RegisterZone(this);
        }

        public void MethodStart()
        {
            // Стартовый уровень - только если уровень не пришёл из сохранения. Иначе подписчики (например
            // прогрессия аномалий, которая аномалии только открывает) успели бы отреагировать на стартовое значение.
            if (!m_IsLevelRestored)
            {
                SetInfection(m_StartInfectionLevel);
            }
        }

        private void OnDestroy()
        {
            m_Registry?.UnregisterZone(this);
            m_InfectionLevel.Dispose();
        }

        public void IncreaseInfection(float amount)
        {
            if (amount < 0f)
            {
                Debug.LogWarning($"[InfectionZone:{m_ZoneId}] IncreaseInfection получил отрицательное значение, используйте DecreaseInfection.");
                return;
            }

            SetInfection(m_InfectionLevel.Value + amount);
        }

        public void DecreaseInfection(float amount)
        {
            if (amount < 0f)
            {
                Debug.LogWarning($"[InfectionZone:{m_ZoneId}] DecreaseInfection получил отрицательное значение, используйте IncreaseInfection.");
                return;
            }

            SetInfection(m_InfectionLevel.Value - amount);
        }

        public void SetInfection(float value)
        {
            m_InfectionLevel.Value = Mathf.Clamp(value, 0f, 100f);
        }

        public void RestoreInfection(float value)
        {
            m_IsLevelRestored = true;
            SetInfection(value);
        }
    }
}
