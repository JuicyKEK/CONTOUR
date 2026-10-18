using System;
using UnityEngine;

namespace GBS.Data
{
    /// <summary>
    /// Операции булевой алгебры для нод-логики.
    /// </summary>
    public enum GBSLogicOperation
    {
        And = 0,
        Or = 1,
        Not = 2,
        Nand = 3,
        Nor = 4,
        Xor = 5,
        Xnor = 6
    }

    /// <summary>
    /// Нода булевой алгебры. Количество входов настраивается кнопками "+"/"-"
    /// прямо в редакторе графа (минимум 1).
    /// </summary>
    [Serializable]
    public class GBSLogicNodeData : GBSNodeData
    {
        public const int MinInputCount = 1;
        public const int MaxInputCount = 16;

        [SerializeField] private GBSLogicOperation m_Operation = GBSLogicOperation.And;
        [SerializeField] private int m_InputCount = 2;

        public GBSLogicOperation Operation
        {
            get => m_Operation;
            set => m_Operation = value;
        }

        public int InputCount
        {
            get => Mathf.Clamp(m_InputCount, MinInputCount, MaxInputCount);
            set => m_InputCount = Mathf.Clamp(value, MinInputCount, MaxInputCount);
        }

        public override GBSNodeType NodeType => GBSNodeType.Logic;
    }
}
