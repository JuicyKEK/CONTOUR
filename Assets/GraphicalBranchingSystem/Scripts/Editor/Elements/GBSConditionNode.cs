using GBS.Data;
using GBS.Steps;
using GBS.Utility;
using UnityEditor.Experimental.GraphView;

namespace GBS.Elements
{
    /// <summary>
    /// Нода-условие: превращает ожидание условия в булев сигнал.
    /// Сработав, условие "защёлкивается" (остаётся истинным до сброса прогресса).
    /// </summary>
    public class GBSConditionNode : GBSDataNode<GBSConditionNodeData>
    {
        public override GBSNodeType NodeType => GBSNodeType.Condition;
        protected override string DefaultName => "Condition";

        public override void Draw()
        {
            EnsureState();

            title = "CONDITION";

            var outPort = CreatePort(GBSPortId.Out, Direction.Output, Port.Capacity.Multi, typeof(bool), "Value");
            outputContainer.Add(outPort);

            var container = GBSElementUtility.CreateColumn();
            container.style.minWidth = 0;
            container.Add(GBSManagedReferenceUI.CreateField(
                SerializedState, NodePath + ".m_InlineCondition", typeof(GBSCondition), "Condition"));

            extensionContainer.Add(container);

            RefreshExpandedState();
            RefreshPorts();
        }

        protected override void OnStateCreated(GBSConditionNodeData data)
        {
            GBSLegacyConverter.UpgradeConditionNode(data);
        }
    }
}
