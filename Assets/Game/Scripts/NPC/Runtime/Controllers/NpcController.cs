using Game.Scripts.InfectionZone.Runtime.Interfaces;
using Game.Scripts.NPC.Runtime.AggressionSources;
using Game.Scripts.NPC.Runtime.Core;
using Game.Scripts.NPC.Runtime.Data;
using Game.Scripts.NPC.Runtime.Interfaces;
using Game.Scripts.NPC.Runtime.States;
using Game.Scripts.Story;
using JuicyDI;
using JuicyDI.Attributes;
using UnityEngine;
using UnityEngine.AI;

namespace Game.Scripts.NPC.Runtime.Controllers
{
    /// <summary>
    /// Единый контроллер поведения бота на FSM. Один и тот же класс обслуживает
    /// любой тип NPC - вся разница задаётся ассетом <see cref="NpcTypeDefinitionSO"/>
    /// (скорости, агрессия, восприятие, двери) и сценными ссылками (маршрут,
    /// стартовое состояние).
    ///
    /// Управление состояниями "снаружи" (сюжет/триггеры) идёт через публичные
    /// no-op методы Force*/SetAggressive* - их удобно вызывать из инспектора:
    ///  - как UnityEvent на <see cref="StoryEventChannelListener"/> (сюжетный SO-граф);
    ///  - как обычный UnityEvent геймплейного триггера (шкала агрессии симулякра и т.п.).
    ///
    /// Примеры сборки конкретных NPC из этих кубиков - см. комментарий в конце файла.
    /// </summary>
    [JDIMonoController]
    [SequenceParticipant(150)]
    public class NpcController : MonoBehaviour, ISequence, IUpdateSequence
    {
        [Header("Конфигурация типа бота")]
        [SerializeField] private NpcTypeDefinitionSO m_Definition;

        [Header("Компоненты")]
        [SerializeField] private NavMeshAgent m_Agent;
        [SerializeField] private Animator m_Animator;
        [SerializeField] private AudioSource m_AudioSource;

        [Header("Патрулирование")]
        [SerializeField] private NpcPatrolRoute m_PatrolRoute;

        [Header("Старт")]
        [SerializeField] private NpcStateId m_InitialState = NpcStateId.Patrol;

        [Header("Сюжетные события (все опциональны)")]
        [Tooltip("Раздаётся в момент, когда бот догнал и 'схватил' игрока.")]
        [SerializeField] private StoryEventChannelSO m_OnPlayerCaughtChannel; //TODO: Переделать нафиг потом
        [Tooltip("Раздаётся в момент начала преследования (полезно для катсцен/скримеров).")]
        [SerializeField] private StoryEventChannelSO m_OnChaseStartedChannel;
        [Tooltip("Раздаётся один раз, когда бот дошёл до последней точки НЕзацикленного маршрута патрулирования (NpcPatrolRoute.Loop == false).")]
        [SerializeField] private StoryEventChannelSO m_OnPatrolRouteCompletedChannel;

        [Header("Зона заражения (только для AggressionMode = InfectionZoneLinked)")]
        [Tooltip("Компонент, реализующий IInfectionZone - зона, к заражению которой привязана агрессия.")]
        [SerializeField] private MonoBehaviour m_InfectionZoneSource;

        [Inject] private IPlayerLocator m_PlayerLocator;

        private NpcBlackboard m_Blackboard;
        private NpcStateMachine m_StateMachine;
        private ManualAggressionSource m_ManualAggressionSource; // валиден только при AggressionMode = Manual

        public void MethodInit()
        {
        }

        public void MethodStart()
        {
            if (m_Definition == null)
            {
                Debug.LogError($"{nameof(NpcController)} on '{name}': NpcTypeDefinitionSO не назначен.", this);
                enabled = false;
                return;
            }

            m_Blackboard = new NpcBlackboard(this, m_Agent, m_Definition)
            {
                PlayerTransform = m_PlayerLocator?.PlayerTransform,
                PatrolRoute = m_PatrolRoute,
                AggressionSource = CreateAggressionSource()
            };

            m_StateMachine = new NpcStateMachine(m_Blackboard);
            m_StateMachine.Register(new NpcIdleState());
            m_StateMachine.Register(new NpcObserveState());
            m_StateMachine.Register(new NpcPatrolState());
            m_StateMachine.Register(new NpcChaseState());
            m_StateMachine.Register(new NpcPanicState());
            m_StateMachine.Register(new NpcWanderState());
            m_StateMachine.Start(m_InitialState);
        }

        private INpcAggressionSource CreateAggressionSource()
        {
            switch (m_Definition.AggressionMode)
            {
                case NpcAggressionMode.Manual:
                    m_ManualAggressionSource = new ManualAggressionSource(m_Definition.StartsAggressive);
                    return m_ManualAggressionSource;

                case NpcAggressionMode.InfectionZoneLinked:
                    var zone = m_InfectionZoneSource as IInfectionZone;

                    if (zone == null)
                    {
                        Debug.LogWarning($"{nameof(NpcController)} on '{name}': AggressionMode = InfectionZoneLinked, но m_InfectionZoneSource не реализует IInfectionZone. Использую Fixed.", this);
                        return new FixedAggressionSource(m_Definition.StartsAggressive);
                    }

                    return new InfectionZoneAggressionSource(zone, m_Definition.InfectionAggressionThreshold);

                default:
                    return new FixedAggressionSource(m_Definition.StartsAggressive);
            }
        }

        public void CustomUpdate()
        {
            if (m_Blackboard == null)
            {
                return;
            }

            UpdatePerception();
            m_StateMachine.Tick(Time.deltaTime);
        }

        private void UpdatePerception()
        {
            var player = m_Blackboard.PlayerTransform;

            if (player == null)
            {
                m_Blackboard.CanSeePlayer = false;
                m_Blackboard.DistanceToPlayer = float.MaxValue;
                return;
            }

            var toPlayer = player.position - transform.position;
            float distance = toPlayer.magnitude;
            m_Blackboard.DistanceToPlayer = distance;

            if (distance > m_Definition.ViewDistance)
            {
                m_Blackboard.CanSeePlayer = false;
                return;
            }

            float angle = Vector3.Angle(transform.forward, toPlayer);

            if (angle > m_Definition.ViewHalfAngle)
            {
                m_Blackboard.CanSeePlayer = false;
                return;
            }

            if (m_Definition.RequireLineOfSight)
            {
                var eyeOrigin = transform.position + Vector3.up * 1.6f;
                var toPlayerEye = (player.position + Vector3.up * 1.0f) - eyeOrigin;

                if (Physics.Raycast(eyeOrigin, toPlayerEye.normalized, out _, toPlayerEye.magnitude, m_Definition.LineOfSightObstacles))
                {
                    m_Blackboard.CanSeePlayer = false;
                    return;
                }
            }

            m_Blackboard.CanSeePlayer = true;
        }

        // ---------------------------------------------------------------
        // Публичное API для сюжета/триггеров (UnityEvent-совместимые, без аргументов).
        // ---------------------------------------------------------------

        public void ForceIdle() => m_StateMachine?.ChangeState(NpcStateId.Idle);
        public void ForceObserve() => m_StateMachine?.ChangeState(NpcStateId.Observe);
        public void ForcePatrol() => m_StateMachine?.ChangeState(NpcStateId.Patrol);
        public void ForceChase() => m_StateMachine?.ChangeState(NpcStateId.Chase);
        public void ForcePanic() => m_StateMachine?.ChangeState(NpcStateId.Panic);
        public void ForceWander() => m_StateMachine?.ChangeState(NpcStateId.Wander);

        /// <summary>
        /// Переключить агрессивность бота (работает только если Definition.AggressionMode = Manual).
        /// Пример: мимик активирует агрессию после провала проверки документов;
        /// симулякр - когда шкала ярости заполнена триггерами игрока.
        /// </summary>
        public void SetAggressive(bool value)
        {
            if (m_ManualAggressionSource == null)
            {
                Debug.LogWarning($"{nameof(NpcController)} on '{name}': SetAggressive вызван, но AggressionMode != Manual.", this);
                return;
            }

            m_ManualAggressionSource.SetAggressive(value);
        }

        public void SetAggressiveOn() => SetAggressive(true);
        public void SetAggressiveOff() => SetAggressive(false);

        /// <summary>
        /// Подменить маршрут патрулирования целиком (сюжет может выбрать другой
        /// NpcPatrolRoute как статический параметр UnityEvent в инспекторе).
        /// </summary>
        public void SetPatrolRoute(NpcPatrolRoute newRoute)
        {
            m_PatrolRoute = newRoute;

            if (m_Blackboard != null)
            {
                m_Blackboard.PatrolRoute = newRoute;
                m_Blackboard.CurrentPatrolIndex = 0;
            }
        }

        /// <summary>Вызывается NpcChaseState, когда дистанция до игрока меньше CatchDistance.</summary>
        public void CatchPlayer()
        {
            PlayAnimationSound("Catch");
            m_OnPlayerCaughtChannel?.Raise();
            // Дальше сценарий обычно продолжает сюжет (StoryEventCondition на этот
            // канал): затемнение экрана, переспавн игрока и т.д. Сам бот после
            // этого чаще всего должен быть переведён в Idle/Patrol сюжетным
            // событием (ForceIdle/ForcePatrol) либо деактивирован.
        }

        public void RaiseReachedChaseStart()
        {
            m_OnChaseStartedChannel?.Raise();
        }
        
        /// <summary>Вызывается NpcPatrolState, когда бот дошёл до конца незацикленного маршрута патрулирования.</summary>
        public void RaisePatrolRouteCompleted()
        {
            m_OnPatrolRouteCompletedChannel?.Raise();
        }

        public void PlayAnimationSound(string key)
        {
            if (m_Definition?.AnimationSoundProfile == null)
            {
                return;
            }

            if (!m_Definition.AnimationSoundProfile.TryGet(key, out var entry))
            {
                return;
            }

            if (m_Animator != null && !string.IsNullOrEmpty(entry.AnimatorTrigger))
            {
                m_Animator.SetTrigger(entry.AnimatorTrigger);
            }

            if (m_AudioSource != null && entry.Sound != null)
            {
                m_AudioSource.loop = entry.LoopSound;
                m_AudioSource.clip = entry.Sound;
                m_AudioSource.Play();
            }
        }
    }

    // ---------------------------------------------------------------------
    // Как собрать три примера из задачи, используя только данные/инспектор
    // и композицию поведений (NpcInteractionPoint + INpcInteractionBehaviour),
    // НЕ наследование от NpcController:
    //
    // 1) Мирный сотрудник газовой службы:
    //    Definition: AggressionMode=Fixed, StartsAggressive=false, CanOpenDoors=false.
    //    InitialState=Idle. Рядом с ботом - NpcInteractionPoint со ссылкой на
    //    NpcDocumentCheckBehaviour (m_HasForgedDocuments=false). На воротах -
    //    StoryEventChannelListener, который по открытию ворот вызывает
    //    ForcePatrol() (отдельная NpcPatrolRoute из одной точки = "подойти к
    //    игроку"). После проверки документов NpcDocumentCheckBehaviour поднимает
    //    m_OnDocumentsValidChannel - сюжетная ветка (StoryEventCondition) ждёт
    //    этот канал, затем открывает ворота, что через StoryEventChannelListener
    //    вызывает ForcePatrol() бота с маршрутом "дойти до игрока и отдать посылку".
    //    Сценарий "ворота открыты и бот может достать игрока" реализуется через
    //    AggressionMode=Manual: StoryEventChannelListener на "ворота открыты,
    //    документы не проверялись" вызывает SetAggressiveOn() + ForceChase().
    //
    // 2) Мимик:
    //    Тот же NpcController/Definition, что и у "нормального" сотрудника
    //    (внешне и в Patrol/Idle/Observe неотличим), AggressionMode=Manual.
    //    Единственная разница - на NpcInteractionPoint подключена ДРУГАЯ
    //    настройка NpcDocumentCheckBehaviour (m_HasForgedDocuments=true).
    //    Как только документы проверены -> m_OnDocumentsForgedChannel.Raise()
    //    ловится StoryEventChannelListener, который вызывает SetAggressiveOn()
    //    + ForceChase() (у мимика ChaseStopsOnlyByStory=true, чтобы "не
    //    успокаивался", пока сюжет явно не вызовет ForceIdle()). Никакого
    //    нового кода/наследника не потребовалось - только другой набор
    //    компонентов-поведений на префабе.
    //
    // 3) Симулякр:
    //    AggressionMode=InfectionZoneLinked (агрессия включается сама выше
    //    порога заражения зоны) ИЛИ AggressionMode=Manual, если шкалу агрессии
    //    ведёт отдельный скрипт-триггер (реагирующий на действия игрока через
    //    UnityEvent), который при заполнении шкалы вызывает SetAggressiveOn().
    //    У симулякра NpcInteractionPoint скорее всего не нужен вообще (нет
    //    реакции на взаимодействие) - компонент просто не добавляется на
    //    префаб. Само "мытьё посуды" - обычная NpcPatrolRoute из 1-2 точек в
    //    Idle/Observe с анимацией по ключу "Idle"/"Wash".
    //
    // Итого: любое уникальное "что бот делает по интеракции" - это отдельный
    // маленький класс INpcInteractionBehaviour, подключаемый через
    // NpcInteractionPoint.m_Behaviour. NpcController остаётся один на всех
    // ботов и не знает ни о документах, ни о диалогах, ни о чём-либо ещё
    // специфичном для конкретного NPC.
    // ---------------------------------------------------------------------
}




