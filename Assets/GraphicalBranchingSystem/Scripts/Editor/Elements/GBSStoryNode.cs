using System;
using System.Collections.Generic;
using GBS.Data;
using GBS.Events;
using GBS.Utility;
using Game.Scripts.Story;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
namespace GBS.Elements
{
    /// <summary>
    /// Сюжетная нода.
    ///
    /// Порядок работы в рантайме:
    ///  1) при входе поднимаются GBSEvent'ы и последовательно выполняются StoryAction'ы;
    ///  2) затем нода начинает проверять свои условия перехода. Побеждает то,
    ///     которое уже выполнено на момент проверки или выполнится первым;
    ///  3) сюжет уходит по ребру, выходящему ИЗ ЭТОГО условия (а не по ссылке на SO).
    ///
    /// ВХОД у ноды ровно ОДИН (порт "In"). У переходов булевых входов нет:
    /// условие задаётся полем Condition (StoryCondition), а результат уводит
    /// поток по собственному выходному порту строки перехода.
    /// </summary>
    public class GBSStoryNode : GBSNode
    {
        private readonly List<GBSBranchData> m_Branches = new List<GBSBranchData>();
        private readonly Dictionary<string, VisualElement> m_BranchRows = new Dictionary<string, VisualElement>();
        private List<StoryAction> m_Actions = new List<StoryAction>();
        private List<GBSEvent> m_Events = new List<GBSEvent>();
        private GBSObjectListField<StoryAction> m_ActionsList;
        private GBSObjectListField<GBSEvent> m_EventsList;
        private VisualElement m_TransitionsContainer;
        public override GBSNodeType NodeType => GBSNodeType.Story;
        protected override string DefaultName => "StoryNode";
        public override void Draw()
        {
            title = "STORY";
            titleContainer.Insert(0, CreateNameField());
            var inPort = CreatePort(GBSPortId.In, Direction.Input, Port.Capacity.Multi, typeof(GBSFlow), "In");
            inPort.tooltip = "Вход: ребро от предыдущей ноды";
            inputContainer.Add(inPort.AsFixed());
            DrawTransitionsContainer();
            DrawExtensionContainer();
            if (m_Branches.Count == 0)
            {
                CreateBranchData("Next");
            }
            for (int i = 0; i < m_Branches.Count; i++)
            {
                DrawBranchRow(m_Branches[i]);
            }
            expanded = true;
            RefreshExpandedState();
            RefreshPorts();
        }
        public override void LoadFrom(GBSNodeData data)
        {
            base.LoadFrom(data);
            if (data is not GBSStoryNodeData storyData)
            {
                return;
            }
            m_Actions = new List<StoryAction>(storyData.OnEnterActions);
            m_Events = new List<GBSEvent>(storyData.OnEnterEvents);
            m_Branches.Clear();
            for (int i = 0; i < storyData.Branches.Count; i++)
            {
                var source = storyData.Branches[i];
                m_Branches.Add(new GBSBranchData
                {
                    Id = string.IsNullOrEmpty(source.Id) ? Guid.NewGuid().ToString() : source.Id,
                    BranchName = source.BranchName,
                    Condition = source.Condition
                });
            }
        }
        public override GBSNodeData Save()
        {
            var data = new GBSStoryNodeData
            {
                OnEnterActions = new List<StoryAction>(m_Actions),
                OnEnterEvents = new List<GBSEvent>(m_Events),
                Branches = new List<GBSBranchData>(m_Branches)
            };
            PopulateBaseData(data);
            return data;
        }
        /// <summary>
        /// Секция переходов живёт в outputContainer и растянута на всю ширину,
        /// иначе выходные порты уезжают за правый край ноды.
        /// </summary>
        private void DrawTransitionsContainer()
        {
            var header = GBSElementUtility.CreateRow();
            var addButton = GBSElementUtility.CreateButton("Add Transition", () =>
            {
                DrawBranchRow(CreateBranchData("Transition " + (m_Branches.Count + 1)));
                RefreshExpandedState();
                RefreshPorts();
            });
            addButton.style.flexGrow = 1;
            addButton.style.flexShrink = 1;
            var removeButton = GBSElementUtility.CreateDeleteButton(() =>
            {
                if (m_Branches.Count == 0)
                {
                    return;
                }
                var last = m_Branches[m_Branches.Count - 1];
                schedule.Execute(() => RemoveBranch(last));
            }, "Удалить последний переход");
            header.Add(addButton);
            header.Add(removeButton);
            m_TransitionsContainer = GBSElementUtility.CreateColumn();
            m_TransitionsContainer.style.minWidth = 0;
            var root = GBSElementUtility.CreateColumn();
            root.style.minWidth = 0;
            root.Add(header);
            root.Add(m_TransitionsContainer);
            outputContainer.style.flexGrow = 1;
            // #output в GraphView по умолчанию имеет flex-shrink: 0 и
            // align-items: flex-end - из-за этого содержимое раздувало контейнер
            // и уезжало за правый край ноды.
            outputContainer.style.flexShrink = 1;
            outputContainer.style.minWidth = 0;
            outputContainer.style.alignItems = Align.Stretch;
            outputContainer.Add(root);
        }
        private void DrawExtensionContainer()
        {
            m_EventsList = new GBSObjectListField<GBSEvent>("On Enter Events (GBSEvent)", m_Events, "Add Event", true);
            m_ActionsList = new GBSObjectListField<StoryAction>("On Enter Actions (StoryAction)", m_Actions, "Add Action");
            var customDataContainer = GBSElementUtility.CreateColumn();
            customDataContainer.AddToClassList("gbs-node_custom-data-container");
            customDataContainer.style.minWidth = 0;
            customDataContainer.Add(m_EventsList.Root);
            customDataContainer.Add(m_ActionsList.Root);
            extensionContainer.Add(customDataContainer);
        }
        /// <summary>
        /// Дубли действий из ПКМ по ноде - на случай, если кнопки внутри ноды
        /// перекрыты другим элементом интерфейса.
        /// </summary>
        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            evt.menu.AppendAction("GBS/Add Transition", _ =>
            {
                DrawBranchRow(CreateBranchData("Transition " + (m_Branches.Count + 1)));
                RefreshExpandedState();
                RefreshPorts();
            });
            evt.menu.AppendAction("GBS/Remove Last Transition", _ =>
            {
                if (m_Branches.Count > 0)
                {
                    RemoveBranch(m_Branches[m_Branches.Count - 1]);
                }
            });
            evt.menu.AppendAction("GBS/Add Action", _ => m_ActionsList?.Add());
            evt.menu.AppendAction("GBS/Remove Last Action", _ => m_ActionsList?.RemoveLast());
            evt.menu.AppendAction("GBS/Add Event", _ => m_EventsList?.Add());
            evt.menu.AppendAction("GBS/Remove Last Event", _ => m_EventsList?.RemoveLast());
            base.BuildContextualMenu(evt);
        }
        private GBSBranchData CreateBranchData(string branchName)
        {
            var branch = new GBSBranchData
            {
                Id = Guid.NewGuid().ToString(),
                BranchName = branchName
            };
            m_Branches.Add(branch);
            return branch;
        }
        /// <summary>
        /// Строка перехода состоит из двух рядов:
        ///  1) имя + кнопка удаления + выходной порт (порт всегда внутри ноды);
        ///  2) поле StoryCondition во всю ширину, чтобы "кружок" выбора ассета был виден.
        /// </summary>
        private void DrawBranchRow(GBSBranchData branch)
        {
            var container = GBSElementUtility.CreateColumn();
            container.AddToClassList("gbs-node__transition");
            var headerRow = GBSElementUtility.CreateRow();
            var nameField = GBSElementUtility.CreateTextField(branch.BranchName, null, callback =>
            {
                branch.BranchName = callback.newValue;
            });
            nameField.AddToClassList("ds-node__text-field");
            nameField.AddToClassList("ds-node__choice-text-field");
            nameField.AddToClassList("ds-node__text-field__hidden");
            nameField.style.flexGrow = 1;
            nameField.style.flexShrink = 1;
            nameField.style.minWidth = 0;
            var deleteButton = GBSElementUtility.CreateDeleteButton(
                () => schedule.Execute(() => RemoveBranch(branch)), "Удалить переход");
            var outPort = CreatePort(
                branch.Id,
                Direction.Output,
                Port.Capacity.Single,
                typeof(GBSFlow),
                string.Empty);
            outPort.tooltip = "Ребро на следующую ноду для этого условия";
            headerRow.Add(nameField);
            headerRow.Add(deleteButton);
            headerRow.Add(outPort.AsFixed());
            var conditionRow = GBSElementUtility.CreateRow();
            // Подписи слева нет намеренно - вся ширина строки отдана под ассет условия.
            var conditionField = GBSElementUtility.CreateObjectField(
                typeof(StoryCondition),
                branch.Condition,
                callback => branch.Condition = callback.newValue as StoryCondition);

            conditionField.tooltip = "Условие перехода (StoryCondition). Пусто - переход срабатывает сразу.";

            conditionRow.Add(conditionField);
            container.Add(headerRow);
            container.Add(conditionRow);
            m_TransitionsContainer.Add(container);
            m_BranchRows[branch.Id] = container;
        }
        private void RemoveBranch(GBSBranchData branch)
        {
            if (!m_Branches.Contains(branch))
            {
                return;
            }
            RemovePort(branch.Id);
            if (m_BranchRows.TryGetValue(branch.Id, out var row))
            {
                if (row.parent != null)
                {
                    row.parent.Remove(row);
                }
                m_BranchRows.Remove(branch.Id);
            }
            m_Branches.Remove(branch);
            RefreshExpandedState();
            RefreshPorts();
        }
    }
}
