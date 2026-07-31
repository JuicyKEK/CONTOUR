using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Game.Scripts.Story
{
    /// <summary>
    /// Показывает нижнюю текстовую подсказку игроку на заданное время.
    /// </summary>
    [CreateAssetMenu(menuName = "Story/Actions/Show Hint", fileName = "ShowHintAction")]
    public class ShowHintAction : StoryAction
    {
        [SerializeField, TextArea] private string m_Text;
        [SerializeField] private float m_Duration = 4f;
        [SerializeField] private bool m_WaitUntilHidden;

        public override async UniTask ExecuteAsync(StoryContext context, CancellationToken token)
        {
            if (context.HintView == null)
            {
                return;
            }

            var task = context.HintView.ShowAsync(m_Text, m_Duration, token);

            if (m_WaitUntilHidden)
            {
                await task;
            }
        }
    }
}

