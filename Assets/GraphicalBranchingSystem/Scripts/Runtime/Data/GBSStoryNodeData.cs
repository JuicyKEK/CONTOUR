using System;
using System.Collections.Generic;
using GBS.Events;
using Game.Scripts.Story;
using UnityEngine;

namespace GBS.Data
{
    /// <summary>
    /// Ветка перехода из сюжетной ноды. Условие берётся:
    ///  1) из подключённого к порту "BranchId#cond" булева выражения (приоритет);
    ///  2) иначе из поля Condition (StoryCondition SO);
    ///  3) если ничего не задано - ветка срабатывает мгновенно.
    /// </summary>
    [Serializable]
    public class GBSBranchData
    {
        [SerializeField] private string m_Id;
        [SerializeField] private string m_BranchName;
        [SerializeField] private StoryCondition m_Condition;

        public string Id
        {
            get => m_Id;
            set => m_Id = value;
        }

        public string BranchName
        {
            get => m_BranchName;
            set => m_BranchName = value;
        }

        public StoryCondition Condition
        {
            get => m_Condition;
            set => m_Condition = value;
        }
    }

    /// <summary>
    /// Сюжетная нода: то, что раньше было StoryNodeSO. Теперь и действия входа,
    /// и ветки перехода живут прямо в ноде графа GBS.
    /// </summary>
    [Serializable]
    public class GBSStoryNodeData : GBSNodeData
    {
        [SerializeField] private List<StoryAction> m_OnEnterActions = new List<StoryAction>();
        [SerializeField] private List<GBSEvent> m_OnEnterEvents = new List<GBSEvent>();
        [SerializeField] private List<GBSBranchData> m_Branches = new List<GBSBranchData>();

        public List<StoryAction> OnEnterActions
        {
            get => m_OnEnterActions ??= new List<StoryAction>();
            set => m_OnEnterActions = value;
        }

        public List<GBSEvent> OnEnterEvents
        {
            get => m_OnEnterEvents ??= new List<GBSEvent>();
            set => m_OnEnterEvents = value;
        }

        public List<GBSBranchData> Branches
        {
            get => m_Branches ??= new List<GBSBranchData>();
            set => m_Branches = value;
        }

        public override GBSNodeType NodeType => GBSNodeType.Story;
    }
}
