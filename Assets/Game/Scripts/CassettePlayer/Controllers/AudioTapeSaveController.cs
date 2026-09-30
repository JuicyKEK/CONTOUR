using System;
using Game.Scripts.Instructions.Data;
using Game.Scripts.Instructions.Interfaces;
using Game.Scripts.Utilities.Save;
using JuicyDI;
using JuicyDI.Attributes;
using JuicyDI.Context;
using UnityEngine;

namespace Game.Scripts.Instructions.Controllers
{
    /// <summary>
    /// Сохранение прогресса кассет в JSON-файл (Application.persistentDataPath/Saves, см. <see cref="JsonSaveFile"/>).
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
        [Tooltip("Имя JSON-файла сохранения кассет (лежит в Application.persistentDataPath/Saves).")]
        [SerializeField] private string m_FileName = "audio_tapes.json";
        [Tooltip("Загружать сохранение при первом старте игровой сессии.")]
        [SerializeField] private bool m_LoadOnStart = true;

        [Inject] private IAudioTapeFoundRegistry m_Registry;

        private bool m_IsStartLoadDone;

        public string SavePath => JsonSaveFile.GetPath(m_FileName);
        public bool HasSave => JsonSaveFile.Exists(m_FileName);

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

            JsonSaveFile.TryWrite(m_FileName, data, this);
        }

        public bool Load()
        {
            if (m_Registry == null)
            {
                Debug.LogError("[AudioTapeSaveController] Реестр кассет не заинжекчен - загрузка невозможна.", this);
                return false;
            }

            if (!JsonSaveFile.TryRead(m_FileName, out AudioTapeSaveData data, this))
            {
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
            JsonSaveFile.Delete(m_FileName, this);
        }

        [ContextMenu("Load")]
        private void LoadFromContextMenu()
        {
            Load();
        }
    }
}
