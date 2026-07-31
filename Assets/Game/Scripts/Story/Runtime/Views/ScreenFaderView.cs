using System.Threading;
using Cysharp.Threading.Tasks;
using JuicyDI.Attributes;
using UnityEngine;

namespace Game.Scripts.Story
{
    /// <summary>
    /// Простой fade-to-black для плавных переходов между катсценой и камерой игрока.
    /// При необходимости легко заменяется на Cinemachine blend - интерфейс IScreenFader
    /// изолирует StoryAction'ы от конкретной реализации.
    /// </summary>
    [JDIMonoController]
    public class ScreenFaderView : MonoBehaviour, IScreenFader
    {
        [SerializeField] private CanvasGroup m_CanvasGroup;

        public UniTask FadeOutAsync(float duration, CancellationToken token) => FadeAsync(1f, duration, token);

        public UniTask FadeInAsync(float duration, CancellationToken token) => FadeAsync(0f, duration, token);

        private async UniTask FadeAsync(float targetAlpha, float duration, CancellationToken token)
        {
            if (m_CanvasGroup == null)
            {
                return;
            }

            float startAlpha = m_CanvasGroup.alpha;

            if (duration <= 0f)
            {
                m_CanvasGroup.alpha = targetAlpha;
                return;
            }

            float elapsed = 0f;

            while (elapsed < duration)
            {
                token.ThrowIfCancellationRequested();
                elapsed += Time.deltaTime;
                m_CanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }

            m_CanvasGroup.alpha = targetAlpha;
        }
    }
}

