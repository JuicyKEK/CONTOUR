using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using GBS.Data;
using GBS.Runtime;
using GBS.Save;
using Game.Scripts.Story;
using Game.Scripts.Utilities.Save;
using JuicyDI;
using JuicyDI.Attributes;
using UnityEngine;

namespace GBS
{
    /// <summary>
    /// Точка входа графов сюжета в сцене. Заменяет StoryManager:
    ///  - строит StoryContext (сервисы сцены он берёт из JuicyDI в момент обращения);
    ///  - запускает все графы из списка (граф со связанной Start-нодой сам
    ///    подождёт своё условие, например завершение другого графа);
    ///  - умеет сохранять/загружать прогресс во время игры.
    ///
    /// Зависимости сюжета НЕ инжектятся через [Inject]: контекст резолвит их сам и
    /// опционально (см. StoryContext, GBSStoryContextBuilder), поэтому сцена может работать вообще
    /// без ICameraDirector / ICutsceneDirector / IScreenFader и т.д.
    /// Для нестандартной сборки контекста повесьте рядом свой GBSStoryContextProvider
    /// или унаследуйтесь от GBSStarter и переопределите BuildContext().
    ///
    /// GBSStarter - владелец состояния сюжета (флаги и сигналы по ключам): отдаёт его сцене как
    /// IStoryState (StorySignalEmitter, StoryTriggerZone, NPC и т.д.) и сохраняет вместе с прогрессом.
    ///
    /// Сохранение - только по вызову: общий контроллер сохранений (GameSaveController, ISaveParticipant)
    /// на чекпоинте, в том числе из графа действием "Save/Save Game". Загрузка - при старте сцены.
    /// </summary>
    [JDIMonoController]
    [SequenceParticipant(200)]
    public class GBSStarter : MonoBehaviour, ISequence, IGBSGraphProgressProvider, IStoryState, ISaveParticipant
    {
        [Header("Graphs")]
        [SerializeField] private List<GBSGraphSO> m_Graphs = new List<GBSGraphSO>();

        [Header("Save")]
        [SerializeField] private bool m_LoadOnStart = true;

        [Header("Dependencies")]
        [Tooltip("Обычно пусто. Компонент-наследник GBSStoryContextProvider, если нужно полностью подменить " +
                 "сборку контекста сюжета (тесты, заглушки сервисов). Пусто - работает стандартная сборка ниже.")]
        [SerializeField] private GBSStoryContextProvider m_ContextProvider;

        [Tooltip("Стандартная сборка контекста. Сервисы (подсказка, камеры, ввод...) берутся из JuicyDI сами. " +
                 "Галочки - проверка на старте: отметьте те, без которых сцена не работает, - если их нет, " +
                 "будет ошибка в консоли. Остальные могут отсутствовать.")]
        [SerializeField] private GBSStoryContextBuilder m_ContextBuilder = new GBSStoryContextBuilder();

        private readonly List<GBSGraphRunner> m_Runners = new List<GBSGraphRunner>();
        private readonly HashSet<string> m_CompletedGraphIds = new HashSet<string>();

        // Создаётся сразу (а не в MethodInit), чтобы сцена могла писать в состояние в любой момент.
        private readonly StoryState m_State = new StoryState();

        private StoryContext m_Context;
        private CancellationTokenSource m_LifetimeCts;

        public StoryContext Context => m_Context;
        public StoryState State => m_State;
        public IReadOnlyList<GBSGraphRunner> Runners => m_Runners;

        public void MethodInit()
        {
            m_Context = BuildContext();

            CreateRunners();

            // Сейв грузим в MethodInit: он у всех участников идёт раньше любого MethodStart, поэтому
            // восстановление состояния не затрёт флаги, которые сцена выставит в своих MethodStart
            // (например DoorStoryFlagBridge - текущее состояние двери).
            if (m_LoadOnStart)
            {
                LoadProgress();
            }
        }

        /// <summary>
        /// Подменить провайдер контекста до инициализации (например, из тестов или бутстрапа).
        /// </summary>
        public void SetContextProvider(GBSStoryContextProvider provider)
        {
            m_ContextProvider = provider;
        }

        /// <summary>
        /// Сборка контекста. Порядок: явный провайдер -> провайдер на этом же объекте ->
        /// дефолтный билдер с опциональными зависимостями.
        /// Переопределяется в наследниках, если нужен полностью свой набор зависимостей.
        /// </summary>
        protected virtual StoryContext BuildContext()
        {
            var resolver = new GBSDependencyResolver();

            if (m_ContextProvider == null)
            {
                m_ContextProvider = GetComponent<GBSStoryContextProvider>();
            }

            if (m_ContextProvider != null)
            {
                return m_ContextProvider.CreateContext(resolver, m_State);
            }

            return m_ContextBuilder.Build(resolver, m_State);
        }

        public void MethodStart()
        {
            m_LifetimeCts = new CancellationTokenSource();

            for (int i = 0; i < m_Runners.Count; i++)
            {
                m_Runners[i].RunAsync(m_LifetimeCts.Token).Forget();
            }
        }

        public bool IsGraphCompleted(GBSGraphSO graph)
        {
            if (graph == null)
            {
                return false;
            }

            return m_CompletedGraphIds.Contains(graph.GraphId);
        }

        /// <summary>Сохранить прогресс сюжета в файл.</summary>
        [ContextMenu("Save Progress")]
        public void SaveProgress()
        {
            var data = new GBSSaveData();

            for (int i = 0; i < m_Runners.Count; i++)
            {
                data.Graphs.Add(m_Runners[i].CaptureProgress());
            }

            foreach (var pair in m_State.Values)
            {
                data.StateKeys.Add(pair.Key);
                data.StateValues.Add(pair.Value);
            }

            GBSSaveSystem.Save(data);
        }

        /// <summary>Загрузить прогресс. Вызывается автоматически на старте (m_LoadOnStart).</summary>
        [ContextMenu("Load Progress")]
        public void LoadProgress()
        {
            var data = GBSSaveSystem.Load();

            if (data == null)
            {
                return;
            }

            m_CompletedGraphIds.Clear();

            for (int i = 0; i < data.Graphs.Count; i++)
            {
                var graphProgress = data.Graphs[i];

                if (graphProgress != null && graphProgress.IsCompleted)
                {
                    m_CompletedGraphIds.Add(graphProgress.GraphId);
                }
            }

            for (int i = 0; i < m_Runners.Count; i++)
            {
                var runner = m_Runners[i];
                runner.RestoreProgress(data.FindGraph(runner.Graph.GraphId));
            }

            m_State.Restore(data.GetStateValues());
        }

        /// <summary>Удалить файл сейва и сбросить прогресс в рантайме.</summary>
        [ContextMenu("Clear Progress")]
        public void ClearProgress()
        {
            GBSSaveSystem.Clear();

            m_CompletedGraphIds.Clear();
            m_State.Clear();

            for (int i = 0; i < m_Runners.Count; i++)
            {
                m_Runners[i].ResetProgress();
            }
        }

        private void OnDestroy()
        {
            m_LifetimeCts?.Cancel();
            m_LifetimeCts?.Dispose();
        }

        #region IStoryState - состояние сюжета для сцены

        public long Sequence => m_State.Sequence;

        public event Action<string> Changed
        {
            add => m_State.Changed += value;
            remove => m_State.Changed -= value;
        }

        public bool HasValue(string key) => m_State.HasValue(key);
        public int GetValue(string key) => m_State.GetValue(key);
        public bool GetFlag(string key) => m_State.GetFlag(key);
        public long GetLastChangeSequence(string key) => m_State.GetLastChangeSequence(key);
        public void SetValue(string key, int value) => m_State.SetValue(key, value);
        public void SetFlag(string key, bool value) => m_State.SetFlag(key, value);
        public void RaiseSignal(string key) => m_State.RaiseSignal(key);

        #endregion

        #region ISaveParticipant - участие в общем сохранении игры

        bool ISaveParticipant.HasSave => GBSSaveSystem.Exists();

        void ISaveParticipant.Save() => SaveProgress();

        void ISaveParticipant.DeleteSave() => GBSSaveSystem.Clear();

        #endregion

        private void CreateRunners()
        {
            m_Runners.Clear();

            for (int i = 0; i < m_Graphs.Count; i++)
            {
                var graph = m_Graphs[i];

                if (graph == null)
                {
                    continue;
                }

                var runner = new GBSGraphRunner(graph, m_Context, this);
                runner.Completed += OnGraphCompleted;
                m_Runners.Add(runner);
            }
        }

        private void OnGraphCompleted(GBSGraphRunner runner)
        {
            m_CompletedGraphIds.Add(runner.Graph.GraphId);
        }
    }
}

