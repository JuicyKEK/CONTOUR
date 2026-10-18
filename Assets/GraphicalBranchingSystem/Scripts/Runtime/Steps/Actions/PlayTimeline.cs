using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Scripts.Story;
using UnityEngine;
using UnityEngine.Playables;

namespace GBS.Steps
{
    /// <summary>
    /// Проигрывает Timeline-ролик (катсцену) через ICutsceneDirector.
    /// </summary>
    [Serializable, GBSMenu("Camera/Play Timeline")]
    public class PlayTimeline : GBSAction
    {
        [SerializeField] private PlayableAsset m_Timeline;
        [SerializeField] private bool m_WaitForCompletion = true;

        public PlayTimeline()
        {
        }

        public PlayTimeline(PlayableAsset timeline, bool waitForCompletion)
        {
            m_Timeline = timeline;
            m_WaitForCompletion = waitForCompletion;
        }

        public override async UniTask ExecuteAsync(StoryContext context, CancellationToken token)
        {
            var cutsceneDirector = context.CutsceneDirector;

            if (cutsceneDirector == null || m_Timeline == null)
            {
                Debug.LogWarning("[GBS] Play Timeline: нет ICutsceneDirector на сцене или не задан ролик.");
                return;
            }

            var task = cutsceneDirector.PlayAsync(m_Timeline, token);

            if (m_WaitForCompletion)
            {
                await task;
            }
            else
            {
                task.Forget();
            }
        }
    }
}
