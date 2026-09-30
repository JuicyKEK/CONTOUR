using System;
using System.Collections.Generic;

namespace Game.Scripts.Instructions.Data
{
    /// <summary>
    /// Сохраняемое состояние кассет - то, что пишется в JSON-файл (через <see cref="UnityEngine.JsonUtility"/>).
    /// Хранятся только Id кассет: полученных игроком, уже прослушанных (без метки "НОВОЕ")
    /// и тех, задачи из которых уже выполнены.
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
        public List<string> ListenedTapeIds = new();
        public List<string> CompletedTapeIds = new();
    }
}
