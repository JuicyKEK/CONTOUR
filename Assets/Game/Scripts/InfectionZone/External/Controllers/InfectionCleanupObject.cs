using Game.Scripts.InfectionZone.Runtime.Interfaces;
using JuicyDI.Attributes;
using R3;
using UnityEngine;

namespace Game.Scripts.InfectionZone.External.Controllers
{
    /// <summary>
    /// Интерактивный объект заражения (например: гнездо плесени, зараженный предмет).
    /// Очищается только если у игрока в инвентаре выбран предмет с ключом <see cref="m_RequiredItemKey"/>
    /// (например "Spray with vinegar"). При успешном взаимодействии предмет расходуется, степень
    /// заражения зоны уменьшается, а объект становится очищенным (<see cref="IsCleaned"/>). Как он исчезает
    /// со сцены, показывает вид: InfectionCleanupMeltView расплавляет его и выключает.
    /// Снова появившаяся аномалия (её заново показал контроллер прогрессии) - опять не очищена.
    /// Общая логика - в <see cref="InfectionItemInteraction"/>.
    /// </summary>
    [JDIMonoController]
    public class InfectionCleanupObject : InfectionItemInteraction
    {
        public ReadOnlyReactiveProperty<bool> IsCleaned => m_IsCleaned;

        [Header("Настройки очистки")]
        [Tooltip("Ключ предмета инвентаря, необходимого для очистки этой аномалии.")]
        [SerializeField] private string m_RequiredItemKey = "Spray with vinegar";

        [Tooltip("На сколько процентов уменьшится заражение зоны при успешной очистке.")]
        [SerializeField] private float m_DecreaseAmount = 10f;

        private readonly ReactiveProperty<bool> m_IsCleaned = new(false);

        protected override string RequiredItemKey => m_RequiredItemKey;

        // Очищенная аномалия ещё исчезает - второй раз её не чистят.
        protected override bool CanInteract => !m_IsCleaned.Value;

        private void OnEnable()
        {
            m_IsCleaned.Value = false;
        }

        protected override void ChangeInfection(IInfectionZone zone)
        {
            zone.DecreaseInfection(m_DecreaseAmount);
        }

        protected override void OnUsed()
        {
            m_IsCleaned.Value = true;
        }

        private void OnDestroy()
        {
            m_IsCleaned.Dispose();
        }
    }
}
