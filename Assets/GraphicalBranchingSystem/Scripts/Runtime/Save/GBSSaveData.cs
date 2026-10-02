using System;
using System.Collections.Generic;

namespace GBS.Save
{
    /// <summary>Прогресс одного графа.</summary>
    [Serializable]
    public class GBSGraphProgressData
    {
        public string GraphId;
        public string GraphName;
        public string CurrentNodeId;
        public bool IsCompleted;
        public List<string> LatchedNodes = new List<string>();
    }

    /// <summary>
    /// Полный игровой сейв сюжета: прогресс по всем графам + состояние сюжета (флаги и сигналы).
    /// </summary>
    [Serializable]
    public class GBSSaveData
    {
        public const int CurrentVersion = 2;

        public int Version = CurrentVersion;
        public List<GBSGraphProgressData> Graphs = new List<GBSGraphProgressData>();

        /// <summary>Состояние сюжета: ключи и значения (флаг - 0/1, сигнал - счётчик).</summary>
        public List<string> StateKeys = new List<string>();
        public List<int> StateValues = new List<int>();

        /// <summary>Версия 1: только булевы флаги blackboard. Читается для совместимости.</summary>
        public List<string> FlagKeys = new List<string>();
        public List<bool> FlagValues = new List<bool>();

        public GBSGraphProgressData FindGraph(string graphId)
        {
            for (int i = 0; i < Graphs.Count; i++)
            {
                if (Graphs[i] != null && Graphs[i].GraphId == graphId)
                {
                    return Graphs[i];
                }
            }

            return null;
        }

        public IEnumerable<KeyValuePair<string, int>> GetStateValues()
        {
            for (int i = 0; i < StateKeys.Count && i < StateValues.Count; i++)
            {
                yield return new KeyValuePair<string, int>(StateKeys[i], StateValues[i]);
            }

            for (int i = 0; i < FlagKeys.Count && i < FlagValues.Count; i++)
            {
                yield return new KeyValuePair<string, int>(FlagKeys[i], FlagValues[i] ? 1 : 0);
            }
        }
    }
}
