using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.Instructions.View
{
    /// <summary>
    /// Одна строка кассеты в списке: название (или "?????", если кассета не найдена)
    /// и кнопка, по которой запускается проигрывание (неактивна для ненайденных кассет).
    /// Метка "новая" пропадает после первого проигрывания, метки "искажённая" и "задача выполнена"
    /// видны всегда, пока верны.
    /// </summary>
    public class CassetteTapeRowView : MonoBehaviour
    {
        [SerializeField] private TMP_Text m_TapeTitle;
        [SerializeField] private Button m_Button;
        [SerializeField] private GameObject m_InformationNew;
        [SerializeField] private GameObject m_InformationEvel;
        [Tooltip("Метка 'задача с кассеты выполнена'.")]
        [SerializeField] private GameObject m_InformationCompleted;

        private Action m_OnClick;
        private bool m_IsFound;
        private bool m_IsNew;

        private void Awake()
        {
            if (m_Button != null)
            {
                m_Button.onClick.AddListener(HandleClick);
            }
        }

        /// <summary>
        /// Настраивает строку. Если кассета не найдена - текст "?????", кнопка неактивна,
        /// метки не показываются (чтобы не раскрывать, какая кассета искажённая/выполненная).
        /// </summary>
        public void Setup(CassetteTapeRowData data, Action onClick)
        {
            bool isFound = data.IsFound;

            if (m_TapeTitle != null)
            {
                m_TapeTitle.text = isFound ? data.DisplayName : "?????";
            }

            m_OnClick = onClick;
            m_IsFound = isFound;
            m_IsNew = !data.IsListened;

            if (m_Button != null)
            {
                m_Button.interactable = isFound;
            }

            SetMarkActive(m_InformationEvel, isFound && data.IsEvil);
            SetMarkActive(m_InformationCompleted, isFound && data.IsCompleted);
            SetInformationNew();
        }

        private static void SetMarkActive(GameObject mark, bool isActive)
        {
            if (mark != null)
            {
                mark.SetActive(isActive);
            }
        }

        private void HandleClick()
        {
            // Прослушанной кассету отмечает контроллер (в реестре, оттуда она попадёт в сохранение),
            // а здесь метка прячется сразу, не дожидаясь перестроения страницы.
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

