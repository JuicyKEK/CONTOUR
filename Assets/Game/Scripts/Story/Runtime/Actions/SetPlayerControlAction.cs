using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Game.Scripts.Story
{
    /// <summary>
    /// Включает/выключает управление игрока (все зарегистрированные
    /// IPlayerControlHandle) - используется в начале катсцены и при
    /// возврате управления после неё.
    /// </summary>
    [CreateAssetMenu(menuName = "Story/Actions/Set Player Control", fileName = "SetPlayerControlAction")]
    public class SetPlayerControlAction : StoryAction
    {
        [SerializeField] private bool m_IsEnabled = true;

        public override UniTask ExecuteAsync(StoryContext context, CancellationToken token)
        {
            context.SetControlEnabled(m_IsEnabled);
            return UniTask.CompletedTask;
        }
    }
}

