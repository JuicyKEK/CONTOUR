using System;
using UnityEngine;

namespace GBS.Data
{
    /// <summary>
    /// Связь между портами двух нод. From - всегда выходной порт, To - входной.
    /// </summary>
    [Serializable]
    public class GBSEdgeData
    {
        [SerializeField] private string m_FromNodeId;
        [SerializeField] private string m_FromPortId;
        [SerializeField] private string m_ToNodeId;
        [SerializeField] private string m_ToPortId;

        public string FromNodeId
        {
            get => m_FromNodeId;
            set => m_FromNodeId = value;
        }

        public string FromPortId
        {
            get => m_FromPortId;
            set => m_FromPortId = value;
        }

        public string ToNodeId
        {
            get => m_ToNodeId;
            set => m_ToNodeId = value;
        }

        public string ToPortId
        {
            get => m_ToPortId;
            set => m_ToPortId = value;
        }
    }
}

