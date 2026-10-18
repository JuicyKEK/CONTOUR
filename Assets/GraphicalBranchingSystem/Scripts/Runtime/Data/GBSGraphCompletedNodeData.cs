using System;
using UnityEngine;

namespace GBS.Data
{
    /// <summary>
    /// Булев сигнал "указанный граф пройден до конца".
    /// Позволяет стартовать один граф только после завершения другого.
    /// </summary>
    [Serializable]
    public class GBSGraphCompletedNodeData : GBSNodeData
    {
        [SerializeField] private GBSGraphSO m_TargetGraph;

        public GBSGraphSO TargetGraph
        {
            get => m_TargetGraph;
            set => m_TargetGraph = value;
        }

        public override GBSNodeType NodeType => GBSNodeType.GraphCompleted;
    }
}

