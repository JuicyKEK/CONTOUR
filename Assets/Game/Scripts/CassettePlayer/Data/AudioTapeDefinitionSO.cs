using UnityEngine;

namespace Game.Scripts.Instructions.Data
{
    /// <summary>
    /// Описание одной аудио-кассеты: уникальный ключ (для отслеживания "найдена/не найдена"),
    /// отображаемое имя (показывается в списке плеера, если кассета найдена) и сам аудиофайл,
    /// который проигрывается при выборе кассеты в списке.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Instructions/Audio Tape", fileName = "AudioTape_")]
    public class AudioTapeDefinitionSO : ScriptableObject
    {
        [Tooltip("Уникальный ключ кассеты. Используется для хранения прогресса 'найдена/не найдена'.")]
        [SerializeField] private string m_TapeId;
        [Tooltip("Отображаемое имя кассеты в списке плеера (видно только если кассета найдена).")]
        [SerializeField] private string m_DisplayName;
        [Tooltip("Аудиофайл, который проигрывается при выборе этой кассеты.")]
        [SerializeField] private AudioClip m_Clip;
        [Tooltip("Доступна ли кассета на старте")]
        [SerializeField] private bool m_OnStart;
        [Tooltip("Является ли данная кассета искаженной")]
        [SerializeField] private bool m_IsEvilCassette;

        public string TapeId => m_TapeId;
        public string DisplayName => m_DisplayName;
        public AudioClip Clip => m_Clip;
        public bool OnStart => m_OnStart;
        public bool IsEvilCassette => m_IsEvilCassette;
    }
}

