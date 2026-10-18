using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.Instructions.View
{
    /// <summary>
    /// Тогл страницы кассет: название и цвет страницы. Включение тогла переключает страницу
    /// в <see cref="CassettePlayerView"/>, взаимоисключение тоглов обеспечивает общий ToggleGroup.
    /// </summary>
    public class CassettePageToggleView : MonoBehaviour
    {
        [SerializeField] private Toggle m_Toggle;
        [SerializeField] private TMP_Text m_PageTitle;
        [Tooltip("Графика, которая окрашивается в цвет страницы (например, фон тогла). Можно не назначать.")]
        [SerializeField] private Graphic m_ColorTarget;

        private void Reset()
        {
            m_Toggle = GetComponent<Toggle>();
        }

        public void Setup(string pageName, Color pageColor, ToggleGroup group, Action onSelected)
        {
            if (m_PageTitle != null)
            {
                m_PageTitle.text = pageName;
            }

            if (m_ColorTarget != null)
            {
                m_ColorTarget.color = pageColor;
            }

            if (m_Toggle == null)
            {
                return;
            }

            m_Toggle.group = group;
            m_Toggle.onValueChanged.RemoveAllListeners();
            m_Toggle.onValueChanged.AddListener(isOn =>
            {
                if (isOn)
                {
                    onSelected?.Invoke();
                }
            });
        }

        public void SetIsOnWithoutNotify(bool isOn)
        {
            if (m_Toggle != null)
            {
                m_Toggle.SetIsOnWithoutNotify(isOn);
            }
        }
    }
}
