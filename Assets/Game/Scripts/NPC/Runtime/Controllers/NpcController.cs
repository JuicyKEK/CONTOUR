using System;
using Cysharp.Threading.Tasks;
using Game.Scripts.InfectionZone.Runtime.Interfaces;
using Game.Scripts.InteractionObjects.Interfaces;
using Game.Scripts.LocationScene.Runtime.Interfaces;
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
using UnityEngine.Events;

namespace Game.Scripts.NPC.Runtime.Controllers
{
    /// <summary>
    /// Единый контроллер поведения бота на FSM. Один и тот же класс обслуживает
    /// любой тип NPC - вся разница задаётся ассетом <see cref="NpcTypeDefinitionSO"/>
    /// (скорости, агрессия и шкала злости, восприятие, атака, поиск, двери) и сценными ссылками
    /// (маршрут, облики, запасы, граф бота).
    ///
    /// Поведение "само по себе": спокойное (обход маршрута/роль мимика) -> увидел игрока и агрессивен ->
    /// погоня -> атака (удар или убийство) / потерял из виду -> добежал до места, где видел последним ->
    /// поиск вокруг -> при высоком заражении поиск по всей зоне -> не нашёл - снова спокойное поведение.
    ///
    /// Управление "снаружи":
    ///  - публичные no-op методы Force*/SetAggressive*/MoveTo/... - их удобно вызывать из инспектора
    ///    (UnityEvent на StoryEventChannelListener, StorySceneReactions, триггерах);
    ///  - свой граф бота (<see cref="NpcGraphRunner"/> + шаги графа "NPC/...") - для ботов со сценой
    ///    (сотрудник газовой службы, неявный мимик у будки).
    ///
    /// Удар и убийство бот только сообщает (сигналы сюжета, события ниже); последствия решает сюжет.
    /// Исключение - галочка "вернуть к последнему сохранению" при убийстве.
    /// </summary>
    [JDIMonoController]
    [SequenceParticipant(150)]
    public class NpcController : MonoBehaviour, ISequence, IUpdateSequence
    {
        [Header("Конфигурация типа бота")]
        [SerializeField] private NpcTypeDefinitionSO m_Definition;

        [Header("Компоненты")]
        [SerializeField] private NavMeshAgent m_Agent;
        [Tooltip("Animator модели. Если есть NpcAppearance - берётся Animator текущего облика.")]
        [SerializeField] private Animator m_Animator;
        [Tooltip("Источник звуков действий по ключу (как раньше).")]
        [SerializeField] private AudioSource m_AudioSource;
        [Tooltip("Источник шагов. Пусто - бот создаст свой (3D-звук).")]
        [SerializeField] private AudioSource m_FootstepSource;
        [Tooltip("Источник голоса (сопение, рык...). Пусто - бот создаст свой (3D-звук).")]
        [SerializeField] private AudioSource m_VoiceSource;
        [Tooltip("Облики бота (мимик: человек/монстр). Пусто - ищется на этом объекте; может не быть вовсе.")]
        [SerializeField] private NpcAppearance m_Appearance;

        [Header("Патрулирование")]
        [SerializeField] private NpcPatrolRoute m_PatrolRoute;

        [Header("Старт")]
        [SerializeField] private NpcStateId m_InitialState = NpcStateId.Patrol;
        [Tooltip("Спокойное поведение: в это состояние бот возвращается, не найдя игрока, или когда агрессию сняли " +
                 "(мимик - Patrol по точкам своей роли, монстр - Patrol по обходу, сотрудник - Idle).")]
        [SerializeField] private NpcStateId m_CalmState = NpcStateId.Patrol;

        [Header("Поиск по всей зоне")]
        [Tooltip("Область поиска 'по всей зоне'. Пусто - территория зоны заражения бота (её InfectionZoneAreaTrigger).")]
        [SerializeField] private NpcSearchArea m_SearchArea;

        [Header("Сюжетные сигналы (все опциональны)")]
        [Tooltip("Сигнал сюжета в момент, когда бот догнал и схватил игрока - при любой удачной атаке (удар или убийство).")]
        [SerializeField, StoryKey(StoryKeyKind.Signal)] private string m_PlayerCaughtSignal;
        [Tooltip("Сигнал сюжета: бот ударил игрока (атака 'Удар').")]
        [SerializeField, StoryKey(StoryKeyKind.Signal)] private string m_PlayerHitSignal;
        [Tooltip("Сигнал сюжета: бот убил игрока (атака 'Убийство').")]
        [SerializeField, StoryKey(StoryKeyKind.Signal)] private string m_PlayerKilledSignal;
        [Tooltip("Сигнал сюжета в момент начала преследования (полезно для катсцен/скримеров).")]
        [SerializeField, StoryKey(StoryKeyKind.Signal)] private string m_ChaseStartedSignal;

        [Header("События сцены")]
        [Tooltip("Бот ударил игрока - например затемнение и толчок камеры.")]
        [SerializeField] private UnityEvent m_OnPlayerHit;
        [Tooltip("Бот убил игрока.")]
        [SerializeField] private UnityEvent m_OnPlayerKilled;

        [Header("Смерть игрока")]
        [Tooltip("Когда бот убил игрока, вернуть игру к последнему сохранению (через IPlayerCheckpointService). " +
                 "Выключите, если смерть показывает сюжет сам.")]
        [SerializeField] private bool m_ReturnToLastSaveOnKill = true;
        [Tooltip("Через сколько секунд после убийства вернуться к сохранению (успеть показать поедание).")]
        [SerializeField, Min(0f)] private float m_ReturnToLastSaveDelay = 3f;
        [Tooltip("Через сколько секунд после убийства бот возвращается к спокойному поведению (если сюжет не дал ему " +
                 "другую команду). 0 - стоит, пока не придёт команда. Нужно, когда смерть показывает сюжет без перезагрузки.")]
        [SerializeField, Min(0f)] private float m_CalmDownAfterKillDelay;

        [Header("Сюжетные события - устаревшие SO-каналы (используйте сигналы выше)")]
        [Tooltip("Раздаётся при любой удачной атаке бота (удар или убийство).")]
        [SerializeField] private StoryEventChannelSO m_OnPlayerCaughtChannel;
        [Tooltip("Раздаётся в момент начала преследования (полезно для катсцен/скримеров).")]
        [SerializeField] private StoryEventChannelSO m_OnChaseStartedChannel;
        [Tooltip("Раздаётся один раз, когда бот дошёл до последней точки НЕзацикленного маршрута патрулирования (NpcPatrolRoute.Loop == false).")]
        [SerializeField] private StoryEventChannelSO m_OnPatrolRouteCompletedChannel;

        [Header("Зона заражения")]
        [Tooltip("Компонент, реализующий IInfectionZone - зона бота (агрессия от заражения, шкала злости, поиск по зоне). " +
                 "Пусто - зона определяется по триггеру зоны (InfectionZoneAreaTrigger), в котором стоит бот.")]
        [SerializeField] private MonoBehaviour m_InfectionZoneSource;

        [Header("Отладка")]
        [Tooltip("Писать в консоль смену состояний и события бота ([NPC] ...).")]
        [SerializeField] private bool m_DebugLog;

        [Inject] private IPlayerLocator m_PlayerLocator;

        private readonly NpcAnimatorDriver m_AnimatorDriver = new();

        private NpcBlackboard m_Blackboard;
        private NpcStateMachine m_StateMachine;
        private NpcPerception m_Perception;
        private NpcAudioPlayer m_Audio;
        private ManualAggressionSource m_ManualAggressionSource; // валиден только при AggressionMode = Manual
        private AngerMeterAggressionSource m_AngerMeter;          // валиден только при AggressionMode = AngerMeter
        private float m_CurrentSpeed;

        /// <summary>Смена состояния (from, to).</summary>
        public event Action<NpcStateId, NpcStateId> StateChanged;
        public event Action PlayerHit;
        public event Action PlayerKilled;

        public bool IsInitialized => m_StateMachine != null;
        public NpcTypeDefinitionSO Definition => m_Definition;
        public NpcStateId CurrentState => m_StateMachine?.CurrentStateId ?? m_InitialState;
        public bool IsAggressive => m_Blackboard != null && m_Blackboard.IsAggressive;
        public bool IsHunting => m_Blackboard != null && m_Blackboard.IsHunting;
        public bool CanSeePlayer => m_Blackboard != null && m_Blackboard.CanSeePlayer;
        public float DistanceToPlayer => m_Blackboard?.DistanceToPlayer ?? float.MaxValue;
        public IInfectionZone Zone => m_Blackboard?.Zone;
        public NpcSearchArea SearchArea => m_SearchArea;
        public NpcAppearance Appearance => m_Appearance;
        public NavMeshAgent Agent => m_Agent;

        /// <summary>Шкала злости 0..1 (только для режима "Шкала злости", иначе 0).</summary>
        public float Anger => m_AngerMeter?.Anger.CurrentValue ?? 0f;

        // Счётчики событий - по ним условия графа бота ждут "ударил", "дошёл" и т.п. после входа в ноду.
        public int HitCount { get; private set; }
        public int KillCount { get; private set; }
        public int ArrivedCount { get; private set; }
        public int InteractedCount { get; private set; }
        public int ActionFinishedCount { get; private set; }

        /// <summary>Номер последнего начатого действия на месте (PlayAction) и последнего доигравшего до конца.</summary>
        public int ActionSerial { get; private set; }
        public int FinishedActionSerial { get; private set; }
        public string LastArrivedPointKey { get; private set; }

        private NpcAnimationSoundProfileSO CurrentProfile =>
            m_Appearance != null && m_Appearance.Current?.SoundProfile != null
                ? m_Appearance.Current.SoundProfile
                : m_Definition != null ? m_Definition.AnimationSoundProfile : null;

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

            if (m_Agent == null)
            {
                m_Agent = GetComponent<NavMeshAgent>();
            }

            m_Blackboard = new NpcBlackboard(this, m_Agent, m_Definition)
            {
                PlayerTransform = m_PlayerLocator?.PlayerTransform,
                PatrolRoute = m_PatrolRoute,
                CalmState = m_CalmState
            };

            m_Blackboard.Zone = ResolveZone();
            m_Blackboard.AggressionSource = CreateAggressionSource();
            m_Perception = new NpcPerception(transform, m_Definition);
            m_Audio = new NpcAudioPlayer(m_AudioSource, EnsureSource(ref m_FootstepSource, "Footsteps"), EnsureSource(ref m_VoiceSource, "Voice"));

            if (m_Appearance == null)
            {
                m_Appearance = GetComponent<NpcAppearance>();
            }

            if (m_Appearance != null)
            {
                m_Appearance.FormChanged += OnFormChanged;
            }

            BindForm();

            m_StateMachine = new NpcStateMachine(m_Blackboard);
            m_StateMachine.Register(new NpcIdleState());
            m_StateMachine.Register(new NpcObserveState());
            m_StateMachine.Register(new NpcPatrolState());
            m_StateMachine.Register(new NpcChaseState());
            m_StateMachine.Register(new NpcPanicState());
            m_StateMachine.Register(new NpcWanderState());
            m_StateMachine.Register(new NpcSearchState());
            m_StateMachine.Register(new NpcAttackState());
            m_StateMachine.Register(new NpcPerformState());
            m_StateMachine.Register(new NpcMoveToState());
            m_StateMachine.StateChanged += OnStateChanged;
            m_StateMachine.Start(m_InitialState);
        }

        private void OnEnable()
        {
            // Бот, выключенный на старте сцены (появляется по ходу сюжета), включился: заново выставить
            // агенту цель текущего состояния, а аниматору - анимацию.
            if (m_StateMachine == null)
            {
                return;
            }

            if (m_InfectionZoneSource == null)
            {
                m_Blackboard.Zone = ResolveZone();
            }

            m_AngerMeter?.SetStoryState(StorySignals.Current);
            m_StateMachine.Reenter();
        }

        private void OnDisable()
        {
            m_Audio?.StopAll();
        }

        private void OnDestroy()
        {
            if (m_Appearance != null)
            {
                m_Appearance.FormChanged -= OnFormChanged;
            }

            (m_Blackboard?.AggressionSource as IDisposable)?.Dispose();
        }

        private IInfectionZone ResolveZone()
        {
            if (m_InfectionZoneSource != null)
            {
                if (m_InfectionZoneSource is IInfectionZone zone)
                {
                    return zone;
                }

                Debug.LogWarning($"{nameof(NpcController)} on '{name}': m_InfectionZoneSource не реализует IInfectionZone.", this);
            }

            return NpcZoneUtility.FindZoneAt(transform.position);
        }

        private INpcAggressionSource CreateAggressionSource()
        {
            switch (m_Definition.AggressionMode)
            {
                case NpcAggressionMode.Manual:
                    m_ManualAggressionSource = new ManualAggressionSource(m_Definition.StartsAggressive);
                    return m_ManualAggressionSource;

                case NpcAggressionMode.InfectionZoneLinked:
                    if (m_Blackboard.Zone == null)
                    {
                        Debug.LogWarning($"{nameof(NpcController)} on '{name}': AggressionMode = InfectionZoneLinked, но зона бота не найдена " +
                                         "(не задан m_InfectionZoneSource и бот не стоит в триггере зоны). Использую Fixed.", this);
                        return new FixedAggressionSource(m_Definition.StartsAggressive);
                    }

                    return new InfectionZoneAggressionSource(m_Blackboard.Zone, m_Definition.InfectionAggressionThreshold);

                case NpcAggressionMode.AngerMeter:
                    m_AngerMeter = new AngerMeterAggressionSource(m_Definition, StorySignals.Current);
                    return m_AngerMeter;

                default:
                    return new FixedAggressionSource(m_Definition.StartsAggressive);
            }
        }

        private AudioSource EnsureSource(ref AudioSource source, string objectName)
        {
            if (source != null)
            {
                return source;
            }

            var profile = CurrentProfile;
            var holder = new GameObject(objectName);
            holder.transform.SetParent(transform, false);
            holder.transform.localPosition = Vector3.up * 1f;
            source = holder.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 1f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = profile != null ? profile.MinDistance : 1f;
            source.maxDistance = profile != null ? profile.MaxDistance : 20f;
            source.outputAudioMixerGroup = m_AudioSource != null ? m_AudioSource.outputAudioMixerGroup : null;
            return source;
        }

        public void CustomUpdate()
        {
            if (m_Blackboard == null || !isActiveAndEnabled)
            {
                return;
            }

            float deltaTime = Time.deltaTime;

            if (m_Blackboard.PlayerTransform == null)
            {
                m_Blackboard.PlayerTransform = m_PlayerLocator?.PlayerTransform;
            }

            UpdatePerception(deltaTime);

            if (m_Blackboard.AttackCooldownTimer > 0f)
            {
                m_Blackboard.AttackCooldownTimer -= deltaTime;
            }

            m_AngerMeter?.Tick(m_Blackboard.CanSeePlayer, m_Blackboard.ZoneInfectionLevel, deltaTime);
            m_StateMachine.Tick(deltaTime);

            m_CurrentSpeed = 0f;

            if (NpcNavigation.IsReady(m_Agent))
            {
                var velocity = m_Agent.velocity;
                velocity.y = 0f;
                m_CurrentSpeed = velocity.magnitude;
            }

            m_AnimatorDriver.Update(m_CurrentSpeed, m_StateMachine.CurrentStateId, m_Blackboard.IsAggressive, Anger);
            m_Audio.Tick(transform.position, m_CurrentSpeed, deltaTime);
        }

        private void UpdatePerception(float deltaTime)
        {
            var player = m_Blackboard.PlayerTransform;

            if (player == null)
            {
                m_Blackboard.CanSeePlayer = false;
                m_Blackboard.DistanceToPlayer = float.MaxValue;
                return;
            }

            bool canSee = m_Perception.CanSee(player, out float distance);
            m_Blackboard.DistanceToPlayer = distance;

            if (canSee && !m_Blackboard.CanSeePlayer)
            {
                LogDebug("видит игрока");
            }
            else if (!canSee && m_Blackboard.CanSeePlayer)
            {
                LogDebug("потерял игрока из виду");
            }

            m_Blackboard.CanSeePlayer = canSee;

            if (canSee)
            {
                m_Blackboard.LastKnownPlayerPosition = player.position;
                m_Blackboard.HasLastKnownPlayerPosition = true;
                m_Blackboard.TimeSincePlayerLastSeen = 0f;
            }
            else
            {
                m_Blackboard.TimeSincePlayerLastSeen += deltaTime;
            }
        }

        private void OnStateChanged(NpcStateId from, NpcStateId to)
        {
            if (from != to)
            {
                LogDebug($"{from} -> {to}");
            }

            m_Audio?.OnStateChanged(to);
            StateChanged?.Invoke(from, to);
        }

        private void OnFormChanged(NpcAppearance.Form form)
        {
            BindForm();
            LogDebug($"облик '{form?.Key}'");
        }

        private void BindForm()
        {
            var formAnimator = m_Appearance != null ? m_Appearance.Current?.Animator : null;
            m_AnimatorDriver.Bind(formAnimator != null ? formAnimator : m_Animator);
            m_Audio?.SetProfile(CurrentProfile);

            if (m_StateMachine != null)
            {
                m_Audio?.OnStateChanged(m_StateMachine.CurrentStateId);
            }
        }

        // ---------------------------------------------------------------
        // Публичное API для сюжета/триггеров (UnityEvent-совместимые, без аргументов).
        // ---------------------------------------------------------------

        public void ForceIdle() => ChangeState(NpcStateId.Idle);
        public void ForceObserve() => ChangeState(NpcStateId.Observe);
        public void ForcePanic() => ChangeState(NpcStateId.Panic);
        public void ForceWander() => ChangeState(NpcStateId.Wander);

        /// <summary>Спокойный обход текущего маршрута с первой точки.</summary>
        public void ForcePatrol()
        {
            StopHunting();
            ChangeState(NpcStateId.Patrol);
        }

        /// <summary>Погоня по обычным правилам: потеряет игрока из виду - будет искать.</summary>
        public void ForceChase()
        {
            ChangeState(NpcStateId.Chase);
        }

        /// <summary>
        /// Сюжетная погоня: бот всегда знает, где игрок, не ищет его и сам не останавливается -
        /// пока сюжет не вызовет LoseInterest / ForcePatrol / ForceIdle / Despawn.
        /// </summary>
        public void ForceHunt()
        {
            if (m_Blackboard == null)
            {
                return;
            }

            m_Blackboard.IsHunting = true;
            ChangeState(NpcStateId.Chase);
        }

        /// <summary>Искать игрока вокруг места, где бот видел его последним (или вокруг себя).</summary>
        public void ForceSearch()
        {
            if (m_Blackboard == null)
            {
                return;
            }

            m_Blackboard.SearchCenter = m_Blackboard.HasLastKnownPlayerPosition
                ? m_Blackboard.LastKnownPlayerPosition
                : transform.position;
            ChangeState(NpcStateId.Search);
        }

        /// <summary>
        /// Потерять интерес к игроку: снять сюжетную погоню, успокоиться (шкала злости сбрасывается)
        /// и вернуться к спокойному поведению.
        /// </summary>
        public void LoseInterest()
        {
            StopHunting();
            PrepareReturnToCalm();
            ChangeState(m_Blackboard?.CalmState ?? m_CalmState);
        }

        /// <summary>Убрать бота со сцены (выключить объект).</summary>
        public void Despawn()
        {
            LogDebug("исчез");
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Переключить агрессивность бота. Работает в режимах "Ручное переключение" и "Шкала злости"
        /// (там - разозлить сразу или успокоить).
        /// </summary>
        public void SetAggressive(bool value)
        {
            if (m_ManualAggressionSource != null)
            {
                m_ManualAggressionSource.SetAggressive(value);
                return;
            }

            if (m_AngerMeter != null)
            {
                if (value)
                {
                    m_AngerMeter.Enrage();
                }
                else
                {
                    m_AngerMeter.CalmDown();
                }

                return;
            }

            Debug.LogWarning($"{nameof(NpcController)} on '{name}': SetAggressive вызван, но AggressionMode не Manual и не AngerMeter.", this);
        }

        public void SetAggressiveOn() => SetAggressive(true);
        public void SetAggressiveOff() => SetAggressive(false);

        /// <summary>
        /// Изменить шкалу злости (режим "Шкала злости"): больше нуля - злит, меньше - успокаивает.
        /// Для UnityEvent на предметах, с которыми взаимодействует игрок.
        /// </summary>
        public void AddAnger(float delta)
        {
            if (m_AngerMeter == null)
            {
                Debug.LogWarning($"{nameof(NpcController)} on '{name}': AddAnger вызван, но AggressionMode не 'Шкала злости'.", this);
                return;
            }

            m_AngerMeter.AddAnger(delta);
        }

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

        /// <summary>Сменить спокойное поведение (например мимик раскрылся - теперь спокойно он бродит, а не изображает роль).</summary>
        public void SetCalmState(NpcStateId calmState)
        {
            m_CalmState = calmState;

            if (m_Blackboard != null)
            {
                m_Blackboard.CalmState = calmState;
            }
        }

        /// <summary>Идти в точку (UnityEvent): спокойно, встать как повёрнута точка.</summary>
        public void MoveTo(Transform point)
        {
            if (point != null)
            {
                MoveTo(point.position, point.rotation, NpcPace.Calm, point.name);
            }
        }

        /// <summary>
        /// Идти в точку с заданной скоростью; rotation - как встать по прибытии (null - как пришёл).
        /// pointKey попадает в LastArrivedPointKey (по нему граф понимает, куда бот дошёл).
        /// </summary>
        public void MoveTo(Vector3 position, Quaternion? rotation, NpcPace pace, string pointKey = null)
        {
            if (m_Blackboard == null)
            {
                return;
            }

            m_Blackboard.MoveTarget = position;
            m_Blackboard.MoveTargetRotation = rotation;
            m_Blackboard.MovePace = pace;
            m_Blackboard.MoveTargetKey = pointKey;
            ChangeState(NpcStateId.MoveTo);
        }

        /// <summary>
        /// Действие на месте по ключу анимации/звука: duration секунд, потом состояние nextState
        /// (duration 0 - стоит с этим действием до следующей команды).
        /// </summary>
        public void PlayAction(string key, float duration, NpcStateId nextState = NpcStateId.Idle)
        {
            if (m_Blackboard == null)
            {
                return;
            }

            ActionSerial++;
            m_Blackboard.PerformKey = key;
            m_Blackboard.PerformDuration = duration;
            m_Blackboard.PerformNextState = nextState;
            ChangeState(NpcStateId.Perform);
        }

        /// <summary>Сменить облик (NpcAppearance) - например мимик превращается в монстра.</summary>
        public bool ChangeForm(string formKey)
        {
            if (m_Appearance == null)
            {
                Debug.LogWarning($"{nameof(NpcController)} on '{name}': у бота нет NpcAppearance - облик '{formKey}' не сменить.", this);
                return false;
            }

            return m_Appearance.SetForm(formKey);
        }

        /// <summary>Сменить облик (UnityEvent).</summary>
        public void SetForm(string formKey)
        {
            ChangeForm(formKey);
        }

        private void ChangeState(NpcStateId stateId)
        {
            m_StateMachine?.ChangeState(stateId);
        }

        private void StopHunting()
        {
            if (m_Blackboard != null)
            {
                m_Blackboard.IsHunting = false;
            }
        }

        // ---------------------------------------------------------------
        // Вызывается состояниями и компонентами бота.
        // ---------------------------------------------------------------

        /// <summary>
        /// Бот возвращается к спокойному поведению: шкала злости мимика сбрасывается, обход маршрута
        /// продолжается с ближайшей точки.
        /// </summary>
        public void PrepareReturnToCalm()
        {
            if (m_Blackboard == null)
            {
                return;
            }

            m_Blackboard.IsHunting = false;
            m_Blackboard.PatrolResumeNearest = true;
            m_AngerMeter?.CalmDown();
            LogDebug("возвращается к спокойному поведению");
        }

        /// <summary>Как этот бот сейчас открывает двери (по облику, иначе по настройкам типа).</summary>
        public DoorOpenerKind GetDoorOpener(bool isChasing)
        {
            if (m_Appearance != null && m_Appearance.Current != null)
            {
                return m_Appearance.Current.DoorOpener;
            }

            return isChasing ? m_Definition.ChaseDoorOpenerKind : m_Definition.PatrolDoorOpenerKind;
        }

        /// <summary>
        /// Удар попал (вызывает состояние атаки): удар или убийство - по NpcTypeDefinitionSO.AttackKind.
        /// </summary>
        public void ApplyAttack()
        {
            if (m_Definition.AttackKind == NpcAttackKind.Kill)
            {
                RaisePlayerKilled();
            }
            else
            {
                RaisePlayerHit();
            }
        }

        /// <summary>Старое имя: засчитать удачную атаку (удар или убийство - по настройкам типа бота).</summary>
        public void CatchPlayer()
        {
            ApplyAttack();
        }

        private void RaisePlayerHit()
        {
            HitCount++;
            LogDebug("ударил игрока");
            PlayAnimationSound("AttackHit");
            StorySignals.Raise(m_PlayerCaughtSignal);
            StorySignals.Raise(m_PlayerHitSignal);
            m_OnPlayerCaughtChannel?.Raise();
            m_OnPlayerHit?.Invoke();
            PlayerHit?.Invoke();
        }

        private void RaisePlayerKilled()
        {
            KillCount++;
            LogDebug("убил игрока");
            PlayAnimationSound("Kill");
            StorySignals.Raise(m_PlayerCaughtSignal);
            StorySignals.Raise(m_PlayerKilledSignal);
            m_OnPlayerCaughtChannel?.Raise();
            m_OnPlayerKilled?.Invoke();
            PlayerKilled?.Invoke();

            if (m_ReturnToLastSaveOnKill)
            {
                ReturnToLastSaveAsync().Forget();
            }

            if (m_CalmDownAfterKillDelay > 0f)
            {
                CalmDownAfterKillAsync(KillCount).Forget();
            }
        }

        private async UniTaskVoid CalmDownAfterKillAsync(int killNumber)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(m_CalmDownAfterKillDelay), cancellationToken: destroyCancellationToken);

            // Пока бот "ел", сюжет мог дать ему другую команду - тогда её не перебиваем.
            bool isStillAfterThisKill = KillCount == killNumber
                                        && (CurrentState == NpcStateId.Idle || CurrentState == NpcStateId.Attack);

            if (isActiveAndEnabled && isStillAfterThisKill)
            {
                LoseInterest();
            }
        }

        private async UniTaskVoid ReturnToLastSaveAsync()
        {
            await UniTask.Delay(TimeSpan.FromSeconds(m_ReturnToLastSaveDelay), cancellationToken: destroyCancellationToken);

            var checkpointService = BinController.GetContext()?.GetBean<IPlayerCheckpointService>();

            if (checkpointService == null)
            {
                Debug.LogWarning($"[NPC] {name}: игрок убит, но в сцене нет IPlayerCheckpointService (PlayerCheckpointController ядра игры) - " +
                                 "вернуться к сохранению нельзя (например, сцена запущена сама по себе, без ядра).", this);
                return;
            }

            checkpointService.ReturnToLastSave();
        }

        public void RaiseReachedChaseStart()
        {
            StorySignals.Raise(m_ChaseStartedSignal);
            m_OnChaseStartedChannel?.Raise();
        }

        /// <summary>Вызывается NpcPatrolState, когда бот дошёл до конца незацикленного маршрута патрулирования.</summary>
        public void RaisePatrolRouteCompleted()
        {
            //m_OnPatrolRouteCompletedChannel?.Raise();
        }

        /// <summary>Игрок взаимодействовал с ботом (NpcInteractionPoint).</summary>
        public void NotifyPlayerInteracted()
        {
            InteractedCount++;
            LogDebug("игрок взаимодействовал с ботом");
        }

        public void NotifyArrived(string pointKey)
        {
            ArrivedCount++;
            LastArrivedPointKey = pointKey;
            LogDebug($"дошёл до точки '{pointKey}'");
        }

        public void NotifyActionFinished()
        {
            ActionFinishedCount++;
            FinishedActionSerial = ActionSerial;
        }

        /// <summary>
        /// Анимация (триггер аниматора) и звук по ключу из профиля текущего облика/типа бота.
        /// Если записи с таким ключом нет, но в контроллере аниматора есть триггер с именем ключа - сработает он.
        /// </summary>
        public void PlayAnimationSound(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            var profile = CurrentProfile;
            NpcAnimationSoundEntry entry = default;
            bool hasEntry = profile != null && profile.TryGet(key, out entry);
            string trigger = hasEntry && !string.IsNullOrEmpty(entry.AnimatorTrigger) ? entry.AnimatorTrigger : key;

            if (m_StateMachine == null)
            {
                // Вызов до старта бота (например из взаимодействия) - аниматор ещё не привязан.
                m_AnimatorDriver.Bind(m_Animator);
            }

            m_AnimatorDriver.TrySetTrigger(trigger);

            if (hasEntry)
            {
                m_Audio?.PlayAction(entry);
            }
        }

        /// <summary>Шаг (событие анимации, NpcAnimationEventRelay).</summary>
        public void PlayFootstep()
        {
            m_Audio?.PlayFootstep(m_CurrentSpeed);
        }

        public void LogDebug(string message)
        {
            if (m_DebugLog)
            {
                Debug.Log($"[NPC] {name}: {message}", this);
            }
        }

        // ---------------------------------------------------------------
        // Отладка в сцене: конус обзора, где видел игрока, область поиска, цель.
        // ---------------------------------------------------------------

        private void OnDrawGizmosSelected()
        {
            if (m_Definition == null)
            {
                return;
            }

            var eye = transform.position + Vector3.up * m_Definition.EyeHeight;
            Gizmos.color = CanSeePlayer ? new Color(1f, 0.2f, 0.2f) : new Color(1f, 0.9f, 0.2f);
            DrawViewCone(eye);

            if (m_Blackboard == null)
            {
                return;
            }

            if (m_Blackboard.HasLastKnownPlayerPosition)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(m_Blackboard.LastKnownPlayerPosition + Vector3.up * 0.1f, 0.3f);
            }

            if (CurrentState == NpcStateId.Search)
            {
                Gizmos.color = new Color(1f, 0.6f, 0f);
                DrawCircle(m_Blackboard.SearchCenter, m_Definition.SearchRadius);
            }

            if (CurrentState == NpcStateId.MoveTo)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(transform.position, m_Blackboard.MoveTarget);
                Gizmos.DrawWireSphere(m_Blackboard.MoveTarget, 0.25f);
            }

            if (m_Perception != null && CanSeePlayer)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(m_Perception.LastEyePosition, m_Perception.LastTargetPosition);
            }
        }

        private void DrawViewCone(Vector3 eye)
        {
            float distance = m_Definition.ViewDistance;
            float halfAngle = Mathf.Clamp(m_Definition.ViewHalfAngle, 0f, 180f);
            const int segments = 24;
            var previous = eye + Quaternion.Euler(0f, -halfAngle, 0f) * transform.forward * distance;
            Gizmos.DrawLine(eye, previous);

            for (int i = 1; i <= segments; i++)
            {
                float angle = Mathf.Lerp(-halfAngle, halfAngle, i / (float)segments);
                var next = eye + Quaternion.Euler(0f, angle, 0f) * transform.forward * distance;
                Gizmos.DrawLine(previous, next);
                previous = next;
            }

            Gizmos.DrawLine(eye, previous);
        }

        private static void DrawCircle(Vector3 center, float radius)
        {
            const int segments = 32;
            var previous = center + new Vector3(radius, 0.1f, 0f);

            for (int i = 1; i <= segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                var next = center + new Vector3(Mathf.Cos(angle) * radius, 0.1f, Mathf.Sin(angle) * radius);
                Gizmos.DrawLine(previous, next);
                previous = next;
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!Application.isPlaying || m_StateMachine == null)
            {
                return;
            }

            string label = $"{name}\n{CurrentState}" +
                           (IsHunting ? " (сюжетная погоня)" : string.Empty) +
                           (IsAggressive ? " · агрессивен" : string.Empty) +
                           (m_AngerMeter != null ? $"\nзлость {Anger:P0}" : string.Empty);
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2.3f, label);
        }
#endif
    }

    // ---------------------------------------------------------------------
    // Ниже - исходный рецепт сборки ботов через старый сюжетный менеджер (StoryManager + UnityEvent).
    // Он по-прежнему работает (так собран сотрудник газовой службы в LoacationMain). Новые варианты -
    // свой граф бота (NpcGraphRunner, шаги графа "NPC/..."), шкала злости, поиск, облики - см. описания
    // классов NpcGraphRunner, NpcTypeDefinitionSO, NpcAppearance.
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
