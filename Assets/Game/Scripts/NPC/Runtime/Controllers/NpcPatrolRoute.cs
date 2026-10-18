using System.Collections.Generic;
using Game.Scripts.InteractionObjects.Interfaces;
using Game.Scripts.Story;
using UnityEngine;

namespace Game.Scripts.NPC.Runtime.Controllers
{
    [System.Serializable]
    public struct NpcPatrolPoint
    {
        public Transform Point;

        [Tooltip("Необязательно: если по пути к этой точке бот должен пройти через дверь, укажите её здесь - бот попробует её открыть/подождать, если заперта (см. NpcTypeDefinitionSO.CanOpenDoors).")]
        public MonoBehaviour DoorToPass; // должен реализовывать IInteractionDoor

        [Tooltip("Необязательно: сигнал сюжета при достижении этой точки (для сюжетных триггеров вида 'бот дошёл до точки X').")]
        [StoryKey(StoryKeyKind.Signal)] public string ArrivalSignal;

        [Tooltip("Устаревший SO-канал (используйте ArrivalSignal): при достижении точки бот вызовет ArrivalEventChannel.Raise().")]
        public StoryEventChannelSO ArrivalEventChannel;

        [Tooltip("Необязательно: сколько секунд бот задерживается на точке (например мимик 'чинит механизм' или 'моет посуду'). 0 - сразу идёт дальше.")]
        [Min(0f)] public float WaitSeconds;

        [Tooltip("Необязательно: ключ анимации/звука, который бот играет, пока стоит на точке (например 'Repair', 'Wash').")]
        public string ActionKey;

        [Tooltip("Пока стоит на точке, повернуться так же, как повёрнут объект точки (лицом к механизму, к раковине).")]
        public bool FacePointDirection;

        public IInteractionDoor ResolveDoor() => DoorToPass as IInteractionDoor;
    }

    /// <summary>
    /// Маршрут патрулирования - отдельный компонент сцены (а не массив прямо на
    /// NpcController), чтобы сюжет мог целиком подменить маршрут бота простым
    /// вызовом NpcController.SetPatrolRoute(newRoute) со статическим параметром
    /// UnityEvent/StoryEventChannelListener, не трогая точки внутри самого бота.
    /// Можно держать несколько NpcPatrolRoute на сцене (например "обычный обход"
    /// и "тревожный обход") и переключаться между ними по триггерам.
    ///
    /// Это же - "роль" мимика в спокойном состоянии: точки с задержкой и ключом действия
    /// (WaitSeconds/ActionKey) - бот подходит к механизму и "чинит" его, идёт к раковине и "моет посуду".
    /// </summary>
    public class NpcPatrolRoute : MonoBehaviour
    {
        [SerializeField] private NpcPatrolPoint[] m_Points;
        [SerializeField] private bool m_Loop = true;

        public IReadOnlyList<NpcPatrolPoint> Points => m_Points ?? System.Array.Empty<NpcPatrolPoint>();
        public bool Loop => m_Loop;

        private void OnDrawGizmosSelected()
        {
            if (m_Points == null)
            {
                return;
            }

            Gizmos.color = new Color(0.3f, 0.8f, 1f);

            for (int i = 0; i < m_Points.Length; i++)
            {
                if (m_Points[i].Point == null)
                {
                    continue;
                }

                Gizmos.DrawWireSphere(m_Points[i].Point.position, m_Points[i].WaitSeconds > 0f ? 0.35f : 0.2f);
                int next = i + 1 < m_Points.Length ? i + 1 : (m_Loop ? 0 : -1);

                if (next >= 0 && m_Points[next].Point != null)
                {
                    Gizmos.DrawLine(m_Points[i].Point.position, m_Points[next].Point.position);
                }
            }
        }
    }
}