using GBS.Data;
using GBS.Utility;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace GBS.Elements
{
    /// <summary>
    /// Нода булевой алгебры (AND/OR/NOT/NAND/NOR/XOR/XNOR).
    /// Количество входов меняется кнопками "+" и "-", поэтому в одну ноду
    /// можно свести сколько угодно условий.
    /// </summary>
    public class GBSLogicNode : GBSNode
    {
        private GBSLogicOperation m_Operation = GBSLogicOperation.And;
        private int m_InputCount = 2;

        private Label m_CountLabel;

        public override GBSNodeType NodeType => GBSNodeType.Logic;

        protected override string DefaultName => "Logic";

        public override void Draw()
        {
            UpdateTitle();

            var operationField = new EnumField(m_Operation);

            operationField.RegisterValueChangedCallback(callback =>
            {
                m_Operation = (GBSLogicOperation)callback.newValue;
                UpdateTitle();
            });

            extensionContainer.Add(operationField);

            var controlsRow = GBSElementUtility.CreateRow();

            controlsRow.Add(GBSElementUtility.CreateButton("-", RemoveInput));
            controlsRow.Add(GBSElementUtility.CreateButton("+", AddInput));

            m_CountLabel = new Label($"Inputs: {m_InputCount}");
            controlsRow.Add(m_CountLabel);

            extensionContainer.Add(controlsRow);

            var outPort = CreatePort(GBSPortId.Out, Direction.Output, Port.Capacity.Multi, typeof(bool), "Value");
            outputContainer.Add(outPort);

            RebuildInputPorts();

            RefreshExpandedState();
            RefreshPorts();
        }

        public override void LoadFrom(GBSNodeData data)
        {
            base.LoadFrom(data);

            if (data is GBSLogicNodeData logicData)
            {
                m_Operation = logicData.Operation;
                m_InputCount = logicData.InputCount;
            }
        }

        public override GBSNodeData Save()
        {
            var data = new GBSLogicNodeData
            {
                Operation = m_Operation,
                InputCount = m_InputCount
            };

            PopulateBaseData(data);

            return data;
        }

        private void AddInput()
        {
            if (m_InputCount >= GBSLogicNodeData.MaxInputCount)
            {
                return;
            }

            m_InputCount++;

            RebuildInputPorts();
            UpdateCountLabel();

            RefreshExpandedState();
            RefreshPorts();
        }

        private void RemoveInput()
        {
            if (m_InputCount <= GBSLogicNodeData.MinInputCount)
            {
                return;
            }

            m_InputCount--;

            RemovePort(GBSPortId.LogicInput(m_InputCount));
            UpdateCountLabel();

            RefreshExpandedState();
            RefreshPorts();
        }

        private void RebuildInputPorts()
        {
            for (int i = 0; i < m_InputCount; i++)
            {
                var portId = GBSPortId.LogicInput(i);

                if (GetPort(portId) != null)
                {
                    continue;
                }

                var port = CreatePort(portId, Direction.Input, Port.Capacity.Single, typeof(bool), $"In {i}");

                inputContainer.Add(port);
            }
        }

        private void UpdateCountLabel()
        {
            if (m_CountLabel != null)
            {
                m_CountLabel.text = $"Inputs: {m_InputCount}";
            }
        }

        private void UpdateTitle()
        {
            title = m_Operation.ToString().ToUpperInvariant();
            NodeName = m_Operation.ToString();
        }
    }
}
