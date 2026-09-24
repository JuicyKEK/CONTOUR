using System;
using System.Collections.Generic;
using GBS.Events;
using UnityEngine;

namespace GBS.Data
{
    /// <summary>
    /// Финальная нода. Как только сюжет доходит до неё, граф считается
    /// завершённым (это состояние видно другим графам через ноду "Graph Completed").
    /// </summary>
    [Serializable]
    public class GBSEndNodeData : GBSNodeData
    {
        [SerializeField] private List<GBSEvent> m_OnCompleteEvents = new List<GBSEvent>();

        public List<GBSEvent> OnCompleteEvents
        {
            get => m_OnCompleteEvents ??= new List<GBSEvent>();
            set => m_OnCompleteEvents = value;
        }

        public override GBSNodeType NodeType => GBSNodeType.End;
    }
}


