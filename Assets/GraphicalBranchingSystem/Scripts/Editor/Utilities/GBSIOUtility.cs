using System.Collections.Generic;
using System.IO;
using System.Linq;
using GBS.Data;
using GBS.Elements;
using GBS.Windows;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace GBS.Utility
{
    /// <summary>
    /// Сохранение графа в ассет GBSGraphSO и загрузка его обратно в окно редактора.
    /// Каждый граф - отдельный ассет, поэтому графов можно делать сколько угодно.
    /// </summary>
    public static class GBSIOUtility
    {
        public const string GraphsFolder = "Assets/GraphicalBranchingSystem/Graphs";

        public static GBSGraphSO Save(GBSGraphView graphView, string fileName, GBSGraphSO target)
        {
            if (graphView == null)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(fileName))
            {
                Debug.LogError("[GBS] Укажите имя файла графа перед сохранением.");
                return null;
            }

            var nodes = new List<GBSNodeData>();
            var editorNodes = graphView.GetNodes().ToList();

            for (int i = 0; i < editorNodes.Count; i++)
            {
                nodes.Add(editorNodes[i].Save());
            }

            var edges = CollectEdges(graphView);

            var graph = target != null ? target : LoadOrCreateAsset(fileName);

            graph.SetContent(fileName, nodes, edges);

            EditorUtility.SetDirty(graph);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[GBS] Граф '{fileName}' сохранён ({nodes.Count} нод, {edges.Count} связей).");

            return graph;
        }

        public static void Load(GBSGraphView graphView, GBSGraphSO graph)
        {
            if (graphView == null || graph == null)
            {
                return;
            }

            graphView.ClearGraph();

            var createdNodes = new Dictionary<string, GBSNode>();
            var nodes = graph.Nodes;

            for (int i = 0; i < nodes.Count; i++)
            {
                var data = nodes[i];

                if (data == null)
                {
                    continue;
                }

                var node = graphView.CreateNode(data.NodeType, data.Position, data);

                if (node != null)
                {
                    createdNodes[node.ID] = node;
                }
            }

            ConnectEdges(graphView, graph.Edges, createdNodes);
        }

        /// <summary>
        /// Восстанавливает рёбра между нодами окна (загрузка графа, вставка из буфера обмена).
        /// Ребро, у которого нет ноды или порта, пропускается.
        /// </summary>
        public static List<Edge> ConnectEdges(GBSGraphView graphView, IReadOnlyList<GBSEdgeData> edges, IReadOnlyDictionary<string, GBSNode> nodesById)
        {
            var result = new List<Edge>();

            for (int i = 0; i < edges.Count; i++)
            {
                var edgeData = edges[i];

                if (edgeData == null)
                {
                    continue;
                }

                if (!nodesById.TryGetValue(edgeData.FromNodeId ?? string.Empty, out var fromNode) ||
                    !nodesById.TryGetValue(edgeData.ToNodeId ?? string.Empty, out var toNode))
                {
                    continue;
                }

                var fromPort = fromNode.GetPort(edgeData.FromPortId);
                var toPort = toNode.GetPort(edgeData.ToPortId);

                if (fromPort == null || toPort == null)
                {
                    continue;
                }

                var edge = fromPort.ConnectTo(toPort);

                graphView.AddElement(edge);
                result.Add(edge);
            }

            return result;
        }

        private static List<GBSEdgeData> CollectEdges(GBSGraphView graphView)
        {
            return CollectEdges(graphView.edges.ToList());
        }

        /// <summary>
        /// Данные рёбер окна редактора (сохранение графа, копирование нод).
        /// </summary>
        public static List<GBSEdgeData> CollectEdges(IEnumerable<Edge> edges)
        {
            var result = new List<GBSEdgeData>();

            foreach (var edge in edges)
            {
                if (edge?.output?.node is not GBSNode fromNode || edge.input?.node is not GBSNode toNode)
                {
                    continue;
                }

                var fromPortId = fromNode.FindPortId(edge.output);
                var toPortId = toNode.FindPortId(edge.input);

                if (string.IsNullOrEmpty(fromPortId) || string.IsNullOrEmpty(toPortId))
                {
                    continue;
                }

                result.Add(new GBSEdgeData
                {
                    FromNodeId = fromNode.ID,
                    FromPortId = fromPortId,
                    ToNodeId = toNode.ID,
                    ToPortId = toPortId
                });
            }

            return result;
        }

        private static GBSGraphSO LoadOrCreateAsset(string fileName)
        {
            EnsureFolder();

            var path = $"{GraphsFolder}/{fileName}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<GBSGraphSO>(path);

            if (existing != null)
            {
                return existing;
            }

            var graph = ScriptableObject.CreateInstance<GBSGraphSO>();

            AssetDatabase.CreateAsset(graph, path);

            return graph;
        }

        private static void EnsureFolder()
        {
            if (AssetDatabase.IsValidFolder(GraphsFolder))
            {
                return;
            }

            var parent = Path.GetDirectoryName(GraphsFolder)?.Replace('\\', '/');
            var folderName = Path.GetFileName(GraphsFolder);

            AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
