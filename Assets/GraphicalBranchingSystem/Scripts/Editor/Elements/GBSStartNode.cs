using GBS.Data;
using GBS.Utility;
using UnityEditor.Experimental.GraphView;

namespace GBS.Elements
{
    /// <summary>
    /// Стартовая нода графа. К порту "Start If" можно подключить булево выражение
    /// (например ноду "Graph Completed") - тогда граф стартует только когда оно истинно.
    /// Если порт свободен, граф стартует сразу при запуске GBSStarter.
    /// </summary>
    public class GBSStartNode : GBSNode
    {
        public override GBSNodeType NodeType => GBSNodeType.Start;

        protected override string DefaultName => "Start";

        public override void Draw()
        {
            title = "START";

            var gatePort = CreatePort(GBSPortId.Gate, Direction.Input, Port.Capacity.Single, typeof(bool), "Start If");
            inputContainer.Add(gatePort);

            var outPort = CreatePort(GBSPortId.Out, Direction.Output, Port.Capacity.Single, typeof(GBSFlow), "Out");
            outputContainer.Add(outPort);

            RefreshExpandedState();
            RefreshPorts();
        }

        public override GBSNodeData Save()
        {
            var data = new GBSStartNodeData();

            PopulateBaseData(data);

            return data;
        }
    }
}
