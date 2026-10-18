using System.Collections.Generic;
using Game.Scripts.Story;
using UnityEngine;

namespace Game.Scripts.LocationScene.Runtime.Data
{
    /// <summary>
    /// Описание полноценной локации: уникальный ключ (пишется в сохранение), её сцена и чекпоинт,
    /// на котором игрок появляется в локации по умолчанию. С какими переходными сценами локация
    /// связана - описано в самих переходах (<see cref="LocationTransitionDefinitionSO"/>).
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Locations/Location", fileName = "Location_")]
    public class LocationDefinitionSO : ScriptableObject
    {
        [Tooltip("Уникальный ключ локации. Пишется в сохранение игрока.")]
        [SerializeField] private string m_LocationId;
        [Tooltip("Сцена локации (должна быть добавлена в Build Settings).")]
        [SerializeField] private SceneReference m_Scene = new();
        [Tooltip("Чекпоинт (LocationCheckpoint в сцене локации), на котором игрок появляется, если в " +
                 "сохранении чекпоинта этой локации нет (новая игра) или сохранённый чекпоинт не найден.")]
        [SerializeField, StoryKey(StoryKeyKind.Checkpoint)] private string m_DefaultCheckpointId;
        [Tooltip("Ключи всех чекпоинтов (LocationCheckpoint) в сцене локации. Руками не заполняется: список " +
                 "обновляется сам при сохранении сцены локации (или меню UnityDev/Locations/Collect Checkpoints). " +
                 "По нему игра понимает, на какой локации стоит чекпоинт, пока её сцена не загружена.")]
        [SerializeField] private List<string> m_CheckpointIds = new();

        public string LocationId => m_LocationId;
        public string ScenePath => m_Scene.ScenePath;
        public string DefaultCheckpointId => m_DefaultCheckpointId;
        public IReadOnlyList<string> CheckpointIds => m_CheckpointIds;

        /// <summary>
        /// Стоит ли в сцене локации чекпоинт с таким ключом (по списку, собранному при сохранении сцены).
        /// </summary>
        public bool HasCheckpoint(string checkpointId)
        {
            return !string.IsNullOrEmpty(checkpointId) && m_CheckpointIds.Contains(checkpointId);
        }

#if UNITY_EDITOR
        /// <summary>
        /// Только для редактора: записывает ключи чекпоинтов, найденных в сцене локации.
        /// Возвращает false, если список не изменился.
        /// </summary>
        public bool SetCheckpointIds(List<string> checkpointIds)
        {
            if (m_CheckpointIds.Count == checkpointIds.Count)
            {
                bool isSame = true;

                for (int i = 0; i < checkpointIds.Count && isSame; i++)
                {
                    isSame = m_CheckpointIds[i] == checkpointIds[i];
                }

                if (isSame)
                {
                    return false;
                }
            }

            m_CheckpointIds.Clear();
            m_CheckpointIds.AddRange(checkpointIds);
            return true;
        }
#endif

        private void OnValidate()
        {
            m_Scene.Validate();
        }
    }
}
