using GBS.Data;
using GBS.Save;
using GBS.Windows;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace GBS.EditorTools
{
    /// <summary>
    /// Пункты меню GBS: работа с игровыми сейвами сюжета и открытие графа
    /// двойным кликом по ассету.
    /// </summary>
    public static class GBSMenu
    {
        [MenuItem("UnityDev/GBS/Saves/Clear Saves")]
        public static void ClearSaves()
        {
            if (!EditorUtility.DisplayDialog(
                    "GBS",
                    $"Удалить файл прогресса сюжета?\n{GBSSaveSystem.FilePath}",
                    "Удалить",
                    "Отмена"))
            {
                return;
            }

            GBSSaveSystem.Clear();

            Debug.Log("[GBS] Сейв прогресса удалён.");
        }

        [MenuItem("UnityDev/GBS/Saves/Show Save Path")]
        public static void ShowSavePath()
        {
            Debug.Log($"[GBS] Файл сейва: {GBSSaveSystem.FilePath} (существует: {GBSSaveSystem.Exists()})");
        }

        [OnOpenAsset(0)]
        public static bool OnOpenGraphAsset(int instanceId, int line)
        {
            if (EditorUtility.InstanceIDToObject(instanceId) is not GBSGraphSO graph)
            {
                return false;
            }

            GBSEditorWindow.Open(graph);

            return true;
        }
    }
}
