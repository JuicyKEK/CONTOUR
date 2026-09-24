using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using GBS.Data;
using GBS.Runtime;
using GBS.Save;
using Game.Scripts.Story;
using JuicyDI;
using JuicyDI.Attributes;
using UnityEngine;

namespace GBS
{
    /// <summary>
    /// Точка входа графов сюжета в сцене. Заменяет StoryManager:
    ///  - собирает зависимости через JuicyDI и строит StoryContext;
    ///  - запускает все графы из списка (граф со связанной Start-нодой сам
    ///    подождёт своё условие, например завершение другого графа);
    ///  - умеет сохранять/загружать прогресс во время игры.
    ///
    /// Зависимости сюжета НЕ инжектятся через [Inject]: они резолвятся вручную и
    /// опционально (см. GBSStoryContextBuilder), поэтому сцена может работать вообще
    /// без ICameraDirector / ICutsceneDirector / IScreenFader и т.д.
    /// Для нестандартной сборки контекста повесьте рядом свой GBSStoryContextProvider
    /// или унаследуйтесь от GBSStarter и переопределите BuildContext().
    /// </summary>
    [JDIMonoController]
    [SequenceParticipant(200)]
    public class GBSStarter : MonoBehaviour, ISequence, IGBSGraphProgressProvider
    {
        [Header("Graphs")]
        [SerializeField] private List<GBSGraphSO> m_Graphs = new List<GBSGraphSO>();

        [Header("Save")]
        [SerializeField] private bool m_LoadOnStart = true;
        [SerializeField] private bool m_AutoSaveOnNodeEnter = true;

        [Header("Dependencies")]
        [Tooltip("Необязательно. Если не задан - контекст собирается через настройки ниже.")]
        [SerializeField] private GBSStoryContextProvider m_ContextProvider;

        [Tooltip("Отметьте только те зависимости, без которых сцена не работает: " +
                 "остальные могут отсутствовать и будут null.")]
        [SerializeField] private GBSStoryContextBuilder m_ContextBuilder = new GBSStoryContextBuilder();

        private readonly List<GBSGraphRunner> m_Runners = new List<GBSGraphRunner>();
        private readonly HashSet<string> m_CompletedGraphIds = new HashSet<string>();

        private StoryContext m_Context;
        private CancellationTokenSource m_LifetimeCts;

        public StoryContext Context => m_Context;
        public IReadOnlyList<GBSGraphRunner> Runners => m_Runners;

        public void MethodInit()
        {
            m_Context = BuildContext();

            CreateRunners();
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
                return m_ContextProvider.CreateContext(resolver);
            }

            return m_ContextBuilder.Build(resolver);
        }

        public void MethodStart()
        {
            if (m_LoadOnStart)
            {
                LoadProgress();
            }

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

            if (m_Context != null)
            {
                foreach (var flag in m_Context.GetBoolFlags())
                {
                    data.FlagKeys.Add(flag.Key);
                    data.FlagValues.Add(flag.Value);
                }
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

            if (m_Context == null)
            {
                return;
            }

            var flags = new Dictionary<string, bool>();

            for (int i = 0; i < data.FlagKeys.Count && i < data.FlagValues.Count; i++)
            {
                flags[data.FlagKeys[i]] = data.FlagValues[i];
            }

            m_Context.RestoreFlags(flags);
        }

        /// <summary>Удалить файл сейва и сбросить прогресс в рантайме.</summary>
        [ContextMenu("Clear Progress")]
        public void ClearProgress()
        {
            GBSSaveSystem.Clear();

            m_CompletedGraphIds.Clear();
            m_Context?.ClearFlags();

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
                runner.NodeEntered += OnNodeEntered;

                m_Runners.Add(runner);
            }
        }

        private void OnNodeEntered(GBSGraphRunner runner)
        {
            if (m_AutoSaveOnNodeEnter)
            {
                SaveProgress();
            }
        }

        private void OnGraphCompleted(GBSGraphRunner runner)
        {
            m_CompletedGraphIds.Add(runner.Graph.GraphId);

            if (m_AutoSaveOnNodeEnter)
            {
                SaveProgress();
            }
        }
    }
}

