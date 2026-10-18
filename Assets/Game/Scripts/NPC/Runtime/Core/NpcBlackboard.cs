using Game.Scripts.InfectionZone.Runtime.Interfaces;
using Game.Scripts.NPC.Runtime.Controllers;
using Game.Scripts.NPC.Runtime.Data;
using Game.Scripts.NPC.Runtime.Interfaces;
using UnityEngine;
using UnityEngine.AI;

namespace Game.Scripts.NPC.Runtime.Core
{
    /// <summary>
    /// Скорость, с которой бот идёт по команде (шаг графа "NPC/Move To Point" и т.п.).
    /// </summary>
    public enum NpcPace
    {
        [InspectorName("Спокойно")]
        Calm = 0,

        [InspectorName("Быстро (как при поиске)")]
        Search = 1,

        [InspectorName("Бегом (как в погоне)")]
        Chase = 2
    }

    /// <summary>
    /// Общий "рабочий стол" FSM: всё, что нужно состояниям, чтобы принимать
    /// решения и управлять ботом, без прямой связи состояний друг с другом.
    /// Заполняется и обновляется NpcController каждый кадр перед Tick() машины.
    /// </summary>
    public class NpcBlackboard
    {
        public NpcController Controller { get; }
        public NavMeshAgent Agent { get; }
        public NpcTypeDefinitionSO Definition { get; }
        public INpcAggressionSource AggressionSource { get; set; }

        public Transform Self => Controller.transform;
        public Transform PlayerTransform { get; set; }

        public NpcPatrolRoute PatrolRoute { get; set; }
        public int CurrentPatrolIndex { get; set; }

        /// <summary>
        /// Patrol при входе продолжает с ближайшей точки маршрута, а не с первой (так бот возвращается
        /// к обходу после поиска). Сбрасывается самим Patrol.
        /// </summary>
        public bool PatrolResumeNearest { get; set; }

        /// <summary>Состояние спокойного поведения - в него бот возвращается, не найдя игрока.</summary>
        public NpcStateId CalmState { get; set; } = NpcStateId.Patrol;

        /// <summary>Зона заражения, в которой бот живёт (для агрессии, злости и поиска по зоне). Может быть null.</summary>
        public IInfectionZone Zone { get; set; }

        public float ZoneInfectionLevel => Zone != null ? Zone.InfectionLevel.CurrentValue : 0f;

        public bool IsAggressive => AggressionSource != null && AggressionSource.IsAggressive.CurrentValue;

        // --- Восприятие, пересчитывается каждый кадр в NpcController ---
        public bool CanSeePlayer { get; set; }
        public float DistanceToPlayer { get; set; } = float.MaxValue;
        public float TimeSincePlayerLastSeen { get; set; }
        public Vector3 LastKnownPlayerPosition { get; set; }

        /// <summary>Бот хоть раз видел игрока (LastKnownPlayerPosition имеет смысл).</summary>
        public bool HasLastKnownPlayerPosition { get; set; }

        /// <summary>
        /// Сюжетная погоня: бот всегда знает, где игрок, не ищет его и сам погоню не бросает.
        /// Включается командой ForceHunt (или настройкой ChaseStopsOnlyByStory), выключается командами сюжета.
        /// </summary>
        public bool IsHunting { get; set; }

        // --- Поиск ---
        /// <summary>Вокруг этой точки бот ищет игрока (обычно - где видел его последним).</summary>
        public Vector3 SearchCenter { get; set; }

        // --- Атака ---
        /// <summary>Пока больше нуля - бот не начинает новую атаку.</summary>
        public float AttackCooldownTimer { get; set; }

        // --- Команда "идти в точку" (MoveTo) ---
        public Vector3 MoveTarget { get; set; }
        public Quaternion? MoveTargetRotation { get; set; }
        public NpcPace MovePace { get; set; }
        public string MoveTargetKey { get; set; }

        // --- Команда "действие на месте" (Perform) ---
        public string PerformKey { get; set; }
        /// <summary>Сколько секунд длится действие; 0 и меньше - пока не придёт другая команда.</summary>
        public float PerformDuration { get; set; }
        public NpcStateId PerformNextState { get; set; } = NpcStateId.Idle;

        // --- Служебное состояние для ожидания у запертой двери ---
        public float DoorKnockTimer { get; set; }

        /// <summary>Сколько секунд подряд бот уже стоит перед текущей запертой дверью (Chase).</summary>
        public float DoorBlockedTimer { get; set; }

        /// <summary>
        /// Пока > 0 - бот (Search/Wander) не возвращается в погоню, даже если видит игрока. Выставляется
        /// погоней при сдаче перед запертой дверью - иначе бот, едва начав поиск, снова видел бы игрока
        /// (через ту же дверь/поблизости) и возвращался к той же двери по кругу.
        /// </summary>
        public float ChaseReentrySuppressTimer { get; set; }

        public NpcBlackboard(NpcController controller, NavMeshAgent agent, NpcTypeDefinitionSO definition)
        {
            Controller = controller;
            Agent = agent;
            Definition = definition;
        }

        public float GetSpeed(NpcPace pace)
        {
            switch (pace)
            {
                case NpcPace.Search:
                    return Definition.SearchSpeed;
                case NpcPace.Chase:
                    return Definition.ChaseSpeed;
                default:
                    return Definition.PatrolSpeed;
            }
        }
    }
}
