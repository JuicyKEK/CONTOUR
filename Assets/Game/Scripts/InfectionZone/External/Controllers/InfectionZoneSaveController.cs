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
    ///
    /// Переходы между локациями: когда сцена с зоной выгружается, контроллер запоминает уровень зоны
    /// в памяти, а когда сцена загружается снова - зона получает этот уровень вместо стартового (зона,
    /// которой нет в памяти, но есть в файле, получает уровень из файла). В файл запомненные уровни
    /// попадают при следующем сохранении игры вместе с остальными механиками. Сам переход в файл ничего
    /// не пишет: иначе возврат к последнему сохранению (смерть) возвращал бы зоны к уровням, которых в
    /// момент сохранения ещё не было, - сюжет откатился бы, а заражение нет.
    /// </summary>
    [JDIMonoController]
    [SequenceParticipant(15)]
    public class InfectionZoneSaveController : MonoBehaviour, IInfectionZoneSaveService, ISaveParticipant, ISequence
    {
        [Tooltip("Имя JSON-файла сохранения зон (лежит в Application.persistentDataPath/Saves).")]
        [SerializeField] private string m_FileName = "infection_zones.json";
        [Tooltip("Загружать сохранение при старте сцены.")]
        [SerializeField] private bool m_LoadOnStart = true;
        [Tooltip("Писать в консоль, какой уровень зоны запомнен при выгрузке её сцены и какой восстановлен при загрузке.")]
        [SerializeField] private bool m_LogZoneLevels = true;

        [Inject] private IInfectionZoneRegistry m_Registry;

        // Последние известные уровни всех зон (в т.ч. тех, что сейчас не на сцене), ключ - ZoneId.
        private readonly Dictionary<string, float> m_SavedLevels = new();

        // Реестр, на события которого подписан контроллер (JuicyDI может переинжектить поле).
        private IInfectionZoneRegistry m_SubscribedRegistry;

        public string SavePath => JsonSaveFile.GetPath(m_FileName);
        public bool HasSave => JsonSaveFile.Exists(m_FileName);

        public void MethodInit()
        {
            // Зоны стартовых сцен уже зарегистрированы (их MethodInit идёт раньше) - им уровни выставит Load ниже.
            // Подписка - для зон, чьи сцены догрузятся и выгрузятся во время игры.
            if (m_Registry != null && m_SubscribedRegistry == null)
            {
                m_SubscribedRegistry = m_Registry;
                m_SubscribedRegistry.ZoneRegistered += OnZoneRegistered;
                m_SubscribedRegistry.ZoneUnregistering += OnZoneUnregistering;
            }

            if (m_LoadOnStart)
            {
                Load();
            }
        }

        private void OnDestroy()
        {
            if (m_SubscribedRegistry != null)
            {
                m_SubscribedRegistry.ZoneRegistered -= OnZoneRegistered;
                m_SubscribedRegistry.ZoneUnregistering -= OnZoneUnregistering;
                m_SubscribedRegistry = null;
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

        /// <summary>
        /// Сцена с зоной загрузилась: зона получает последний известный уровень вместо стартового.
        /// Вызывается из MethodInit зоны - до её MethodStart, где применился бы стартовый уровень.
        /// </summary>
        private void OnZoneRegistered(IInfectionZone zone)
        {
            if (!m_SavedLevels.TryGetValue(zone.ZoneId, out float level))
            {
                return;
            }

            zone.RestoreInfection(level);
            Log($"зона '{zone.ZoneId}' загрузилась - восстановлен уровень заражения {level:0.##}");
        }

        /// <summary>
        /// Сцена с зоной выгружается: уровень зоны запоминается до её следующей загрузки или сохранения игры.
        /// </summary>
        private void OnZoneUnregistering(IInfectionZone zone)
        {
            // Выгружается всё ядро вместе с этим контроллером (возврат к сохранению, выход в меню) - запоминать некуда.
            if (this == null || !gameObject.scene.isLoaded)
            {
                return;
            }

            float level = zone.InfectionLevel.CurrentValue;
            m_SavedLevels[zone.ZoneId] = level;
            Log($"зона '{zone.ZoneId}' выгружается - запомнен уровень заражения {level:0.##}");
        }

        private void Log(string message)
        {
            if (m_LogZoneLevels)
            {
                Debug.Log($"[InfectionZoneSaveController] {message}", this);
            }
        }
    }
}
