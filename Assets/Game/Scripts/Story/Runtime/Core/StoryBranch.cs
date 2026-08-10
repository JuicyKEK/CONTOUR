using System;
using UnityEngine;

namespace Game.Scripts.Story
{
    /// <summary>
    /// Ветка перехода из ноды: условие + следующая нода. У ноды может быть
    /// несколько веток - StoryManager запускает их условия параллельно
    /// (race) и выбирает ту, что сработала первой, отменяя остальные.
    /// Это реализует "развилки" (игрок выполнил / отказался / проигнорировал).
    /// </summary>
    [Serializable]
    public class StoryBranch
    {
        [SerializeField] private string m_BranchName;
        [SerializeField] private StoryCondition m_Condition;
        [SerializeField] private StoryNodeSO m_NextNode;

        public string BranchName => m_BranchName;
        public StoryCondition Condition => m_Condition;
        public StoryNodeSO NextNode => m_NextNode;
    }
}

