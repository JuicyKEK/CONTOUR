using System;
using System.Collections.Generic;
using GBS.Events;
using GBS.Steps;
using UnityEngine;

namespace GBS.Data
{
    /// <summary>
    /// Финальная нода. Как только сюжет доходит до неё, выполняются её действия и граф
    /// считается завершённым (это состояние видно другим графам через ноду "Graph Completed").
    /// </summary>
    [Serializable]
    public class GBSEndNodeData : GBSNodeData
    {
        [SerializeReference] private List<GBSAction> m_OnCompleteActions = new List<GBSAction>();

        // До v3.0: SO-события GBSEvent (см. GBSStoryNodeData).
        [SerializeField] private List<GBSEvent> m_OnCompleteEvents = new List<GBSEvent>();

        [NonSerialized] private List<GBSAction> m_RuntimeActionsCache;

        public List<GBSAction> OnCompleteActions
        {
            get => m_OnCompleteActions ??= new List<GBSAction>();
            set => m_OnCompleteActions = value;
        }

        public List<GBSEvent> LegacyEvents => m_OnCompleteEvents ??= new List<GBSEvent>();

        public IReadOnlyList<GBSAction> GetActions()
        {
            if (LegacyEvents.Count == 0)
            {
                return OnCompleteActions;
            }

            if (m_RuntimeActionsCache == null)
            {
                m_RuntimeActionsCache = new List<GBSAction>();

                foreach (var legacyEvent in LegacyEvents)
                {
                    m_RuntimeActionsCache.Add(new LegacyGBSEventAsset(legacyEvent));
                }

                m_RuntimeActionsCache.AddRange(OnCompleteActions);
            }

            return m_RuntimeActionsCache;
        }

        public void ClearLegacyContent()
        {
            LegacyEvents.Clear();
            m_RuntimeActionsCache = null;
        }

        public override GBSNodeType NodeType => GBSNodeType.End;
    }
}
