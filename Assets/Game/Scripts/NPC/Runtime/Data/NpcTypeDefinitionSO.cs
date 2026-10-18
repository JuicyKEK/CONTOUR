using System;
using Game.Scripts.InteractionObjects.Interfaces;
using Game.Scripts.Story;
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
        InfectionZoneLinked = 2,

        [InspectorName("Шкала злости (явный мимик)")]
        AngerMeter = 3
    }

    /// <summary>
    /// Чем заканчивается атака бота.
    /// </summary>
    public enum NpcAttackKind
    {
        [InspectorName("Удар (игрок жив)")]
        Hit = 0,

        [InspectorName("Убийство")]
        Kill = 1
    }

    /// <summary>
    /// Что бот делает после удара (для <see cref="NpcAttackKind.Hit"/>).
    /// </summary>
    public enum NpcAfterHitBehaviour
    {
        [InspectorName("Остановиться и ждать команды (графа/сюжета)")]
        StandStill = 0,

        [InspectorName("Вернуться к спокойному поведению")]
        ReturnToCalm = 1,

        [InspectorName("Продолжить погоню")]
        KeepChasing = 2
    }

    /// <summary>
    /// Действие игрока, которое меняет шкалу злости мимика: сюжетный сигнал (например его поднимает
    /// StorySignalInteract на механизме, который игрок починил) и на сколько он меняет шкалу.
    /// </summary>
    [Serializable]
    public struct NpcAngerAction
    {
        [Tooltip("Сигнал сюжета - действие игрока (починил механизм, помыл посуду, сломал что-то...).")]
        [StoryKey(StoryKeyKind.Signal)] public string SignalKey;

        [Tooltip("Насколько меняется шкала злости (шкала от 0 до 1). Меньше нуля - действие поддерживает " +
                 "симуляцию мимика и успокаивает его; больше нуля - 'неправильное' действие злит.")]
        [Range(-1f, 1f)] public float Delta;

        [Tooltip("Считается, только если мимик в этот момент видит игрока.")]
        public bool OnlyInSight;
    }

    /// <summary>
    /// Данные-конфигурация одного "типа" бота (сотрудник газовой службы, мимик, монстр и т.д.).
    /// Один и тот же NpcController настраивается под любой тип бота заменой этого ассета -
    /// никакого нового C#-класса под каждого NPC создавать не нужно.
    /// </summary>
    [CreateAssetMenu(menuName = "NPC/Npc Type Definition", fileName = "NpcTypeDefinition")]
    public class NpcTypeDefinitionSO : ScriptableObject
    {
        [Header("Общее")]
        [SerializeField] private string m_TypeName;
        [Tooltip("Анимации, звуки действий, шаги и голос бота. У облика бота (NpcAppearance) может быть свой профиль.")]
        [SerializeField] private NpcAnimationSoundProfileSO m_AnimationSoundProfile;

        [Header("Восприятие игрока")]
        [Tooltip("Дистанция, на которой бот вообще способен заметить игрока.")]
        [SerializeField] private float m_ViewDistance = 12f;
        [Tooltip("Половина угла обзора в градусах (0..180): 60 - конус 120 градусов, 180 - видит во все стороны.")]
        [SerializeField] private float m_ViewHalfAngle = 60f;
        [Tooltip("Включено - стены и закрытые двери загораживают игрока. Выключено - бот видит игрока сквозь стены " +
                 "(в пределах дальности и угла обзора).")]
        [SerializeField] private bool m_RequireLineOfSight = true;
        [Tooltip("Слои, которые загораживают обзор. Nothing - все обычные слои (кроме Ignore Raycast).")]
        [SerializeField] private LayerMask m_LineOfSightObstacles;
        [Tooltip("Высота глаз бота над его опорной точкой (метры).")]
        [SerializeField] private float m_EyeHeight = 1.6f;

        [Header("Агрессия")]
        [SerializeField] private NpcAggressionMode m_AggressionMode = NpcAggressionMode.Fixed;
        [Tooltip("Используется, если AggressionMode = Fixed или как стартовое значение для Manual.")]
        [SerializeField] private bool m_StartsAggressive;
        [Tooltip("Используется, если AggressionMode = InfectionZoneLinked. Уровень заражения (0..100), начиная с которого бот становится агрессивным.")]
        [SerializeField] private float m_InfectionAggressionThreshold = 60f;

        [Header("Шкала злости (AggressionMode = Шкала злости)")]
        [Tooltip("На сколько шкала (от 0 до 1) растёт за секунду, пока мимик видит игрока. 0 - сама по себе не растёт, " +
                 "только от 'неправильных' действий игрока.")]
        [SerializeField, Min(0f)] private float m_AngerRisePerSecond = 0.1f;
        [Tooltip("На сколько шкала убывает за секунду, пока мимик игрока не видит. 0 - не убывает.")]
        [SerializeField, Min(0f)] private float m_AngerDecayPerSecond;
        [Tooltip("Во сколько раз быстрее растёт злость при заражении зоны 100% (между 0% и 100% - плавно). " +
                 "Действует и на рост от взгляда, и на 'неправильные' действия.")]
        [SerializeField, Min(1f)] private float m_AngerInfectionMultiplier = 3f;
        [Tooltip("С какого уровня заражения зоны (0..100) мимик при виде игрока сразу становится агрессивным. 101 - никогда.")]
        [SerializeField, Range(0f, 101f)] private float m_InstantAggressionInfectionLevel = 101f;
        [Tooltip("Действия игрока, которые меняют шкалу (сюжетные сигналы).")]
        [SerializeField] private NpcAngerAction[] m_AngerActions = Array.Empty<NpcAngerAction>();
        [Tooltip("Значение шкалы, когда мимик, не найдя игрока, возвращается к своей роли.")]
        [SerializeField, Range(0f, 1f)] private float m_AngerAfterCalm;

        [Header("Скорости (NavMeshAgent.speed)")]
        [Tooltip("Спокойное поведение: обход маршрута, роль мимика, походка по команде графа.")]
        [SerializeField] private float m_PatrolSpeed = 1.6f;
        [Tooltip("Поиск игрока - быстрее спокойного шага, но медленнее погони.")]
        [SerializeField] private float m_SearchSpeed = 2.8f;
        [SerializeField] private float m_ChaseSpeed = 4.5f;
        [SerializeField] private float m_PanicSpeed = 5f;

        [Header("Атака")]
        [Tooltip("Дистанция, с которой бот атакует игрока.")]
        [SerializeField] private float m_CatchDistance = 1.2f;
        [SerializeField] private NpcAttackKind m_AttackKind = NpcAttackKind.Hit;
        [Tooltip("Через сколько секунд от начала атаки удар попадает (замах). Если игрок за это время отбежал - промах.")]
        [SerializeField, Min(0f)] private float m_AttackHitDelay = 0.4f;
        [Tooltip("Сколько секунд длится атака целиком (анимация удара или поедания).")]
        [SerializeField, Min(0f)] private float m_AttackDuration = 1.2f;
        [Tooltip("Пауза перед следующей атакой (после промаха или при 'продолжить погоню').")]
        [SerializeField, Min(0f)] private float m_AttackCooldown = 1.5f;
        [SerializeField] private NpcAfterHitBehaviour m_AfterHit = NpcAfterHitBehaviour.StandStill;

        [Header("Преследование")]
        [Tooltip("Потеряв игрока из виду, бот бежит к месту, где видел его последним; если не добежал за столько секунд - " +
                 "всё равно начинает поиск. Игнорируется в сюжетной погоне.")]
        [SerializeField] private float m_LoseSightGiveUpDelay = 5f;
        [Tooltip("Сюжетная погоня: бот всегда знает, где игрок, не ищет его и сам погоню не бросает - " +
                 "только по команде сюжета (ForcePatrol/ForceIdle/LoseInterest, шаги графа).")]
        [SerializeField] private bool m_ChaseStopsOnlyByStory;

        [Header("Поиск игрока")]
        [Tooltip("Радиус поиска вокруг места, где бот видел игрока последним.")]
        [SerializeField, Min(1f)] private float m_SearchRadius = 6f;
        [Tooltip("Сколько секунд бот ищет вокруг этого места.")]
        [SerializeField, Min(0f)] private float m_LocalSearchDuration = 15f;
        [Tooltip("Сколько секунд бот стоит и осматривается в каждой точке поиска (случайно между min и max).")]
        [SerializeField, Min(0f)] private float m_SearchLookAroundMin = 1f;
        [SerializeField, Min(0f)] private float m_SearchLookAroundMax = 3f;
        [Tooltip("Если заражение зоны бота (0..100) не ниже этого значения - после поиска вокруг бот ищет игрока по всей зоне. " +
                 "Ниже - возвращается к спокойному поведению. 101 - по всей зоне не ищет никогда.")]
        [SerializeField, Range(0f, 101f)] private float m_ZoneSearchInfectionLevel = 70f;
        [Tooltip("Сколько секунд длится поиск по всей зоне. 0 - пока не найдёт игрока.")]
        [SerializeField, Min(0f)] private float m_ZoneSearchDuration = 60f;
        [Tooltip("Если зону бота определить не удалось (нет триггеров зоны и NpcSearchArea) - поиск 'по всей зоне' идёт в этом радиусе.")]
        [SerializeField, Min(1f)] private float m_ZoneSearchFallbackRadius = 25f;

        [Header("Двери")]
        [Tooltip("Может ли бот открывать незапертые двери на своём пути (IsLocked == false). Если нет - закрытая дверь для него как запертая.")]
        [SerializeField] private bool m_CanOpenDoors;
        [Tooltip("Как часто (сек) бот дёргает запертую дверь.")]
        [SerializeField] private float m_KnockInterval = 2f;
        [Tooltip("Слои коллайдеров дверей для поиска двери по курсу движения. Nothing - все слои.")]
        [SerializeField] private LayerMask m_DoorDetectionMask;
        [Tooltip("На каком расстоянии перед собой бот 'видит' дверь.")]
        [SerializeField] private float m_DoorDetectionDistance = 1.5f;
        [Tooltip("Радиус SphereCast для поиска двери впереди.")]
        [SerializeField] private float m_DoorDetectionRadius = 0.4f;
        [Tooltip("Сколько секунд бот, преследуя игрока, пытается открыть запертую дверь, прежде чем перейти к поиску игрока.")]
        [SerializeField] private float m_LockedDoorGiveUpDelay = 5f;
        [Tooltip("Какой анимацией/звуком открытия пользоваться, когда бот открывает дверь спокойно (обычно Human). " +
                 "Если у бота есть NpcAppearance - берётся из текущего облика.")]
        [SerializeField] private DoorOpenerKind m_PatrolDoorOpenerKind = DoorOpenerKind.Human;
        [Tooltip("Какой анимацией/звуком открытия пользоваться, когда бот открывает дверь в погоне (например монстр - Monster). " +
                 "Если у бота есть NpcAppearance - берётся из текущего облика.")]
        [SerializeField] private DoorOpenerKind m_ChaseDoorOpenerKind = DoorOpenerKind.Human;

        [Header("Блуждание (Wander)")]
        [Tooltip("Радиус, в котором бот выбирает случайные точки для блуждания.")]
        [SerializeField] private float m_WanderRadius = 6f;
        [Tooltip("Минимальный интервал (сек) между выбором новых случайных точек.")]
        [SerializeField] private float m_WanderMinInterval = 3f;
        [Tooltip("Максимальный интервал (сек) между выбором новых случайных точек.")]
        [SerializeField] private float m_WanderMaxInterval = 7f;
        [Tooltip("Сколько секунд после того, как бот сдался у запертой двери, он не возвращается в погоню, даже если " +
                 "видит игрока (например сквозь ту же дверь) - иначе он сразу вернулся бы к ней.")]
        [SerializeField] private float m_WanderChaseSuppressDuration = 6f;

        public string TypeName => m_TypeName;
        public NpcAnimationSoundProfileSO AnimationSoundProfile => m_AnimationSoundProfile;

        public float ViewDistance => m_ViewDistance;
        public float ViewHalfAngle => m_ViewHalfAngle;
        public bool RequireLineOfSight => m_RequireLineOfSight;
        public LayerMask LineOfSightObstacles => m_LineOfSightObstacles;
        public float EyeHeight => m_EyeHeight;

        public NpcAggressionMode AggressionMode => m_AggressionMode;
        public bool StartsAggressive => m_StartsAggressive;
        public float InfectionAggressionThreshold => m_InfectionAggressionThreshold;

        public float AngerRisePerSecond => m_AngerRisePerSecond;
        public float AngerDecayPerSecond => m_AngerDecayPerSecond;
        public float AngerInfectionMultiplier => m_AngerInfectionMultiplier;
        public float InstantAggressionInfectionLevel => m_InstantAggressionInfectionLevel;
        public NpcAngerAction[] AngerActions => m_AngerActions ?? Array.Empty<NpcAngerAction>();
        public float AngerAfterCalm => m_AngerAfterCalm;

        public float PatrolSpeed => m_PatrolSpeed;
        public float SearchSpeed => m_SearchSpeed;
        public float ChaseSpeed => m_ChaseSpeed;
        public float PanicSpeed => m_PanicSpeed;

        public float CatchDistance => m_CatchDistance;
        public NpcAttackKind AttackKind => m_AttackKind;
        public float AttackHitDelay => m_AttackHitDelay;
        public float AttackDuration => Mathf.Max(m_AttackDuration, m_AttackHitDelay);
        public float AttackCooldown => m_AttackCooldown;
        public NpcAfterHitBehaviour AfterHit => m_AfterHit;

        public float LoseSightGiveUpDelay => m_LoseSightGiveUpDelay;
        public bool ChaseStopsOnlyByStory => m_ChaseStopsOnlyByStory;

        public float SearchRadius => m_SearchRadius;
        public float LocalSearchDuration => m_LocalSearchDuration;
        public float SearchLookAroundMin => Mathf.Min(m_SearchLookAroundMin, m_SearchLookAroundMax);
        public float SearchLookAroundMax => Mathf.Max(m_SearchLookAroundMin, m_SearchLookAroundMax);
        public float ZoneSearchInfectionLevel => m_ZoneSearchInfectionLevel;
        public float ZoneSearchDuration => m_ZoneSearchDuration;
        public float ZoneSearchFallbackRadius => m_ZoneSearchFallbackRadius;

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

        private void OnValidate()
        {
            if (m_SearchSpeed <= m_PatrolSpeed || m_SearchSpeed >= m_ChaseSpeed)
            {
                Debug.LogWarning($"[NPC] {name}: скорость поиска ({m_SearchSpeed}) должна быть больше спокойной ({m_PatrolSpeed}) " +
                                 $"и меньше скорости погони ({m_ChaseSpeed}).", this);
            }
        }
    }
}
