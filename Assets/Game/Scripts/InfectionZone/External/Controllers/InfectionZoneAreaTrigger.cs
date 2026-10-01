using Game.Scripts.InfectionZone.Runtime.Interfaces;
using JuicyDI.Attributes;
using UnityEngine;

namespace Game.Scripts.InfectionZone.External.Controllers
{
    /// <summary>
    /// Триггер-область на уровне (статический коллайдер с IsTrigger = true), покрывающий
    /// физическую территорию зоны. Сообщает <see cref="IPlayerInfectionZoneTracker"/> о входе/выходе
    /// игрока, а тот определяет текущую зону - при её смене HUD показывает уровень заражения новой зоны
    /// (см. InfectionLevelHudController).
    ///
    /// Настройка: GameObject с коллайдером (или несколькими) по границам зоны, ссылка на зону в m_ZoneSource.
    /// Триггеры соседних зон могут перекрываться на стыке, у одной зоны может быть несколько триггеров.
    /// При добавлении компонента коллайдер сам становится триггером, а объект - слоем Ignore Raycast
    /// (иначе большие триггеры перехватывают лучи взаимодействия игрока).
    ///
    /// Анимированный показ изменения уровня (<see cref="IInfectionLevelHud.ShowLevelChange"/>) сюда
    /// намеренно не вынесен - его вызывают напрямую скрипты, которыми меняется заражение
    /// (<see cref="InfectionCleanupObject"/>, <see cref="InfectionSpreadObject"/>,
    /// <see cref="Runtime.Controllers.StoryInfectionTrigger"/>).
    /// </summary>
    [JDIMonoController]
    public class InfectionZoneAreaTrigger : MonoBehaviour
    {
        private const int IgnoreRaycastLayer = 2;

        [Header("Ссылка на зону")]
        [Tooltip("Зона, территорию которой покрывает этот триггер.")]
        [SerializeField] private MonoBehaviour m_ZoneSource; // должен реализовывать IInfectionZone

        [Header("Игрок")]
        [Tooltip("Тег объекта игрока - вход коллайдера с этим тегом в триггер считается 'входом в зону'.")]
        [SerializeField] private string m_PlayerTag = "Player";

        [Inject] private IPlayerInfectionZoneTracker m_ZoneTracker;

        private IInfectionZone m_Zone;
        private int m_PlayerOverlapCount;

        private void Reset()
        {
            var trigger = GetComponent<Collider>();

            if (trigger == null)
            {
                trigger = gameObject.AddComponent<BoxCollider>();
            }

            trigger.isTrigger = true;
            gameObject.layer = IgnoreRaycastLayer;
        }

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
            if (m_Zone == null || !other.CompareTag(m_PlayerTag))
            {
                return;
            }

            m_PlayerOverlapCount++;
            m_ZoneTracker?.EnterZone(m_Zone);
        }

        private void OnTriggerExit(Collider other)
        {
            if (m_Zone == null || m_PlayerOverlapCount == 0 || !other.CompareTag(m_PlayerTag))
            {
                return;
            }

            m_PlayerOverlapCount--;
            m_ZoneTracker?.ExitZone(m_Zone);
        }

        private void OnDisable()
        {
            // При выключении триггера Unity не присылает OnTriggerExit - снимаем пересечения сами.
            while (m_PlayerOverlapCount > 0)
            {
                m_PlayerOverlapCount--;
                m_ZoneTracker?.ExitZone(m_Zone);
            }
        }
    }
}
