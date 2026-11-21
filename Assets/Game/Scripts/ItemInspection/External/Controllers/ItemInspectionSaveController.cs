using System;
using System.Collections.Generic;
using Game.Scripts.ItemInspection.Data;
using Game.Scripts.Utilities.Save;
using JuicyDI;
using JuicyDI.Attributes;
using UnityEngine;

namespace Game.Scripts.ItemInspection.Controllers
{
    /// <summary>
    /// Сохранение осмотров в свой JSON-файл (Application.persistentDataPath/Saves, см. <see cref="JsonSaveFile"/>):
    /// какие типы предметов игрок уже осматривал при подборе (<see cref="IItemInspectionRegistry"/>).
    ///
    /// Сохранение вызывается общим контроллером сохранений (GameSaveController, ISaveParticipant) - в графе сюжета
    /// это действие "Save/Save Game". Загрузка - при старте игровой сессии, в MethodInit (один раз на этот компонент).
    /// Для проверки в Play Mode методы доступны из контекстного меню компонента.
    /// </summary>
    [JDIMonoController]
    [SequenceParticipant(106)]
    public class ItemInspectionSaveController : MonoBehaviour, ISaveParticipant, ISequence
    {
        [Tooltip("Имя JSON-файла сохранения осмотров (лежит в Application.persistentDataPath/Saves).")]
        [SerializeField] private string m_FileName = "item_inspection.json";
        [Tooltip("Загружать сохранение при старте игровой сессии.")]
        [SerializeField] private bool m_LoadOnStart = true;

        [Inject] private IItemInspectionRegistry m_Registry;

        private readonly List<string> m_KeysBuffer = new();
        private bool m_IsStartLoadDone;

        public string SavePath => JsonSaveFile.GetPath(m_FileName);
        public bool HasSave => JsonSaveFile.Exists(m_FileName);

        public void MethodInit()
        {
            // При загрузке следующей локации MethodInit зовётся снова - сохранение читается только в первый раз.
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
                Debug.LogError("[ItemInspectionSaveController] Реестр осмотров не заинжекчен - сохранение невозможно.", this);
                return;
            }

            var data = new ItemInspectionSaveData();
            m_Registry.GetInspectedKeys(m_KeysBuffer);
            data.InspectedTypes.AddRange(m_KeysBuffer);
            data.InspectedTypes.Sort(StringComparer.Ordinal);
            m_KeysBuffer.Clear();

            JsonSaveFile.TryWrite(m_FileName, data, this);
        }

        public bool Load()
        {
            if (m_Registry == null)
            {
                Debug.LogError("[ItemInspectionSaveController] Реестр осмотров не заинжекчен - загрузка невозможна.", this);
                return false;
            }

            if (!JsonSaveFile.TryRead(m_FileName, out ItemInspectionSaveData data, this))
            {
                return false;
            }

            m_Registry.ClearInspected();

            if (data.InspectedTypes != null)
            {
                foreach (string typeKey in data.InspectedTypes)
                {
                    m_Registry.MarkInspected(typeKey);
                }
            }

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
