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
        // Виньетка: прозрачный круг в центре (до VignetteClear по расстоянию от центра в текстуре), к VignetteBlack - чёрный.
        private const int VignetteSize = 128;
        private const float VignetteClear = 0.3f;
        private const float VignetteBlack = 0.5f;
        // Радиус прозрачного круга (в высотах экрана от центра) при виньетке 0 - у углов экрана: едва начавшись, она уже
        // трогает углы, к 0.3 заметно темнит края.
        private const float VignetteOpenRadius = 0.9f;

        private static CanvasGroup s_Overlay;
        private static RawImage s_Vignette;
        private static Texture2D s_VignetteTexture;

        public static async UniTask FadeOutAsync(float duration, CancellationToken token)
        {
            var fader = BinController.GetContext()?.GetBean<IScreenFader>();

            if (fader != null)
            {
                await fader.FadeOutAsync(duration, token);

                // Экран уже чёрный от fader'а - своё затемнение и виньетку (PlayDarknessAsync) убираем, чтобы не остались
                // после возврата.
                ClearDarkness();
                return;
            }

            var overlay = EnsureOverlay();
            float startAlpha = overlay.alpha;
            float time = 0f;

            while (time < duration)
            {
                token.ThrowIfCancellationRequested();
                time += Time.unscaledDeltaTime;
                overlay.alpha = Mathf.Lerp(startAlpha, 1f, Mathf.Clamp01(time / duration));
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }

            overlay.alpha = 1f;
        }

        /// <summary>
        /// Затемнение и виньетка по кривым (по горизонтали - секунды от начала, по вертикали - 0..1) до момента until -
        /// своим слоем поверх всего. darkness - чёрный экран целиком (мерцание), vignette - чёрное кольцо от краёв к
        /// центру (1 - сомкнулось, экран чёрный). Любая из кривых может быть пустой. Время берётся из clipTime (время
        /// анимации, под которую сделаны кривые), чтобы всё шло ровно с картинкой. После until слой остаётся как есть -
        /// дальше его ведёт FadeOutAsync. Если прервали - слой убирается.
        /// </summary>
        public static async UniTask PlayDarknessAsync(AnimationCurve darkness, AnimationCurve vignette, float until,
            System.Func<float> clipTime, CancellationToken token)
        {
            bool hasDarkness = darkness != null && darkness.length > 0;
            bool hasVignette = vignette != null && vignette.length > 0;

            if ((!hasDarkness && !hasVignette) || clipTime == null)
            {
                return;
            }

            var overlay = EnsureOverlay();

            try
            {
                float time = clipTime();

                while (time < until)
                {
                    token.ThrowIfCancellationRequested();
                    overlay.alpha = hasDarkness ? Mathf.Clamp01(darkness.Evaluate(time)) : 0f;
                    SetVignette(s_Vignette, hasVignette ? vignette.Evaluate(time) : 0f);
                    await UniTask.Yield(PlayerLoopTiming.Update, token);
                    time = clipTime();
                }

                overlay.alpha = hasDarkness ? Mathf.Clamp01(darkness.Evaluate(until)) : 0f;
                SetVignette(s_Vignette, hasVignette ? vignette.Evaluate(until) : 0f);
            }
            catch (System.OperationCanceledException)
            {
                ClearDarkness();
                throw;
            }
        }

        /// <summary>Текущая прозрачность своего слоя затемнения (для отладки и проверок; 0 - слоя нет).</summary>
        public static float OverlayAlpha => s_Overlay != null ? s_Overlay.alpha : 0f;

        /// <summary>Текущая виньетка своего слоя (для отладки и проверок; 0 - нет, 1 - сомкнулась).</summary>
        public static float VignetteAmount { get; private set; }

        /// <summary>
        /// Показать виньетку на RawImage во весь экран: amount 0 - нет, 1 - сомкнулась (весь экран чёрный). Общая для
        /// игры и сцены просмотра противников, чтобы выглядела одинаково.
        /// </summary>
        public static void SetVignette(RawImage image, float amount)
        {
            if (image == null)
            {
                return;
            }

            amount = Mathf.Clamp01(amount);

            if (image == s_Vignette)
            {
                VignetteAmount = amount;
            }

            if (amount <= 0f)
            {
                image.enabled = false;
                return;
            }

            image.enabled = true;
            image.texture = GetVignetteTexture();

            if (amount >= 0.999f)
            {
                // Сомкнулась: вся картинка берётся за краем текстуры, где она чёрная.
                image.uvRect = new Rect(2f, 2f, 1f, 1f);
                return;
            }

            // Прозрачный круг радиусом radius (в высотах экрана) - текстура растягивается так, что её VignetteClear
            // приходится на radius.
            float radius = Mathf.Lerp(VignetteOpenRadius, 0f, amount);
            float scale = VignetteClear / Mathf.Max(radius, 0.0001f);
            var size = image.rectTransform.rect.size;
            float aspect = size.y > 0f ? size.x / size.y : (float)Screen.width / Mathf.Max(1, Screen.height);
            image.uvRect = new Rect(0.5f - scale * aspect * 0.5f, 0.5f - scale * 0.5f, scale * aspect, scale);
        }

        /// <summary>Текстура виньетки: чёрная, прозрачная в центре, по краю (и за краем) непрозрачная.</summary>
        public static Texture2D GetVignetteTexture()
        {
            if (s_VignetteTexture != null)
            {
                return s_VignetteTexture;
            }

            s_VignetteTexture = new Texture2D(VignetteSize, VignetteSize, TextureFormat.RGBA32, false)
            {
                name = "NpcVignette",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.DontSave,
            };

            var pixels = new Color32[VignetteSize * VignetteSize];

            for (int y = 0; y < VignetteSize; y++)
            {
                for (int x = 0; x < VignetteSize; x++)
                {
                    float dx = (x + 0.5f) / VignetteSize - 0.5f;
                    float dy = (y + 0.5f) / VignetteSize - 0.5f;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(VignetteClear, VignetteBlack, distance));
                    pixels[y * VignetteSize + x] = new Color32(0, 0, 0, (byte)Mathf.RoundToInt(alpha * 255f));
                }
            }

            s_VignetteTexture.SetPixels32(pixels);
            s_VignetteTexture.Apply(false, false);
            return s_VignetteTexture;
        }

        private static void ClearDarkness()
        {
            if (s_Overlay != null)
            {
                s_Overlay.alpha = 0f;
            }

            SetVignette(s_Vignette, 0f);
        }

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
            if (s_Overlay != null && s_Vignette != null)
            {
                return s_Overlay;
            }

            // Свой слой: виньетка (под ним) и чёрный экран (сверху, прозрачность - CanvasGroup).
            var root = new GameObject("NpcDeathFade");
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue;

            s_Vignette = Stretch(new GameObject("Vignette", typeof(RectTransform), typeof(RawImage)), root).GetComponent<RawImage>();
            s_Vignette.color = Color.white;
            s_Vignette.raycastTarget = false;
            s_Vignette.enabled = false;
            VignetteAmount = 0f;

            var black = Stretch(new GameObject("Black", typeof(RectTransform), typeof(Image), typeof(CanvasGroup)), root);
            var image = black.GetComponent<Image>();
            image.color = Color.black;
            image.raycastTarget = false;
            s_Overlay = black.GetComponent<CanvasGroup>();
            s_Overlay.alpha = 0f;
            s_Overlay.blocksRaycasts = false;
            s_Overlay.interactable = false;
            return s_Overlay;
        }

        private static GameObject Stretch(GameObject child, GameObject parent)
        {
            child.transform.SetParent(parent.transform, false);
            var rect = (RectTransform)child.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return child;
        }
    }
}
