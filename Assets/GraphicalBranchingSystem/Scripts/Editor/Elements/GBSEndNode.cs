using System.Collections.Generic;
using GBS.Data;
using GBS.Events;
using GBS.Utility;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace GBS.Elements
{
    /// <summary>
    /// Финальная нода. Как только сюжет доходит сюда, граф помечается пройденным
    /// (это можно использовать как условие старта другого графа).
    /// </summary>
    public class GBSEndNode : GBSNode
    {
        private List<GBSEvent> m_OnCompleteEvents = new List<GBSEvent>();

        private GBSObjectListField<GBSEvent> m_EventsList;

        public override GBSNodeType NodeType => GBSNodeType.End;

        protected override string DefaultName => "End";

        public override void Draw()
        {
            title = "END";

            var inPort = CreatePort(GBSPortId.In, Direction.Input, Port.Capacity.Multi, typeof(GBSFlow), "In");
            inputContainer.Add(inPort);

            m_EventsList = new GBSObjectListField<GBSEvent>("On Complete Events (GBSEvent)", m_OnCompleteEvents, "Add Event");

            extensionContainer.Add(m_EventsList.Root);

            expanded = true;

            RefreshExpandedState();
            RefreshPorts();
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            evt.menu.AppendAction("GBS/Add Event", _ => m_EventsList?.Add());
            evt.menu.AppendAction("GBS/Remove Last Event", _ => m_EventsList?.RemoveLast());

            base.BuildContextualMenu(evt);
        }

        public override void LoadFrom(GBSNodeData data)
        {
            base.LoadFrom(data);

            if (data is GBSEndNodeData endData)
            {
                m_OnCompleteEvents = new List<GBSEvent>(endData.OnCompleteEvents);
            }
        }

        public override GBSNodeData Save()
        {
            var data = new GBSEndNodeData
            {
                OnCompleteEvents = new List<GBSEvent>(m_OnCompleteEvents)
            };

            PopulateBaseData(data);

            return data;
        }
    }
}


