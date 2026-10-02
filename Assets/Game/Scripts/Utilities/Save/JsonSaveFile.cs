using System;
using System.IO;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Scripts.Utilities.Save
{
    /// <summary>
    /// Чтение/запись JSON-сохранений (через <see cref="JsonUtility"/>) в папку
    /// Application.persistentDataPath/Saves. Общий код для сохранений разных механик
    /// (кассеты, зоны заражения и т.д.) - каждая механика пишет в свой файл.
    /// </summary>
    public static class JsonSaveFile
    {
        public const string SaveFolderName = "Saves";

        public static string FolderPath => Path.Combine(Application.persistentDataPath, SaveFolderName);

        public static string GetPath(string fileName)
        {
            return Path.Combine(FolderPath, fileName);
        }

        public static bool Exists(string fileName)
        {
            return File.Exists(GetPath(fileName));
        }

        /// <summary>
        /// Сохраняет данные в файл. Пишет во временный файл и подменяет им сохранение, чтобы падение
        /// посреди записи не оставило игрока с повреждённым файлом. Ошибки пишутся в лог.
        /// </summary>
        public static bool TryWrite<T>(string fileName, T data, Object context = null)
        {
            string path = GetPath(fileName);
            string tempPath = path + ".tmp";

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(tempPath, JsonUtility.ToJson(data, true));

                if (File.Exists(path))
                {
                    File.Replace(tempPath, path, null);
                }
                else
                {
                    File.Move(tempPath, path);
                }

                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[JsonSaveFile] Не удалось записать сохранение '{path}': {exception}", context);
                return false;
            }
        }

        /// <summary>
        /// Читает сохранение. Возвращает false, если файла нет (без ошибки в логе)
        /// или его не удалось прочитать (с ошибкой в логе).
        /// </summary>
        public static bool TryRead<T>(string fileName, out T data, Object context = null) where T : class
        {
            data = null;
            string path = GetPath(fileName);

            if (!File.Exists(path))
            {
                return false;
            }

            try
            {
                data = JsonUtility.FromJson<T>(File.ReadAllText(path));
            }
            catch (Exception exception)
            {
                Debug.LogError($"[JsonSaveFile] Не удалось прочитать сохранение '{path}': {exception}", context);
                return false;
            }

            if (data == null)
            {
                Debug.LogError($"[JsonSaveFile] Сохранение '{path}' пустое.", context);
                return false;
            }

            return true;
        }

        public static void Delete(string fileName, Object context = null)
        {
            string path = GetPath(fileName);

            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (Exception exception)
            {
                Debug.LogError($"[JsonSaveFile] Не удалось удалить сохранение '{path}': {exception}", context);
            }
        }
    }
}
