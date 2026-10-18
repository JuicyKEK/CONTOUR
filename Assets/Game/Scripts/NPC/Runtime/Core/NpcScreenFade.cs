using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Scripts.Story;
using JuicyDI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.Scripts.NPC.Runtime.Core
{
    /// <summary>
    /// Затемнение экрана при смерти игрока и перезагрузка сцены без сохранений. Если в сцене есть IScreenFader -
    /// затемняет он; иначе создаётся свой чёрный слой поверх всего интерфейса (уходит вместе со сценой при загрузке).
    /// </summary>
    public static class NpcScreenFade
    {
        private static CanvasGroup s_Overlay;

        public static async UniTask FadeOutAsync(float duration, CancellationToken token)
        {
            var fader = BinController.GetContext()?.GetBean<IScreenFader>();

            if (fader != null)
            {
                await fader.FadeOutAsync(duration, token);
                return;
            }

            var overlay = EnsureOverlay();
            float time = 0f;

            while (time < duration)
            {
                token.ThrowIfCancellationRequested();
                time += Time.unscaledDeltaTime;
                overlay.alpha = Mathf.Clamp01(time / duration);
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }

            overlay.alpha = 1f;
        }

        /// <summary>Текущая прозрачность своего слоя затемнения (для отладки и проверок; 0 - слоя нет).</summary>
        public static float OverlayAlpha => s_Overlay != null ? s_Overlay.alpha : 0f;

        /// <summary>
        /// Перезапуск текущей сцены - когда игра запущена без ядра с сохранениями (тестовая сцена).
        /// </summary>
        public static void ReloadActiveScene()
        {
            var scene = SceneManager.GetActiveScene();

            if (scene.buildIndex >= 0)
            {
                SceneManager.LoadScene(scene.buildIndex, LoadSceneMode.Single);
                return;
            }

#if UNITY_EDITOR
            // Сцены нет в Build Settings (тестовая) - в редакторе её можно загрузить по пути.
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(scene.path, new LoadSceneParameters(LoadSceneMode.Single));
#else
            Debug.LogWarning($"[NPC] Сцена '{scene.name}' не добавлена в Build Settings - перезагрузить её нельзя.");
#endif
        }

        private static CanvasGroup EnsureOverlay()
        {
            if (s_Overlay != null)
            {
                return s_Overlay;
            }

            var root = new GameObject("NpcDeathFade");
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue;
            s_Overlay = root.AddComponent<CanvasGroup>();
            s_Overlay.alpha = 0f;
            s_Overlay.blocksRaycasts = false;

            var image = new GameObject("Black", typeof(RectTransform), typeof(Image));
            image.transform.SetParent(root.transform, false);
            var rect = (RectTransform)image.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            image.GetComponent<Image>().color = Color.black;
            return s_Overlay;
        }
    }
}
