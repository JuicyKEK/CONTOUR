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
    /// Полный игровой сейв сюжета: прогресс по всем графам + флаги blackboard.
    /// </summary>
    [Serializable]
    public class GBSSaveData
    {
        public int Version = 1;
        public List<GBSGraphProgressData> Graphs = new List<GBSGraphProgressData>();
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
    }
}

