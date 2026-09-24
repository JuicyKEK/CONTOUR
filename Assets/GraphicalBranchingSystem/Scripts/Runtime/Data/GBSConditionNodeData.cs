using System;
using Game.Scripts.Story;
using UnityEngine;

namespace GBS.Data
{
    /// <summary>
    /// Обёртка над StoryCondition: превращает ожидание условия в булев сигнал,
    /// который можно скормить нодам булевой алгебры или напрямую ветке.
    /// Значение "защёлкивается": сработав один раз, условие остаётся истинным.
    /// </summary>
    [Serializable]
    public class GBSConditionNodeData : GBSNodeData
    {
        [SerializeField] private StoryCondition m_Condition;

        public StoryCondition Condition
        {
            get => m_Condition;
            set => m_Condition = value;
        }

        public override GBSNodeType NodeType => GBSNodeType.Condition;
    }
}

