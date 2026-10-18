using System;
using UnityEngine;

namespace Game.Scripts.LocationScene.Runtime.Data
{
    /// <summary>
    /// Ссылка на сцену для ассетов локаций. В инспекторе сцена назначается перетаскиванием ассета,
    /// а в рантайме (и в билде, где SceneAsset нет) используется её путь - по нему сцена грузится
    /// через SceneManager. Путь обновляется в редакторе в <see cref="Validate"/> (его вызывает OnValidate
    /// ассета-владельца), поэтому переименование или перенос сцены ссылку не ломает.
    /// Сцена должна быть добавлена в Build Settings.
    /// </summary>
    [Serializable]
    public class SceneReference
    {
#if UNITY_EDITOR
        [SerializeField] private UnityEditor.SceneAsset m_SceneAsset;
#endif
        [SerializeField] private string m_ScenePath;

        public string ScenePath => m_ScenePath;
        public bool IsAssigned => !string.IsNullOrEmpty(m_ScenePath);

        /// <summary>
        /// Обновляет путь по назначенному ассету сцены. Вне редактора ничего не делает.
        /// </summary>
        public void Validate()
        {
#if UNITY_EDITOR
            m_ScenePath = m_SceneAsset != null
                ? UnityEditor.AssetDatabase.GetAssetPath(m_SceneAsset)
                : string.Empty;
#endif
        }
    }
}
