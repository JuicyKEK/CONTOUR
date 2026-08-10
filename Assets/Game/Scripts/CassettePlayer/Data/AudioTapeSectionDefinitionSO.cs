using UnityEngine;

namespace Game.Scripts.Instructions.Data
{
    /// <summary>
    /// Раздел списка кассетного плеера (например "Дневники", "Радиопередачи").
    /// Содержит набор кассет, которые относятся к этому разделу.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Instructions/Audio Tape Section", fileName = "AudioTapeSection_")]
    public class AudioTapeSectionDefinitionSO : ScriptableObject
    {
        [Tooltip("Отображаемое имя раздела в UI плеера.")]
        [SerializeField] private string m_DisplayName;

        [Tooltip("Кассеты, входящие в этот раздел (порядок совпадает с порядком в UI).")]
        [SerializeField] private AudioTapeDefinitionSO[] m_Tapes;

        public string DisplayName => m_DisplayName;
        public AudioTapeDefinitionSO[] Tapes => m_Tapes;
    }
}

