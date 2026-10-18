using UnityEngine;

namespace Game.Scripts.LocationScene.Runtime.Data
{
    /// <summary>
    /// Описание переходной (смежной) сцены: сама сцена и две локации, которые она соединяет.
    /// Переходная сцена загружена, пока загружена хотя бы одна из её локаций. В самой сцене на обоих
    /// концах стоят триггеры LocationTransitionGate - по ним подгружается и выгружается соседняя локация.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Locations/Location Transition", fileName = "Transition_")]
    public class LocationTransitionDefinitionSO : ScriptableObject
    {
        [Tooltip("Переходная сцена (должна быть добавлена в Build Settings).")]
        [SerializeField] private SceneReference m_Scene = new();
        [Tooltip("Локация на одном конце перехода.")]
        [SerializeField] private LocationDefinitionSO m_LocationA;
        [Tooltip("Локация на другом конце перехода.")]
        [SerializeField] private LocationDefinitionSO m_LocationB;

        public string ScenePath => m_Scene.ScenePath;
        public LocationDefinitionSO LocationA => m_LocationA;
        public LocationDefinitionSO LocationB => m_LocationB;

        /// <summary>
        /// Связан ли переход с локацией.
        /// </summary>
        public bool Connects(LocationDefinitionSO location)
        {
            return location != null && (location == m_LocationA || location == m_LocationB);
        }

        /// <summary>
        /// Локация на другом конце перехода (null, если переход с локацией не связан).
        /// </summary>
        public LocationDefinitionSO GetOpposite(LocationDefinitionSO location)
        {
            if (location == null)
            {
                return null;
            }

            if (location == m_LocationA)
            {
                return m_LocationB;
            }

            return location == m_LocationB ? m_LocationA : null;
        }

        private void OnValidate()
        {
            m_Scene.Validate();
        }
    }
}
