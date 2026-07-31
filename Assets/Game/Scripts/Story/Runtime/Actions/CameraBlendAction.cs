using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Game.Scripts.Story
{
    /// <summary>
    /// Плавный переход камеры: fade-out -> переключение активной камеры
    /// по ключу (через ICameraDirector) -> fade-in. Именно этим действием
    /// реализуется "камера плавно перетекает в камеру игрока" из ТЗ.
    /// </summary>
    [CreateAssetMenu(menuName = "Story/Actions/Blend Camera", fileName = "CameraBlendAction")]
    public class CameraBlendAction : StoryAction
    {
        [SerializeField] private string m_TargetCameraKey;
        [SerializeField] private float m_FadeDuration = 0.5f;

        public override async UniTask ExecuteAsync(StoryContext context, CancellationToken token)
        {
            if (context.ScreenFader != null)
            {
                await context.ScreenFader.FadeOutAsync(m_FadeDuration, token);
            }

            context.CameraDirector?.SetActiveCamera(m_TargetCameraKey);

            if (context.ScreenFader != null)
            {
                await context.ScreenFader.FadeInAsync(m_FadeDuration, token);
            }
        }
    }
}

