using UnityEngine;
using UnityEngine.UI;

public class SprintBarView : MonoBehaviour
{
    [SerializeField] private bool hideBarWhenFull = true;
    [SerializeField] private CanvasGroup sprintBarCG;
    [SerializeField] private Image sprintBarBG;
    [SerializeField] private Image sprintBar;
    [SerializeField] private float sprintBarWidthPercent = .3f;
    [SerializeField] private float sprintBarHeightPercent = .015f;

    [Header("Fade")]
    [Tooltip("Скорость появления/скрытия полоски (в единицах альфы в секунду).")]
    [SerializeField] private float fadeSpeed = 5f;

    [Header("Colors")]
    [Tooltip("Цвет полоски, когда стамина не израсходована полностью.")]
    [SerializeField] private Color normalColor = Color.white;
    [Tooltip("Цвет полоски, когда стамина полностью израсходована (обнулилась).")]
    [SerializeField] private Color depletedColor = new Color(0.4f, 0f, 0f, 1f);

    private float sprintBarWidth;
    private float sprintBarHeight;

    private void Start()
    {
        sprintBarBG.gameObject.SetActive(true);
        sprintBar.gameObject.SetActive(true);

        float screenWidth = Screen.width;
        float screenHeight = Screen.height;

        sprintBarWidth = screenWidth * sprintBarWidthPercent;
        sprintBarHeight = screenHeight * sprintBarHeightPercent;

        sprintBarBG.rectTransform.sizeDelta = new Vector3(sprintBarWidth, sprintBarHeight, 0f);
        sprintBar.rectTransform.sizeDelta = new Vector3(sprintBarWidth - 2, sprintBarHeight - 2, 0f);

        if (sprintBar != null)
        {
            sprintBar.color = normalColor;
        }

        if (hideBarWhenFull && sprintBarCG != null)
        {
            sprintBarCG.alpha = 0;
        }
    }

    /// <summary>
    /// Обновляет заполнение, видимость и цвет полоски спринта.
    /// Полоска скрыта, пока стамина полная, появляется при первом расходе.
    /// Цвет становится тёмно-красным, как только стамина обнулилась (isDepleted == true),
    /// и возвращается в исходный (normalColor) только когда стамина восстановилась
    /// ПОЛНОСТЬЮ (isDepleted == false), а не при первом же приросте выше нуля.
    /// </summary>
    public void SprintBarViewUpdate(float sprintRemaining, float sprintDuration, bool isDepleted)
    {
        float percent = sprintDuration > 0f ? Mathf.Clamp01(sprintRemaining / sprintDuration) : 0f;

        if (sprintBar != null)
        {
            sprintBar.transform.localScale = new Vector3(percent, 1f, 1f);
        }

        if (hideBarWhenFull && sprintBarCG != null)
        {
            bool isFull = percent >= 1f;
            float targetAlpha = isFull ? 0f : 1f;
            sprintBarCG.alpha = Mathf.MoveTowards(sprintBarCG.alpha, targetAlpha, fadeSpeed * Time.deltaTime);
        }

        if (sprintBar != null)
        {
            sprintBar.color = isDepleted ? depletedColor : normalColor;
        }
    }
}

