using System.Collections.Generic;
using Game.Scripts.InfectionZone.External.Data;
using Game.Scripts.InfectionZone.Runtime.Interfaces;
using Game.Scripts.Utilities.Save;
using JuicyDI;
using JuicyDI.Attributes;
using UnityEngine;

namespace Game.Scripts.InfectionZone.External.Controllers
{
    /// <summary>
    /// Сохранение уровней заражения зон в JSON-файл (Application.persistentDataPath/Saves, см. <see cref="JsonSaveFile"/>).
    ///
    /// Загрузка при старте сцены: зоны регистрируются в реестре в своём MethodInit (порядок 10), этот
    /// контроллер в MethodInit (порядок 15) выставляет им сохранённые уровни, а стартовый уровень из
    /// инспектора зона применяет в MethodStart только если уровень не восстановлен из сохранения.
    /// Бин сцены: при перезагрузке сцены (откат на чекпоинт) зоны создаются заново и снова получают
    /// уровни из файла.
    ///
    /// Сохранение вызывается общим контроллером сохранений (GameSaveController, ISaveParticipant) на
    /// чекпоинтах. Уровни зон, которых сейчас нет на сцене (другие локации), из файла не теряются.
    /// Для проверки в Play Mode методы доступны из контекстного меню компонента.
    /// </summary>
    [JDIMonoController]
    [SequenceParticipant(15)]
    public class InfectionZoneSaveController : MonoBehaviour, IInfectionZoneSaveService, ISaveParticipant, ISequence
    {
        [Tooltip("Имя JSON-файла сохранения зон (лежит в Application.persistentDataPath/Saves).")]
        [SerializeField] private string m_FileName = "infection_zones.json";
        [Tooltip("Загружать сохранение при старте сцены.")]
        [SerializeField] private bool m_LoadOnStart = true;

        [Inject] private IInfectionZoneRegistry m_Registry;

        // Последние известные уровни всех зон (в т.ч. тех, что сейчас не на сцене), ключ - ZoneId.
        private readonly Dictionary<string, float> m_SavedLevels = new();

        public string SavePath => JsonSaveFile.GetPath(m_FileName);
        public bool HasSave => JsonSaveFile.Exists(m_FileName);

        public void MethodInit()
        {
            if (m_LoadOnStart)
            {
                Load();
            }
        }

        public void MethodStart()
        {
        }

        [ContextMenu("Save")]
        public void Save()
        {
            if (m_Registry == null)
            {
                Debug.LogError("[InfectionZoneSaveController] Реестр зон не заинжекчен - сохранение невозможно.", this);
                return;
            }

            foreach (var zone in m_Registry.Zones)
            {
                m_SavedLevels[zone.ZoneId] = zone.InfectionLevel.CurrentValue;
            }

            var data = new InfectionZoneSaveData();

            foreach (var savedLevel in m_SavedLevels)
            {
                data.Zones.Add(new InfectionZoneSaveEntry(savedLevel.Key, savedLevel.Value));
            }

            data.Zones.Sort((a, b) => string.CompareOrdinal(a.ZoneId, b.ZoneId));
            JsonSaveFile.TryWrite(m_FileName, data, this);
        }

        public bool Load()
        {
            if (m_Registry == null)
            {
                Debug.LogError("[InfectionZoneSaveController] Реестр зон не заинжекчен - загрузка невозможна.", this);
                return false;
            }

            if (!JsonSaveFile.TryRead(m_FileName, out InfectionZoneSaveData data, this))
            {
                return false;
            }

            m_SavedLevels.Clear();

            if (data.Zones != null)
            {
                foreach (var entry in data.Zones)
                {
                    if (entry != null && !string.IsNullOrEmpty(entry.ZoneId))
                    {
                        m_SavedLevels[entry.ZoneId] = entry.InfectionLevel;
                    }
                }
            }

            foreach (var zone in m_Registry.Zones)
            {
                if (m_SavedLevels.TryGetValue(zone.ZoneId, out float level))
                {
                    zone.RestoreInfection(level);
                }
            }

            return true;
        }

        [ContextMenu("Delete Save")]
        public void DeleteSave()
        {
            JsonSaveFile.Delete(m_FileName, this);
            m_SavedLevels.Clear();
        }

        [ContextMenu("Load")]
        private void LoadFromContextMenu()
        {
            Load();
        }
    }
}
