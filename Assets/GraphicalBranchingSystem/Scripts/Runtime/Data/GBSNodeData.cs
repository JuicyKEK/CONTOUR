using System;
using UnityEngine;

namespace GBS.Data
{
    /// <summary>
    /// Базовые данные ноды графа. Хранятся в GBSGraphSO через [SerializeReference],
    /// поэтому список может содержать разные типы нод.
    /// </summary>
    [Serializable]
    public abstract class GBSNodeData
    {
        [SerializeField] private string m_Id;
        [SerializeField] private string m_NodeName;
        [SerializeField] private Vector2 m_Position;

        public string Id
        {
            get => m_Id;
            set => m_Id = value;
        }

        public string NodeName
        {
            get => m_NodeName;
            set => m_NodeName = value;
        }

        public Vector2 Position
        {
            get => m_Position;
            set => m_Position = value;
        }

        public abstract GBSNodeType NodeType { get; }
    }
}
