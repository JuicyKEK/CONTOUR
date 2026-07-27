using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.InfectionZone.External.View
{
    /// <summary>
    /// Чисто визуальный HUD "Уровень заражения {Имя зоны}:" со слайдером (0..100) вверху экрана.
    /// Сам по себе ничего не решает "когда показывать" - только предоставляет примитивы
    /// (заголовок, мгновенная установка значения, анимация значения, fade всего блока),
    /// которыми управляет <see cref="Controllers.InfectionLevelHudController"/>.
    /// </summary>
    public class InfectionLevelHudView : MonoBehaviour
    {
        [Header("Ссылки")]
        [SerializeField] private CanvasGroup m_CanvasGroup;
        [SerializeField] private TMP_Text m_TitleText;
        [SerializeField] private Slider m_Slider;

        [Header("Текст")]
        [Tooltip("Формат заголовка. {0} подставляется именем текущей зоны.")]
        [SerializeField] private string m_TitleFormat = "Уровень заражения {0}:";

        private void Awake()
        {
            if (m_Slider != null)
            {
                m_Slider.minValue = 0f;
                m_Slider.maxValue = 100f;
            }
        }

        public void SetTitle(string zoneDisplayName)
        {
            if (m_TitleText != null)
            {
                m_TitleText.text = string.Format(m_TitleFormat, zoneDisplayName);
            }
        }

        public void SetSliderValueImmediate(float value)
        {
            if (m_Slider != null)
            {
                m_Slider.value = value;
            }
        }

        /// <summary>
        /// Мгновенно показать/скрыть HUD без анимации (используется при старте, чтобы
        /// HUD не мигал на экране в первом кадре до первого реального показа).
        /// </summary>
        public void SetVisibleInstant(bool isVisible)
        {
            if (m_CanvasGroup == null)
            {
                return;
            }

            DOTween.Kill(m_CanvasGroup);
            m_CanvasGroup.alpha = isVisible ? 1f : 0f;
            m_CanvasGroup.blocksRaycasts = isVisible;
            m_CanvasGroup.interactable = isVisible;
        }

        /// <summary>
        /// Твин плавного появления/исчезания всего блока HUD (fade по CanvasGroup.alpha).
        /// </summary>
        public Tween FadeTween(bool fadeIn, float duration)
        {
            if (m_CanvasGroup == null)
            {
                return DOTween.Sequence();
            }

            m_CanvasGroup.blocksRaycasts = fadeIn;
            m_CanvasGroup.interactable = fadeIn;

            return m_CanvasGroup.DOFade(fadeIn ? 1f : 0f, duration);
        }

        /// <summary>
        /// Твин плавной анимации значения слайдера от fromValue к toValue.
        /// </summary>
        public Tween SliderValueTween(float fromValue, float toValue, float duration)
        {
            if (m_Slider == null)
            {
                return DOTween.Sequence();
            }

            m_Slider.value = fromValue;
            return m_Slider.DOValue(toValue, duration).SetEase(Ease.InOutSine);
        }
    }
}

