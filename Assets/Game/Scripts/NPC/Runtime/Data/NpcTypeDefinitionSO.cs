using Game.Scripts.InteractionObjects.Interfaces;
using UnityEngine;

namespace Game.Scripts.NPC.Runtime.Data
{
    /// <summary>
    /// Откуда берётся признак "агрессивен ли бот" - см. INpcAggressionSource
    /// и конкретные реализации в Runtime/AggressionSources.
    /// </summary>
    public enum NpcAggressionMode
    {
        [InspectorName("Фиксированное значение (никогда не меняется)")]
        Fixed = 0,

        [InspectorName("Ручное переключение (сюжет/триггер вызывает SetAggressiveOn/Off)")]
        Manual = 1,

        [InspectorName("Зависит от уровня заражения зоны")]
        InfectionZoneLinked = 2
    }

    /// <summary>
    /// Данные-конфигурация одного "типа" бота (мирный сотрудник, мимик, симулякр
    /// и т.д.). Один и тот же NpcController может быть настроен под любой тип
    /// боту простой заменой этого ассета - никакого нового C#-класса под каждого
    /// NPC создавать не нужно.
    /// </summary>
    [CreateAssetMenu(menuName = "NPC/Npc Type Definition", fileName = "NpcTypeDefinition")]
    public class NpcTypeDefinitionSO : ScriptableObject
    {
        [Header("Общее")]
        [SerializeField] private string m_TypeName;
        [SerializeField] private NpcAnimationSoundProfileSO m_AnimationSoundProfile;

        [Header("Восприятие игрока")]
        [Tooltip("Дистанция, на которой бот вообще способен заметить игрока.")]
        [SerializeField] private float m_ViewDistance = 12f;
        [Tooltip("Половина угла обзора в градусах (0..180).")]
        [SerializeField] private float m_ViewHalfAngle = 60f;
        [Tooltip("Нужна ли прямая видимость (Raycast) или дистанции+угла достаточно.")]
        [SerializeField] private bool m_RequireLineOfSight = true;
        [SerializeField] private LayerMask m_LineOfSightObstacles;

        [Header("Агрессия")]
        [SerializeField] private NpcAggressionMode m_AggressionMode = NpcAggressionMode.Fixed;
        [Tooltip("Используется, если AggressionMode = Fixed или как стартовое значение для Manual.")]
        [SerializeField] private bool m_StartsAggressive;
        [Tooltip("Используется, если AggressionMode = InfectionZoneLinked. Уровень заражения (0..100), начиная с которого бот становится агрессивным.")]
        [SerializeField] private float m_InfectionAggressionThreshold = 60f;

        [Header("Скорости (NavMeshAgent.speed)")]
        [SerializeField] private float m_PatrolSpeed = 1.6f;
        [SerializeField] private float m_ChaseSpeed = 4.5f;
        [SerializeField] private float m_PanicSpeed = 5f;

        [Header("Преследование")]
        [SerializeField] private float m_CatchDistance = 1.2f;
        [Tooltip("Сколько секунд агрессивный бот продолжает идти к последней известной позиции игрока, потеряв его из виду, прежде чем сдаться и вернуться патрулировать. Игнорируется, если ChaseStopsOnlyByStory = true.")]
        [SerializeField] private float m_LoseSightGiveUpDelay = 5f;
        [Tooltip("Если true - погоня НИКОГДА не останавливается сама по себе (по потере видимости), только принудительно из сюжета вызовом NpcController.ForcePatrol()/ForceIdle().")]
        [SerializeField] private bool m_ChaseStopsOnlyByStory;

        [Header("Двери")]
        [Tooltip("Может ли бот открывать незапертые двери на своём пути (IsLocked == false).")]
        [SerializeField] private bool m_CanOpenDoors;
        [Tooltip("Сколько секунд бот стоит и 'стучит' перед запертой дверью, ожидая пока её откроют, прежде чем повторить попытку.")]
        [SerializeField] private float m_KnockInterval = 2f;
        [Tooltip("Слой(и) коллайдеров дверей - используется для живого поиска двери по курсу движения (например во время Chase, где маршрут не расставлен заранее).")]
        [SerializeField] private LayerMask m_DoorDetectionMask;
        [Tooltip("На каком расстоянии перед собой бот 'видит' дверь.")]
        [SerializeField] private float m_DoorDetectionDistance = 1.5f;
        [Tooltip("Радиус SphereCast для поиска двери впереди.")]
        [SerializeField] private float m_DoorDetectionRadius = 0.4f;
        [Tooltip("Сколько секунд бот, преследуя игрока, пытается открыть запертую дверь, прежде чем сдаться и начать случайно бродить (см. NpcStateId.Wander).")]
        [SerializeField] private float m_LockedDoorGiveUpDelay = 5f;
        [Tooltip("Какой анимацией/звуком открытия пользоваться, когда бот открывает дверь во время Patrol/Idle/Observe (обычно Human).")]
        [SerializeField] private DoorOpenerKind m_PatrolDoorOpenerKind = DoorOpenerKind.Human;
        [Tooltip("Какой анимацией/звуком открытия пользоваться, когда бот открывает дверь во время Chase (например мимик после деформации - Monster).")]
        [SerializeField] private DoorOpenerKind m_ChaseDoorOpenerKind = DoorOpenerKind.Human;

        [Header("Блуждание (Wander)")]
        [Tooltip("Радиус, в котором бот выбирает случайные точки для блуждания.")]
        [SerializeField] private float m_WanderRadius = 6f;
        [Tooltip("Минимальный интервал (сек) между выбором новых случайных точек.")]
        [SerializeField] private float m_WanderMinInterval = 3f;
        [Tooltip("Максимальный интервал (сек) между выбором новых случайных точек.")]
        [SerializeField] private float m_WanderMaxInterval = 7f;
        [Tooltip("После того, как бот сдался перед запертой дверью и ушёл в Wander, столько секунд он игнорирует видимость игрока и НЕ возвращается в Chase, даже если технически всё ещё его видит (например через ту же дверь) - иначе он мгновенно бы возвращался обратно к той же двери по кругу.")]
        [SerializeField] private float m_WanderChaseSuppressDuration = 6f;

        public string TypeName => m_TypeName;
        public NpcAnimationSoundProfileSO AnimationSoundProfile => m_AnimationSoundProfile;

        public float ViewDistance => m_ViewDistance;
        public float ViewHalfAngle => m_ViewHalfAngle;
        public bool RequireLineOfSight => m_RequireLineOfSight;
        public LayerMask LineOfSightObstacles => m_LineOfSightObstacles;

        public NpcAggressionMode AggressionMode => m_AggressionMode;
        public bool StartsAggressive => m_StartsAggressive;
        public float InfectionAggressionThreshold => m_InfectionAggressionThreshold;

        public float PatrolSpeed => m_PatrolSpeed;
        public float ChaseSpeed => m_ChaseSpeed;
        public float PanicSpeed => m_PanicSpeed;

        public float CatchDistance => m_CatchDistance;
        public float LoseSightGiveUpDelay => m_LoseSightGiveUpDelay;
        public bool ChaseStopsOnlyByStory => m_ChaseStopsOnlyByStory;

        public bool CanOpenDoors => m_CanOpenDoors;
        public float KnockInterval => m_KnockInterval;
        public LayerMask DoorDetectionMask => m_DoorDetectionMask;
        public float DoorDetectionDistance => m_DoorDetectionDistance;
        public float DoorDetectionRadius => m_DoorDetectionRadius;
        public float LockedDoorGiveUpDelay => m_LockedDoorGiveUpDelay;
        public DoorOpenerKind PatrolDoorOpenerKind => m_PatrolDoorOpenerKind;
        public DoorOpenerKind ChaseDoorOpenerKind => m_ChaseDoorOpenerKind;

        public float WanderRadius => m_WanderRadius;
        public float WanderMinInterval => m_WanderMinInterval;
        public float WanderMaxInterval => m_WanderMaxInterval;
        public float WanderChaseSuppressDuration => m_WanderChaseSuppressDuration;
    }
}

