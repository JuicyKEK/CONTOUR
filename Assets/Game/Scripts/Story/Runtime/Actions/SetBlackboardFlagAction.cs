using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Game.Scripts.Story
{
    /// <summary>
    /// Записывает результат выбора игрока в blackboard StoryContext
    /// (например "TookNpcTask" = true), чтобы дальнейшие ноды/условия
    /// могли на него ссылаться (BlackboardFlagCondition).
    /// </summary>
    [CreateAssetMenu(menuName = "Story/Actions/Set Blackboard Flag", fileName = "SetBlackboardFlagAction")]
    public class SetBlackboardFlagAction : StoryAction
    {
        [SerializeField] private string m_Key;
        [SerializeField] private bool m_Value = true;

        public override UniTask ExecuteAsync(StoryContext context, CancellationToken token)
        {
            context.SetFlag(m_Key, m_Value);
            return UniTask.CompletedTask;
        }
    }
}

