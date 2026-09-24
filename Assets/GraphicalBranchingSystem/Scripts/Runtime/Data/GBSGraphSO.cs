using System;
using System.Collections.Generic;
using UnityEngine;

namespace GBS.Data
{
    /// <summary>
    /// Сохранённый граф сюжета. Создаётся/перезаписывается из окна редактора GBS
    /// (UnityDev/GBS/Open Graph) и кладётся в инспектор GBSStarter.
    /// В проекте может быть сколько угодно графов: один большой сюжет и
    /// сколько нужно микро-сюжетов.
    /// </summary>
    [CreateAssetMenu(fileName = "GBSGraph", menuName = "GBS/Graph", order = 50)]
    public class GBSGraphSO : ScriptableObject
    {
        [SerializeField] private string m_GraphId;
        [SerializeField] private string m_GraphName;

        [SerializeReference] private List<GBSNodeData> m_Nodes = new List<GBSNodeData>();
        [SerializeField] private List<GBSEdgeData> m_Edges = new List<GBSEdgeData>();

        private Dictionary<string, GBSNodeData> m_NodeById;
        private Dictionary<string, GBSEdgeData> m_EdgeByFrom;
        private Dictionary<string, GBSEdgeData> m_EdgeByTo;

        public string GraphId
        {
            get
            {
                if (string.IsNullOrEmpty(m_GraphId))
                {
                    m_GraphId = Guid.NewGuid().ToString();
                }

                return m_GraphId;
            }
        }

        public string GraphName => string.IsNullOrEmpty(m_GraphName) ? name : m_GraphName;

        public IReadOnlyList<GBSNodeData> Nodes => m_Nodes ??= new List<GBSNodeData>();
        public IReadOnlyList<GBSEdgeData> Edges => m_Edges ??= new List<GBSEdgeData>();

        public void SetContent(string graphName, List<GBSNodeData> nodes, List<GBSEdgeData> edges)
        {
            if (string.IsNullOrEmpty(m_GraphId))
            {
                m_GraphId = Guid.NewGuid().ToString();
            }

            m_GraphName = graphName;
            m_Nodes = nodes ?? new List<GBSNodeData>();
            m_Edges = edges ?? new List<GBSEdgeData>();

            InvalidateIndex();
        }

        public void InvalidateIndex()
        {
            m_NodeById = null;
            m_EdgeByFrom = null;
            m_EdgeByTo = null;
        }

        public GBSNodeData GetNode(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId))
            {
                return null;
            }

            EnsureIndex();

            return m_NodeById.TryGetValue(nodeId, out var node) ? node : null;
        }

        public GBSStartNodeData GetStartNode()
        {
            var nodes = Nodes;

            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i] is GBSStartNodeData startNode)
                {
                    return startNode;
                }
            }

            return null;
        }

        /// <summary>Связь, исходящая из выходного порта.</summary>
        public GBSEdgeData GetEdgeFrom(string nodeId, string portId)
        {
            EnsureIndex();

            return m_EdgeByFrom.TryGetValue(MakeKey(nodeId, portId), out var edge) ? edge : null;
        }

        /// <summary>Связь, входящая во входной порт.</summary>
        public GBSEdgeData GetEdgeTo(string nodeId, string portId)
        {
            EnsureIndex();

            return m_EdgeByTo.TryGetValue(MakeKey(nodeId, portId), out var edge) ? edge : null;
        }

        private void OnEnable()
        {
            InvalidateIndex();
        }

        private void EnsureIndex()
        {
            if (m_NodeById != null)
            {
                return;
            }

            m_NodeById = new Dictionary<string, GBSNodeData>();
            m_EdgeByFrom = new Dictionary<string, GBSEdgeData>();
            m_EdgeByTo = new Dictionary<string, GBSEdgeData>();

            var nodes = Nodes;

            for (int i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];

                if (node == null || string.IsNullOrEmpty(node.Id))
                {
                    continue;
                }

                m_NodeById[node.Id] = node;
            }

            var edges = Edges;

            for (int i = 0; i < edges.Count; i++)
            {
                var edge = edges[i];

                if (edge == null)
                {
                    continue;
                }

                m_EdgeByFrom[MakeKey(edge.FromNodeId, edge.FromPortId)] = edge;
                m_EdgeByTo[MakeKey(edge.ToNodeId, edge.ToPortId)] = edge;
            }
        }

        private static string MakeKey(string nodeId, string portId)
        {
            return nodeId + "/" + portId;
        }
    }
}

