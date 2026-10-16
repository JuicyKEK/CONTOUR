using System;
using System.Collections.Generic;
using System.Text;
using Game.Scripts.LocationScene.Runtime.Interfaces;
using Game.Scripts.LocationState.External.Data;
using Game.Scripts.LocationState.Runtime.Interfaces;
using Game.Scripts.Utilities.Save;
using JuicyDI;
using JuicyDI.Attributes;
using R3;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Game.Scripts.LocationState.External.Controllers
{
    /// <summary>
    /// Хранилище состояний объектов локаций (двери, подбираемые предметы, выключатели) и их сохранение в
    /// JSON-файл (Application.persistentDataPath/Saves, см. <see cref="JsonSaveFile"/>). Лежит в сцене ядра игры.
    ///
    /// Хранятся только объекты, состояние которых отличается от сцены (подобранный предмет, открытая дверь,
    /// выключенный свет), - остальные после загрузки просто лежат так, как в сцене.
    ///
    /// Пока игра идёт, состояния держатся в памяти:
    ///  - сцена локации или перехода загрузилась - её объекты регистрируются (LocationStateSaver) и сразу
    ///    получают свои состояния;
    ///  - сцена начинает выгружаться (ILocationSceneService.SceneUnloading) - состояния её объектов запоминаются;
    ///  - объект сам просит запомнить состояние сейчас (подобранный предмет перед удалением).
    /// В файл состояния пишутся только при общем сохранении игры (GameSaveController, ISaveParticipant; в графе -
    /// действие "Save/Save Game"), вместе с остальными механиками. Переходы между локациями в файл ничего не пишут:
    /// иначе возврат к последнему сохранению (смерть) вернул бы сюжет, но не двери и не предметы.
    /// Загрузка файла - на старте ядра: после смерти, при загрузке из меню и при Play в редакторе.
    ///
    /// Для проверки в Play Mode: контекстное меню компонента - Log State, Save, Delete Save.
    /// </summary>
    [JDIMonoController]
    public class LocationStateController : MonoBehaviour, ILocationStateService, ISaveParticipant, ISequence
    {
        [Tooltip("Имя JSON-файла сохранения состояния объектов локаций (лежит в Application.persistentDataPath/Saves).")]
        [SerializeField] private string m_FileName = "location_state.json";
        [Tooltip("Загружать сохранение при старте игры.")]
        [SerializeField] private bool m_LoadOnStart = true;
        [Tooltip("Писать в консоль, какие состояния объектов восстановлены и запомнены.")]
        [SerializeField] private bool m_LogStateChanges = true;

        [Inject] private ILocationSceneService m_LocationService;

        // Последние известные состояния всех объектов (в т.ч. на выгруженных сценах), ключ - StateKey объекта.
        private readonly Dictionary<string, string> m_States = new();
        // Объекты загруженных сцен.
        private readonly List<ILocationStateObject> m_Objects = new();
        private readonly CompositeDisposable m_Disposables = new();

        private bool m_IsLoaded;
        private bool m_IsSubscribed;

        public string SavePath => JsonSaveFile.GetPath(m_FileName);
        public bool HasSave => JsonSaveFile.Exists(m_FileName);

        public void MethodInit()
        {
            EnsureLoaded();

            // В старых сценах "всё в одном" загрузчика локаций нет - там сцены не выгружаются.
            if (m_LocationService != null && !m_IsSubscribed)
            {
                m_IsSubscribed = true;
                m_LocationService.SceneUnloading
                    .Subscribe(OnSceneUnloading)
                    .AddTo(m_Disposables);
            }
        }

        public void MethodStart()
        {
        }

        private void OnDestroy()
        {
            m_Disposables.Dispose();
        }

        public void Register(ILocationStateObject stateObject)
        {
            if (stateObject == null)
            {
                return;
            }

            // Объекты стартовых сцен могут зарегистрироваться раньше MethodInit этого контроллера.
            EnsureLoaded();

            string key = stateObject.StateKey;

            for (int i = 0; i < m_Objects.Count; i++)
            {
                if (m_Objects[i] != null && m_Objects[i].StateKey == key && !ReferenceEquals(m_Objects[i], stateObject))
                {
                    Debug.LogError($"[LocationState] Два объекта с одним ключом сохранения '{key}' ('{NameOf(m_Objects[i])}' и " +
                                   $"'{NameOf(stateObject)}') - сохраните сцену, ключи станут разными.", stateObject as Object);
                    return;
                }
            }

            if (!m_Objects.Contains(stateObject))
            {
                m_Objects.Add(stateObject);
            }

            if (!m_States.TryGetValue(key, out string state))
            {
                return;
            }

            try
            {
                stateObject.ApplySavedState(state);
                Log($"восстановлено: {NameOf(stateObject)} ({key}) = {state}");
            }
            catch (Exception exception)
            {
                Debug.LogError($"[LocationState] Не удалось восстановить состояние '{NameOf(stateObject)}' ({key}) из '{state}'.",
                    stateObject as Object);
                Debug.LogException(exception, stateObject as Object);
            }
        }

        public void Unregister(ILocationStateObject stateObject)
        {
            m_Objects.Remove(stateObject);
        }

        public void StoreState(ILocationStateObject stateObject)
        {
            if (Capture(stateObject))
            {
                Log($"запомнено: {NameOf(stateObject)} ({stateObject.StateKey}) = " +
                    $"{(m_States.TryGetValue(stateObject.StateKey, out string state) ? state : "как в сцене")}");
            }
        }

        [ContextMenu("Save")]
        public void Save()
        {
            CaptureAll();

            var data = new LocationStateSaveData();

            foreach (var pair in m_States)
            {
                data.Objects.Add(new LocationStateSaveEntry(pair.Key, pair.Value));
            }

            data.Objects.Sort((a, b) => string.CompareOrdinal(a.Key, b.Key));
            JsonSaveFile.TryWrite(m_FileName, data, this);
        }

        public bool Load()
        {
            if (!JsonSaveFile.TryRead(m_FileName, out LocationStateSaveData data, this))
            {
                return false;
            }

            m_States.Clear();

            if (data.Objects != null)
            {
                foreach (var entry in data.Objects)
                {
                    if (entry != null && !string.IsNullOrEmpty(entry.Key))
                    {
                        m_States[entry.Key] = entry.State;
                    }
                }
            }

            return true;
        }

        [ContextMenu("Delete Save")]
        public void DeleteSave()
        {
            JsonSaveFile.Delete(m_FileName, this);
            m_States.Clear();
        }

        [ContextMenu("Log State")]
        private void LogState()
        {
            CaptureAll();

            var builder = new StringBuilder();
            builder.AppendLine($"[LocationState] Объектов на загруженных сценах: {m_Objects.Count}, запомненных состояний: {m_States.Count}");

            foreach (var pair in m_States)
            {
                builder.AppendLine($"  {pair.Key} = {pair.Value}");
            }

            Debug.Log(builder.ToString(), this);
        }

        private void EnsureLoaded()
        {
            if (m_IsLoaded)
            {
                return;
            }

            m_IsLoaded = true;

            if (m_LoadOnStart)
            {
                Load();
            }
        }

        /// <summary>
        /// Сцена вот-вот начнёт выгружаться: её объекты ещё живы - запоминаем их состояния и перестаём их опрашивать.
        /// </summary>
        private void OnSceneUnloading(Scene scene)
        {
            int changedCount = 0;

            for (int i = m_Objects.Count - 1; i >= 0; i--)
            {
                var stateObject = m_Objects[i];

                if (IsDestroyed(stateObject))
                {
                    m_Objects.RemoveAt(i);
                    continue;
                }

                if (stateObject.Scene != scene)
                {
                    continue;
                }

                if (Capture(stateObject) && m_States.ContainsKey(stateObject.StateKey))
                {
                    changedCount++;
                }

                m_Objects.RemoveAt(i);
            }

            Log($"сцена '{scene.name}' выгружается - запомнены объекты не в исходном состоянии: {changedCount}");
        }

        private void CaptureAll()
        {
            for (int i = m_Objects.Count - 1; i >= 0; i--)
            {
                if (IsDestroyed(m_Objects[i]))
                {
                    m_Objects.RemoveAt(i);
                    continue;
                }

                Capture(m_Objects[i]);
            }
        }

        private bool Capture(ILocationStateObject stateObject)
        {
            if (IsDestroyed(stateObject))
            {
                return false;
            }

            try
            {
                string state = stateObject.CaptureState();

                // Объект в том же состоянии, что и в сцене, - хранить нечего.
                if (state == null)
                {
                    m_States.Remove(stateObject.StateKey);
                }
                else
                {
                    m_States[stateObject.StateKey] = state;
                }

                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[LocationState] Не удалось запомнить состояние '{NameOf(stateObject)}'.", stateObject as Object);
                Debug.LogException(exception, stateObject as Object);
                return false;
            }
        }

        private static bool IsDestroyed(ILocationStateObject stateObject)
        {
            return stateObject == null || (stateObject is Object unityObject && unityObject == null);
        }

        private static string NameOf(ILocationStateObject stateObject)
        {
            return stateObject is Object unityObject && unityObject != null ? unityObject.name : stateObject?.GetType().Name;
        }

        private void Log(string message)
        {
            if (m_LogStateChanges)
            {
                Debug.Log($"[LocationState] {message}", this);
            }
        }
    }
}
