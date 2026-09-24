using System.Collections.Generic;
using System.Linq;
using GBS.Data;
using GBS.Elements;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace GBS.Windows
{
    /// <summary>
    /// Полотно графа GBS: создание нод, совместимость портов, удаление элементов.
    /// </summary>
    public class GBSGraphView : GraphView
    {
        private const string GraphStylePath = "Assets/GraphicalBranchingSystem/Scripts/Editor Default Resources/GBSView/GraphViewStyles.uss";
        private const string NodeStylePath = "Assets/GraphicalBranchingSystem/Scripts/Editor Default Resources/GBSView/GBSNodeStyles.uss";

        private readonly GBSEditorWindow m_EditorWindow;

        private GBSSearchWindow m_SearchWindow;

        public GBSGraphView(GBSEditorWindow editorWindow)
        {
            m_EditorWindow = editorWindow;

            AddManipulators();
            AddSearchWindow();
            AddGridBackground();

            OnElementsDeleted();

            AddStyles();
        }

        public GBSEditorWindow Window => m_EditorWindow;

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            var compatiblePorts = new List<Port>();

            ports.ForEach(port =>
            {
                if (startPort == port)
                {
                    return;
                }

                if (startPort.node == port.node)
                {
                    return;
                }

                if (startPort.direction == port.direction)
                {
                    return;
                }

                if (startPort.portType != port.portType)
                {
                    return;
                }

                compatiblePorts.Add(port);
            });

            return compatiblePorts;
        }

        public GBSNode CreateNode(GBSNodeType nodeType, Vector2 position, GBSNodeData data = null)
        {
            GBSNode node = nodeType switch
            {
                GBSNodeType.Start => new GBSStartNode(),
                GBSNodeType.Story => new GBSStoryNode(),
                GBSNodeType.End => new GBSEndNode(),
                GBSNodeType.Condition => new GBSConditionNode(),
                GBSNodeType.Logic => new GBSLogicNode(),
                GBSNodeType.GraphCompleted => new GBSGraphCompletedNode(),
                _ => null
            };

            if (node == null)
            {
                Debug.LogError($"[GBS] Неизвестный тип ноды: {nodeType}");
                return null;
            }

            node.Init(this, position);

            if (data != null)
            {
                node.LoadFrom(data);
            }

            node.Draw();

            AddElement(node);

            return node;
        }

        public IEnumerable<GBSNode> GetNodes()
        {
            return nodes.ToList().OfType<GBSNode>();
        }

        public void ClearGraph()
        {
            DeleteElements(graphElements.ToList());
        }

        public Vector2 GetLocalMousePosition(Vector2 mousePosition, bool isSearchWindow = false)
        {
            var worldMousePosition = mousePosition;

            if (isSearchWindow)
            {
                worldMousePosition -= m_EditorWindow.position.position;
            }

            return contentViewContainer.WorldToLocal(worldMousePosition);
        }

        private void AddSearchWindow()
        {
            if (m_SearchWindow == null)
            {
                m_SearchWindow = ScriptableObject.CreateInstance<GBSSearchWindow>();
                m_SearchWindow.Init(this);
            }

            nodeCreationRequest = context =>
                SearchWindow.Open(new SearchWindowContext(context.screenMousePosition), m_SearchWindow);
        }

        private void AddGridBackground()
        {
            var gridBackground = new GridBackground();

            gridBackground.StretchToParentSize();

            Insert(0, gridBackground);
        }

        private void AddStyles()
        {
            AddStyleSheet(GraphStylePath);
            AddStyleSheet(NodeStylePath);
        }

        private void AddStyleSheet(string path)
        {
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(path);

            if (styleSheet != null)
            {
                styleSheets.Add(styleSheet);
            }
        }

        private void AddManipulators()
        {
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);

            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            this.AddManipulator(CreateNodeContextualMenu("Add Node/Start", GBSNodeType.Start));
            this.AddManipulator(CreateNodeContextualMenu("Add Node/Story", GBSNodeType.Story));
            this.AddManipulator(CreateNodeContextualMenu("Add Node/End", GBSNodeType.End));
            this.AddManipulator(CreateNodeContextualMenu("Add Node/Condition", GBSNodeType.Condition));
            this.AddManipulator(CreateNodeContextualMenu("Add Node/Logic", GBSNodeType.Logic));
            this.AddManipulator(CreateNodeContextualMenu("Add Node/Graph Completed", GBSNodeType.GraphCompleted));
        }

        private IManipulator CreateNodeContextualMenu(string actionTitle, GBSNodeType nodeType)
        {
            return new ContextualMenuManipulator(menuEvent => menuEvent.menu.AppendAction(
                actionTitle,
                actionEvent => CreateNode(nodeType, GetLocalMousePosition(actionEvent.eventInfo.localMousePosition))));
        }

        private void OnElementsDeleted()
        {
            deleteSelection = (operationName, askUser) =>
            {
                var nodesToDelete = new List<GBSNode>();
                var edgesToDelete = new List<Edge>();

                foreach (var element in selection.OfType<GraphElement>())
                {
                    switch (element)
                    {
                        case GBSNode node:
                            nodesToDelete.Add(node);
                            break;

                        case Edge edge:
                            edgesToDelete.Add(edge);
                            break;
                    }
                }

                DeleteElements(edgesToDelete);

                foreach (var node in nodesToDelete)
                {
                    node.DisconnectAllPorts();
                    RemoveElement(node);
                }
            };
        }
    }
}

