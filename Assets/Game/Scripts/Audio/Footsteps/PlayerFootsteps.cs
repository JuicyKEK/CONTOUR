using System;
using AZE.AdvancedFirstPerson;
using UnityEngine;

namespace Game.Scripts.Audio.Footsteps
{
    /// <summary>
    /// Шаги игрока. Шаг звучит через каждые N метров пройденного по земле пути: длина шага своя для ходьбы, бега и
    /// приседа, громкость тоже (присед тише). Приземление после прыжка/падения - тоже шаг.
    ///
    /// Поверхность под ногами определяется только в момент шага: один луч вниз и одно обращение к кешу
    /// <see cref="FootstepSurfaceResolver"/> (поиск метки/материалов для каждого коллайдера - один раз).
    /// Звук - PlayOneShot на одном источнике, без создания объектов: шаги не мешают друг другу и не нагружают кадр.
    /// </summary>
    public class PlayerFootsteps : MonoBehaviour
    {
        [SerializeField] private PlayerMovementStateMachine m_Movement;
        [Tooltip("Источник звука шагов (2D - шаги самого игрока).")]
        [SerializeField] private AudioSource m_AudioSource;
        [SerializeField] private FootstepSurfaceLibrarySO m_Library;

        [Header("Длина шага (метры пути на один звук)")]
        [SerializeField, Min(0.1f)] private float m_WalkStepLength = 1.2f;
        [SerializeField, Min(0.1f)] private float m_SprintStepLength = 1.8f;
        [SerializeField, Min(0.1f)] private float m_CrouchStepLength = 0.8f;

        [Header("Громкость")]
        [SerializeField, Range(0f, 1f)] private float m_WalkVolume = 0.8f;
        [SerializeField, Range(0f, 1f)] private float m_SprintVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float m_CrouchVolume = 0.35f;
        [SerializeField, Range(0f, 1f)] private float m_LandingVolume = 1f;

        [Header("Прочее")]
        [Tooltip("Ниже этой скорости (м/с) игрок стоит - шагов нет.")]
        [SerializeField, Min(0f)] private float m_MinSpeed = 0.3f;
        [Tooltip("Сколько секунд игрок должен провести в воздухе, чтобы приземление прозвучало шагом.")]
        [SerializeField, Min(0f)] private float m_MinAirTimeForLanding = 0.25f;
        [Tooltip("Слои пола для определения поверхности.")]
        [SerializeField] private LayerMask m_GroundMask = ~(1 << 2);
        [Tooltip("Насколько ниже ног ищется пол.")]
        [SerializeField, Min(0.1f)] private float m_GroundProbeDistance = 1f;

        private FootstepSurfaceResolver m_Resolver;
        private Vector3 m_LastPosition;
        private float m_DistanceSinceStep;
        private float m_AirTime;
        private bool m_WasGrounded = true;
        private bool m_WasMoving;
        private AudioClip m_LastClip;

        /// <summary>Сколько шагов прозвучало (для отладки и проверок).</summary>
        public int StepCount { get; private set; }

        /// <summary>Поверхность последнего шага.</summary>
        public FootstepSurfaceSO LastSurface { get; private set; }

        public event Action<FootstepSurfaceSO> Stepped;

        private void Awake()
        {
            if (m_Movement == null)
            {
                m_Movement = GetComponent<PlayerMovementStateMachine>();
            }

            m_Resolver = new FootstepSurfaceResolver(m_Library);
            m_LastPosition = transform.position;
        }

        private void OnEnable()
        {
            m_LastPosition = transform.position;
            m_DistanceSinceStep = 0f;
        }

        private void Update()
        {
            var position = transform.position;
            var moved = position - m_LastPosition;
            moved.y = 0f;
            m_LastPosition = position;

            if (m_Movement == null || m_Movement.Controller == null)
            {
                return;
            }

            if (!m_Movement.IsGrounded)
            {
                m_AirTime += Time.deltaTime;
                m_WasGrounded = false;
                return;
            }

            if (!m_WasGrounded)
            {
                m_WasGrounded = true;

                if (m_AirTime >= m_MinAirTimeForLanding)
                {
                    PlayStep(m_LandingVolume, false);
                    m_DistanceSinceStep = 0f;
                }
            }

            m_AirTime = 0f;

            var velocity = m_Movement.Controller.velocity;
            velocity.y = 0f;

            if (velocity.magnitude < m_MinSpeed)
            {
                m_WasMoving = false;
                return;
            }

            GetStep(out float stepLength, out float volume, out bool isRunning);

            // Первый шаг после остановки - через полшага, а не через целый: так звук совпадает с началом движения.
            if (!m_WasMoving)
            {
                m_WasMoving = true;
                m_DistanceSinceStep = stepLength * 0.5f;
            }

            // Телепорт (чекпоинт, загрузка) - не шаги.
            if (moved.magnitude > 2f)
            {
                return;
            }

            m_DistanceSinceStep += moved.magnitude;

            if (m_DistanceSinceStep >= stepLength)
            {
                m_DistanceSinceStep -= stepLength;
                PlayStep(volume, isRunning);
            }
        }

        private void GetStep(out float stepLength, out float volume, out bool isRunning)
        {
            bool isCrouching = m_Movement.TargetHeight < m_Movement.GetStandingHeight() - 0.01f;
            isRunning = m_Movement.IsSprinting && !isCrouching;

            if (isCrouching)
            {
                stepLength = m_CrouchStepLength;
                volume = m_CrouchVolume;
            }
            else if (isRunning)
            {
                stepLength = m_SprintStepLength;
                volume = m_SprintVolume;
            }
            else
            {
                stepLength = m_WalkStepLength;
                volume = m_WalkVolume;
            }
        }

        private void PlayStep(float volume, bool isRunning)
        {
            var surface = ResolveSurface();

            if (surface == null || m_AudioSource == null)
            {
                return;
            }

            var clip = surface.PickClip(isRunning, m_LastClip);

            if (clip == null)
            {
                return;
            }

            m_LastClip = clip;
            m_AudioSource.pitch = surface.RandomPitch();
            m_AudioSource.PlayOneShot(clip, volume * surface.Volume);
            StepCount++;
            LastSurface = surface;
            Stepped?.Invoke(surface);
        }

        private FootstepSurfaceSO ResolveSurface()
        {
            var origin = transform.position + Vector3.up * 0.3f;

            if (Physics.Raycast(origin, Vector3.down, out var hit, 0.3f + m_GroundProbeDistance, m_GroundMask, QueryTriggerInteraction.Ignore))
            {
                return m_Resolver.Resolve(hit.collider);
            }

            return m_Resolver.Default;
        }
    }
}
