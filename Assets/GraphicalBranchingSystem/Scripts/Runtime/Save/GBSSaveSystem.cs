using Game.Scripts.Utilities.Save;

namespace GBS.Save
{
    /// <summary>
    /// Файловое хранилище прогресса сюжета (JSON в Application.persistentDataPath/Saves, см. JsonSaveFile).
    /// Очистить сейвы можно из редактора: UnityDev/GBS/Saves/Clear Saves.
    /// </summary>
    public static class GBSSaveSystem
    {
        public const string FileName = "gbs_story_save.json";

        public static string FilePath => JsonSaveFile.GetPath(FileName);

        public static bool Exists()
        {
            return JsonSaveFile.Exists(FileName);
        }

        public static void Save(GBSSaveData data)
        {
            if (data != null)
            {
                JsonSaveFile.TryWrite(FileName, data);
            }
        }

        public static GBSSaveData Load()
        {
            return JsonSaveFile.TryRead(FileName, out GBSSaveData data) ? data : null;
        }

        public static void Clear()
        {
            JsonSaveFile.Delete(FileName);
        }
    }
}
