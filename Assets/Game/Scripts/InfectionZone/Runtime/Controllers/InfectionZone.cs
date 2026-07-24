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

        [Tooltip("Стартовая степень заражения зоны (0..100%).")]
        [Range(0f, 100f)]
        [SerializeField] private float m_StartInfectionLevel;

        [Inject] private IInfectionZoneRegistry m_Registry;

        private readonly ReactiveProperty<float> m_InfectionLevel = new(0f);

        public string ZoneId => m_ZoneId;
        public ReadOnlyReactiveProperty<float> InfectionLevel => m_InfectionLevel;

        public void MethodInit()
        {
            m_InfectionLevel.Value = Mathf.Clamp(m_StartInfectionLevel, 0f, 100f);
        }

        public void MethodStart()
        {
            m_Registry?.RegisterZone(this);
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
    }
}
