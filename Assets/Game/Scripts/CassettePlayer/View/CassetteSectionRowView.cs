using TMPro;
using UnityEngine;

namespace Game.Scripts.Instructions.View
{
    /// <summary>
    /// Заголовок раздела списка кассет (например "Дневники") и контейнер, в который
    /// контроллер помещает строки кассет этого раздела (<see cref="CassetteTapeRowView"/>).
    /// </summary>
    public class CassetteSectionRowView : MonoBehaviour
    {
        [SerializeField] private TMP_Text m_SectionTitle;
        [SerializeField] private Transform m_TapesContainer;

        public Transform TapesContainer => m_TapesContainer;

        public void Setup(string displayName)
        {
            if (m_SectionTitle != null)
            {
                m_SectionTitle.text = displayName;
            }
        }
    }
}

