using System.IO;
using UnityEngine;

namespace GBS.Save
{
    /// <summary>
    /// Файловое хранилище прогресса сюжета (JSON в persistentDataPath).
    /// Очистить сейвы можно из редактора: UnityDev/GBS/Saves/Clear Saves.
    /// </summary>
    public static class GBSSaveSystem
    {
        public const string FileName = "gbs_story_save.json";

        public static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

        public static bool Exists()
        {
            return File.Exists(FilePath);
        }

        public static void Save(GBSSaveData data)
        {
            if (data == null)
            {
                return;
            }

            try
            {
                var json = JsonUtility.ToJson(data, true);
                File.WriteAllText(FilePath, json);
            }
            catch (IOException exception)
            {
                Debug.LogError($"[GBS] Не удалось сохранить прогресс: {exception.Message}");
            }
        }

        public static GBSSaveData Load()
        {
            if (!Exists())
            {
                return null;
            }

            try
            {
                var json = File.ReadAllText(FilePath);
                return JsonUtility.FromJson<GBSSaveData>(json);
            }
            catch (IOException exception)
            {
                Debug.LogError($"[GBS] Не удалось прочитать прогресс: {exception.Message}");
                return null;
            }
        }

        public static void Clear()
        {
            if (!Exists())
            {
                return;
            }

            try
            {
                File.Delete(FilePath);
            }
            catch (IOException exception)
            {
                Debug.LogError($"[GBS] Не удалось удалить сейв: {exception.Message}");
            }
        }
    }
}
