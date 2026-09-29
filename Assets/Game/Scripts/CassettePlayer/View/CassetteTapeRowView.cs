using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.Instructions.View
{
    /// <summary>
    /// Одна строка кассеты в списке: название (или "?????", если кассета не найдена)
    /// и кнопка, по которой запускается проигрывание (неактивна для ненайденных кассет).
    /// Метка "новая" пропадает после первого проигрывания, метка "искажённая" видна всегда.
    /// </summary>
    public class CassetteTapeRowView : MonoBehaviour
    {
        [SerializeField] private TMP_Text m_TapeTitle;
        [SerializeField] private Button m_Button;
        [SerializeField] private GameObject m_InformationNew;
        [SerializeField] private GameObject m_InformationEvel;

        private Action m_OnClick;
        private bool m_IsFound;
        private bool m_IsNew = true;

        private void Awake()
        {
            if (m_Button != null)
            {
                m_Button.onClick.AddListener(HandleClick);
            }
        }

        /// <summary>
        /// Настраивает строку. Если кассета не найдена - текст "?????", кнопка неактивна,
        /// метки не показываются (чтобы не раскрывать, какая кассета искажённая).
        /// </summary>
        public void Setup(string displayName, bool isFound, bool isEvil, Action onClick)
        {
            if (m_TapeTitle != null)
            {
                m_TapeTitle.text = isFound ? displayName : "?????";
            }

            m_OnClick = onClick;
            m_IsFound = isFound;

            if (m_Button != null)
            {
                m_Button.interactable = isFound;
            }

            if (m_InformationEvel != null)
            {
                m_InformationEvel.SetActive(isFound && isEvil);
            }

            SetInformationNew();
        }

        private void HandleClick()
        {
            m_OnClick?.Invoke();
            if (m_IsFound)
            {
                m_IsNew = false;
            }
            
            SetInformationNew();
        }

        private void SetInformationNew()
        {
            m_InformationNew.SetActive(m_IsFound && m_IsNew);
        }
    }
}

