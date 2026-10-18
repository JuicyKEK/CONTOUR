using System;
using GBS.Steps;
using Game.Scripts.Story;
using UnityEngine;

namespace GBS.Data
{
    /// <summary>
    /// Превращает ожидание условия в булев сигнал, который можно скормить нодам булевой алгебры
    /// или Start-ноде. Значение "защёлкивается": сработав один раз, условие остаётся истинным.
    /// </summary>
    [Serializable]
    public class GBSConditionNodeData : GBSNodeData
    {
        [SerializeReference] private GBSCondition m_InlineCondition;

        // До v3.0: условие - SO-ассет (см. GBSBranchData).
        [SerializeField] private StoryCondition m_Condition;

        [NonSerialized] private GBSCondition m_LegacyConditionCache;

        public GBSCondition InlineCondition
        {
            get => m_InlineCondition;
            set => m_InlineCondition = value;
        }

        public StoryCondition LegacyCondition
        {
            get => m_Condition;
            set => m_Condition = value;
        }

        public GBSCondition GetCondition()
        {
            if (m_InlineCondition != null)
            {
                return m_InlineCondition;
            }

            if (m_Condition == null)
            {
                return null;
            }

            return m_LegacyConditionCache ??= new LegacyStoryConditionAsset(m_Condition);
        }

        public override GBSNodeType NodeType => GBSNodeType.Condition;
    }
}
