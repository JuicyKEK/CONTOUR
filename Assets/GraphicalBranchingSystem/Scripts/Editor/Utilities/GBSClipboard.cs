using System;
using System.Collections.Generic;
using System.Linq;
using GBS.Data;
using GBS.Elements;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GBS.Utility
{
    /// <summary>
    /// Copy / Cut / Paste / Duplicate нод GBS. Ноды (со всеми встроенными действиями и условиями) и связи
    /// между ними кладутся в системный буфер обмена как JSON - вставлять можно и в другой граф.
    /// </summary>
    public static class GBSClipboard
    {
        private const string Prefix = "GBS_NODES:";

        public static string Serialize(IEnumerable<GBSNode> nodes, IEnumerable<Edge> graphEdges)
        {
            var nodeSet = new HashSet<GBSNode>(nodes);

            if (nodeSet.Count == 0)
            {
                return string.Empty;
            }

            var data = ScriptableObject.CreateInstance<GBSClipboardData>();
            data.hideFlags = HideFlags.DontSave;

            try
            {
                foreach (var node in nodeSet)
                {
                    data.Nodes.Add(node.Save());
                }

                // Только связи внутри копии: ребро на нескопированную ноду вставить некуда.
                var innerEdges = graphEdges.Where(edge =>
                    edge?.output?.node is GBSNode fromNode && nodeSet.Contains(fromNode) &&
                    edge.input?.node is GBSNode toNode && nodeSet.Contains(toNode));

                data.Edges.AddRange(GBSIOUtility.CollectEdges(innerEdges));

                return Prefix + EditorJsonUtility.ToJson(data);
            }
            finally
            {
                Object.DestroyImmediate(data);
            }
        }

        public static bool CanPaste(string serializedData)
        {
            return !string.IsNullOrEmpty(serializedData) && serializedData.StartsWith(Prefix, StringComparison.Ordinal);
        }

        /// <summary>
        /// Читает буфер обмена. Каждый вызов даёт новые независимые копии данных, поэтому
        /// одну и ту же копию можно вставлять много раз.
        /// </summary>
        public static bool TryRead(string serializedData, out List<GBSNodeData> nodes, out List<GBSEdgeData> edges)
        {
            nodes = new List<GBSNodeData>();
            edges = new List<GBSEdgeData>();

            if (!CanPaste(serializedData))
            {
                return false;
            }

            var data = ScriptableObject.CreateInstance<GBSClipboardData>();
            data.hideFlags = HideFlags.DontSave;

            try
            {
                EditorJsonUtility.FromJsonOverwrite(serializedData.Substring(Prefix.Length), data);

                nodes.AddRange(data.Nodes.Where(node => node != null));
                edges.AddRange(data.Edges.Where(edge => edge != null));

                if (nodes.Count == 0)
                {
                    Debug.LogWarning("[GBS] В буфере обмена нет нод, которые удалось прочитать - нечего вставлять.");
                    return false;
                }

                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[GBS] Не удалось вставить ноды из буфера обмена: {exception.Message}");
                return false;
            }
            finally
            {
                Object.DestroyImmediate(data);
            }
        }

        /// <summary>
        /// Новые Id для вставляемых нод (Id обязан быть уникальным в графе) и перепривязка связей к ним.
        /// Id переходов сюжетной ноды не меняются: порты различаются в пределах своей ноды.
        /// </summary>
        public static void RenewNodeIds(List<GBSNodeData> nodes, List<GBSEdgeData> edges)
        {
            var newIds = new Dictionary<string, string>();

            foreach (var node in nodes)
            {
                var newId = Guid.NewGuid().ToString();

                if (!string.IsNullOrEmpty(node.Id))
                {
                    newIds[node.Id] = newId;
                }

                node.Id = newId;
            }

            foreach (var edge in edges)
            {
                edge.FromNodeId = newIds.TryGetValue(edge.FromNodeId ?? string.Empty, out var fromId) ? fromId : null;
                edge.ToNodeId = newIds.TryGetValue(edge.ToNodeId ?? string.Empty, out var toId) ? toId : null;
            }
        }
    }
}
