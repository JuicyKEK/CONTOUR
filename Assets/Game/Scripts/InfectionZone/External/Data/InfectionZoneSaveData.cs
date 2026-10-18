using System;
using System.Collections.Generic;

namespace Game.Scripts.InfectionZone.External.Data
{
    /// <summary>
    /// Сохраняемое состояние зон заражения - то, что пишется в JSON-файл (через UnityEngine.JsonUtility).
    /// </summary>
    [Serializable]
    public class InfectionZoneSaveData
    {
        public const int CurrentVersion = 1;

        /// <summary>
        /// Версия формата - пригодится для миграции, если структура сохранения изменится.
        /// </summary>
        public int Version = CurrentVersion;

        public List<InfectionZoneSaveEntry> Zones = new();
    }

    /// <summary>
    /// Уровень заражения одной зоны (JsonUtility не умеет сериализовать Dictionary, поэтому - список пар).
    /// </summary>
    [Serializable]
    public class InfectionZoneSaveEntry
    {
        public string ZoneId;
        public float InfectionLevel;

        public InfectionZoneSaveEntry()
        {
        }

        public InfectionZoneSaveEntry(string zoneId, float infectionLevel)
        {
            ZoneId = zoneId;
            InfectionLevel = infectionLevel;
        }
    }
}
