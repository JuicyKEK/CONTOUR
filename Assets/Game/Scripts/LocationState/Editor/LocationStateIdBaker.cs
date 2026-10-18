using System;
using System.Collections.Generic;
using Game.Scripts.LocationState.Runtime.Controllers;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Scripts.LocationState.EditorTools
{
    /// <summary>
    /// Проставляет ключи сохранения объектам с LocationStateSaver при каждом сохранении сцены: тем, у кого
    /// ключа нет (новый объект, новый экземпляр префаба), и тем, у кого ключ совпал с другим объектом сцены
    /// (копия через Ctrl+D). Уже проставленные уникальные ключи не меняются - иначе потерялись бы сохранённые
    /// состояния. Проставить ключи в открытых сценах вручную: UnityDev/Locations/Assign Object State Ids.
    /// </summary>
    [InitializeOnLoad]
    public static class LocationStateIdBaker
    {
        private const string StateIdProperty = "m_StateId";

        static LocationStateIdBaker()
        {
            EditorSceneManager.sceneSaving += OnSceneSaving;
        }

        [MenuItem("UnityDev/Locations/Assign Object State Ids")]
        public static void AssignIdsInOpenScenes()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);

                if (scene.isLoaded && AssignIds(scene) > 0)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                }
            }
        }

        private static void OnSceneSaving(Scene scene, string path)
        {
            AssignIds(scene);
        }

        /// <summary>
        /// Возвращает, скольким объектам сцены присвоены новые ключи.
        /// </summary>
        public static int AssignIds(Scene scene)
        {
            var usedIds = new HashSet<string>();
            int assignedCount = 0;

            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var saver in root.GetComponentsInChildren<LocationStateSaver>(true))
                {
                    var serializedObject = new SerializedObject(saver);
                    var property = serializedObject.FindProperty(StateIdProperty);

                    if (!string.IsNullOrEmpty(property.stringValue) && usedIds.Add(property.stringValue))
                    {
                        continue;
                    }

                    property.stringValue = Guid.NewGuid().ToString("N");
                    serializedObject.ApplyModifiedPropertiesWithoutUndo();
                    usedIds.Add(property.stringValue);
                    assignedCount++;
                }
            }

            if (assignedCount > 0)
            {
                Debug.Log($"[LocationState] Сцена '{scene.name}': ключи сохранения присвоены объектам - {assignedCount}.");
            }

            return assignedCount;
        }
    }
}
