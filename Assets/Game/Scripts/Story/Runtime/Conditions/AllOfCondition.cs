using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Game.Scripts.Story
{
    /// <summary>
    /// Композитное условие: выполняется, когда завершились ВСЕ вложенные условия.
    /// </summary>
    [CreateAssetMenu(menuName = "Story/Conditions/Composite/All Of", fileName = "AllOfCondition")]
    public class AllOfCondition : StoryCondition
    {
        [SerializeField] private StoryCondition[] m_Conditions;

        public override async UniTask WaitAsync(StoryContext context, CancellationToken token)
        {
            if (m_Conditions == null || m_Conditions.Length == 0)
            {
                return;
            }

            var tasks = new UniTask[m_Conditions.Length];
            for (int i = 0; i < m_Conditions.Length; i++)
            {
                tasks[i] = m_Conditions[i].WaitAsync(context, token);
            }

            await UniTask.WhenAll(tasks);
        }
    }
}

