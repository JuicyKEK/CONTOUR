using GBS.Data;
using GBS.Steps;
using GBS.Utility;
using UnityEditor.Experimental.GraphView;

namespace GBS.Elements
{
    /// <summary>
    /// Финальная нода. Как только сюжет доходит сюда, выполняются её действия и граф помечается
    /// пройденным (это можно использовать как условие старта другого графа).
    /// </summary>
    public class GBSEndNode : GBSDataNode<GBSEndNodeData>
    {
        public override GBSNodeType NodeType => GBSNodeType.End;
        protected override string DefaultName => "End";

        public override void Draw()
        {
            EnsureState();

            title = "END";

            var inPort = CreatePort(GBSPortId.In, Direction.Input, Port.Capacity.Multi, typeof(GBSFlow), "In");
            inputContainer.Add(inPort);

            var actionsFoldout = GBSElementUtility.CreateFoldout("On Complete Actions");
            actionsFoldout.Add(GBSManagedReferenceUI.CreateList(
                SerializedState, NodePath + ".m_OnCompleteActions", typeof(GBSAction), "Add Action"));

            extensionContainer.Add(actionsFoldout);

            expanded = true;
            RefreshExpandedState();
            RefreshPorts();
        }

        protected override void OnStateCreated(GBSEndNodeData data)
        {
            GBSLegacyConverter.UpgradeEndNode(data);
        }
    }
}
