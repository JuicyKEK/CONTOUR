using Game.Scripts.NPC.Runtime.Data;
using UnityEngine;

namespace Game.Scripts.NPC.Runtime.Core
{
    /// <summary>
    /// Звуки бота на трёх источниках:
    ///  - действия по ключу (как раньше: "Chase", "Attack"...; зацикленный звук ключа звучит, пока не придёт другой);
    ///  - шаги: по пройденному пути (спокойный шаг и бег звучат разными звуками и с разной частотой)
    ///    или из событий анимации (Footstep);
    ///  - голос: свой для каждого состояния поведения (сопение в покое, рык в погоне...).
    /// Источники шагов и голоса бот создаёт сам (3D-звук), если они не заданы в NpcController.
    /// </summary>
    public class NpcAudioPlayer
    {
        private readonly AudioSource m_ActionSource;
        private readonly AudioSource m_FootstepSource;
        private readonly AudioSource m_VoiceSource;

        private NpcAnimationSoundProfileSO m_Profile;
        private float m_DistanceSinceStep;
        private Vector3 m_LastPosition;
        private bool m_HasLastPosition;

        private NpcVoiceEntry m_Voice;
        private bool m_HasVoice;
        private float m_VoiceTimer;

        public NpcAudioPlayer(AudioSource actionSource, AudioSource footstepSource, AudioSource voiceSource)
        {
            m_ActionSource = actionSource;
            m_FootstepSource = footstepSource;
            m_VoiceSource = voiceSource;
        }

        public NpcAnimationSoundProfileSO Profile => m_Profile;

        public void SetProfile(NpcAnimationSoundProfileSO profile)
        {
            m_Profile = profile;
        }

        /// <summary>
        /// Звук действия по ключу (поведение как у старого PlayAnimationSound: зацикленный звук заменяет текущий).
        /// </summary>
        public void PlayAction(NpcAnimationSoundEntry entry)
        {
            // Выключенный бот звук не играет - при включении состояние перезапустится и включит его заново.
            if (m_ActionSource == null || entry.Sound == null || !m_ActionSource.isActiveAndEnabled)
            {
                return;
            }

            if (entry.LoopSound)
            {
                m_ActionSource.loop = true;
                m_ActionSource.clip = entry.Sound;
                m_ActionSource.Play();
                return;
            }

            // Разовый звук не обрывает текущий зацикленный (дыхание, бег) - играет поверх.
            if (m_ActionSource.isPlaying && m_ActionSource.loop)
            {
                m_ActionSource.PlayOneShot(entry.Sound);
                return;
            }

            m_ActionSource.loop = false;
            m_ActionSource.clip = entry.Sound;
            m_ActionSource.Play();
        }

        /// <summary>
        /// Состояние поведения сменилось - сменить голос.
        /// </summary>
        public void OnStateChanged(NpcStateId state)
        {
            bool hadLoop = m_HasVoice && m_Voice.Loop;
            m_HasVoice = m_Profile != null && m_Profile.TryGetVoice(state, out m_Voice);

            if (m_VoiceSource == null)
            {
                return;
            }

            if (!m_HasVoice)
            {
                if (hadLoop)
                {
                    m_VoiceSource.Stop();
                }

                return;
            }

            if (m_Voice.Loop)
            {
                var clip = RandomClip(m_Voice.Clips);

                if (m_VoiceSource.clip != clip || !m_VoiceSource.isPlaying || !m_VoiceSource.loop)
                {
                    m_VoiceSource.loop = true;
                    m_VoiceSource.clip = clip;
                    m_VoiceSource.volume = m_Voice.EffectiveVolume;
                    m_VoiceSource.Play();
                }

                return;
            }

            if (hadLoop)
            {
                m_VoiceSource.Stop();
            }

            m_VoiceSource.loop = false;
            // Первый звук нового состояния - сразу (например рык в начале погони), дальше - с паузами.
            m_VoiceTimer = 0f;
        }

        public void StopAll()
        {
            if (m_VoiceSource != null)
            {
                m_VoiceSource.Stop();
            }

            if (m_FootstepSource != null)
            {
                m_FootstepSource.Stop();
            }

            m_HasLastPosition = false;
        }

        public void Tick(Vector3 position, float speed, float deltaTime)
        {
            TickFootsteps(position, speed);
            TickVoice(deltaTime);
        }

        /// <summary>
        /// Шаг из события анимации (NpcAnimationEventRelay.Footstep).
        /// </summary>
        public void PlayFootstep(float speed)
        {
            if (m_Profile == null || m_FootstepSource == null)
            {
                return;
            }

            bool isRunning = speed >= m_Profile.RunSpeedThreshold;
            var clip = RandomClip(isRunning ? m_Profile.RunSteps : m_Profile.WalkSteps);

            if (clip == null)
            {
                return;
            }

            m_FootstepSource.pitch = Random.Range(0.92f, 1.08f);
            m_FootstepSource.PlayOneShot(clip, m_Profile.StepVolume);
        }

        private void TickFootsteps(Vector3 position, float speed)
        {
            if (!m_HasLastPosition)
            {
                m_LastPosition = position;
                m_HasLastPosition = true;
                return;
            }

            var moved = position - m_LastPosition;
            moved.y = 0f;
            m_LastPosition = position;

            if (m_Profile == null || m_Profile.FootstepsFromAnimation)
            {
                return;
            }

            m_DistanceSinceStep += moved.magnitude;
            bool isRunning = speed >= m_Profile.RunSpeedThreshold;
            float stepDistance = isRunning ? m_Profile.RunStepDistance : m_Profile.WalkStepDistance;

            if (m_DistanceSinceStep >= stepDistance)
            {
                m_DistanceSinceStep = 0f;
                PlayFootstep(speed);
            }
        }

        private void TickVoice(float deltaTime)
        {
            if (!m_HasVoice || m_Voice.Loop || m_VoiceSource == null)
            {
                return;
            }

            // Пауза считается после окончания предыдущего звука.
            if (m_VoiceSource.isPlaying)
            {
                return;
            }

            m_VoiceTimer -= deltaTime;

            if (m_VoiceTimer > 0f)
            {
                return;
            }

            var clip = RandomClip(m_Voice.Clips);

            if (clip != null)
            {
                m_VoiceSource.clip = clip;
                m_VoiceSource.volume = m_Voice.EffectiveVolume;
                m_VoiceSource.pitch = Random.Range(0.95f, 1.05f);
                m_VoiceSource.Play();
            }

            m_VoiceTimer = Random.Range(Mathf.Min(m_Voice.MinInterval, m_Voice.MaxInterval), Mathf.Max(m_Voice.MinInterval, m_Voice.MaxInterval));
        }

        private static AudioClip RandomClip(AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0)
            {
                return null;
            }

            return clips[Random.Range(0, clips.Length)];
        }
    }
}
