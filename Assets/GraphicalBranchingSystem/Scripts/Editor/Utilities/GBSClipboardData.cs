using System.Collections.Generic;
using GBS.Data;
using UnityEngine;

namespace GBS.Utility
{
    /// <summary>
    /// Временный контейнер скопированных нод и связей между ними. Нужен, чтобы сериализовать
    /// [SerializeReference]-данные нод в JSON для буфера обмена (EditorJsonUtility работает с UnityEngine.Object).
    /// </summary>
    public class GBSClipboardData : ScriptableObject
    {
        [SerializeReference] private List<GBSNodeData> m_Nodes = new List<GBSNodeData>();
        [SerializeField] private List<GBSEdgeData> m_Edges = new List<GBSEdgeData>();

        public List<GBSNodeData> Nodes => m_Nodes ??= new List<GBSNodeData>();
        public List<GBSEdgeData> Edges => m_Edges ??= new List<GBSEdgeData>();
    }
}
