using UnityEngine;

namespace Game.Scripts.Instructions.Data
{
    /// <summary>
    /// Страница списка кассетного плеера (в UI - отдельный тогл и отдельный ScrollView).
    /// Содержит набор разделов, которые относятся к этой странице.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Instructions/Audio Tape Page", fileName = "AudioTapePage_")]
    public class AudioTapePageSO : ScriptableObject
    {
        [Tooltip("Разделы, входящие в эту страницу (порядок совпадает с порядком в UI).")]
        [SerializeField] private AudioTapeSectionDefinitionSO[] m_Sections;
        [Tooltip("Название страницы (отображается на тогле страницы).")]
        [SerializeField] private string m_PageName;
        [Tooltip("Цвет страницы (применяется к тоглу страницы).")]
        [SerializeField] private Color m_PageColor = Color.white;

        public AudioTapeSectionDefinitionSO[] Sections => m_Sections;
        public string PageName => m_PageName;
        public Color PageColor => m_PageColor;
    }
}
