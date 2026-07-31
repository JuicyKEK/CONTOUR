using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Game.Scripts.Story
{
    /// <summary>
    /// Проверяет значение флага в blackboard StoryContext (устанавливается,
    /// например, действием SetBlackboardFlagAction в другой ветке). Если
    /// флаг уже соответствует ожидаемому значению на момент старта ожидания -
    /// условие срабатывает сразу, иначе не срабатывает никогда (используется
    /// в связке с AnyOfCondition/AllOfCondition как "барьер", а не самостоятельно).
    /// </summary>
    [CreateAssetMenu(menuName = "Story/Conditions/Blackboard Flag", fileName = "BlackboardFlagCondition")]
    public class BlackboardFlagCondition : StoryCondition
    {
        [SerializeField] private string m_Key;
        [SerializeField] private bool m_ExpectedValue = true;

        public override UniTask WaitAsync(StoryContext context, CancellationToken token)
        {
            if (context.TryGetFlag(m_Key, out var value) && value == m_ExpectedValue)
            {
                return UniTask.CompletedTask;
            }

            return UniTask.Never(token);
        }
    }
}

