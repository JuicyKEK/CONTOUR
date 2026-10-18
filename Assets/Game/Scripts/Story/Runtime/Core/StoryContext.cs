using System.Collections.Generic;
using Game.Scripts.InputController;

namespace Game.Scripts.Story
{
    /// <summary>
    /// Всё, что нужно действиям/условиям сюжета при выполнении: частые сервисы сцены (свойствами),
    /// состояние сюжета (<see cref="State"/>) и доступ к любым другим бинам JuicyDI через
    /// <see cref="Resolve{T}"/> - новый сервис не требует правок StoryContext.
    /// Сами действия/условия не резолвят бины напрямую - только через контекст.
    ///
    /// Контекст GBS (с резолвером) не запоминает сервисы: каждое свойство берёт бин из JuicyDI в момент
    /// обращения. Поэтому подсказка, камеры и т.д. находятся, даже если их сцена загрузилась позже
    /// GBSStarter, и не остаются мёртвыми ссылками после её выгрузки.
    /// Старый StoryManager передаёт сервисы в конструктор - тогда используются они.
    /// </summary>
    public class StoryContext
    {
        private readonly IInputActions m_InputActions;
        private readonly IInputSelectionActions m_InputSelectionActions;
        private readonly IReadOnlyList<IPlayerControlHandle> m_PlayerControlHandles;
        private readonly IStoryHintView m_HintView;
        private readonly ICutsceneDirector m_CutsceneDirector;
        private readonly ICameraDirector m_CameraDirector;
        private readonly IScreenFader m_ScreenFader;
        private readonly IStoryServiceResolver m_Resolver;

        public IInputActions InputActions => m_InputActions ?? Resolve<IInputActions>();
        public IInputSelectionActions InputSelectionActions => m_InputSelectionActions ?? Resolve<IInputSelectionActions>();
        public IReadOnlyList<IPlayerControlHandle> PlayerControlHandles => m_PlayerControlHandles ?? ResolveAll<IPlayerControlHandle>();
        public IStoryHintView HintView => m_HintView ?? Resolve<IStoryHintView>();
        public ICutsceneDirector CutsceneDirector => m_CutsceneDirector ?? Resolve<ICutsceneDirector>();
        public ICameraDirector CameraDirector => m_CameraDirector ?? Resolve<ICameraDirector>();
        public IScreenFader ScreenFader => m_ScreenFader ?? Resolve<IScreenFader>();

        /// <summary>
        /// Состояние сюжета: флаги и сигналы по ключам (сохраняется вместе с прогрессом графов).
        /// </summary>
        public StoryState State { get; }

        /// <summary>
        /// Контекст GBS: все сервисы берутся из резолвера в момент обращения.
        /// </summary>
        public StoryContext(StoryState state, IStoryServiceResolver resolver)
        {
            State = state ?? new StoryState();
            m_Resolver = resolver;
        }

        /// <summary>
        /// Контекст старого StoryManager: сервисы переданы готовыми.
        /// </summary>
        public StoryContext(
            IInputActions inputActions,
            IInputSelectionActions inputSelectionActions,
            IReadOnlyList<IPlayerControlHandle> playerControlHandles,
            IStoryHintView hintView,
            ICutsceneDirector cutsceneDirector,
            ICameraDirector cameraDirector,
            IScreenFader screenFader,
            StoryState state = null,
            IStoryServiceResolver resolver = null)
        {
            m_InputActions = inputActions;
            m_InputSelectionActions = inputSelectionActions;
            m_PlayerControlHandles = playerControlHandles;
            m_HintView = hintView;
            m_CutsceneDirector = cutsceneDirector;
            m_CameraDirector = cameraDirector;
            m_ScreenFader = screenFader;
            State = state ?? new StoryState();
            m_Resolver = resolver;
        }

        /// <summary>
        /// Любой бин сцены (JuicyDI) или null, если его нет / контекст собран без резолвера (старый StoryManager).
        /// </summary>
        public T Resolve<T>() where T : class
        {
            return m_Resolver?.Resolve<T>();
        }

        /// <summary>
        /// Все бины контракта на загруженных сценах.
        /// </summary>
        public List<T> ResolveAll<T>() where T : class
        {
            return m_Resolver?.ResolveAll<T>() ?? new List<T>();
        }

        public void SetControlEnabled(bool isEnabled)
        {
            var handles = PlayerControlHandles;

            if (handles == null)
            {
                return;
            }

            for (int i = 0; i < handles.Count; i++)
            {
                handles[i]?.SetControlEnabled(isEnabled);
            }
        }

        // Ниже - старый API "blackboard" (SetBlackboardFlagAction / BlackboardFlagCondition).
        // Флаги теперь живут в State, поэтому видны и новым условиям графа, и сохраняются.

        public void SetFlag(string key, bool value)
        {
            State.SetFlag(key, value);
        }

        public bool HasFlag(string key)
        {
            return State.HasValue(key);
        }

        public bool TryGetFlag(string key, out bool value)
        {
            value = State.GetFlag(key);
            return State.HasValue(key);
        }
    }
}
