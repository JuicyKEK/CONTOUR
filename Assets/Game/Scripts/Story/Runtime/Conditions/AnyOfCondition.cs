using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Game.Scripts.Story
{
    /// <summary>
    /// Композитное условие: выполняется, как только завершится ЛЮБОЕ
    /// из вложенных условий (остальные отменяются через связанный токен).
    /// </summary>
    [CreateAssetMenu(menuName = "Story/Conditions/Composite/Any Of", fileName = "AnyOfCondition")]
    public class AnyOfCondition : StoryCondition
    {
        [SerializeField] private StoryCondition[] m_Conditions;

        public override async UniTask WaitAsync(StoryContext context, CancellationToken token)
        {
            if (m_Conditions == null || m_Conditions.Length == 0)
            {
                return;
            }

            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(token);

            var tasks = new UniTask[m_Conditions.Length];
            for (int i = 0; i < m_Conditions.Length; i++)
            {
                tasks[i] = m_Conditions[i].WaitAsync(context, linkedCts.Token);
            }

            await UniTask.WhenAny(tasks);
            linkedCts.Cancel();
        }
    }
}

