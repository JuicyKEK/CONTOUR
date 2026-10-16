using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Scripts.LocationScene.Runtime.Controllers;
using Game.Scripts.LocationScene.Runtime.Data;
using JuicyDI;
using JuicyDI.Attributes;
using UnityEngine;

namespace Game.Scripts.LocationScene.External.Controllers
{
    /// <summary>
    /// Стартер сцены ядра игры - то же, что TestRoomStarter для сцены "всё в одном", но для игры из
    /// нескольких сцен: ядро (игрок, UI, контроллеры) + локации и переходы, которые грузятся аддитивно.
    ///
    /// Порядок старта:
    ///  1) выбирает локацию и чекпоинт: по сохранению игрока; если его нет - локацию, сцену которой
    ///     запустили саму по себе (Play в редакторе); иначе - стартовую локацию каталога;
    ///  2) грузит сцену локации и все её переходные сцены;
    ///  3) инициализирует JuicyDI: сначала бины ядра, затем бины загруженных сцен;
    ///  4) ставит игрока на чекпоинт и включает его;
    ///  5) запускает одну общую последовательность MethodInit/MethodStart на ядро и стартовые сцены -
    ///     как в обычной сцене, поэтому механики восстанавливают сохранения и сюжет стартует, когда
    ///     локация уже на месте.
    /// Дальше ядро каждый кадр обновляет только свои бины (CustomUpdate); бины сцены локации обновляет
    /// её LocationSceneStarter, а сцены, догруженные позже, целиком запускает LocationSceneController.
    ///
    /// Игрок выключен, пока локация не загрузилась, - иначе он падал бы в пустоту.
    /// </summary>
    [JDIMonoController]
    public class LocationGameStarter : MonoBehaviour
    {
        [Inject] private List<ISequence> m_StartSequence;
        [Inject] private List<IUpdateSequence> m_UpdateSequence;

        [Header("Ядро")]
        [SerializeField] private MainJDIController m_MainJDIController;
        [SerializeField] private LocationSceneController m_LocationSceneController;
        [SerializeField] private PlayerCheckpointController m_PlayerCheckpointController;

        [Header("Игрок")]
        [Tooltip("Корневой объект игрока в сцене ядра.")]
        [SerializeField] private GameObject m_Player;

        [Header("Загрузка")]
        [Tooltip("Объект экрана загрузки: включён, пока грузится локация. Можно не назначать.")]
        [SerializeField] private GameObject m_LoadingScreen;

        private readonly List<LocationSceneStarter> m_SceneStarters = new();

        private IGameUpdateSequenceController m_GameUpdateSequenceController;
        private CancellationTokenSource m_LifetimeCts;

        private void Awake()
        {
            if (m_Player != null)
            {
                m_Player.SetActive(false);
            }

            SetLoadingScreenVisible(true);
        }

        private void Start()
        {
            m_LifetimeCts = new CancellationTokenSource();
            StartGameAsync(m_LifetimeCts.Token).Forget();
        }

        private void Update()
        {
            m_GameUpdateSequenceController?.UpdateSequence();
        }

        private void OnDestroy()
        {
            m_LifetimeCts?.Cancel();
            m_LifetimeCts?.Dispose();
        }

        private async UniTaskVoid StartGameAsync(CancellationToken token)
        {
            ResolveStartPoint(out var location, out string checkpointId);

            if (location != null)
            {
                await m_LocationSceneController.LoadStartLocationAsync(location, token);
            }
            else
            {
                Debug.LogError("[LocationScene] Не с чего начать игру: в каталоге локаций не назначена стартовая локация.", this);
            }

            // Сначала ядро, потом сцены локаций: их бины зависят от сервисов ядра.
            m_MainJDIController.Init();

            m_LocationSceneController.CollectLoadedStarters(m_SceneStarters);

            for (int i = 0; i < m_SceneStarters.Count; i++)
            {
                m_SceneStarters[i].RegisterBeans();
            }

            // Игрок включается до общей последовательности: в своих MethodStart его уже ищут другие бины.
            SpawnPlayer(location, checkpointId);

            // После регистрации сцен локаций JuicyDI переинжектил списки - в них бины и ядра, и локаций.
            _ = new GameSequenceController(m_StartSequence);

            for (int i = 0; i < m_SceneStarters.Count; i++)
            {
                m_SceneStarters[i].StartUpdates();
            }

            m_GameUpdateSequenceController = new GameUpdateSequenceController(CollectCoreUpdateSequence());
            m_SceneStarters.Clear();

            m_LocationSceneController.CompleteStart();
            SetLoadingScreenVisible(false);
        }

        private void ResolveStartPoint(out LocationDefinitionSO location, out string checkpointId)
        {
            var catalog = m_LocationSceneController.Catalog;

            if (catalog == null)
            {
                location = null;
                checkpointId = null;
                return;
            }

            // Сцена локации, которую запустили саму по себе (Play в редакторе), - ядро догрузилось к ней.
            var openedLocation = m_LocationSceneController.FindLoadedLocation();

            // Сохранение главнее открытой сцены: сюжет и остальные механики продолжат с сохранения,
            // значит и игрок должен стоять там, где сохранился.
            if (m_PlayerCheckpointController.TryReadSave(out var save))
            {
                location = catalog.FindLocation(save.LocationId);

                if (location != null)
                {
                    checkpointId = save.CheckpointId;

                    if (openedLocation != null && openedLocation != location)
                    {
                        Debug.Log($"[LocationScene] В сохранении игрока - локация '{location.name}': игра продолжается с неё, " +
                                  $"а открытая сцена локации '{openedLocation.name}' выгружается. Чтобы начать с открытой " +
                                  "сцены, удалите сохранения: UnityDev/Saves/Delete All Saves.", this);
                    }

                    return;
                }

                Debug.LogError($"[LocationScene] В сохранении игрока записана локация '{save.LocationId}', которой нет " +
                               $"в каталоге '{catalog.name}' - сохранённое местоположение игрока не используется.", this);
            }

            location = openedLocation != null ? openedLocation : catalog.StartLocation;
            checkpointId = location != null ? location.DefaultCheckpointId : null;
        }

        private void SpawnPlayer(LocationDefinitionSO location, string checkpointId)
        {
            if (m_Player == null)
            {
                Debug.LogError("[LocationScene] У LocationGameStarter не назначен объект игрока.", this);
                return;
            }

            if (location != null
                && m_PlayerCheckpointController.TryGetSpawnCheckpoint(location, checkpointId, out var checkpoint))
            {
                // Игрок выключен, поэтому его CharacterController не мешает телепортации.
                m_Player.transform.SetPositionAndRotation(checkpoint.Position, checkpoint.Rotation);
                m_PlayerCheckpointController.RestoreCheckpoint(location.LocationId, checkpoint.CheckpointId);
            }

            m_Player.SetActive(true);
        }

        /// <summary>
        /// Бины, которые обновляет ядро: все, кроме бинов стартовых сцен локаций, - тех обновляет стартер
        /// их сцены и перестаёт обновлять, когда сцена выгружается.
        /// </summary>
        private List<IUpdateSequence> CollectCoreUpdateSequence()
        {
            var result = new List<IUpdateSequence>();

            if (m_UpdateSequence == null)
            {
                return result;
            }

            var locationSceneHandles = new HashSet<int>();

            for (int i = 0; i < m_SceneStarters.Count; i++)
            {
                locationSceneHandles.Add(m_SceneStarters[i].gameObject.scene.handle);
            }

            for (int i = 0; i < m_UpdateSequence.Count; i++)
            {
                var bean = m_UpdateSequence[i];

                if (bean is Component component && locationSceneHandles.Contains(component.gameObject.scene.handle))
                {
                    continue;
                }

                result.Add(bean);
            }

            return result;
        }

        private void SetLoadingScreenVisible(bool isVisible)
        {
            if (m_LoadingScreen != null)
            {
                m_LoadingScreen.SetActive(isVisible);
            }
        }
    }
}
