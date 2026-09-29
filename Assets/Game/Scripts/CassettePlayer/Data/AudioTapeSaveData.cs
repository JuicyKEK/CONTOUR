using System;
using System.Collections.Generic;

namespace Game.Scripts.Instructions.Data
{
    /// <summary>
    /// Сохраняемое состояние кассет - то, что пишется в JSON-файл (через <see cref="UnityEngine.JsonUtility"/>).
    /// Хранятся только Id кассет, которые игрок уже получил.
    /// </summary>
    [Serializable]
    public class AudioTapeSaveData
    {
        public const int CurrentVersion = 1;

        /// <summary>
        /// Версия формата - пригодится для миграции, если структура сохранения изменится.
        /// </summary>
        public int Version = CurrentVersion;

        public List<string> FoundTapeIds = new();
    }
}
