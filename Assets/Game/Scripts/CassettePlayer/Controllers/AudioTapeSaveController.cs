using System;
using System.IO;
using Game.Scripts.Instructions.Data;
using Game.Scripts.Instructions.Interfaces;
using JuicyDI;
using JuicyDI.Attributes;
using JuicyDI.Context;
using UnityEngine;

namespace Game.Scripts.Instructions.Controllers
{
    /// <summary>
    /// Сохранение прогресса кассет в JSON-файл (Application.persistentDataPath/Saves).
    /// Пишет/читает только Id найденных, прослушанных и выполненных кассет из <see cref="IAudioTapeFoundRegistry"/>.
    ///
    /// Сохранение вызывается снаружи (чекпоинты) через <see cref="IAudioTapeSaveService.Save"/>.
    /// Загрузка при старте - один раз за сессию, повторная (например, откат на чекпоинт) -
    /// явным вызовом <see cref="IAudioTapeSaveService.Load"/>.
    /// Для проверки в Play Mode методы доступны из контекстного меню компонента.
    /// </summary>
    [JDIMonoController(Context = typeof(GlobalBean))]
    public class AudioTapeSaveController : MonoBehaviour, IAudioTapeSaveService, ISequence
    {
        private const string SaveFolderName = "Saves";

        [Tooltip("Имя JSON-файла сохранения кассет (лежит в Application.persistentDataPath/Saves).")]
        [SerializeField] private string m_FileName = "audio_tapes.json";
        [Tooltip("Загружать сохранение при первом старте игровой сессии.")]
        [SerializeField] private bool m_LoadOnStart = true;

        [Inject] private IAudioTapeFoundRegistry m_Registry;

        private bool m_IsStartLoadDone;

        public string SavePath => Path.Combine(Application.persistentDataPath, SaveFolderName, m_FileName);
        public bool HasSave => File.Exists(SavePath);

        public void MethodInit()
        {
            // MethodInit всех участников вызывается раньше любого MethodStart, поэтому к моменту,
            // когда CassettePlayerController строит страницы, реестр уже восстановлен из сохранения.
            // Глобальный бин переживает смену сцены - автоматически грузимся только один раз.
            if (!m_LoadOnStart || m_IsStartLoadDone)
            {
                return;
            }

            m_IsStartLoadDone = true;
            Load();
        }

        public void MethodStart()
        {
        }

        [ContextMenu("Save")]
        public void Save()
        {
            if (m_Registry == null)
            {
                Debug.LogError("[AudioTapeSaveController] Реестр кассет не заинжекчен - сохранение невозможно.", this);
                return;
            }

            var data = new AudioTapeSaveData();
            data.FoundTapeIds.AddRange(m_Registry.FoundTapeIds);
            data.FoundTapeIds.Sort(StringComparer.Ordinal);
            data.ListenedTapeIds.AddRange(m_Registry.ListenedTapeIds);
            data.ListenedTapeIds.Sort(StringComparer.Ordinal);
            data.CompletedTapeIds.AddRange(m_Registry.CompletedTapeIds);
            data.CompletedTapeIds.Sort(StringComparer.Ordinal);

            string path = SavePath;
            string tempPath = path + ".tmp";

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));

                // Пишем во временный файл и подменяем им сохранение, чтобы падение посреди записи
                // не оставило игрока с повреждённым файлом.
                File.WriteAllText(tempPath, JsonUtility.ToJson(data, true));

                if (File.Exists(path))
                {
                    File.Replace(tempPath, path, null);
                }
                else
                {
                    File.Move(tempPath, path);
                }
            }
            catch (Exception exception)
            {
                Debug.LogError($"[AudioTapeSaveController] Не удалось сохранить кассеты в '{path}': {exception}", this);
            }
        }

        public bool Load()
        {
            if (m_Registry == null)
            {
                Debug.LogError("[AudioTapeSaveController] Реестр кассет не заинжекчен - загрузка невозможна.", this);
                return false;
            }

            string path = SavePath;

            if (!File.Exists(path))
            {
                return false;
            }

            AudioTapeSaveData data;

            try
            {
                data = JsonUtility.FromJson<AudioTapeSaveData>(File.ReadAllText(path));
            }
            catch (Exception exception)
            {
                Debug.LogError($"[AudioTapeSaveController] Не удалось прочитать сохранение кассет '{path}': {exception}", this);
                return false;
            }

            if (data == null)
            {
                Debug.LogError($"[AudioTapeSaveController] Сохранение кассет '{path}' пустое.", this);
                return false;
            }

            m_Registry.RestoreFoundTapes(data.FoundTapeIds);
            m_Registry.RestoreListenedTapes(data.ListenedTapeIds);
            m_Registry.RestoreCompletedTapes(data.CompletedTapeIds);
            return true;
        }

        [ContextMenu("Delete Save")]
        public void DeleteSave()
        {
            string path = SavePath;

            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (Exception exception)
            {
                Debug.LogError($"[AudioTapeSaveController] Не удалось удалить сохранение кассет '{path}': {exception}", this);
            }
        }

        [ContextMenu("Load")]
        private void LoadFromContextMenu()
        {
            Load();
        }
    }
}
