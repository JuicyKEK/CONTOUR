using System;
using System.Collections.Generic;
using GBS.Data;
using GBS.Steps;
using GBS.Utility;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace GBS.Elements
{
    /// <summary>
    /// Сюжетная нода.
    ///
    /// Порядок работы в рантайме:
    ///  1) при входе по очереди (с ожиданием) выполняются действия ноды;
    ///  2) затем нода ждёт условия переходов. Побеждает то, которое уже выполнено на момент
    ///     проверки или выполнится первым; при одновременной готовности - верхнее в списке;
    ///  3) сюжет уходит по ребру, выходящему ИЗ ЭТОГО перехода.
    ///
    /// Действия и условия - встроенные шаги графа (GBSAction / GBSCondition), выбираются из меню
    /// прямо в ноде; SO-ассеты на каждое действие не нужны. ВХОД у ноды ровно ОДИН (порт "In").
    /// </summary>
    public class GBSStoryNode : GBSDataNode<GBSStoryNodeData>
    {
        private readonly Dictionary<string, VisualElement> m_BranchRows = new Dictionary<string, VisualElement>();
        private readonly Dictionary<string, VisualElement> m_BranchConditions = new Dictionary<string, VisualElement>();

        private VisualElement m_TransitionsContainer;

        public override GBSNodeType NodeType => GBSNodeType.Story;
        protected override string DefaultName => "StoryNode";

        public override void Draw()
        {
            EnsureState();

            title = "STORY";
            titleContainer.Insert(0, CreateNameField());

            var inPort = CreatePort(GBSPortId.In, Direction.Input, Port.Capacity.Multi, typeof(GBSFlow), "In");
            inPort.tooltip = "Вход: ребро от предыдущей ноды";
            inputContainer.Add(inPort.AsFixed());

            DrawTransitionsContainer();
            DrawExtensionContainer();

            if (Data.Branches.Count == 0)
            {
                CreateBranchData("Next");
            }

            foreach (var branch in Data.Branches)
            {
                DrawBranchRow(branch);
            }

            RefreshBranchConditions();

            expanded = true;
            RefreshExpandedState();
            RefreshPorts();
        }

        protected override void OnStateCreated(GBSStoryNodeData data)
        {
            // Графы до v3.0: SO-ассеты действий/условий переезжают во встроенные шаги.
            GBSLegacyConverter.UpgradeStoryNode(data);

            foreach (var branch in data.Branches)
            {
                if (string.IsNullOrEmpty(branch.Id))
                {
                    branch.Id = Guid.NewGuid().ToString();
                }
            }
        }

        /// <summary>
        /// Секция переходов живёт в outputContainer и растянута на всю ширину,
        /// иначе выходные порты уезжают за правый край ноды.
        /// </summary>
        private void DrawTransitionsContainer()
        {
            var header = GBSElementUtility.CreateRow();

            var addButton = GBSElementUtility.CreateButton("Add Transition", AddTransition);
            addButton.style.flexGrow = 1;
            addButton.style.flexShrink = 1;

            var removeButton = GBSElementUtility.CreateDeleteButton(() =>
            {
                if (Data.Branches.Count == 0)
                {
                    return;
                }

                var last = Data.Branches[Data.Branches.Count - 1];
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

            // #output в GraphView по умолчанию имеет flex-shrink: 0 и
            // align-items: flex-end - из-за этого содержимое раздувало контейнер
            // и уезжало за правый край ноды.
            outputContainer.style.flexGrow = 1;
            outputContainer.style.flexShrink = 1;
            outputContainer.style.minWidth = 0;
            outputContainer.style.alignItems = Align.Stretch;
            outputContainer.Add(root);
        }

        private void DrawExtensionContainer()
        {
            var actionsFoldout = GBSElementUtility.CreateFoldout("On Enter Actions");
            actionsFoldout.Add(GBSManagedReferenceUI.CreateList(
                SerializedState, NodePath + ".m_Actions", typeof(GBSAction), "Add Action"));

            var customDataContainer = GBSElementUtility.CreateColumn();
            customDataContainer.AddToClassList("gbs-node_custom-data-container");
            customDataContainer.style.minWidth = 0;
            customDataContainer.Add(actionsFoldout);

            extensionContainer.Add(customDataContainer);
        }

        /// <summary>
        /// Дубли действий из ПКМ по ноде - на случай, если кнопки внутри ноды
        /// перекрыты другим элементом интерфейса.
        /// </summary>
        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            evt.menu.AppendAction("GBS/Add Transition", _ => AddTransition());
            evt.menu.AppendAction("GBS/Remove Last Transition", _ =>
            {
                if (Data.Branches.Count > 0)
                {
                    RemoveBranch(Data.Branches[Data.Branches.Count - 1]);
                }
            });

            base.BuildContextualMenu(evt);
        }

        private void AddTransition()
        {
            var branch = CreateBranchData("Transition " + (Data.Branches.Count + 1));
            DrawBranchRow(branch);
            RefreshBranchConditions();
            RefreshExpandedState();
            RefreshPorts();
        }

        private GBSBranchData CreateBranchData(string branchName)
        {
            ApplySerializedState();

            var branch = new GBSBranchData
            {
                Id = Guid.NewGuid().ToString(),
                BranchName = branchName
            };

            Data.Branches.Add(branch);
            SyncSerializedState();
            return branch;
        }

        /// <summary>
        /// Строка перехода состоит из двух рядов:
        ///  1) имя + кнопка удаления + выходной порт (порт всегда внутри ноды);
        ///  2) условие перехода (встроенное, выбирается из меню; None - переход сразу).
        /// </summary>
        private void DrawBranchRow(GBSBranchData branch)
        {
            var container = GBSElementUtility.CreateColumn();
            container.AddToClassList("gbs-node__transition");

            var headerRow = GBSElementUtility.CreateRow();

            var nameField = GBSElementUtility.CreateTextField(branch.BranchName, null, callback =>
            {
                ApplySerializedState();
                branch.BranchName = callback.newValue;
                SyncSerializedState();
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

            outPort.tooltip = "Ребро на следующую ноду для этого перехода";

            headerRow.Add(nameField);
            headerRow.Add(deleteButton);
            headerRow.Add(outPort.AsFixed());

            var conditionContainer = GBSElementUtility.CreateColumn();
            conditionContainer.style.minWidth = 0;

            container.Add(headerRow);
            container.Add(conditionContainer);

            m_TransitionsContainer.Add(container);
            m_BranchRows[branch.Id] = container;
            m_BranchConditions[branch.Id] = conditionContainer;
        }

        /// <summary>
        /// Условия переходов привязаны к SerializedProperty по индексу перехода - после добавления
        /// или удаления перехода индексы сдвигаются, поэтому поля условий пересоздаются.
        /// </summary>
        private void RefreshBranchConditions()
        {
            SyncSerializedState();

            for (int i = 0; i < Data.Branches.Count; i++)
            {
                var branch = Data.Branches[i];

                if (!m_BranchConditions.TryGetValue(branch.Id, out var conditionContainer))
                {
                    continue;
                }

                conditionContainer.Clear();
                conditionContainer.Add(GBSManagedReferenceUI.CreateField(
                    SerializedState,
                    $"{NodePath}.m_Branches.Array.data[{i}].m_InlineCondition",
                    typeof(GBSCondition),
                    "If"));
            }
        }

        private void RemoveBranch(GBSBranchData branch)
        {
            if (!Data.Branches.Contains(branch))
            {
                return;
            }

            ApplySerializedState();

            RemovePort(branch.Id);

            if (m_BranchRows.TryGetValue(branch.Id, out var row))
            {
                row.parent?.Remove(row);
                m_BranchRows.Remove(branch.Id);
            }

            m_BranchConditions.Remove(branch.Id);
            Data.Branches.Remove(branch);

            RefreshBranchConditions();
            RefreshExpandedState();
            RefreshPorts();
        }
    }
}
