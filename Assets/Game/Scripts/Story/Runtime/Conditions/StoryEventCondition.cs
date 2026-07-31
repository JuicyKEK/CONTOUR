using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Game.Scripts.Story
{
    /// <summary>
    /// Условие срабатывает при вызове StoryEventChannelSO.Raise() -
    /// например NPC сообщает "я подошёл к игроку" или "квест выполнен".
    /// </summary>
    [CreateAssetMenu(menuName = "Story/Conditions/Story Event", fileName = "StoryEventCondition")]
    public class StoryEventCondition : StoryCondition
    {
        [SerializeField] private StoryEventChannelSO m_EventChannel;

        public override async UniTask WaitAsync(StoryContext context, CancellationToken token)
        {
            if (m_EventChannel == null)
            {
                return;
            }

            var completionSource = new UniTaskCompletionSource();

            void Handler() => completionSource.TrySetResult();

            m_EventChannel.Subscribe(Handler);

            try
            {
                using (token.Register(() => completionSource.TrySetCanceled(token)))
                {
                    await completionSource.Task;
                }
            }
            finally
            {
                m_EventChannel.Unsubscribe(Handler);
            }
        }
    }
}

