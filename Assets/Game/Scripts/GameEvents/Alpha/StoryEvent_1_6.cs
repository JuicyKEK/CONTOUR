using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Сюжетное событие "удар": экран затемняется, камера резко отклоняется в
/// сторону (будто игрока толкнули/ударили), пока экран закрыт чёрной пеленой
/// игрок мгновенно телепортируется на заданную точку, после чего пелена
/// спадает, а камера плавно возвращается в нормальное положение.
///
/// Вызывается извне (например из UnityEvent сюжетного триггера/NPC) методом
/// <see cref="TriggerHitAndTeleport"/>.
/// </summary>
public class StoryEvent_1_6 : MonoBehaviour
{
    [Header("Игрок")]
    [Tooltip("Что телепортировать. Если не задано - используется этот же GameObject.")]
    [SerializeField] private Transform m_PlayerRoot;

    [Tooltip("Камера игрока, которую нужно наклонить в момент удара. Если не задана - берётся Camera.main.")]
    [SerializeField] private Transform m_CameraTransform;

    [Header("Точка телепортации")]
    [SerializeField] private Transform m_TeleportTarget;

    [Header("Затемнение экрана")]
    [Tooltip("CanvasGroup чёрного полноэкранного Image. Если не задан - будет создан автоматически в Awake (изначально выключен, alpha = 0).")]
    [SerializeField] private CanvasGroup m_FadeCanvasGroup;
    [SerializeField] private float m_FadeInDuration = 0.15f;
    [SerializeField] private float m_FadeOutDuration = 0.35f;
    [Tooltip("Сколько секунд экран остаётся полностью чёрным (в этот момент и происходит телепорт).")]
    [SerializeField] private float m_BlackScreenHold = 0.25f;

    [Header("Наклон камеры")]
    [Tooltip("На сколько градусов камера отклоняется по оси Z (крен), имитируя удар.")]
    [SerializeField] private float m_TiltAngle = 20f;
    [SerializeField] private float m_TiltOutDuration = 0.35f;

    private Coroutine m_Routine;
    private Quaternion m_OriginalCameraRotation; // зафиксирована один раз, а не считывается заново при каждом запуске

    private void Awake()
    {
        if (m_PlayerRoot == null)
        {
            m_PlayerRoot = transform;
        }

        if (m_CameraTransform == null && Camera.main != null)
        {
            m_CameraTransform = Camera.main.transform;
        }

        if (m_CameraTransform != null)
        {
            m_OriginalCameraRotation = m_CameraTransform.localRotation;
        }

        EnsureFadeCanvasGroup();
        m_FadeCanvasGroup.alpha = 0f;
        m_FadeCanvasGroup.gameObject.SetActive(false);
    }

    public void TriggerHitAndTeleport()
    {
        if (m_Routine != null)
        {
            StopCoroutine(m_Routine);

            // Предыдущий запуск мог быть прерван на середине наклона/фейда -
            // принудительно возвращаем камеру и пелену к нейтральному состоянию,
            // чтобы новый запуск точно стартовал "с нуля", а не суммировал наклон.
            SetCameraRotation(m_OriginalCameraRotation);
            SetFadeAlpha(0f);
            m_FadeCanvasGroup.gameObject.SetActive(false);
        }

        m_Routine = StartCoroutine(HitAndTeleportRoutine());
    }

    private IEnumerator HitAndTeleportRoutine()
    {
        m_FadeCanvasGroup.gameObject.SetActive(true);

        Quaternion tiltedRotation = m_OriginalCameraRotation * Quaternion.Euler(0f, 0f, m_TiltAngle);

        // ---- Фаза 1: затемнение + резкий наклон камеры ("удар") ----
        yield return Animate(m_FadeInDuration, t =>
        {
            SetFadeAlpha(t);
            SetCameraRotation(Quaternion.Slerp(m_OriginalCameraRotation, tiltedRotation, t));
        });

        SetFadeAlpha(1f);
        //SetCameraRotation(tiltedRotation);

        // ---- Фаза 2: экран полностью чёрный - телепортируем игрока ----
        if (m_BlackScreenHold > 0f)
        {
            yield return new WaitForSeconds(m_BlackScreenHold);
        }

        TeleportPlayer();

        // ---- Фаза 3: пелена спадает, камера плавно возвращается в норму ----
        yield return Animate(m_FadeOutDuration, t =>
        {
            SetFadeAlpha(1f - t);
        });

        // yield return Animate(m_TiltOutDuration, t =>
        // {
        //     SetCameraRotation(Quaternion.Slerp(tiltedRotation, m_OriginalCameraRotation, t));
        // });

        SetFadeAlpha(0f);
        //SetCameraRotation(m_OriginalCameraRotation);

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

    private void SetCameraRotation(Quaternion rotation)
    {
        if (m_CameraTransform != null)
        {
            m_CameraTransform.localRotation = rotation;
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
    /// чёрный Image поверх всего интерфейса. Вызывается один раз из Awake -
    /// сам объект и его alpha приводятся к исходному состоянию сразу после.
    /// </summary>
    private void EnsureFadeCanvasGroup()
    {
        if (m_FadeCanvasGroup != null)
        {
            return;
        }

        var canvasGo = new GameObject("StoryEvent_1_6_FadeCanvas");
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