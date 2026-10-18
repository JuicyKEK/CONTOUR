using System.Collections.Generic;
using UnityEngine;

namespace Game.Scripts.LocationScene.Runtime.Data
{
    /// <summary>
    /// Карта игры: сцена ядра (игрок, UI и контроллеры, которые живут всю игру), стартовая локация
    /// новой игры, все локации и все переходы между ними. По каталогу загрузчик локаций понимает,
    /// какие сцены должны быть загружены вместе с локацией, а сохранение - находит локацию по её ключу.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Locations/Location Catalog", fileName = "LocationCatalog")]
    public class LocationCatalogSO : ScriptableObject
    {
        [Tooltip("Сцена ядра игры: игрок, интерфейс и контроллеры, которые не выгружаются при смене " +
                 "локаций (должна быть добавлена в Build Settings).")]
        [SerializeField] private SceneReference m_GameCoreScene = new();
        [Tooltip("Локация, с которой начинается новая игра.")]
        [SerializeField] private LocationDefinitionSO m_StartLocation;
        [Tooltip("Все локации игры.")]
        [SerializeField] private List<LocationDefinitionSO> m_Locations = new();
        [Tooltip("Все переходные сцены игры.")]
        [SerializeField] private List<LocationTransitionDefinitionSO> m_Transitions = new();

        public string GameCoreScenePath => m_GameCoreScene.ScenePath;
        public LocationDefinitionSO StartLocation => m_StartLocation;
        public IReadOnlyList<LocationDefinitionSO> Locations => m_Locations;
        public IReadOnlyList<LocationTransitionDefinitionSO> Transitions => m_Transitions;

        public LocationDefinitionSO FindLocation(string locationId)
        {
            if (string.IsNullOrEmpty(locationId))
            {
                return null;
            }

            for (int i = 0; i < m_Locations.Count; i++)
            {
                if (m_Locations[i] != null && m_Locations[i].LocationId == locationId)
                {
                    return m_Locations[i];
                }
            }

            return null;
        }

        public LocationDefinitionSO FindLocationByScenePath(string scenePath)
        {
            if (string.IsNullOrEmpty(scenePath))
            {
                return null;
            }

            for (int i = 0; i < m_Locations.Count; i++)
            {
                if (m_Locations[i] != null && m_Locations[i].ScenePath == scenePath)
                {
                    return m_Locations[i];
                }
            }

            return null;
        }

        /// <summary>
        /// Локация, в сцене которой стоит чекпоинт с таким ключом. Работает и для незагруженных сцен:
        /// ищет по спискам чекпоинтов в описаниях локаций (обновляются при сохранении сцены локации).
        /// </summary>
        public LocationDefinitionSO FindLocationByCheckpoint(string checkpointId)
        {
            if (string.IsNullOrEmpty(checkpointId))
            {
                return null;
            }

            for (int i = 0; i < m_Locations.Count; i++)
            {
                if (m_Locations[i] != null && m_Locations[i].HasCheckpoint(checkpointId))
                {
                    return m_Locations[i];
                }
            }

            return null;
        }

        public LocationTransitionDefinitionSO FindTransitionByScenePath(string scenePath)
        {
            if (string.IsNullOrEmpty(scenePath))
            {
                return null;
            }

            for (int i = 0; i < m_Transitions.Count; i++)
            {
                if (m_Transitions[i] != null && m_Transitions[i].ScenePath == scenePath)
                {
                    return m_Transitions[i];
                }
            }

            return null;
        }

        private void OnValidate()
        {
            m_GameCoreScene.Validate();
        }
    }
}
