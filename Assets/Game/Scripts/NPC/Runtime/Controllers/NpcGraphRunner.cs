using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using GBS;
using GBS.Data;
using GBS.Runtime;
using Game.Scripts.Story;
using UnityEngine;

namespace Game.Scripts.NPC.Runtime.Controllers
{
    /// <summary>
    /// Свой граф GBS у бота: поведение бота со сценой (сотрудник газовой службы с протоколом, мимик у будки)
    /// описывается отдельным графом, а не веткой основного сюжета. Граф идёт параллельно основному сюжету
    /// и видит то же состояние сюжета (флаги и сигналы: "ворота открыты", "документы проверены"...).
    ///
    /// Шаги графа для бота - в меню "NPC/..." (идти в точку, погоня, превращение, отдать запасы, исчезнуть;
    /// условия "дошёл", "ударил игрока", "игрок взаимодействовал"...). Они управляют именно этим ботом.
    /// Точки и маршруты сцены граф называет ключами - их сопоставление объектам сцены задаётся здесь
    /// (в графе нельзя хранить ссылки на объекты сцены).
    ///
    /// Граф запускается заново каждый раз, когда бот появляется (объект включается), и останавливается,
    /// когда бот исчезает. Прогресс графа бота не сохраняется: после загрузки сохранения сцена бота
    /// начинается сначала.
    /// </summary>
    public class NpcGraphRunner : MonoBehaviour
    {
        [Serializable]
        public struct NamedPoint
        {
            [Tooltip("Ключ точки, по которому её называет граф (например 'Gate', 'BoothWindow', 'Exit').")]
            public string Key;
            public Transform Point;
        }

        [Serializable]
        public struct NamedRoute
        {
            [Tooltip("Ключ маршрута, по которому его называет граф.")]
            public string Key;
            public NpcPatrolRoute Route;
        }

        /// <summary>
        /// Счётчики событий бота на входе в текущую ноду графа: условия "NPC/..." ждут событий после входа в ноду.
        /// </summary>
        public struct Snapshot
        {
            public int Hits;
            public int Kills;
            public int Arrivals;
            public int Interactions;
            public int ActionSerial;
        }

        [SerializeField] private NpcController m_Npc;
        [SerializeField] private GBSGraphSO m_Graph;
        [Tooltip("Запускать граф (с начала), когда бот появляется - включается объект.")]
        [SerializeField] private bool m_RunOnEnable = true;
        [SerializeField] private NamedPoint[] m_Points = Array.Empty<NamedPoint>();
        [SerializeField] private NamedRoute[] m_Routes = Array.Empty<NamedRoute>();
        [Tooltip("Писать в консоль, в какую ноду графа вошёл бот.")]
        [SerializeField] private bool m_DebugLog;

        private CancellationTokenSource m_Cts;
        private GBSGraphRunner m_Runner;

        public NpcController Npc => m_Npc;
        public GBSGraphSO Graph => m_Graph;
        public Snapshot NodeEnterSnapshot { get; private set; }
        public bool IsRunning => m_Runner != null && m_Runner.IsRunning;
        public bool IsCompleted => m_Runner != null && m_Runner.IsCompleted;

        /// <summary>Имя текущей ноды графа (для отладки и проверок).</summary>
        public string CurrentNodeName
        {
            get
            {
                if (m_Runner == null || string.IsNullOrEmpty(m_Runner.CurrentNodeId) || m_Graph == null)
                {
                    return null;
                }

                return m_Graph.GetNode(m_Runner.CurrentNodeId)?.NodeName;
            }
        }

        public event Action<string> NodeEntered;

        private void Awake()
        {
            if (m_Npc == null)
            {
                m_Npc = GetComponentInParent<NpcController>();
            }
        }

        private void OnEnable()
        {
            if (m_RunOnEnable)
            {
                Run();
            }
        }

        private void OnDisable()
        {
            Stop();
        }

        /// <summary>Запустить граф с начала (UnityEvent/сюжет).</summary>
        public void Run()
        {
            Stop();

            if (m_Graph == null || m_Npc == null)
            {
                Debug.LogWarning($"[NPC] {name}: у NpcGraphRunner не назначен граф или бот.", this);
                return;
            }

            m_Cts = new CancellationTokenSource();
            RunAsync(m_Cts.Token).Forget();
        }

        /// <summary>Остановить граф.</summary>
        public void Stop()
        {
            if (m_Cts != null)
            {
                m_Cts.Cancel();
                m_Cts.Dispose();
                m_Cts = null;
            }

            m_Runner = null;
        }

        public bool TryGetPoint(string key, out Transform point)
        {
            foreach (var entry in m_Points)
            {
                if (entry.Key == key && entry.Point != null)
                {
                    point = entry.Point;
                    return true;
                }
            }

            point = null;
            return false;
        }

        public bool TryGetRoute(string key, out NpcPatrolRoute route)
        {
            foreach (var entry in m_Routes)
            {
                if (entry.Key == key && entry.Route != null)
                {
                    route = entry.Route;
                    return true;
                }
            }

            route = null;
            return false;
        }

        private async UniTaskVoid RunAsync(CancellationToken token)
        {
            try
            {
                // Бот запускается JuicyDI (MethodStart) - граф ждёт его, чтобы команды не уходили в пустоту.
                await UniTask.WaitUntil(() => m_Npc.IsInitialized, cancellationToken: token);

                var starter = StorySignals.Current as GBSStarter;

                if (starter == null)
                {
                    Debug.LogWarning($"[NPC] {name}: в сцене нет GBSStarter - граф бота работает со своим пустым состоянием сюжета " +
                                     "(флаги и сигналы основного сюжета ему не видны).", this);
                }

                var state = starter != null ? starter.State : new StoryState();
                var context = new StoryContext(state, new NpcGraphResolver(this, new GBSDependencyResolver()));
                var runner = new GBSGraphRunner(m_Graph, context, new ProgressProvider(starter));
                runner.NodeEntered += OnNodeEntered;
                m_Runner = runner;
                TakeSnapshot();

                if (m_DebugLog)
                {
                    Debug.Log($"[NPC] {m_Npc.name}: граф '{m_Graph.GraphName}' запущен.", this);
                }

                await runner.RunAsync(token);

                if (m_DebugLog && runner.IsCompleted)
                {
                    Debug.Log($"[NPC] {m_Npc.name}: граф '{m_Graph.GraphName}' завершён.", this);
                }
            }
            catch (OperationCanceledException)
            {
                // Бот исчез или граф перезапущен.
            }
        }

        private void OnNodeEntered(GBSGraphRunner runner)
        {
            TakeSnapshot();
            string nodeName = CurrentNodeName;

            if (m_DebugLog)
            {
                Debug.Log($"[NPC] {m_Npc.name}: нода графа '{nodeName}'.", this);
            }

            NodeEntered?.Invoke(nodeName);
        }

        private void TakeSnapshot()
        {
            NodeEnterSnapshot = new Snapshot
            {
                Hits = m_Npc.HitCount,
                Kills = m_Npc.KillCount,
                Arrivals = m_Npc.ArrivedCount,
                Interactions = m_Npc.InteractedCount,
                ActionSerial = m_Npc.ActionSerial
            };
        }

        /// <summary>
        /// Шаги графа получают "своего" бота через context.Resolve&lt;NpcGraphRunner&gt;() / Resolve&lt;NpcController&gt;();
        /// всё остальное (подсказка, камера, фейдер...) - как в основном сюжете, из JuicyDI.
        /// </summary>
        private sealed class NpcGraphResolver : IStoryServiceResolver
        {
            private readonly NpcGraphRunner m_Owner;
            private readonly IStoryServiceResolver m_Base;

            public NpcGraphResolver(NpcGraphRunner owner, IStoryServiceResolver baseResolver)
            {
                m_Owner = owner;
                m_Base = baseResolver;
            }

            public T Resolve<T>() where T : class
            {
                if (m_Owner is T owner)
                {
                    return owner;
                }

                if (m_Owner.m_Npc is T npc)
                {
                    return npc;
                }

                return m_Base?.Resolve<T>();
            }

            public List<T> ResolveAll<T>() where T : class
            {
                return m_Base?.ResolveAll<T>() ?? new List<T>();
            }
        }

        /// <summary>
        /// Нода "Graph Completed" в графе бота видит графы основного сюжета.
        /// </summary>
        private sealed class ProgressProvider : IGBSGraphProgressProvider
        {
            private readonly GBSStarter m_Starter;

            public ProgressProvider(GBSStarter starter)
            {
                m_Starter = starter;
            }

            public bool IsGraphCompleted(GBSGraphSO graph)
            {
                return m_Starter != null && m_Starter.IsGraphCompleted(graph);
            }
        }
    }
}
