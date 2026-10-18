using System.Threading;
using Cysharp.Threading.Tasks;
using JuicyDI.Attributes;
using UnityEngine;
using UnityEngine.Playables;

namespace Game.Scripts.Story
{
    /// <summary>
    /// Оборачивает PlayableDirector (Timeline) в async API, чтобы StoryAction
    /// мог дождаться окончания ролика перед переходом к следующему шагу.
    /// </summary>
    [JDIMonoController]
    public class CutsceneDirector : MonoBehaviour, ICutsceneDirector
    {
        [SerializeField] private PlayableDirector m_Director;

        public async UniTask PlayAsync(PlayableAsset timeline, CancellationToken token)
        {
            if (m_Director == null)
            {
                Debug.LogWarning($"[Story] CutsceneDirector: не назначен PlayableDirector - ролик '{(timeline != null ? timeline.name : "null")}' не проигран.", this);
                return;
            }

            if (timeline == null)
            {
                return;
            }

            m_Director.playableAsset = timeline;
            m_Director.time = 0;
            m_Director.Play();

            await UniTask.WaitUntil(() => m_Director.state != PlayState.Playing, cancellationToken: token);
        }
    }
}

