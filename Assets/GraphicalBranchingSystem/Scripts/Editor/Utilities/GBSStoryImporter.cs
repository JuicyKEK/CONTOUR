using System.Collections.Generic;
using GBS.Data;
using GBS.Utility;
using Game.Scripts.Story;
using UnityEditor;
using UnityEngine;

namespace GBS.EditorTools
{
    /// <summary>
    /// Конвертер старого сюжета (цепочка StoryNodeSO) в граф GBS.
    /// Выделите стартовую StoryNodeSO в Project и вызовите
    /// UnityDev/GBS/Import Story From StoryNodeSO.
    ///
    /// Действия и условия переносятся во встроенные шаги графа (см. GBSLegacyConverter):
    /// подсказки/задержки/камера - с теми же настройками, SO-каналы - ключами "Legacy/&lt;имя&gt;",
    /// которые продолжают работать со старой разводкой сцены (каналы и StoryEventChannelListener).
    /// </summary>
    public static class GBSStoryImporter
    {
        private const float ColumnWidth = 420f;
        private const float RowHeight = 320f;

        [MenuItem("UnityDev/GBS/Import Story From StoryNodeSO")]
        public static void ImportSelected()
        {
            if (Selection.activeObject is not StoryNodeSO startNode)
            {
                EditorUtility.DisplayDialog("GBS", "Выделите стартовую StoryNodeSO в окне Project.", "Ок");
                return;
            }

            var graph = Import(startNode, startNode.name + "Graph");

            if (graph != null)
            {
                Selection.activeObject = graph;
                EditorGUIUtility.PingObject(graph);
            }
        }

        public static GBSGraphSO Import(StoryNodeSO startNode, string graphName)
        {
            if (startNode == null)
            {
                return null;
            }

            var nodes = new List<GBSNodeData>();
            var edges = new List<GBSEdgeData>();

            var storyNodes = new Dictionary<StoryNodeSO, GBSStoryNodeData>();
            var depths = new Dictionary<StoryNodeSO, int>();
            var rowsPerDepth = new Dictionary<int, int>();

            var queue = new Queue<StoryNodeSO>();

            queue.Enqueue(startNode);
            depths[startNode] = 1;

            while (queue.Count > 0)
            {
                var storyNode = queue.Dequeue();

                if (storyNodes.ContainsKey(storyNode))
                {
                    continue;
                }

                var depth = depths[storyNode];

                rowsPerDepth.TryGetValue(depth, out var row);
                rowsPerDepth[depth] = row + 1;

                var data = new GBSStoryNodeData
                {
                    Id = System.Guid.NewGuid().ToString(),
                    NodeName = string.IsNullOrEmpty(storyNode.NodeName) ? storyNode.name : storyNode.NodeName,
                    Position = new Vector2(depth * ColumnWidth, row * RowHeight),
                    Branches = new List<GBSBranchData>()
                };

                foreach (var legacyAction in storyNode.OnEnterActions)
                {
                    var action = GBSLegacyConverter.ConvertAction(legacyAction);

                    if (action != null)
                    {
                        data.Actions.Add(action);
                    }
                }

                storyNodes.Add(storyNode, data);
                nodes.Add(data);

                var branches = storyNode.Branches;

                for (int i = 0; i < branches.Count; i++)
                {
                    var branch = branches[i];

                    data.Branches.Add(new GBSBranchData
                    {
                        Id = System.Guid.NewGuid().ToString(),
                        BranchName = string.IsNullOrEmpty(branch.BranchName) ? "Branch " + i : branch.BranchName,
                        InlineCondition = GBSLegacyConverter.ConvertCondition(branch.Condition)
                    });

                    if (branch.NextNode != null && !depths.ContainsKey(branch.NextNode))
                    {
                        depths[branch.NextNode] = depth + 1;
                        queue.Enqueue(branch.NextNode);
                    }
                }
            }

            // Start-нода.
            var startData = new GBSStartNodeData
            {
                Id = System.Guid.NewGuid().ToString(),
                NodeName = "Start",
                Position = Vector2.zero
            };

            nodes.Add(startData);

            edges.Add(new GBSEdgeData
            {
                FromNodeId = startData.Id,
                FromPortId = GBSPortId.Out,
                ToNodeId = storyNodes[startNode].Id,
                ToPortId = GBSPortId.In
            });

            // End-нода.
            var endData = new GBSEndNodeData
            {
                Id = System.Guid.NewGuid().ToString(),
                NodeName = "End",
                Position = new Vector2((rowsPerDepth.Count + 2) * ColumnWidth, 0f)
            };

            nodes.Add(endData);

            foreach (var pair in storyNodes)
            {
                var storyNode = pair.Key;
                var data = pair.Value;
                var branches = storyNode.Branches;

                if (data.Branches.Count == 0)
                {
                    data.Branches.Add(new GBSBranchData
                    {
                        Id = System.Guid.NewGuid().ToString(),
                        BranchName = "End"
                    });
                }

                for (int i = 0; i < data.Branches.Count; i++)
                {
                    var nextNode = i < branches.Count ? branches[i].NextNode : null;

                    var targetId = nextNode != null && storyNodes.TryGetValue(nextNode, out var nextData)
                        ? nextData.Id
                        : endData.Id;

                    edges.Add(new GBSEdgeData
                    {
                        FromNodeId = data.Id,
                        FromPortId = data.Branches[i].Id,
                        ToNodeId = targetId,
                        ToPortId = GBSPortId.In
                    });
                }
            }

            return CreateAsset(graphName, nodes, edges);
        }

        private static GBSGraphSO CreateAsset(string graphName, List<GBSNodeData> nodes, List<GBSEdgeData> edges)
        {
            if (!AssetDatabase.IsValidFolder(GBSIOUtility.GraphsFolder))
            {
                AssetDatabase.CreateFolder("Assets/GraphicalBranchingSystem", "Graphs");
            }

            var path = AssetDatabase.GenerateUniqueAssetPath($"{GBSIOUtility.GraphsFolder}/{graphName}.asset");
            var graph = ScriptableObject.CreateInstance<GBSGraphSO>();

            AssetDatabase.CreateAsset(graph, path);

            graph.SetContent(graphName, nodes, edges);

            EditorUtility.SetDirty(graph);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[GBS] Сюжет импортирован в граф: {path}");

            return graph;
        }
    }
}
