using Game.Scripts.InfectionZone.Runtime.Interfaces;
using JuicyDI.Attributes;
using UnityEngine;

namespace Game.Scripts.InfectionZone.External.Controllers
{
    /// <summary>
    /// Интерактивное место заражения - обратная сторона <see cref="InfectionCleanupObject"/>
    /// (например вентиляция, в которую можно подбросить заражённый образец). Если у игрока в инвентаре
    /// выбран предмет с ключом <see cref="m_RequiredItemKey"/>, предмет расходуется и заражение зоны
    /// увеличивается. Так игрок сам может довести зону до нужного диапазона (например, чтобы открылись
    /// секретные аномалии/двери <see cref="Runtime.Controllers.InfectionRangeObject"/>).
    /// Общая логика - в <see cref="InfectionItemInteraction"/>.
    /// </summary>
    [JDIMonoController]
    public class InfectionSpreadObject : InfectionItemInteraction
    {
        [Header("Настройки заражения")]
        [Tooltip("Ключ предмета инвентаря, которым игрок заражает зону.")]
        [SerializeField] private string m_RequiredItemKey;

        [Tooltip("На сколько процентов увеличится заражение зоны при использовании.")]
        [SerializeField] private float m_IncreaseAmount = 10f;

        [Tooltip("Выключать объект после использования (одноразовое место заражения). " +
                 "Если выключено - место можно использовать повторно, каждый раз расходуя предмет.")]
        [SerializeField] private bool m_DisableAfterUse = true;

        protected override string RequiredItemKey => m_RequiredItemKey;

        protected override void ChangeInfection(IInfectionZone zone)
        {
            zone.IncreaseInfection(m_IncreaseAmount);
        }

        protected override void OnUsed()
        {
            if (m_DisableAfterUse)
            {
                gameObject.SetActive(false);
            }
        }
    }
}
