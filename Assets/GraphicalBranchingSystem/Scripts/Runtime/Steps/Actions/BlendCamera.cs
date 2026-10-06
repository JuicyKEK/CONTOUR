using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Scripts.Story;
using UnityEngine;

namespace GBS.Steps
{
    /// <summary>
    /// Плавная смена камеры: затемнение -> переключение активной камеры по ключу (ICameraDirector) -> осветление.
    /// Без IScreenFader на сцене камера переключается резко.
    /// </summary>
    [Serializable, GBSMenu("Camera/Blend Camera")]
    public class BlendCamera : GBSAction
    {
        [Tooltip("Ключ камеры из CameraDirector на сцене.")]
        [SerializeField] private string m_CameraKey;
        [SerializeField, Min(0f)] private float m_FadeDuration = 0.5f;

        public BlendCamera()
        {
        }

        public BlendCamera(string cameraKey, float fadeDuration)
        {
            m_CameraKey = cameraKey;
            m_FadeDuration = fadeDuration;
        }

        public override async UniTask ExecuteAsync(StoryContext context, CancellationToken token)
        {
            var cameraDirector = context.CameraDirector;

            if (cameraDirector == null)
            {
                Debug.LogWarning($"[GBS] Blend Camera: на сцене нет ICameraDirector (CameraDirector) - камера '{m_CameraKey}' не включена.");
                return;
            }

            var screenFader = context.ScreenFader;

            if (screenFader != null)
            {
                await screenFader.FadeOutAsync(m_FadeDuration, token);
            }

            cameraDirector.SetActiveCamera(m_CameraKey);

            if (screenFader != null)
            {
                await screenFader.FadeInAsync(m_FadeDuration, token);
            }
        }
    }
}
