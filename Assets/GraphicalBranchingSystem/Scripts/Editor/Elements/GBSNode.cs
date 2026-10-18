using System;
using System.Collections.Generic;
using System.Linq;
using GBS.Data;
using GBS.Windows;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace GBS.Elements
{
    /// <summary>
    /// Базовая нода редактора GBS. Все порты регистрируются по строковому Id,
    /// потому что связи сохраняются как пара (NodeId, PortId).
    /// </summary>
    public abstract class GBSNode : Node
    {
        private const string NodeStylePath = "Assets/GraphicalBranchingSystem/Scripts/Editor Default Resources/GBSView/GBSNodeStyles.uss";

        /// <summary>Фиксированная ширина любой ноды GBS в пикселях.</summary>
        public const float NodeWidth = 360f;
        private readonly Dictionary<string, Port> m_PortsById = new Dictionary<string, Port>();

        public string ID { get; set; }
        public string NodeName { get; set; }
        public GBSGraphView GraphView { get; private set; }

        public IReadOnlyDictionary<string, Port> PortsById => m_PortsById;

        public abstract GBSNodeType NodeType { get; }

        protected virtual string DefaultName => "Node";

        public virtual void Init(GBSGraphView graphView, Vector2 position)
        {
            ID = Guid.NewGuid().ToString();
            NodeName = DefaultName;
            GraphView = graphView;

            SetPosition(new Rect(position, Vector2.zero));

            mainContainer.AddToClassList("ds-node__main-container");
            extensionContainer.AddToClassList("gbs-node_extension-container");

            // Нода имеет ФИКСИРОВАННУЮ ширину, а весь контент внутри обязан
            // сжиматься. Иначе контейнер #output (у него flex-shrink: 0 и
            // align-items: flex-end) раздувается по контенту, и кнопки удаления
            // вместе с выходными портами уезжают за правую границу ноды.
            AddToClassList("gbs-node");

            style.width = NodeWidth;
            style.minWidth = NodeWidth;
            style.maxWidth = NodeWidth;

            mainContainer.style.flexShrink = 1;
            mainContainer.style.minWidth = 0;

            extensionContainer.style.flexShrink = 1;
            extensionContainer.style.minWidth = 0;
            extensionContainer.style.alignItems = Align.Stretch;
            // #input в GraphView по умолчанию имеет flex: 1 0 auto и забирает
            // половину ширины ноды. Входной порт нужен только для приёма ребра,
            // поэтому сжимаем его до ширины контента и всё место отдаём #output.
            inputContainer.style.flexGrow = 0;
            inputContainer.style.flexShrink = 0;
            inputContainer.style.minWidth = 0;
            inputContainer.style.width = StyleKeyword.Auto;
            outputContainer.style.flexGrow = 1;
            outputContainer.style.flexShrink = 1;
            outputContainer.style.minWidth = 0;

            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(NodeStylePath);

            if (styleSheet != null)
            {
                styleSheets.Add(styleSheet);
            }

            // Нажатия в текстовых полях ноды не должны доходить до горячих клавиш GraphView
            // (пробел - окно создания ноды, A/O/[ ] - перемещение камеры).
            RegisterCallback<KeyDownEvent>(StopFromTextFields);

            // То же для команд: Ctrl+C / Ctrl+V / Ctrl+D в текстовом поле работают с текстом,
            // а не копируют/вставляют ноды.
            RegisterCallback<ValidateCommandEvent>(StopFromTextFields);
            RegisterCallback<ExecuteCommandEvent>(StopFromTextFields);
        }

        private void StopFromTextFields(EventBase evt)
        {
            if (IsInsideTextField(evt.target as VisualElement))
            {
                evt.StopPropagation();
            }
        }

        private bool IsInsideTextField(VisualElement target)
        {
            for (var element = target; element != null && element != this; element = element.parent)
            {
                // Класс есть у всех текстовых полей: TextField, FloatField, IntegerField...
                if (element.ClassListContains(TextField.ussClassName) || element.ClassListContains("unity-base-text-field"))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Строит UI ноды. Вызывается после LoadFrom.</summary>
        public abstract void Draw();

        /// <summary>Сериализует ноду в данные графа.</summary>
        public abstract GBSNodeData Save();

        /// <summary>Освобождает ресурсы редактора ноды (вызывается при удалении ноды / очистке окна).</summary>
        public virtual void Dispose()
        {
        }

        /// <summary>Заполняет ноду из сохранённых данных (до Draw).</summary>
        public virtual void LoadFrom(GBSNodeData data)
        {
            if (data == null)
            {
                return;
            }

            ID = data.Id;
            NodeName = string.IsNullOrEmpty(data.NodeName) ? DefaultName : data.NodeName;

            SetPosition(new Rect(data.Position, Vector2.zero));
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            evt.menu.AppendAction("Disconnect All Ports", actionEvent => DisconnectAllPorts());

            base.BuildContextualMenu(evt);
        }

        public Port GetPort(string portId)
        {
            return m_PortsById.TryGetValue(portId, out var port) ? port : null;
        }

        public string FindPortId(Port port)
        {
            foreach (var pair in m_PortsById)
            {
                if (pair.Value == port)
                {
                    return pair.Key;
                }
            }

            return null;
        }

        public void DisconnectAllPorts()
        {
            foreach (var port in m_PortsById.Values.ToList())
            {
                DisconnectPort(port);
            }
        }

        protected Port CreatePort(string portId, Direction direction, Port.Capacity capacity, Type portType, string portName)
        {
            var port = InstantiatePort(Orientation.Horizontal, direction, capacity, portType);

            port.portName = portName ?? string.Empty;

            m_PortsById[portId] = port;

            return port;
        }

        protected void RemovePort(string portId)
        {
            if (!m_PortsById.TryGetValue(portId, out var port))
            {
                return;
            }

            DisconnectPort(port);

            port.parent?.Remove(port);
            m_PortsById.Remove(portId);
        }

        protected void PopulateBaseData(GBSNodeData data)
        {
            data.Id = ID;
            data.NodeName = NodeName;
            data.Position = GetPosition().position;
        }

        protected TextField CreateNameField()
        {
            var nameField = Utility.GBSElementUtility.CreateTextField(NodeName, null, callback =>
            {
                NodeName = callback.newValue;
            });

            nameField.AddToClassList("ds-node__text-field");
            nameField.AddToClassList("ds-node__filename-text-field");
            nameField.AddToClassList("ds-node__text-field__hidden");

            return nameField;
        }

        private void DisconnectPort(Port port)
        {
            if (port == null || !port.connected)
            {
                return;
            }

            GraphView.DeleteElements(port.connections.ToList());
        }
    }
}
