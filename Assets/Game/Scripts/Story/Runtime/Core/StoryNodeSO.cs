using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Scripts.Story
{
    /// <summary>
    /// Этап сюжета. Хранит:
    ///  - список действий, выполняемых последовательно при входе в ноду (StoryAction);
    ///  - список веток дальнейшего перехода, каждая со своим условием (StoryBranch).
    /// Ноды и условия/действия - переиспользуемые SO-ассеты, граф строится
    /// перетаскиванием ассетов в списки в инспекторе, без написания кода.
    /// </summary>
    [CreateAssetMenu(menuName = "Story/Story Node", fileName = "StoryNode")]
    public class StoryNodeSO : ScriptableObject
    {
        [SerializeField] private string m_NodeName;
        [SerializeField] private StoryAction[] m_OnEnterActions;
        [SerializeField] private StoryBranch[] m_Branches;

        public string NodeName => m_NodeName;
        public IReadOnlyList<StoryAction> OnEnterActions => m_OnEnterActions ?? Array.Empty<StoryAction>();
        public IReadOnlyList<StoryBranch> Branches => m_Branches ?? Array.Empty<StoryBranch>();
    }
}

