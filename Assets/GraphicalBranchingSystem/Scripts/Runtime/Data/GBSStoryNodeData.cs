using System;
using System.Collections.Generic;
using GBS.Events;
using GBS.Steps;
using Game.Scripts.Story;
using UnityEngine;

namespace GBS.Data
{
    /// <summary>
    /// Переход из сюжетной ноды: имя + условие (хранится прямо в графе). Пустое условие -
    /// переход срабатывает сразу. Сюжет уходит по ребру, выходящему из порта этого перехода.
    /// </summary>
    [Serializable]
    public class GBSBranchData
    {
        [SerializeField] private string m_Id;
        [SerializeField] private string m_BranchName;
        [SerializeReference] private GBSCondition m_InlineCondition;

        // До v3.0: условие - SO-ассет. Читается для совместимости, редактор при загрузке
        // переносит его в m_InlineCondition (при пересохранении графа поле очищается).
        [SerializeField] private StoryCondition m_Condition;

        [NonSerialized] private GBSCondition m_LegacyConditionCache;

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

        /// <summary>
        /// Условие для рантайма: встроенное, иначе обёртка над старым SO-ассетом, иначе null (переход сразу).
        /// </summary>
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
    }

    /// <summary>
    /// Сюжетная нода: действия при входе (выполняются по очереди с ожиданием) и переходы.
    /// Действия и условия хранятся прямо в графе ([SerializeReference]) - без SO-ассета на каждое.
    /// </summary>
    [Serializable]
    public class GBSStoryNodeData : GBSNodeData
    {
        [SerializeReference] private List<GBSAction> m_Actions = new List<GBSAction>();
        [SerializeField] private List<GBSBranchData> m_Branches = new List<GBSBranchData>();

        // До v3.0: действия и события - SO-ассеты. Читаются для совместимости, редактор при загрузке
        // переносит их в m_Actions (при пересохранении графа поля очищаются).
        [SerializeField] private List<StoryAction> m_OnEnterActions = new List<StoryAction>();
        [SerializeField] private List<GBSEvent> m_OnEnterEvents = new List<GBSEvent>();

        [NonSerialized] private List<GBSAction> m_RuntimeActionsCache;

        public List<GBSAction> Actions
        {
            get => m_Actions ??= new List<GBSAction>();
            set => m_Actions = value;
        }

        public List<GBSBranchData> Branches
        {
            get => m_Branches ??= new List<GBSBranchData>();
            set => m_Branches = value;
        }

        public List<StoryAction> LegacyActions => m_OnEnterActions ??= new List<StoryAction>();
        public List<GBSEvent> LegacyEvents => m_OnEnterEvents ??= new List<GBSEvent>();

        public bool HasLegacyContent => LegacyActions.Count > 0 || LegacyEvents.Count > 0;

        /// <summary>
        /// Действия для рантайма. Если граф ещё не пересохранён после v3.0 - старые ассеты оборачиваются
        /// (сначала события, затем действия, как раньше), затем встроенные действия.
        /// </summary>
        public IReadOnlyList<GBSAction> GetActions()
        {
            if (!HasLegacyContent)
            {
                return Actions;
            }

            if (m_RuntimeActionsCache == null)
            {
                m_RuntimeActionsCache = new List<GBSAction>();

                foreach (var legacyEvent in LegacyEvents)
                {
                    m_RuntimeActionsCache.Add(new LegacyGBSEventAsset(legacyEvent));
                }

                foreach (var legacyAction in LegacyActions)
                {
                    m_RuntimeActionsCache.Add(new LegacyStoryActionAsset(legacyAction));
                }

                m_RuntimeActionsCache.AddRange(Actions);
            }

            return m_RuntimeActionsCache;
        }

        /// <summary>
        /// Очищает старые поля после переноса их содержимого во встроенные действия (редактор).
        /// </summary>
        public void ClearLegacyContent()
        {
            LegacyActions.Clear();
            LegacyEvents.Clear();
            m_RuntimeActionsCache = null;
        }

        public override GBSNodeType NodeType => GBSNodeType.Story;
    }
}
