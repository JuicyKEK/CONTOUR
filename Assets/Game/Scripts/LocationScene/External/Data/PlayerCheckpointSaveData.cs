using System;

namespace Game.Scripts.LocationScene.External.Data
{
    /// <summary>
    /// Сохраняемое местоположение игрока - то, что пишется в JSON-файл (через UnityEngine.JsonUtility):
    /// ключ локации и ключ чекпоинта на ней.
    /// </summary>
    [Serializable]
    public class PlayerCheckpointSaveData
    {
        public const int CurrentVersion = 1;

        /// <summary>
        /// Версия формата - пригодится для миграции, если структура сохранения изменится.
        /// </summary>
        public int Version = CurrentVersion;

        public string LocationId;
        public string CheckpointId;
    }
}
