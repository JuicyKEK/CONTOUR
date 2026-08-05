using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Сюжетное событие "мгновенное перемещение": экран затемняется, пока он
/// полностью чёрный - игрок телепортируется на заданную точку, после чего
/// пелена спадает. Без наклона камеры (см. StoryEvent_1_6 - там ещё и "удар").
///
/// Вызывается извне (например из UnityEvent сюжетного триггера/NPC) методом
/// <see cref="TriggerFadeTeleport"/>.
/// </summary>
public class StoryEvent_FadeTeleport : MonoBehaviour
{
    [Header("Игрок")]
    [Tooltip("Что телепортировать. Если не задано - используется этот же GameObject.")]
    [SerializeField] private Transform m_PlayerRoot;

    [Header("Точка телепортации")]
    [SerializeField] private Transform m_TeleportTarget;

    [Header("Затемнение экрана")]
    [Tooltip("CanvasGroup чёрного полноэкранного Image. Если не задан - будет создан автоматически в Awake (изначально выключен, alpha = 0).")]
    [SerializeField] private CanvasGroup m_FadeCanvasGroup;
    [SerializeField] private float m_FadeInDuration = 0.25f;
    [SerializeField] private float m_FadeOutDuration = 0.35f;
    [Tooltip("Сколько секунд экран остаётся полностью чёрным (в этот момент и происходит телепорт).")]
    [SerializeField] private float m_BlackScreenHold = 0.25f;

    private Coroutine m_Routine;

    private void Awake()
    {
        if (m_PlayerRoot == null)
        {
            m_PlayerRoot = transform;
        }

        // Создаём (если не назначен в инспекторе) и заранее готовим оверлей
        // затемнения: alpha = 0, сам GameObject выключен.
        EnsureFadeCanvasGroup();
        m_FadeCanvasGroup.alpha = 0f;
        m_FadeCanvasGroup.gameObject.SetActive(false);
    }

    /// <summary>
    /// Запускает эффект: затемнение -> телепорт -> снятие затемнения.
    /// Безопасно вызывать повторно - предыдущий незавершённый запуск будет прерван.
    /// </summary>
    public void TriggerFadeTeleport()
    {
        if (m_Routine != null)
        {
            StopCoroutine(m_Routine);
        }

        m_Routine = StartCoroutine(FadeTeleportRoutine());
    }

    private IEnumerator FadeTeleportRoutine()
    {
        m_FadeCanvasGroup.gameObject.SetActive(true);

        // ---- Фаза 1: затемнение ----
        yield return Animate(m_FadeInDuration, SetFadeAlpha);

        SetFadeAlpha(1f);

        // ---- Фаза 2: экран полностью чёрный - телепортируем игрока ----
        if (m_BlackScreenHold > 0f)
        {
            yield return new WaitForSeconds(m_BlackScreenHold);
        }

        TeleportPlayer();

        // ---- Фаза 3: пелена спадает ----
        yield return Animate(m_FadeOutDuration, t => SetFadeAlpha(1f - t));

        SetFadeAlpha(0f);
        m_FadeCanvasGroup.gameObject.SetActive(false);

        m_Routine = null;
    }

    private void TeleportPlayer()
    {
        if (m_PlayerRoot == null || m_TeleportTarget == null)
        {
            return;
        }

        var characterController = m_PlayerRoot.GetComponent<CharacterController>();

        if (characterController != null)
        {
            characterController.enabled = false;
            m_PlayerRoot.SetPositionAndRotation(m_TeleportTarget.position, m_TeleportTarget.rotation);
            characterController.enabled = true;
        }
        else
        {
            m_PlayerRoot.SetPositionAndRotation(m_TeleportTarget.position, m_TeleportTarget.rotation);
        }
    }

    private void SetFadeAlpha(float alpha)
    {
        m_FadeCanvasGroup.alpha = Mathf.Clamp01(alpha);
    }

    private static IEnumerator Animate(float duration, System.Action<float> onProgress)
    {
        if (duration <= 0f)
        {
            onProgress(1f);
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            onProgress(Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        onProgress(1f);
    }

    /// <summary>
    /// Если m_FadeCanvasGroup не назначен в инспекторе - создаёт полноэкранный
    /// чёрный Image поверх всего интерфейса. Вызывается один раз из Awake.
    /// </summary>
    private void EnsureFadeCanvasGroup()
    {
        if (m_FadeCanvasGroup != null)
        {
            return;
        }

        var canvasGo = new GameObject("StoryEvent_FadeTeleport_FadeCanvas");
        canvasGo.transform.SetParent(transform, false);

        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;

        var imageGo = new GameObject("FadeImage");
        imageGo.transform.SetParent(canvasGo.transform, false);

        var image = imageGo.AddComponent<Image>();
        image.color = Color.black;
        image.raycastTarget = false;

        var rectTransform = image.rectTransform;
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        m_FadeCanvasGroup = imageGo.AddComponent<CanvasGroup>();
        m_FadeCanvasGroup.blocksRaycasts = false;
        m_FadeCanvasGroup.interactable = false;
    }
}