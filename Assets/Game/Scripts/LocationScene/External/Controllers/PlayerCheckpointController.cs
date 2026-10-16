using Game.Scripts.LocationScene.External.Data;
using Game.Scripts.LocationScene.Runtime.Controllers;
using Game.Scripts.LocationScene.Runtime.Data;
using Game.Scripts.LocationScene.Runtime.Interfaces;
using Game.Scripts.Utilities.Save;
using JuicyDI;
using JuicyDI.Attributes;
using R3;
using UnityEngine;

namespace Game.Scripts.LocationScene.External.Controllers
{
    /// <summary>
    /// Чекпоинт игрока и его сохранение в JSON-файл (Application.persistentDataPath/Saves, см. <see cref="JsonSaveFile"/>):
    /// на какой локации и на каком чекпоинте игрок появится при загрузке игры или после смерти.
    /// Лежит в сцене ядра игры.
    ///
    /// Чекпоинт игрока - это пара "локация + чекпоинт на ней". Меняется он в двух случаях:
    ///  - его назначил сюжет - действие графа "Save/Set Player Checkpoint". Чекпоинт может стоять и на
    ///    локации, которая сейчас не загружена: её находят по спискам чекпоинтов в описаниях локаций;
    ///  - игрок перешёл в другую локацию - чекпоинтом становится её чекпоинт по умолчанию (если сюжет уже
    ///    не назначил чекпоинт именно на этой локации).
    /// В файл чекпоинт попадает только при общем сохранении игры (GameSaveController, ISaveParticipant;
    /// в графе - действие "Save/Save Game"), вместе с остальными механиками, чтобы сохранение было цельным.
    /// Где стоит игрок в момент сохранения, значения не имеет - пишется чекпоинт.
    ///
    /// Загрузка - на старте ядра: LocationGameStarter читает файл, грузит сохранённую локацию и ставит
    /// игрока на чекпоинт. Для проверки в Play Mode методы доступны из контекстного меню компонента.
    /// </summary>
    [JDIMonoController]
    public class PlayerCheckpointController : MonoBehaviour, IPlayerCheckpointService, ISaveParticipant, ISequence
    {
        [Tooltip("Имя JSON-файла сохранения местоположения игрока (лежит в Application.persistentDataPath/Saves).")]
        [SerializeField] private string m_FileName = "player_location.json";
        [Tooltip("Писать в консоль, какой чекпоинт назначен игроку и что записано в сохранение.")]
        [SerializeField] private bool m_LogCheckpointChanges = true;

        [Inject] private ILocationSceneService m_LocationService;

        private readonly CompositeDisposable m_Disposables = new();

        private string m_CheckpointLocationId;
        private string m_CheckpointId;

        public string SavePath => JsonSaveFile.GetPath(m_FileName);
        public bool HasSave => JsonSaveFile.Exists(m_FileName);
        public string CheckpointLocationId => m_CheckpointLocationId;
        public string CheckpointId => m_CheckpointId;

        public void MethodInit()
        {
        }

        public void MethodStart()
        {
            // К этому моменту LocationGameStarter уже поставил игрока на чекпоинт и запомнил его здесь.
            m_LocationService?.CurrentLocation
                .Subscribe(OnCurrentLocationChanged)
                .AddTo(m_Disposables);
        }

        private void OnDestroy()
        {
            m_Disposables.Dispose();
        }

        public bool SetCheckpoint(string checkpointId)
        {
            if (m_LocationService == null || m_LocationService.Catalog == null)
            {
                Debug.LogError("[PlayerCheckpoint] Загрузчик локаций не заинжекчен - чекпоинт игрока не изменён.", this);
                return false;
            }

            var catalog = m_LocationService.Catalog;
            var checkpoint = FindCheckpoint(checkpointId, null);

            // Чекпоинт на загруженной сцене: его локация - та, в сцене которой он стоит (для чекпоинта в
            // переходной сцене - текущая локация игрока). Сцена не загружена - локацию находим по спискам
            // чекпоинтов в описаниях локаций.
            var location = checkpoint != null
                ? catalog.FindLocationByScenePath(checkpoint.ScenePath) ?? m_LocationService.CurrentLocation.CurrentValue
                : catalog.FindLocationByCheckpoint(checkpointId);

            if (location == null)
            {
                Debug.LogError($"[PlayerCheckpoint] Чекпоинт '{checkpointId}' не найден: его нет ни на загруженных сценах " +
                               "(LocationCheckpoint с таким ключом), ни в списках чекпоинтов локаций каталога (список " +
                               "обновляется при сохранении сцены локации) - чекпоинт игрока не изменён.", this);
                return false;
            }

            RestoreCheckpoint(location.LocationId, checkpointId);
            Log($"чекпоинт игрока назначен: '{checkpointId}' на локации '{location.LocationId}'");
            return true;
        }

        [ContextMenu("Return To Last Save")]
        public void ReturnToLastSave()
        {
            LocationGameLauncher.LoadGame(m_LocationService?.Catalog);
        }

        [ContextMenu("Save")]
        public void Save()
        {
            string locationId = m_CheckpointLocationId;
            string checkpointId = m_CheckpointId;

            if (string.IsNullOrEmpty(locationId))
            {
                // Чекпоинт ещё не назначался (игрока не ставил LocationGameStarter) - берём текущую локацию.
                var location = m_LocationService?.CurrentLocation.CurrentValue;

                if (location == null)
                {
                    Debug.LogError("[PlayerCheckpoint] Чекпоинт не назначен и текущая локация неизвестна - " +
                                   "местоположение игрока не сохранено.", this);
                    return;
                }

                locationId = location.LocationId;
                checkpointId = location.DefaultCheckpointId;
            }

            var data = new PlayerCheckpointSaveData
            {
                LocationId = locationId,
                CheckpointId = checkpointId
            };

            if (JsonSaveFile.TryWrite(m_FileName, data, this))
            {
                Log($"в сохранение записан чекпоинт '{checkpointId}' на локации '{locationId}'");
            }
        }

        [ContextMenu("Delete Save")]
        public void DeleteSave()
        {
            JsonSaveFile.Delete(m_FileName, this);
        }

        /// <summary>
        /// Читает сохранённое местоположение игрока. Возвращает false, если сохранения нет или оно пустое.
        /// Инжект не нужен - вызывается на старте ядра ещё до инициализации JuicyDI.
        /// </summary>
        public bool TryReadSave(out PlayerCheckpointSaveData data)
        {
            return JsonSaveFile.TryRead(m_FileName, out data, this) && !string.IsNullOrEmpty(data.LocationId);
        }

        /// <summary>
        /// Запоминает чекпоинт игрока без записи в файл (на старте игры - чекпоинт, на котором игрок появился).
        /// </summary>
        public void RestoreCheckpoint(string locationId, string checkpointId)
        {
            m_CheckpointLocationId = locationId;
            m_CheckpointId = checkpointId;
        }

        /// <summary>
        /// Чекпоинт, на котором игрок должен появиться в локации: запрошенный, а если его нет на
        /// загруженных сценах - чекпоинт локации по умолчанию.
        /// </summary>
        public bool TryGetSpawnCheckpoint(LocationDefinitionSO location, string checkpointId, out ILocationCheckpoint checkpoint)
        {
            checkpoint = FindCheckpoint(checkpointId, location.ScenePath);

            if (checkpoint != null)
            {
                return true;
            }

            if (!string.IsNullOrEmpty(checkpointId) && checkpointId != location.DefaultCheckpointId)
            {
                Debug.LogWarning($"[PlayerCheckpoint] Чекпоинт '{checkpointId}' не найден на загруженных сценах - " +
                                 $"игрок появится на чекпоинте по умолчанию локации '{location.name}'.", this);
            }

            checkpoint = FindCheckpoint(location.DefaultCheckpointId, location.ScenePath);

            if (checkpoint == null)
            {
                Debug.LogError($"[PlayerCheckpoint] В локации '{location.name}' не найден чекпоинт по умолчанию " +
                               $"'{location.DefaultCheckpointId}' - поставьте в её сцену LocationCheckpoint с этим ключом.", location);
            }

            return checkpoint != null;
        }

        /// <summary>
        /// Игрок перешёл в другую локацию: чекпоинтом становится её чекпоинт по умолчанию - кроме случая,
        /// когда сюжет уже назначил чекпоинт на этой локации.
        /// </summary>
        private void OnCurrentLocationChanged(LocationDefinitionSO location)
        {
            if (location == null || location.LocationId == m_CheckpointLocationId)
            {
                return;
            }

            RestoreCheckpoint(location.LocationId, location.DefaultCheckpointId);
            Log($"игрок перешёл в локацию '{location.LocationId}' - чекпоинтом стал её чекпоинт по умолчанию '{location.DefaultCheckpointId}'");
        }

        /// <summary>
        /// Чекпоинт с ключом среди загруженных сцен. Если таких несколько, выбирается тот, что стоит
        /// в сцене <paramref name="preferredScenePath"/>.
        /// </summary>
        private static ILocationCheckpoint FindCheckpoint(string checkpointId, string preferredScenePath)
        {
            if (string.IsNullOrEmpty(checkpointId))
            {
                return null;
            }

            // Берём из контейнера в момент обращения, а не через [Inject]: на старте ядра чекпоинтов ещё нет.
            var checkpoints = BinController.GetContext()?.GetBeans<ILocationCheckpoint>();

            if (checkpoints == null)
            {
                return null;
            }

            ILocationCheckpoint result = null;

            for (int i = 0; i < checkpoints.Count; i++)
            {
                var checkpoint = checkpoints[i];

                if (checkpoint == null || checkpoint.CheckpointId != checkpointId)
                {
                    continue;
                }

                if (checkpoint.ScenePath == preferredScenePath)
                {
                    return checkpoint;
                }

                result ??= checkpoint;
            }

            return result;
        }

        private void Log(string message)
        {
            if (m_LogCheckpointChanges)
            {
                Debug.Log($"[PlayerCheckpoint] {message}", this);
            }
        }
    }
}
