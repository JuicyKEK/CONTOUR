using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GBS.Steps
{
    /// <summary>
    /// Выполняется, когда выполнены ВСЕ вложенные условия.
    /// </summary>
    [Serializable, GBSMenu("Logic/All Of")]
    public class AllOf : GBSCondition
    {
        [SerializeReference] private List<GBSCondition> m_Conditions = new();

        public AllOf()
        {
        }

        public AllOf(List<GBSCondition> conditions)
        {
            m_Conditions = conditions ?? new List<GBSCondition>();
        }

        public override UniTask WaitAsync(GBSConditionContext context, CancellationToken token)
        {
            var tasks = new List<UniTask>(m_Conditions.Count);

            foreach (var condition in m_Conditions)
            {
                if (condition != null)
                {
                    tasks.Add(condition.WaitAsync(context, token));
                }
            }

            return tasks.Count == 0 ? UniTask.CompletedTask : UniTask.WhenAll(tasks);
        }
    }

    /// <summary>
    /// Выполняется, как только выполнено ЛЮБОЕ из вложенных условий (остальные отменяются).
    /// </summary>
    [Serializable, GBSMenu("Logic/Any Of")]
    public class AnyOf : GBSCondition
    {
        [SerializeReference] private List<GBSCondition> m_Conditions = new();

        public AnyOf()
        {
        }

        public AnyOf(List<GBSCondition> conditions)
        {
            m_Conditions = conditions ?? new List<GBSCondition>();
        }

        public override async UniTask WaitAsync(GBSConditionContext context, CancellationToken token)
        {
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(token);
            var tasks = new List<UniTask>(m_Conditions.Count);

            foreach (var condition in m_Conditions)
            {
                if (condition != null)
                {
                    tasks.Add(condition.WaitAsync(context, linkedCts.Token));
                }
            }

            if (tasks.Count == 0)
            {
                return;
            }

            await UniTask.WhenAny(tasks.ToArray());
            linkedCts.Cancel();
        }
    }
}
