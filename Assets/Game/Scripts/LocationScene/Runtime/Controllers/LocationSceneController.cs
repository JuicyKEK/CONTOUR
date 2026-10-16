using System.Collections.Generic;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Scripts.LocationScene.Runtime.Data;
using Game.Scripts.LocationScene.Runtime.Interfaces;
using JuicyDI.Attributes;
using R3;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Scripts.LocationScene.Runtime.Controllers
{
    /// <summary>
    /// Загрузчик локаций. Лежит в сцене ядра игры и живёт всю игру.
    ///
    /// Какие сцены должны быть загружены, однозначно определяют две локации:
    ///  - текущая (в ней игрок): её сцена и все её переходные сцены;
    ///  - подгружаемая (соседняя локация, пока игрок в переходе к ней): её сцена и все её переходные сцены.
    /// Всё, что в этот набор не входит, выгружается. Поэтому триггеры перехода (LocationTransitionGate)
    /// только сообщают, где игрок, а контроллер приводит загруженные сцены к нужному набору:
    ///  - игрок задел триггер на входе в переход - соседняя локация начинает грузиться аддитивно;
    ///  - игрок задел триггер на выходе - соседняя локация становится текущей, прежняя выгружается вместе
    ///    со своими переходными сценами (кроме тех, что связаны и с новой локацией);
    ///  - игрок вернулся из перехода обратно - соседняя локация выгружается. Загрузку сцены Unity прервать
    ///    не даёт, поэтому сцена, которая ещё грузится, выгружается сразу, как только догрузится, - её
    ///    бины при этом не регистрируются и не запускаются.
    ///
    /// Сцены грузятся асинхронно (SceneManager.LoadSceneAsync): файл сцены читается в фоне. Но включение
    /// объектов сцены (их Awake/OnEnable) Unity всегда выполняет в основном потоке за один кадр - если
    /// объекты сцены тяжело создаются, этот кадр виден как подвисание. Сколько он длился и сколько из
    /// этого заняла регистрация и запуск бинов сцены, контроллер пишет в консоль.
    ///
    /// На старте игры сцены локации инициализирует LocationGameStarter (общей последовательностью вместе
    /// с ядром), дальше - сам контроллер: у каждой догруженной сцены вызывает LocationSceneStarter.Init().
    /// </summary>
    [JDIMonoController]
    public class LocationSceneController : MonoBehaviour, ILocationSceneService
    {
        private enum SceneLoadState
        {
            Unloaded,
            Loading,
            Loaded,
            Unloading
        }

        private sealed class SceneEntry
        {
            public readonly string Path;
            // Локация, которой принадлежит сцена (null - сцена переходная).
            public readonly LocationDefinitionSO Location;
            public SceneLoadState State;
            // Сцена входит в набор, который должен быть загружен.
            public bool IsWanted;
            // Бины сцены зарегистрированы и запущены.
            public bool IsInitialized;
            // Сцену не удалось начать грузить (нет в Build Settings) - повторно не пробуем.
            public bool IsLoadFailed;

            public SceneEntry(string path, LocationDefinitionSO location)
            {
                Path = path;
                Location = location;
            }
        }

        [Header("Данные")]
        [Tooltip("Каталог локаций и переходов игры.")]
        [SerializeField] private LocationCatalogSO m_Catalog;

        [Header("Настройки")]
        [Tooltip("Делать сцену текущей локации активной сценой Unity: от активной сцены берутся настройки " +
                 "освещения (небо, туман, окружающий свет), в неё же попадают объекты, созданные через Instantiate.")]
        [SerializeField] private bool m_SetCurrentLocationActive = true;
        [Tooltip("После выгрузки сцен освобождать память от ассетов, которые больше нигде не используются " +
                 "(Resources.UnloadUnusedAssets). Это короткая остановка основного потока сразу после выгрузки сцен.")]
        [SerializeField] private bool m_UnloadUnusedAssets = true;
        [Tooltip("Писать в консоль, какие сцены грузятся и выгружаются, сколько это заняло и как меняется текущая локация.")]
        [SerializeField] private bool m_LogSceneChanges = true;

        private readonly Dictionary<string, SceneEntry> m_Scenes = new();
        private readonly List<SceneEntry> m_EntriesBuffer = new();
        private readonly List<GameObject> m_RootObjectsBuffer = new();
        private readonly ReactiveProperty<LocationDefinitionSO> m_CurrentLocation = new(null);
        private readonly Subject<LocationDefinitionSO> m_LocationLoaded = new();
        private readonly Subject<LocationDefinitionSO> m_LocationUnloaded = new();

        private LocationDefinitionSO m_PendingLocation;
        private bool m_IsGameStarted;
        private bool m_IsDestroyed;

        public LocationCatalogSO Catalog => m_Catalog;
        public ReadOnlyReactiveProperty<LocationDefinitionSO> CurrentLocation => m_CurrentLocation;
        public LocationDefinitionSO PendingLocation => m_PendingLocation;
        public Observable<LocationDefinitionSO> LocationLoaded => m_LocationLoaded;
        public Observable<LocationDefinitionSO> LocationUnloaded => m_LocationUnloaded;

        public bool IsLocationLoaded(LocationDefinitionSO location)
        {
            return location != null
                   && m_Scenes.TryGetValue(location.ScenePath, out var entry)
                   && entry.State == SceneLoadState.Loaded
                   && entry.IsInitialized;
        }

        public void PlayerTouchedGate(LocationTransitionDefinitionSO transition, LocationDefinitionSO gateLocation)
        {
            if (!CanHandleGate(transition, gateLocation))
            {
                return;
            }

            if (gateLocation != m_CurrentLocation.Value)
            {
                // Игрок дошёл до выхода из перехода: локация за этим триггером становится текущей,
                // прежняя выгружается.
                SetLocations(gateLocation, null);
                return;
            }

            // Игрок входит в переход из текущей локации: подгружаем локацию на другом конце.
            SetLocations(gateLocation, transition.GetOpposite(gateLocation));
        }

        public void PlayerLeftGate(LocationTransitionDefinitionSO transition, LocationDefinitionSO gateLocation, bool isInLocation)
        {
            if (!CanHandleGate(transition, gateLocation))
            {
                return;
            }

            // Вышел в саму локацию - соседняя больше не нужна; ушёл внутрь перехода - нужна.
            SetLocations(gateLocation, isInLocation ? null : transition.GetOpposite(gateLocation));
        }

        #region Старт игры (вызывает LocationGameStarter)

        /// <summary>
        /// Локация, сцена которой уже загружена к моменту старта ядра: так бывает, когда сцену локации
        /// запустили саму по себе (Play в редакторе) и ядро догрузилось к ней. null - таких нет.
        /// </summary>
        public LocationDefinitionSO FindLoadedLocation()
        {
            if (m_Catalog == null)
            {
                return null;
            }

            AdoptLoadedScenes();

            var activeLocation = m_Catalog.FindLocationByScenePath(SceneManager.GetActiveScene().path);

            if (activeLocation != null)
            {
                return activeLocation;
            }

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                var location = scene.isLoaded ? m_Catalog.FindLocationByScenePath(scene.path) : null;

                if (location != null)
                {
                    return location;
                }
            }

            return null;
        }

        /// <summary>
        /// Делает локацию текущей и ждёт, пока загрузятся её сцена и все её переходные сцены, а сцены
        /// каталога, которые этой локации не нужны (открытая в редакторе сцена другой локации), выгрузятся.
        /// Сцены при этом не инициализируются - это делает LocationGameStarter.
        /// </summary>
        public async UniTask LoadStartLocationAsync(LocationDefinitionSO location, CancellationToken token)
        {
            if (m_Catalog == null)
            {
                Debug.LogError("[LocationScene] У LocationSceneController не назначен каталог локаций.", this);
                return;
            }

            AdoptLoadedScenes();

            m_PendingLocation = null;
            m_CurrentLocation.Value = location;
            Log($"стартовая локация: {GetName(location)}");

            UpdateWantedScenes();

            await UniTask.WaitUntil(() => AreWantedScenesReady() && !IsAnySceneBusy(), cancellationToken: token);
        }

        /// <summary>
        /// Стартеры всех загруженных сцен локаций и переходов.
        /// </summary>
        public void CollectLoadedStarters(List<LocationSceneStarter> result)
        {
            result.Clear();

            foreach (var entry in m_Scenes.Values)
            {
                if (!entry.IsWanted || entry.State != SceneLoadState.Loaded)
                {
                    continue;
                }

                var starter = FindStarter(entry);

                if (starter != null)
                {
                    result.Add(starter);
                }
            }
        }

        /// <summary>
        /// Стартовые сцены проинициализированы - дальше контроллер сам инициализирует догруженные сцены
        /// и начинает слушать триггеры переходов.
        /// </summary>
        public void CompleteStart()
        {
            m_EntriesBuffer.Clear();

            foreach (var entry in m_Scenes.Values)
            {
                if (entry.IsWanted && entry.State == SceneLoadState.Loaded)
                {
                    entry.IsInitialized = true;
                    m_EntriesBuffer.Add(entry);
                }
            }

            m_IsGameStarted = true;
            ApplyActiveScene();

            // Подписчики (LocationLoadEvents в переходных сценах) уже запущены общей последовательностью.
            for (int i = 0; i < m_EntriesBuffer.Count; i++)
            {
                NotifyLocationLoaded(m_EntriesBuffer[i]);
            }

            m_EntriesBuffer.Clear();
        }

        #endregion

        private void OnDestroy()
        {
            m_IsDestroyed = true;
            m_CurrentLocation.Dispose();
            m_LocationLoaded.Dispose();
            m_LocationUnloaded.Dispose();
        }

        private bool CanHandleGate(LocationTransitionDefinitionSO transition, LocationDefinitionSO gateLocation)
        {
            // Пока игра стартует, игрок выключен и переходить никуда не может.
            if (m_IsDestroyed || !m_IsGameStarted)
            {
                return false;
            }

            if (transition == null || gateLocation == null || !transition.Connects(gateLocation))
            {
                Debug.LogError("[LocationScene] Триггер перехода настроен неверно: не назначен переход или локация, " +
                               $"либо переход '{GetName(transition)}' не связан с локацией '{GetName(gateLocation)}'.", this);
                return false;
            }

            return true;
        }

        private void SetLocations(LocationDefinitionSO currentLocation, LocationDefinitionSO pendingLocation)
        {
            if (currentLocation == m_CurrentLocation.Value && pendingLocation == m_PendingLocation)
            {
                return;
            }

            bool isCurrentChanged = currentLocation != m_CurrentLocation.Value;

            m_PendingLocation = pendingLocation;
            Log(isCurrentChanged
                ? $"текущая локация: {GetName(currentLocation)}, подгружаемая: {GetName(pendingLocation)}"
                : $"подгружаемая локация: {GetName(pendingLocation)}");

            m_CurrentLocation.Value = currentLocation;
            UpdateWantedScenes();
            ApplyActiveScene();
        }

        /// <summary>
        /// Пересчитывает набор сцен, которые должны быть загружены, и приводит к нему загруженные сцены.
        /// </summary>
        private void UpdateWantedScenes()
        {
            foreach (var entry in m_Scenes.Values)
            {
                entry.IsWanted = false;
            }

            MarkWanted(m_CurrentLocation.Value);
            MarkWanted(m_PendingLocation);

            m_EntriesBuffer.Clear();
            m_EntriesBuffer.AddRange(m_Scenes.Values);

            for (int i = 0; i < m_EntriesBuffer.Count; i++)
            {
                var entry = m_EntriesBuffer[i];

                // Сцены, которые сейчас грузятся или выгружаются, не трогаем: когда операция закончится,
                // LoadAsync/UnloadAsync сами проверят, нужна ли сцена.
                if (entry.IsWanted && entry.State == SceneLoadState.Unloaded && !entry.IsLoadFailed)
                {
                    LoadAsync(entry).Forget();
                }
                else if (!entry.IsWanted && entry.State == SceneLoadState.Loaded)
                {
                    UnloadAsync(entry).Forget();
                }
            }

            m_EntriesBuffer.Clear();
        }

        /// <summary>
        /// Локация и все её переходные сцены должны быть загружены.
        /// </summary>
        private void MarkWanted(LocationDefinitionSO location)
        {
            if (location == null)
            {
                return;
            }

            MarkWanted(location.ScenePath, location);

            var transitions = m_Catalog.Transitions;

            for (int i = 0; i < transitions.Count; i++)
            {
                if (transitions[i] != null && transitions[i].Connects(location))
                {
                    MarkWanted(transitions[i].ScenePath, transitions[i]);
                }
            }
        }

        private void MarkWanted(string scenePath, Object definition)
        {
            if (string.IsNullOrEmpty(scenePath))
            {
                Debug.LogError($"[LocationScene] У '{GetName(definition)}' не назначена сцена.", definition);
                return;
            }

            GetEntry(scenePath).IsWanted = true;
        }

        private SceneEntry GetEntry(string scenePath)
        {
            if (!m_Scenes.TryGetValue(scenePath, out var entry))
            {
                entry = new SceneEntry(scenePath, m_Catalog.FindLocationByScenePath(scenePath));
                m_Scenes.Add(scenePath, entry);
            }

            return entry;
        }

        private async UniTaskVoid LoadAsync(SceneEntry entry)
        {
            if (SceneUtility.GetBuildIndexByScenePath(entry.Path) < 0)
            {
                entry.IsLoadFailed = true;
                Debug.LogError($"[LocationScene] Сцена '{entry.Path}' не добавлена в Build Settings - загрузить её нельзя.", this);
                return;
            }

            entry.State = SceneLoadState.Loading;
            Log($"загружается сцена {entry.Path}");

            double loadStartTime = Time.realtimeSinceStartupAsDouble;

            await SceneManager.LoadSceneAsync(entry.Path, LoadSceneMode.Additive);

            if (m_IsDestroyed)
            {
                return;
            }

            entry.State = SceneLoadState.Loaded;

            if (!entry.IsWanted)
            {
                // Пока сцена грузилась, игрок вернулся: она больше не нужна. Её бины не запускаем.
                Log($"сцена {entry.Path} догрузилась, но уже не нужна");
                UnloadAsync(entry).Forget();
                return;
            }

            double loadSeconds = Time.realtimeSinceStartupAsDouble - loadStartTime;
            double initSeconds = -1d;

            // На старте игры сцены инициализирует LocationGameStarter - одной последовательностью с ядром.
            if (m_IsGameStarted)
            {
                double initStartTime = Time.realtimeSinceStartupAsDouble;
                InitScene(entry);
                initSeconds = Time.realtimeSinceStartupAsDouble - initStartTime;
            }

            ApplyActiveScene();

            if (m_IsGameStarted)
            {
                NotifyLocationLoaded(entry);
            }

            if (m_LogSceneChanges)
            {
                LogLoadedAsync(entry.Path, loadSeconds, initSeconds).Forget();
            }
        }

        /// <summary>
        /// Пишет в консоль, сколько заняла загрузка сцены. Длительность кадра, в котором Unity включил
        /// объекты сцены, становится известна только в следующем кадре - поэтому запись идёт с задержкой в кадр.
        /// </summary>
        /// <param name="initSeconds">
        /// Сколько заняли регистрация и запуск бинов сцены; меньше нуля - сцену запускает не контроллер
        /// (стартовые сцены запускает LocationGameStarter общей последовательностью).
        /// </param>
        private async UniTaskVoid LogLoadedAsync(string scenePath, double loadSeconds, double initSeconds)
        {
            await UniTask.NextFrame();

            if (m_IsDestroyed)
            {
                return;
            }

            string initText = initSeconds >= 0d ? $" (из них запуск бинов сцены - {initSeconds * 1000d:F1} мс)" : string.Empty;

            Log($"сцена {scenePath} загружена за {loadSeconds:F2} с; кадр, в котором включились её объекты, " +
                $"длился {Time.unscaledDeltaTime * 1000f:F0} мс{initText}");
        }

        private async UniTaskVoid UnloadAsync(SceneEntry entry)
        {
            var scene = SceneManager.GetSceneByPath(entry.Path);
            var operation = scene.IsValid() && scene.isLoaded ? SceneManager.UnloadSceneAsync(scene) : null;
            bool isInitialized = entry.IsInitialized;

            entry.IsInitialized = false;

            if (operation == null)
            {
                entry.State = SceneLoadState.Unloaded;
                return;
            }

            entry.State = SceneLoadState.Unloading;
            Log($"выгружается сцена {entry.Path}");

            if (isInitialized && entry.Location != null)
            {
                m_LocationUnloaded.OnNext(entry.Location);
            }

            await operation;

            if (m_IsDestroyed)
            {
                return;
            }

            entry.State = SceneLoadState.Unloaded;

            if (entry.IsWanted)
            {
                // Пока сцена выгружалась, она снова понадобилась (игрок развернулся в переходе).
                LoadAsync(entry).Forget();
                return;
            }

            if (m_UnloadUnusedAssets && !IsAnySceneBusy())
            {
                // Не ждём: память освобождается в фоне.
                _ = Resources.UnloadUnusedAssets();
            }
        }

        /// <summary>
        /// Регистрирует бины догруженной сцены в JuicyDI и запускает их (MethodInit/MethodStart).
        /// </summary>
        private void InitScene(SceneEntry entry)
        {
            var starter = FindStarter(entry);

            if (starter != null)
            {
                starter.Init();
            }

            entry.IsInitialized = true;
        }

        private void NotifyLocationLoaded(SceneEntry entry)
        {
            if (entry.Location != null)
            {
                m_LocationLoaded.OnNext(entry.Location);
            }
        }

        private LocationSceneStarter FindStarter(SceneEntry entry)
        {
            var scene = SceneManager.GetSceneByPath(entry.Path);

            m_RootObjectsBuffer.Clear();

            if (scene.IsValid() && scene.isLoaded)
            {
                scene.GetRootGameObjects(m_RootObjectsBuffer);
            }

            LocationSceneStarter starter = null;

            for (int i = 0; i < m_RootObjectsBuffer.Count && starter == null; i++)
            {
                starter = m_RootObjectsBuffer[i].GetComponentInChildren<LocationSceneStarter>(true);
            }

            m_RootObjectsBuffer.Clear();

            if (starter == null)
            {
                Debug.LogError($"[LocationScene] В сцене '{entry.Path}' нет LocationSceneStarter - её бины не будут " +
                               "зарегистрированы в JuicyDI. Положите в сцену префаб LocationSceneStarter.", this);
            }

            return starter;
        }

        /// <summary>
        /// Запоминает сцены каталога, которые уже загружены без участия контроллера
        /// (сцену локации запустили саму по себе в редакторе).
        /// </summary>
        private void AdoptLoadedScenes()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);

                if (!scene.isLoaded)
                {
                    continue;
                }

                bool isCatalogScene = m_Catalog.FindLocationByScenePath(scene.path) != null
                                      || m_Catalog.FindTransitionByScenePath(scene.path) != null;

                if (!isCatalogScene)
                {
                    continue;
                }

                var entry = GetEntry(scene.path);

                if (entry.State == SceneLoadState.Unloaded)
                {
                    entry.State = SceneLoadState.Loaded;
                }
            }
        }

        private void ApplyActiveScene()
        {
            if (!m_SetCurrentLocationActive || m_CurrentLocation.Value == null)
            {
                return;
            }

            var scene = SceneManager.GetSceneByPath(m_CurrentLocation.Value.ScenePath);

            if (scene.IsValid() && scene.isLoaded && SceneManager.GetActiveScene() != scene)
            {
                SceneManager.SetActiveScene(scene);
            }
        }

        private bool AreWantedScenesReady()
        {
            foreach (var entry in m_Scenes.Values)
            {
                if (entry.IsWanted && entry.State != SceneLoadState.Loaded && !entry.IsLoadFailed)
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsAnySceneBusy()
        {
            foreach (var entry in m_Scenes.Values)
            {
                if (entry.State == SceneLoadState.Loading || entry.State == SceneLoadState.Unloading)
                {
                    return true;
                }
            }

            return false;
        }

        private void Log(string message)
        {
            if (m_LogSceneChanges)
            {
                Debug.Log($"[LocationScene] {message}", this);
            }
        }

        private static string GetName(Object definition)
        {
            return definition != null ? definition.name : "нет";
        }

        [ContextMenu("Log State")]
        private void LogState()
        {
            var builder = new StringBuilder();
            builder.AppendLine($"[LocationScene] Текущая локация: {GetName(m_CurrentLocation.Value)}, " +
                               $"подгружаемая: {GetName(m_PendingLocation)}");

            foreach (var entry in m_Scenes.Values)
            {
                builder.AppendLine($"  {entry.Path}: {entry.State}, нужна: {entry.IsWanted}, запущена: {entry.IsInitialized}");
            }

            Debug.Log(builder.ToString(), this);
        }
    }
}
