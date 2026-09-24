using GBS.Data;
using GBS.Utility;
using Game.Scripts.Story;
using UnityEditor.Experimental.GraphView;

namespace GBS.Elements
{
    /// <summary>
    /// Нода-условие: оборачивает StoryCondition SO в булев сигнал.
    /// Сработав, условие "защёлкивается" (остаётся истинным до сброса прогресса).
    /// </summary>
    public class GBSConditionNode : GBSNode
    {
        private StoryCondition m_Condition;

        public override GBSNodeType NodeType => GBSNodeType.Condition;

        protected override string DefaultName => "Condition";

        public override void Draw()
        {
            title = "CONDITION";

            var outPort = CreatePort(GBSPortId.Out, Direction.Output, Port.Capacity.Multi, typeof(bool), "Value");
            outputContainer.Add(outPort);

            var conditionField = GBSElementUtility.CreateObjectField(
                typeof(StoryCondition),
                m_Condition,
                callback => m_Condition = callback.newValue as StoryCondition);

            var container = GBSElementUtility.CreateColumn();
            container.style.minWidth = 0;
            container.Add(conditionField);

            extensionContainer.Add(container);

            RefreshExpandedState();
            RefreshPorts();
        }

        public override void LoadFrom(GBSNodeData data)
        {
            base.LoadFrom(data);

            if (data is GBSConditionNodeData conditionData)
            {
                m_Condition = conditionData.Condition;
            }
        }

        public override GBSNodeData Save()
        {
            var data = new GBSConditionNodeData
            {
                Condition = m_Condition
            };

            PopulateBaseData(data);

            return data;
        }
    }
}
