using System.Collections.Generic;
using System.Linq;
using GBS.Data;
using GBS.Elements;
using GBS.Utility;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace GBS.Windows
{
    /// <summary>
    /// Полотно графа GBS: создание нод, совместимость портов, удаление элементов,
    /// Copy / Cut / Paste / Duplicate (Ctrl+C / Ctrl+X / Ctrl+V / Ctrl+D и меню ПКМ).
    /// </summary>
    public class GBSGraphView : GraphView
    {
        private const string GraphStylePath = "Assets/GraphicalBranchingSystem/Scripts/Editor Default Resources/GBSView/GraphViewStyles.uss";
        private const string NodeStylePath = "Assets/GraphicalBranchingSystem/Scripts/Editor Default Resources/GBSView/GBSNodeStyles.uss";

        private const string DuplicateOperationName = "Duplicate";
        private static readonly Vector2 DuplicateOffset = new Vector2(40f, 40f);

        private readonly GBSEditorWindow m_EditorWindow;

        private GBSSearchWindow m_SearchWindow;

        // Последняя позиция курсора над полотном (мировые координаты) - туда вставляются ноды.
        private Vector2 m_MouseWorldPosition;
        private bool m_HasMousePosition;

        public GBSGraphView(GBSEditorWindow editorWindow)
        {
            m_EditorWindow = editorWindow;

            AddManipulators();
            AddSearchWindow();
            AddGridBackground();

            OnElementsDeleted();
            AddCopyPaste();

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
            DisposeNodes();
            DeleteElements(graphElements.ToList());
        }

        /// <summary>Освобождает ресурсы редактора всех нод (временные контейнеры данных).</summary>
        public void DisposeNodes()
        {
            foreach (var node in GetNodes())
            {
                node.Dispose();
            }
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

        private void AddCopyPaste()
        {
            serializeGraphElements = elements => GBSClipboard.Serialize(elements.OfType<GBSNode>(), edges.ToList());
            canPasteSerializedData = GBSClipboard.CanPaste;
            unserializeAndPaste = PasteNodes;

            RegisterCallback<MouseMoveEvent>(evt =>
            {
                m_MouseWorldPosition = evt.mousePosition;
                m_HasMousePosition = true;
            });
        }

        /// <summary>
        /// Вставка (Paste - под курсор, Duplicate - рядом с оригиналом). Вставленные ноды получают новые Id
        /// и становятся выделением; связи между ними восстанавливаются.
        /// </summary>
        private void PasteNodes(string operationName, string serializedData)
        {
            if (!GBSClipboard.TryRead(serializedData, out var nodesData, out var edgesData))
            {
                return;
            }

            // Start в графе должен быть один.
            if (GetNodes().Any(node => node.NodeType == GBSNodeType.Start) &&
                nodesData.RemoveAll(data => data.NodeType == GBSNodeType.Start) > 0)
            {
                Debug.LogWarning("[GBS] Start-нода не вставлена: в графе уже есть Start.");
            }

            if (nodesData.Count == 0)
            {
                return;
            }

            GBSClipboard.RenewNodeIds(nodesData, edgesData);

            var offset = GetPasteOffset(nodesData, operationName == DuplicateOperationName);
            var createdNodes = new Dictionary<string, GBSNode>();

            ClearSelection();

            foreach (var data in nodesData)
            {
                data.Position += offset;

                var node = CreateNode(data.NodeType, data.Position, data);

                if (node == null)
                {
                    continue;
                }

                createdNodes[node.ID] = node;
                AddToSelection(node);
            }

            foreach (var edge in GBSIOUtility.ConnectEdges(this, edgesData, createdNodes))
            {
                AddToSelection(edge);
            }
        }

        private Vector2 GetPasteOffset(List<GBSNodeData> nodesData, bool isDuplicate)
        {
            if (isDuplicate || !m_HasMousePosition)
            {
                return DuplicateOffset;
            }

            var topLeft = new Vector2(float.MaxValue, float.MaxValue);

            foreach (var data in nodesData)
            {
                topLeft = Vector2.Min(topLeft, data.Position);
            }

            return GetLocalMousePosition(m_MouseWorldPosition) - topLeft;
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
                    node.Dispose();
                    RemoveElement(node);
                }
            };
        }
    }
}

