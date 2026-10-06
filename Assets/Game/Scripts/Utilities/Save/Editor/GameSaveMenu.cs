using System.IO;
using JuicyDI;
using UnityEditor;
using UnityEngine;

namespace Game.Scripts.Utilities.Save.EditorTools
{
    /// <summary>
    /// Меню редактора для сохранений игры (все механики пишут в Application.persistentDataPath/Saves).
    /// </summary>
    public static class GameSaveMenu
    {
        [MenuItem("UnityDev/Saves/Delete All Saves")]
        public static void DeleteAllSaves()
        {
            var folder = JsonSaveFile.FolderPath;

            if (!EditorUtility.DisplayDialog(
                    "Сохранения",
                    $"Удалить все сохранения игры?\n{folder}",
                    "Удалить",
                    "Отмена"))
            {
                return;
            }

            // В Play Mode - сначала через механики, чтобы они сбросили и свои кэши.
            if (Application.isPlaying)
            {
                BinController.GetContext()?.GetBean<IGameSaveService>()?.DeleteSaves();
            }

            int deletedCount = 0;

            if (Directory.Exists(folder))
            {
                foreach (var file in Directory.GetFiles(folder))
                {
                    File.Delete(file);
                    deletedCount++;
                }
            }

            Debug.Log($"[GameSave] Удалено файлов сохранений: {deletedCount} ({folder}).");
        }

        [MenuItem("UnityDev/Saves/Open Saves Folder")]
        public static void OpenSavesFolder()
        {
            var folder = JsonSaveFile.FolderPath;
            Directory.CreateDirectory(folder);
            EditorUtility.RevealInFinder(folder);
        }
    }
}
