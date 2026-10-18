using System;
using UnityEngine;

namespace Game.Scripts.Audio.Footsteps
{
    /// <summary>
    /// Звуки шагов по одной поверхности (бетон, металл, дерево, гравий...).
    /// Какая поверхность под ногами - определяет <see cref="FootstepSurfaceLibrarySO"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "Audio/Footstep Surface", fileName = "FootstepSurface")]
    public class FootstepSurfaceSO : ScriptableObject
    {
        [Tooltip("Шаги обычным шагом (выбирается случайный, не повторяя предыдущий).")]
        [SerializeField] private AudioClip[] m_WalkClips = Array.Empty<AudioClip>();
        [Tooltip("Шаги бегом. Пусто - звучат обычные шаги.")]
        [SerializeField] private AudioClip[] m_RunClips = Array.Empty<AudioClip>();
        [SerializeField, Range(0f, 1f)] private float m_Volume = 1f;
        [Tooltip("Разброс высоты звука, чтобы одинаковые шаги не звучали механически.")]
        [SerializeField, Range(0.5f, 2f)] private float m_MinPitch = 0.94f;
        [SerializeField, Range(0.5f, 2f)] private float m_MaxPitch = 1.06f;

        public float Volume => m_Volume;

        public float RandomPitch()
        {
            return UnityEngine.Random.Range(Mathf.Min(m_MinPitch, m_MaxPitch), Mathf.Max(m_MinPitch, m_MaxPitch));
        }

        /// <summary>
        /// Случайный звук шага; если звуков несколько - не тот же, что в прошлый раз.
        /// </summary>
        public AudioClip PickClip(bool isRunning, AudioClip previous)
        {
            var clips = isRunning && m_RunClips != null && m_RunClips.Length > 0 ? m_RunClips : m_WalkClips;

            if (clips == null || clips.Length == 0)
            {
                return null;
            }

            if (clips.Length == 1)
            {
                return clips[0];
            }

            var clip = clips[UnityEngine.Random.Range(0, clips.Length)];

            if (clip == previous)
            {
                clip = clips[(Array.IndexOf(clips, clip) + 1 + UnityEngine.Random.Range(0, clips.Length - 1)) % clips.Length];
            }

            return clip;
        }
    }
}
