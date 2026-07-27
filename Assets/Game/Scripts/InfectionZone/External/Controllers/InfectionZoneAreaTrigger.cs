using Game.Scripts.InfectionZone.Runtime.Interfaces;
using JuicyDI.Attributes;
using UnityEngine;

namespace Game.Scripts.InfectionZone.External.Controllers
{
    /// <summary>
    /// Триггер-область на уровне (статический коллайдер с IsTrigger = true), покрывающий
    /// физическую территорию локации/зоны. Повесьте на GameObject с коллайдером примерно
    /// по границам локации и укажите ссылку на соответствующую <see cref="IInfectionZone"/>.
    ///
    /// Когда игрок входит в триггер - HUD показывает текущий уровень заражения зоны
    /// без анимации (<see cref="IInfectionLevelHud.ShowCurrentLevel"/>).
    ///
    /// Анимированный показ изменения уровня (<see cref="IInfectionLevelHud.ShowLevelChange"/>)
    /// сюда намеренно не вынесен - его вызывают напрямую скрипты, которыми игрок реально
    /// меняет заражение (см. <see cref="InfectionCleanupObject"/>,
    /// <see cref="Runtime.Controllers.StoryInfectionTrigger"/>). Так поведение предсказуемо
    /// и не зависит от того, есть ли на сцене триггер-волюм для конкретной зоны, и не
    /// дублируется/не конфликтует с прямыми вызовами из этих скриптов.
    /// </summary>
    [JDIMonoController]
    public class InfectionZoneAreaTrigger : MonoBehaviour
    {
        [Header("Ссылка на зону")]
        [Tooltip("Зона, за степенью заражения которой следит этот триггер.")]
        [SerializeField] private MonoBehaviour m_ZoneSource; // должен реализовывать IInfectionZone

        [Header("Игрок")]
        [Tooltip("Тег объекта игрока - вход коллайдера с этим тегом в триггер считается 'входом в локацию'.")]
        [SerializeField] private string m_PlayerTag = "Player";

        [Inject] private IInfectionLevelHud m_Hud;

        private IInfectionZone m_Zone;
        private bool m_IsPlayerInside;

        private void Awake()
        {
            m_Zone = m_ZoneSource as IInfectionZone;

            if (m_Zone == null)
            {
                Debug.LogError($"[InfectionZoneAreaTrigger] {name}: m_ZoneSource должен реализовывать IInfectionZone.", this);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (m_Zone == null || m_IsPlayerInside || !other.CompareTag(m_PlayerTag))
            {
                return;
            }

            m_IsPlayerInside = true;
            m_Hud?.ShowCurrentLevel(m_Zone.DisplayName, m_Zone.InfectionLevel.CurrentValue);
        }

        private void OnTriggerExit(Collider other)
        {
            if (m_Zone == null || !m_IsPlayerInside || !other.CompareTag(m_PlayerTag))
            {
                return;
            }

            m_IsPlayerInside = false;
        }
    }
}


