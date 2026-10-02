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

        public IInteractionDoor ResolveDoor() => DoorToPass as IInteractionDoor;
    }

    /// <summary>
    /// Маршрут патрулирования - отдельный компонент сцены (а не массив прямо на
    /// NpcController), чтобы сюжет мог целиком подменить маршрут бота простым
    /// вызовом NpcController.SetPatrolRoute(newRoute) со статическим параметром
    /// UnityEvent/StoryEventChannelListener, не трогая точки внутри самого бота.
    /// Можно держать несколько NpcPatrolRoute на сцене (например "обычный обход"
    /// и "тревожный обход") и переключаться между ними по триггерам.
    /// </summary>
    public class NpcPatrolRoute : MonoBehaviour
    {
        [SerializeField] private NpcPatrolPoint[] m_Points;
        [SerializeField] private bool m_Loop = true;

        public IReadOnlyList<NpcPatrolPoint> Points => m_Points;
        public bool Loop => m_Loop;
    }
}