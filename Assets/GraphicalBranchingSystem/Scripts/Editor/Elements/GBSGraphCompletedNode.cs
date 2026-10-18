using GBS.Data;
using GBS.Utility;
using UnityEditor.Experimental.GraphView;

namespace GBS.Elements
{
    /// <summary>
    /// Булев сигнал "указанный граф пройден до конца".
    /// Используется двумя способами:
    ///  1) как условие: порт "Value" тянется в "Start If" стартовой ноды
    ///     (у переходов сюжетной ноды булевых входов больше нет);
    ///  2) как ожидание внутри потока: flow-вход "In" -> нода ждёт завершения
    ///     указанного графа -> сюжет уходит дальше по flow-выходу "Out".
    /// </summary>
    public class GBSGraphCompletedNode : GBSNode
    {
        private GBSGraphSO m_TargetGraph;

        public override GBSNodeType NodeType => GBSNodeType.GraphCompleted;

        protected override string DefaultName => "Graph Completed";

        public override void Draw()
        {
            title = "GRAPH COMPLETED";

            var flowInPort = CreatePort(GBSPortId.In, Direction.Input, Port.Capacity.Multi, typeof(GBSFlow), "In");
            inputContainer.Add(flowInPort);

            var flowOutPort = CreatePort(GBSPortId.FlowOut, Direction.Output, Port.Capacity.Single, typeof(GBSFlow), "Out");
            outputContainer.Add(flowOutPort);

            var outPort = CreatePort(GBSPortId.Out, Direction.Output, Port.Capacity.Multi, typeof(bool), "Value");
            outputContainer.Add(outPort);

            var graphField = GBSElementUtility.CreateObjectField(
                typeof(GBSGraphSO),
                m_TargetGraph,
                callback => m_TargetGraph = callback.newValue as GBSGraphSO);

            var container = GBSElementUtility.CreateColumn();
            container.style.minWidth = 0;
            container.Add(graphField);

            extensionContainer.Add(container);

            expanded = true;

            RefreshExpandedState();
            RefreshPorts();
        }

        public override void LoadFrom(GBSNodeData data)
        {
            base.LoadFrom(data);

            if (data is GBSGraphCompletedNodeData graphData)
            {
                m_TargetGraph = graphData.TargetGraph;
            }
        }

        public override GBSNodeData Save()
        {
            var data = new GBSGraphCompletedNodeData
            {
                TargetGraph = m_TargetGraph
            };

            PopulateBaseData(data);

            return data;
        }
    }
}

