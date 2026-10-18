using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Scripts.Instructions.View
{
    /// <summary>
    /// Полоса прогресса кассеты (обычный Slider от 0 до 1) с перемоткой. Пока игрок держит ползунок,
    /// полоса не обновляется от проигрывания и перемотки не происходит - кассета перематывается, когда
    /// ползунок отпустили. Клик по полосе и управление с клавиатуры/геймпада перематывают сразу.
    ///
    /// Вешать не обязательно: <see cref="CassettePlayerView"/> сам добавит компонент на объект Slider.
    /// </summary>
    [RequireComponent(typeof(Slider))]
    public class CassetteSeekSliderView : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        private Slider m_Slider;
        private Action<float> m_OnSeek;
        private bool m_IsDragging;

        /// <summary>
        /// Игрок держит ползунок - показывать прогресс проигрывания сейчас не нужно.
        /// </summary>
        public bool IsDragging => m_IsDragging;

        private void Awake()
        {
            EnsureSlider();
        }

        public void Setup(Action<float> onSeek)
        {
            EnsureSlider();
            m_OnSeek = onSeek;
        }

        /// <summary>
        /// Показать прогресс проигрывания (0..1). Пока игрок держит ползунок, ничего не делает.
        /// </summary>
        public void SetProgress(float progress)
        {
            if (m_IsDragging || m_Slider == null)
            {
                return;
            }

            m_Slider.SetValueWithoutNotify(Mathf.Clamp01(progress));
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            m_IsDragging = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!m_IsDragging)
            {
                return;
            }

            m_IsDragging = false;
            m_OnSeek?.Invoke(m_Slider.value);
        }

        private void OnDisable()
        {
            // Панель закрыли, не отпустив ползунок - перематываем туда, где его оставили.
            if (m_IsDragging)
            {
                OnPointerUp(null);
            }
        }

        private void EnsureSlider()
        {
            if (m_Slider != null)
            {
                return;
            }

            m_Slider = GetComponent<Slider>();
            m_Slider.minValue = 0f;
            m_Slider.maxValue = 1f;
            m_Slider.wholeNumbers = false;
            m_Slider.onValueChanged.AddListener(OnValueChanged);
        }

        private void OnValueChanged(float value)
        {
            // Во время перетаскивания перематываем только по отпусканию.
            if (!m_IsDragging)
            {
                m_OnSeek?.Invoke(value);
            }
        }
    }
}
