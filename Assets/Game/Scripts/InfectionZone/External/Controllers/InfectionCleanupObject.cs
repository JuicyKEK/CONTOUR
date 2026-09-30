using Game.Scripts.InfectionZone.Runtime.Interfaces;
using JuicyDI.Attributes;
using UnityEngine;

namespace Game.Scripts.InfectionZone.External.Controllers
{
    /// <summary>
    /// Интерактивный объект заражения (например: гнездо плесени, зараженный предмет).
    /// Очищается только если у игрока в инвентаре выбран предмет с ключом <see cref="m_RequiredItemKey"/>
    /// (например "Spray with vinegar"). При успешном взаимодействии предмет расходуется, степень
    /// заражения зоны уменьшается, а сам объект деактивируется (исчезает со сцены).
    /// Общая логика - в <see cref="InfectionItemInteraction"/>.
    /// </summary>
    [JDIMonoController]
    public class InfectionCleanupObject : InfectionItemInteraction
    {
        [Header("Настройки очистки")]
        [Tooltip("Ключ предмета инвентаря, необходимого для очистки этой аномалии.")]
        [SerializeField] private string m_RequiredItemKey = "Spray with vinegar";

        [Tooltip("На сколько процентов уменьшится заражение зоны при успешной очистке.")]
        [SerializeField] private float m_DecreaseAmount = 10f;

        protected override string RequiredItemKey => m_RequiredItemKey;

        protected override void ChangeInfection(IInfectionZone zone)
        {
            zone.DecreaseInfection(m_DecreaseAmount);
        }

        protected override void OnUsed()
        {
            // Аномалия очищена - исчезает со сцены.
            gameObject.SetActive(false);
        }
    }
}
