using System;
using DG.Tweening;
using JuicyDI.Attributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.ItemInspection.View
{
    /// <summary>
    /// Интерфейс осмотра предмета (<see cref="Controllers.ItemInspectionController"/>): название предмета под ним,
    /// подсказка и кнопка "Принять". Пока осмотра нет, объект выключен. Появлением и исчезновением управляет контроллер.
    /// </summary>
    [JDIMonoController]
    public class ItemInspectionView : MonoBehaviour
    {
        [Tooltip("Прозрачность и клики всего интерфейса осмотра.")]
        [SerializeField] private CanvasGroup m_CanvasGroup;
        [Tooltip("Название предмета.")]
        [SerializeField] private TMP_Text m_NameText;
        [Tooltip("Кнопка \"Принять\" - то же, что Esc: предмет улетает в инвентарь.")]
        [SerializeField] private Button m_AcceptButton;

        private Action m_OnAccept;

        public float Alpha => m_CanvasGroup != null ? m_CanvasGroup.alpha : 0f;

        public void Init(Action onAccept)
        {
            m_OnAccept = onAccept;

            if (m_AcceptButton != null)
            {
                m_AcceptButton.onClick.AddListener(OnAcceptClicked);
            }

            Hide();
        }

        public void Show(string itemName)
        {
            if (m_NameText != null)
            {
                m_NameText.text = itemName;
            }

            gameObject.SetActive(true);
            SetAlpha(0f);
            SetInteractable(true);
        }

        public void Hide()
        {
            SetAlpha(0f);
            SetInteractable(false);
            gameObject.SetActive(false);
        }

        public void SetAlpha(float alpha)
        {
            if (m_CanvasGroup != null)
            {
                m_CanvasGroup.alpha = alpha;
            }
        }

        public Tween Fade(float alpha, float duration)
        {
            return m_CanvasGroup.DOFade(alpha, duration);
        }

        public void SetInteractable(bool isInteractable)
        {
            if (m_CanvasGroup != null)
            {
                m_CanvasGroup.interactable = isInteractable;
                m_CanvasGroup.blocksRaycasts = isInteractable;
            }
        }

        private void OnAcceptClicked()
        {
            m_OnAccept?.Invoke();
        }

        private void OnDestroy()
        {
            if (m_AcceptButton != null)
            {
                m_AcceptButton.onClick.RemoveListener(OnAcceptClicked);
            }
        }
    }
}
