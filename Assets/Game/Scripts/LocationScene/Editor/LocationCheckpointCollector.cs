using System;
using System.Collections.Generic;
using Game.Scripts.LocationScene.Runtime.Controllers;
using Game.Scripts.LocationScene.Runtime.Data;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Scripts.LocationScene.EditorTools
{
    /// <summary>
    /// Записывает в описание локации (<see cref="LocationDefinitionSO"/>) ключи чекпоинтов, которые стоят
    /// в её сцене. По этому списку игра понимает, на какой локации стоит чекпоинт, пока её сцена не
    /// загружена, - так сюжет может назначить игроку чекпоинт на другой локации.
    ///
    /// Список обновляется сам при каждом сохранении сцены локации. Пересобрать его для всех локаций
    /// разом (например, после слияния веток) - меню UnityDev/Locations/Collect Checkpoints.
    /// </summary>
    [InitializeOnLoad]
    public static class LocationCheckpointCollector
    {
        static LocationCheckpointCollector()
        {
            EditorSceneManager.sceneSaved += OnSceneSaved;
        }

        [MenuItem("UnityDev/Locations/Collect Checkpoints")]
        public static void CollectAll()
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("[LocationScene] Собрать чекпоинты локаций можно только вне Play Mode.");
                return;
            }

            var locations = FindLocations();
            int updatedCount = 0;

            for (int i = 0; i < locations.Count; i++)
            {
                var location = locations[i];

                if (string.IsNullOrEmpty(location.ScenePath))
                {
                    continue;
                }

                var scene = SceneManager.GetSceneByPath(location.ScenePath);
                bool isOpenedHere = !scene.isLoaded;

                if (isOpenedHere)
                {
                    scene = EditorSceneManager.OpenScene(location.ScenePath, OpenSceneMode.Additive);
                }

                if (Collect(scene, location, locations))
                {
                    updatedCount++;
                }

                if (isOpenedHere)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[LocationScene] Чекпоинты собраны: локаций {locations.Count}, обновлено {updatedCount}.");
        }

        private static void OnSceneSaved(Scene scene)
        {
            var locations = FindLocations();

            for (int i = 0; i < locations.Count; i++)
            {
                if (locations[i].ScenePath == scene.path && Collect(scene, locations[i], locations))
                {
                    AssetDatabase.SaveAssetIfDirty(locations[i]);
                }
            }
        }

        /// <summary>
        /// Возвращает true, если список чекпоинтов локации изменился.
        /// </summary>
        private static bool Collect(Scene scene, LocationDefinitionSO location, List<LocationDefinitionSO> locations)
        {
            var checkpointIds = new List<string>();

            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var checkpoint in root.GetComponentsInChildren<LocationCheckpoint>(true))
                {
                    string checkpointId = checkpoint.CheckpointId;

                    if (string.IsNullOrEmpty(checkpointId))
                    {
                        Debug.LogWarning($"[LocationScene] У чекпоинта '{checkpoint.name}' в сцене '{scene.name}' не задан ключ.", checkpoint);
                        continue;
                    }

                    if (checkpointIds.Contains(checkpointId))
                    {
                        Debug.LogWarning($"[LocationScene] В сцене '{scene.name}' несколько чекпоинтов с ключом '{checkpointId}'.", checkpoint);
                        continue;
                    }

                    checkpointIds.Add(checkpointId);
                }
            }

            checkpointIds.Sort(StringComparer.Ordinal);

            for (int i = 0; i < locations.Count; i++)
            {
                if (locations[i] == location)
                {
                    continue;
                }

                for (int j = 0; j < checkpointIds.Count; j++)
                {
                    if (locations[i].HasCheckpoint(checkpointIds[j]))
                    {
                        Debug.LogWarning($"[LocationScene] Чекпоинт с ключом '{checkpointIds[j]}' есть и в локации '{location.name}', " +
                                         $"и в локации '{locations[i].name}' - ключи чекпоинтов должны быть уникальны в пределах игры.", location);
                    }
                }
            }

            bool isChanged = location.SetCheckpointIds(checkpointIds);

            if (isChanged)
            {
                EditorUtility.SetDirty(location);
                Debug.Log($"[LocationScene] Чекпоинты локации '{location.name}': " +
                          $"{(checkpointIds.Count > 0 ? string.Join(", ", checkpointIds) : "нет")}.", location);
            }

            if (!string.IsNullOrEmpty(location.DefaultCheckpointId) && !location.HasCheckpoint(location.DefaultCheckpointId))
            {
                Debug.LogWarning($"[LocationScene] В сцене локации '{location.name}' нет её чекпоинта по умолчанию " +
                                 $"'{location.DefaultCheckpointId}'.", location);
            }

            return isChanged;
        }

        private static List<LocationDefinitionSO> FindLocations()
        {
            var result = new List<LocationDefinitionSO>();

            foreach (string guid in AssetDatabase.FindAssets($"t:{nameof(LocationDefinitionSO)}"))
            {
                var location = AssetDatabase.LoadAssetAtPath<LocationDefinitionSO>(AssetDatabase.GUIDToAssetPath(guid));

                if (location != null)
                {
                    result.Add(location);
                }
            }

            return result;
        }
    }
}
