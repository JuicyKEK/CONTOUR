using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Playables;

namespace Game.Scripts.Story
{
    /// <summary>
    /// Проигрывает Timeline-ролик (катсцену) через ICutsceneDirector.
    /// </summary>
    [CreateAssetMenu(menuName = "Story/Actions/Play Timeline", fileName = "PlayTimelineAction")]
    public class PlayTimelineAction : StoryAction
    {
        [SerializeField] private PlayableAsset m_Timeline;
        [SerializeField] private bool m_WaitForCompletion = true;

        public override async UniTask ExecuteAsync(StoryContext context, CancellationToken token)
        {
            if (context.CutsceneDirector == null || m_Timeline == null)
            {
                return;
            }

            var task = context.CutsceneDirector.PlayAsync(m_Timeline, token);

            if (m_WaitForCompletion)
            {
                await task;
            }
        }
    }
}

