using System;
using System.Collections.Generic;

namespace Game.Scripts.LocationState.External.Data
{
    /// <summary>
    /// Сохраняемое состояние объектов локаций - то, что пишется в JSON-файл (через <see cref="UnityEngine.JsonUtility"/>).
    /// Каждая запись - ключ объекта (путь сцены + ключ объекта в сцене) и его состояние строкой (JSON самого объекта).
    /// </summary>
    [Serializable]
    public class LocationStateSaveData
    {
        public const int CurrentVersion = 1;

        /// <summary>
        /// Версия формата - пригодится для миграции, если структура сохранения изменится.
        /// </summary>
        public int Version = CurrentVersion;

        public List<LocationStateSaveEntry> Objects = new();
    }

    [Serializable]
    public class LocationStateSaveEntry
    {
        public string Key;
        public string State;

        public LocationStateSaveEntry()
        {
        }

        public LocationStateSaveEntry(string key, string state)
        {
            Key = key;
            State = state;
        }
    }
}
