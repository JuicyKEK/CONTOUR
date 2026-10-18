using System;
using System.Collections.Generic;
using Game.Scripts.NPC.Runtime.Core;
using UnityEngine;

namespace Game.Scripts.NPC.Runtime.Data
{
    [Serializable]
    public struct NpcAnimationSoundEntry
    {
        [Tooltip("Логический ключ, по которому код запрашивает анимацию/звук (например 'Patrol', 'Chase', 'Attack', 'Transform', 'Eat').")]
        public string Key;

        [Tooltip("Имя триггера Animator, который срабатывает по этому ключу. Пусто - если в контроллере аниматора есть " +
                 "триггер с именем ключа, сработает он.")]
        public string AnimatorTrigger;

        [Tooltip("Опциональный звук, проигрываемый одновременно с анимацией.")]
        public AudioClip Sound;

        [Tooltip("Зациклить звук (например 'дыхание' в покое или 'бег' в погоне) - звучит, пока не придёт другой ключ со звуком.")]
        public bool LoopSound;
    }

    /// <summary>
    /// Голос бота в одном состоянии поведения: сопение, рык, бормотание - у каждого состояния своё.
    /// </summary>
    [Serializable]
    public struct NpcVoiceEntry
    {
        [Tooltip("Состояние поведения, в котором звучит этот голос.")]
        public NpcStateId State;

        [Tooltip("Звуки (выбирается случайный).")]
        public AudioClip[] Clips;

        [Tooltip("Звучать непрерывно (зациклить) вместо отдельных звуков через паузы.")]
        public bool Loop;

        [Tooltip("Пауза (сек) после окончания одного звука до следующего - случайно между min и max.")]
        public float MinInterval;
        public float MaxInterval;

        [Tooltip("Громкость. 0 - полная (как 1).")]
        [Range(0f, 1f)] public float Volume;

        public float EffectiveVolume => Volume > 0f ? Volume : 1f;
    }

    /// <summary>
    /// Анимации и звуки одного типа бота (или одного его облика - см. NpcAppearance):
    ///  - действия по логическому ключу ("Chase", "Attack", "Transform"...) - триггер аниматора и звук;
    ///  - шаги: свои для спокойного шага и для бега;
    ///  - голос по состояниям поведения (в покое сопит, в погоне рычит и т.д.).
    /// Код состояний запрашивает всё только по ключу/состоянию - набор анимаций и звуков меняется данными.
    ///
    /// Параметры аниматора, которые бот выставляет сам каждый кадр, если они есть в контроллере:
    /// Speed (float, м/с), State (int, NpcStateId), IsAggressive (bool), Anger (float 0..1).
    /// </summary>
    [CreateAssetMenu(menuName = "NPC/Animation Sound Profile", fileName = "NpcAnimationSoundProfile")]
    public class NpcAnimationSoundProfileSO : ScriptableObject
    {
        [SerializeField] private NpcAnimationSoundEntry[] m_Entries = Array.Empty<NpcAnimationSoundEntry>();

        [Header("Шаги")]
        [Tooltip("Шаги приходят событиями анимации (функция Footstep у NpcAnimationEventRelay на объекте с Animator) - " +
                 "тогда бот сам их не считает. Выключено - бот звучит шагом каждые N метров пройденного пути.")]
        [SerializeField] private bool m_FootstepsFromAnimation;
        [SerializeField] private AudioClip[] m_WalkSteps = Array.Empty<AudioClip>();
        [Tooltip("Шаги при быстром движении (погоня, бег). Пусто - звучат обычные шаги.")]
        [SerializeField] private AudioClip[] m_RunSteps = Array.Empty<AudioClip>();
        [Tooltip("Со скорости (м/с) звучат шаги бега.")]
        [SerializeField, Min(0f)] private float m_RunSpeedThreshold = 3f;
        [Tooltip("Длина шага (метры) спокойным шагом и бегом.")]
        [SerializeField, Min(0.1f)] private float m_WalkStepDistance = 0.7f;
        [SerializeField, Min(0.1f)] private float m_RunStepDistance = 1.3f;
        [SerializeField, Range(0f, 1f)] private float m_StepVolume = 0.8f;

        [Header("Голос по состояниям")]
        [SerializeField] private NpcVoiceEntry[] m_Voices = Array.Empty<NpcVoiceEntry>();

        [Header("Звук в пространстве")]
        [Tooltip("Для источников шагов и голоса, которые бот создаёт сам: ближе этого расстояния звук на полной громкости...")]
        [SerializeField, Min(0.1f)] private float m_MinDistance = 1f;
        [Tooltip("...и затихает к этому расстоянию.")]
        [SerializeField, Min(0.2f)] private float m_MaxDistance = 20f;

        private Dictionary<string, NpcAnimationSoundEntry> m_Lookup;

        public bool FootstepsFromAnimation => m_FootstepsFromAnimation;
        public AudioClip[] WalkSteps => m_WalkSteps ?? Array.Empty<AudioClip>();
        public AudioClip[] RunSteps => m_RunSteps != null && m_RunSteps.Length > 0 ? m_RunSteps : WalkSteps;
        public float RunSpeedThreshold => m_RunSpeedThreshold;
        public float WalkStepDistance => m_WalkStepDistance;
        public float RunStepDistance => m_RunStepDistance;
        public float StepVolume => m_StepVolume;
        public float MinDistance => m_MinDistance;
        public float MaxDistance => Mathf.Max(m_MaxDistance, m_MinDistance + 0.1f);

        private void OnEnable()
        {
            // Ассет мог поменяться в инспекторе - таблицу ключей собираем заново.
            m_Lookup = null;
        }

        private void OnValidate()
        {
            m_Lookup = null;
        }

        private void EnsureLookup()
        {
            if (m_Lookup != null)
            {
                return;
            }

            m_Lookup = new Dictionary<string, NpcAnimationSoundEntry>(m_Entries?.Length ?? 0);

            if (m_Entries == null)
            {
                return;
            }

            foreach (var entry in m_Entries)
            {
                if (!string.IsNullOrEmpty(entry.Key))
                {
                    m_Lookup[entry.Key] = entry;
                }
            }
        }

        public bool TryGet(string key, out NpcAnimationSoundEntry entry)
        {
            EnsureLookup();

            if (string.IsNullOrEmpty(key))
            {
                entry = default;
                return false;
            }

            return m_Lookup.TryGetValue(key, out entry);
        }

        public bool TryGetVoice(NpcStateId state, out NpcVoiceEntry voice)
        {
            if (m_Voices != null)
            {
                for (int i = 0; i < m_Voices.Length; i++)
                {
                    if (m_Voices[i].State == state && m_Voices[i].Clips != null && m_Voices[i].Clips.Length > 0)
                    {
                        voice = m_Voices[i];
                        return true;
                    }
                }
            }

            voice = default;
            return false;
        }
    }
}
