using UnityEngine;

namespace Game.Scripts.Scares
{
    /// <summary>
    /// Лицо, которое что-то высматривает и не может сфокусироваться: глаза рывком переводят взгляд на новую точку,
    /// проскакивают её и поправляются, на точке дрожат и чуть расходятся друг с другом, а через пару секунд ищут
    /// следующую. Челюсть медленно приоткрывается и так же медленно закрывается - зубы смыкаются со стуком.
    ///
    /// По сюжету (StorySceneReactions или UnityEvent) лицо переводится в режимы:
    /// StartChatter - громко стучит зубами, уставившись на игрока; StartScream - кричит, широко раскрыв рот;
    /// Calm - возвращается к обычному поведению. Звуки - в AudioSource режимов (клипы задаются в инспекторе).
    ///
    /// Углы задаются в осях этого объекта: +Z - куда смотрит лицо, +Y - к макушке, +X - вправо от лица.
    /// Глаза и челюсть поворачиваются относительно положения, в котором были при запуске.
    /// </summary>
    public class StaringFaceAnimator : MonoBehaviour
    {
        [Header("Глаза")]
        [Tooltip("Опора левого глаза - поворачивается вокруг своей точки (центра глаза).")]
        [SerializeField] private Transform m_LeftEye;

        [Tooltip("Опора правого глаза - поворачивается вокруг своей точки (центра глаза).")]
        [SerializeField] private Transform m_RightEye;

        [Tooltip("Насколько далеко в стороны может уйти взгляд, градусы.")]
        [SerializeField] private float m_LookYaw = 28f;

        [Tooltip("Насколько далеко вверх и вниз может уйти взгляд, градусы.")]
        [SerializeField] private float m_LookPitch = 18f;

        [Tooltip("Наименьший скачок взгляда между точками, градусы - чтобы перевод взгляда был заметен.")]
        [SerializeField] private float m_MinJump = 10f;

        [Tooltip("Сколько секунд глаза пытаются рассмотреть одну точку: от и до.")]
        [SerializeField] private Vector2 m_FixationTime = new(0.7f, 2.2f);

        [Tooltip("Скорость рывка глаз, градусов в секунду.")]
        [SerializeField] private float m_SaccadeSpeed = 600f;

        [Tooltip("Перелёт при рывке: доля скачка, на которую глаза проскакивают точку, прежде чем вернуться к ней.")]
        [SerializeField] [Range(0f, 0.5f)] private float m_Overshoot = 0.15f;

        [Tooltip("Через сколько секунд после перелёта глаза возвращаются к точке.")]
        [SerializeField] private float m_CorrectionDelay = 0.07f;

        [Tooltip("Дрожание взгляда на точке, градусы - не может на ней сфокусироваться.")]
        [SerializeField] private float m_Jitter = 1.6f;

        [Tooltip("Сколько раз в секунду взгляд вздрагивает на точке.")]
        [SerializeField] private float m_JitterRate = 8f;

        [Tooltip("Насколько глаза смотрят мимо друг друга (не сводятся на точке), градусы.")]
        [SerializeField] private float m_Disagreement = 2.5f;

        [Header("Челюсть")]
        [Tooltip("Кость челюсти.")]
        [SerializeField] private Transform m_Jaw;

        [Tooltip("Ось, вокруг которой открывается челюсть, в осях самой кости челюсти; знак задаёт сторону открывания. " +
                 "Поворот - в осях кости: так он верен и при неравномерном масштабе модели.")]
        [SerializeField] private Vector3 m_JawAxis = Vector3.right;

        [Tooltip("Насколько приоткрывается рот, градусы: от и до.")]
        [SerializeField] private Vector2 m_JawOpen = new(2.5f, 5f);

        [Tooltip("Сколько секунд рот открывается (и примерно столько же закрывается): от и до.")]
        [SerializeField] private Vector2 m_JawTime = new(0.9f, 1.6f);

        [Tooltip("Сколько секунд рот держится приоткрытым: от и до.")]
        [SerializeField] private Vector2 m_JawHold = new(0.1f, 0.3f);

        [Tooltip("Пауза с сомкнутыми зубами между открываниями, секунды: от и до.")]
        [SerializeField] private Vector2 m_JawPause = new(0.2f, 0.9f);

        [Tooltip("Отскок челюсти, когда зубы стукнулись, градусы.")]
        [SerializeField] private float m_TapBounce = 0.8f;

        [Tooltip("Сколько мелких стуков добавить после смыкания: от и до (целые).")]
        [SerializeField] private Vector2Int m_ExtraTaps = new(0, 1);

        [Header("Взгляд на игрока (стук зубами и крик)")]
        [Tooltip("На что смотреть. Пусто - на камеру игрока (Camera.main), если её нет - на объект с тегом Player.")]
        [SerializeField] private Transform m_PlayerTarget;

        [Tooltip("Дальше этого угла от прямого взгляда глаза за игроком не поворачиваются, градусы.")]
        [SerializeField] private float m_PlayerLookLimit = 45f;

        [Tooltip("Что трясти в режимах стука и крика (обычно модель головы). Пусто - не трясти.")]
        [SerializeField] private Transform m_ShakeTarget;

        [Header("Стук зубами (по сюжету)")]
        [Tooltip("Сколько ударов зубов в секунду.")]
        [SerializeField] private float m_ChatterRate = 13f;

        [Tooltip("Насколько рот открывается при каждом ударе, градусы: от и до.")]
        [SerializeField] private Vector2 m_ChatterOpen = new(1.5f, 4.5f);

        [Tooltip("Тряска головы при стуке, метры.")]
        [SerializeField] private float m_ChatterShake = 0.03f;

        [Tooltip("Звук стука (зацикленный клип). Включается и выключается вместе с режимом.")]
        [SerializeField] private AudioSource m_ChatterAudio;

        [Header("Крик (по сюжету)")]
        [Tooltip("Насколько широко раскрывается рот при крике, градусы.")]
        [SerializeField] private float m_ScreamOpen = 30f;

        [Tooltip("За сколько секунд рот раскрывается для крика.")]
        [SerializeField] private float m_ScreamOpenTime = 0.35f;

        [Tooltip("Сколько секунд длится крик; потом лицо успокаивается. 0 - кричит, пока не вызовут Calm.")]
        [SerializeField] private float m_ScreamDuration = 3.5f;

        [Tooltip("Дрожь раскрытой челюсти при крике, градусы.")]
        [SerializeField] private float m_ScreamTremble = 1.5f;

        [Tooltip("Тряска головы при крике, метры.")]
        [SerializeField] private float m_ScreamShake = 0.06f;

        [Tooltip("Звук крика. Запускается в начале крика.")]
        [SerializeField] private AudioSource m_ScreamAudio;

        /// <summary>Что сейчас делает лицо.</summary>
        public enum FaceMode
        {
            Idle,
            Chatter,
            Scream,
        }

        private enum JawPhase
        {
            Pause,
            Opening,
            Hold,
            Closing,
            Bounce,
        }

        private Quaternion m_LeftRest, m_RightRest, m_JawRest;
        private Vector3 m_ShakeRest;
        private Vector2 m_Gaze, m_Target, m_Aim;
        private Vector2 m_JitterLeft, m_JitterRight, m_JitterLeftGoal, m_JitterRightGoal;
        private Vector2 m_SpreadLeft, m_SpreadRight;
        private float m_FixationLeft, m_CorrectionTimer, m_NextJitter;
        private bool m_Correcting;

        private JawPhase m_JawPhase;
        private float m_JawTimer, m_JawDuration, m_JawAmplitude, m_JawAngle;
        private int m_TapsLeft;
        private bool m_SmallTap;

        private FaceMode m_Mode = FaceMode.Idle;
        private float m_ModeTime, m_TapPhase, m_TapAmplitude, m_ScreamStartAngle;

        /// <summary>Текущий угол челюсти, градусы (для отладки).</summary>
        public float JawAngle => m_JawAngle;

        /// <summary>Куда сейчас смотрят глаза в среднем: (вправо, вверх), градусы (для отладки).</summary>
        public Vector2 Gaze => m_Gaze;

        /// <summary>Текущий режим лица.</summary>
        public FaceMode Mode => m_Mode;

        private void Awake()
        {
            var inverse = Quaternion.Inverse(transform.rotation);
            if (m_LeftEye != null) m_LeftRest = inverse * m_LeftEye.rotation;
            if (m_RightEye != null) m_RightRest = inverse * m_RightEye.rotation;
            if (m_Jaw != null) m_JawRest = m_Jaw.localRotation;
            if (m_ShakeTarget != null) m_ShakeRest = m_ShakeTarget.localPosition;
            m_FixationLeft = Random.Range(m_FixationTime.x, m_FixationTime.y);
            StartJawPause();
        }

        private void Update()
        {
            Step(Time.deltaTime);
        }

        /// <summary>Громко стучит зубами, уставившись на игрока (по сюжету).</summary>
        [ContextMenu("Стучать зубами")]
        public void StartChatter()
        {
            SetMode(FaceMode.Chatter);
        }

        /// <summary>Кричит, широко раскрыв рот (по сюжету).</summary>
        [ContextMenu("Кричать")]
        public void StartScream()
        {
            SetMode(FaceMode.Scream);
        }

        /// <summary>Обычное поведение: высматривает, медленно постукивает зубами.</summary>
        [ContextMenu("Успокоиться")]
        public void Calm()
        {
            SetMode(FaceMode.Idle);
        }

        private void SetMode(FaceMode mode)
        {
            m_Mode = mode;
            m_ModeTime = 0f;
            m_TapPhase = 1f;
            m_ScreamStartAngle = m_JawAngle;
            if (mode == FaceMode.Idle)
            {
                StartJawPause();
                if (m_ShakeTarget != null) m_ShakeTarget.localPosition = m_ShakeRest;
            }

            if (m_ChatterAudio != null)
            {
                if (mode == FaceMode.Chatter && m_ChatterAudio.clip != null)
                {
                    m_ChatterAudio.loop = true;
                    m_ChatterAudio.Play();
                }
                else
                {
                    m_ChatterAudio.Stop();
                }
            }

            if (m_ScreamAudio != null)
            {
                if (mode == FaceMode.Scream && m_ScreamAudio.clip != null)
                {
                    m_ScreamAudio.Play();
                }
                else if (mode != FaceMode.Scream)
                {
                    m_ScreamAudio.Stop();
                }
            }
        }

        private void Step(float dt)
        {
            m_ModeTime += dt;
            if (m_Mode == FaceMode.Idle)
            {
                UpdateGaze(dt);
                UpdateJaw(dt);
            }
            else
            {
                UpdatePlayerGaze(dt);
                if (m_Mode == FaceMode.Chatter) UpdateChatter(dt);
                else UpdateScream();
                Shake(m_Mode == FaceMode.Chatter ? m_ChatterShake : m_ScreamShake);
                if (m_Mode == FaceMode.Scream && m_ScreamDuration > 0f && m_ModeTime >= m_ScreamDuration)
                {
                    Calm();
                }
            }

            Apply();
        }

        private void UpdateGaze(float dt)
        {
            m_FixationLeft -= dt;
            if (m_FixationLeft <= 0f)
            {
                // Следующая точка: не ближе m_MinJump к текущей.
                var next = m_Target;
                for (int i = 0; i < 8 && Vector2.Distance(next, m_Target) < m_MinJump; i++)
                {
                    next = new Vector2(Random.Range(-m_LookYaw, m_LookYaw), Random.Range(-m_LookPitch, m_LookPitch));
                }

                m_Aim = next + (next - m_Target) * m_Overshoot;
                m_Target = next;
                m_Correcting = true;
                m_CorrectionTimer = m_CorrectionDelay;
                m_FixationLeft = Random.Range(m_FixationTime.x, m_FixationTime.y);
                m_SpreadLeft = Random.insideUnitCircle * m_Disagreement;
                m_SpreadRight = Random.insideUnitCircle * m_Disagreement;
            }

            m_Gaze = Vector2.MoveTowards(m_Gaze, m_Aim, m_SaccadeSpeed * dt);
            if (m_Correcting && m_Gaze == m_Aim)
            {
                m_CorrectionTimer -= dt;
                if (m_CorrectionTimer <= 0f)
                {
                    m_Aim = m_Target;
                    m_Correcting = false;
                }
            }

            UpdateJitter(dt, m_Jitter);
        }

        /// <summary>Глаза на игроке: без поиска, только мелкая дрожь.</summary>
        private void UpdatePlayerGaze(float dt)
        {
            var target = PlayerPoint();
            if (target.HasValue)
            {
                var eyes = m_LeftEye != null && m_RightEye != null ? (m_LeftEye.position + m_RightEye.position) * 0.5f : transform.position;
                var local = Quaternion.Inverse(transform.rotation) * (target.Value - eyes);
                float yaw = Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg;
                float pitch = Mathf.Atan2(local.y, new Vector2(local.x, local.z).magnitude) * Mathf.Rad2Deg;
                var wanted = Vector2.ClampMagnitude(new Vector2(yaw, pitch), m_PlayerLookLimit);
                m_Target = wanted;
                m_Aim = wanted;
            }

            m_Gaze = Vector2.MoveTowards(m_Gaze, m_Aim, m_SaccadeSpeed * dt);
            m_SpreadLeft = Vector2.MoveTowards(m_SpreadLeft, Vector2.zero, 10f * dt);
            m_SpreadRight = Vector2.MoveTowards(m_SpreadRight, Vector2.zero, 10f * dt);
            UpdateJitter(dt, m_Jitter * 0.4f);
        }

        private Vector3? PlayerPoint()
        {
            if (m_PlayerTarget != null) return m_PlayerTarget.position;
            var camera = Camera.main;
            if (camera != null) return camera.transform.position;
            var player = GameObject.FindGameObjectWithTag("Player");
            return player != null ? player.transform.position + Vector3.up * 1.6f : null;
        }

        private void UpdateJitter(float dt, float amount)
        {
            // На точке взгляд вздрагивает: мелкие рывки в случайную сторону, у каждого глаза свои.
            m_NextJitter -= dt;
            if (m_NextJitter <= 0f)
            {
                m_JitterLeftGoal = Random.insideUnitCircle * amount + m_SpreadLeft;
                m_JitterRightGoal = Random.insideUnitCircle * amount + m_SpreadRight;
                m_NextJitter = Random.Range(0.5f, 1.5f) / Mathf.Max(0.01f, m_JitterRate);
            }

            float micro = m_SaccadeSpeed * 0.5f * dt;
            m_JitterLeft = Vector2.MoveTowards(m_JitterLeft, m_JitterLeftGoal, micro);
            m_JitterRight = Vector2.MoveTowards(m_JitterRight, m_JitterRightGoal, micro);
        }

        /// <summary>Частые удары зубов: каждый - быстрое приоткрывание и смыкание, сила каждый раз своя.</summary>
        private void UpdateChatter(float dt)
        {
            m_TapPhase += dt * Mathf.Max(0.1f, m_ChatterRate);
            if (m_TapPhase >= 1f)
            {
                m_TapPhase -= Mathf.Floor(m_TapPhase);
                m_TapAmplitude = Random.value < 0.12f ? 0f : Random.Range(m_ChatterOpen.x, m_ChatterOpen.y);
            }

            m_JawAngle = m_TapAmplitude * Mathf.Sin(Mathf.PI * m_TapPhase);
        }

        /// <summary>Крик: рот быстро раскрывается во всю ширину и дрожит.</summary>
        private void UpdateScream()
        {
            float t = m_ScreamOpenTime > 0f ? Mathf.Clamp01(m_ModeTime / m_ScreamOpenTime) : 1f;
            float open = Mathf.Lerp(m_ScreamStartAngle, m_ScreamOpen, 1f - (1f - t) * (1f - t));
            float tremble = m_ScreamTremble * (Mathf.Sin(m_ModeTime * 113f) * 0.6f + Mathf.Sin(m_ModeTime * 71f) * 0.4f) * t;
            m_JawAngle = open + tremble;
        }

        private void Shake(float amount)
        {
            if (m_ShakeTarget == null || amount <= 0f)
            {
                return;
            }

            var noise = new Vector3(Mathf.PerlinNoise(m_ModeTime * 23f, 0.1f) - 0.5f, Mathf.PerlinNoise(0.3f, m_ModeTime * 19f) - 0.5f, Mathf.PerlinNoise(m_ModeTime * 17f, 0.7f) - 0.5f);
            m_ShakeTarget.localPosition = m_ShakeRest + noise * (2f * amount);
        }

        private void UpdateJaw(float dt)
        {
            m_JawTimer += dt;
            float t = m_JawDuration > 0f ? Mathf.Clamp01(m_JawTimer / m_JawDuration) : 1f;
            switch (m_JawPhase)
            {
                case JawPhase.Pause:
                    m_JawAngle = 0f;
                    if (t >= 1f) StartPhase(JawPhase.Opening, Random.Range(m_JawTime.x, m_JawTime.y));
                    break;
                case JawPhase.Opening:
                    m_JawAngle = m_JawAmplitude * Mathf.SmoothStep(0f, 1f, t);
                    if (t >= 1f) StartPhase(JawPhase.Hold, m_SmallTap ? 0.02f : Random.Range(m_JawHold.x, m_JawHold.y));
                    break;
                case JawPhase.Hold:
                    m_JawAngle = m_JawAmplitude;
                    if (t >= 1f) StartPhase(JawPhase.Closing, m_SmallTap ? 0.1f : Random.Range(m_JawTime.x, m_JawTime.y));
                    break;
                case JawPhase.Closing:
                    // Медленно, к концу быстрее - зубы смыкаются со стуком.
                    m_JawAngle = m_JawAmplitude * (1f - t * t);
                    if (t >= 1f) StartPhase(JawPhase.Bounce, 0.09f);
                    break;
                case JawPhase.Bounce:
                    m_JawAngle = m_TapBounce * Mathf.Sin(Mathf.PI * t);
                    if (t >= 1f)
                    {
                        if (m_TapsLeft > 0)
                        {
                            // Ещё один мелкий стук: чуть приоткрыть и сомкнуть.
                            m_TapsLeft--;
                            m_SmallTap = true;
                            m_JawAmplitude = m_TapBounce * 1.8f;
                            StartPhase(JawPhase.Opening, 0.1f);
                        }
                        else
                        {
                            StartJawPause();
                        }
                    }

                    break;
            }
        }

        private void StartJawPause()
        {
            m_SmallTap = false;
            m_JawAmplitude = Random.Range(m_JawOpen.x, m_JawOpen.y);
            m_TapsLeft = Random.Range(m_ExtraTaps.x, m_ExtraTaps.y + 1);
            StartPhase(JawPhase.Pause, Random.Range(m_JawPause.x, m_JawPause.y));
        }

        private void StartPhase(JawPhase phase, float duration)
        {
            m_JawPhase = phase;
            m_JawTimer = 0f;
            m_JawDuration = duration;
        }

        private void Apply()
        {
            var frame = transform.rotation;
            if (m_LeftEye != null) m_LeftEye.rotation = frame * Look(m_Gaze + m_JitterLeft) * m_LeftRest;
            if (m_RightEye != null) m_RightEye.rotation = frame * Look(m_Gaze + m_JitterRight) * m_RightRest;
            if (m_Jaw != null) m_Jaw.localRotation = m_JawRest * Quaternion.AngleAxis(m_JawAngle, m_JawAxis);
        }

        /// <summary>Поворот взгляда: x - вправо, y - вверх, градусы.</summary>
        private static Quaternion Look(Vector2 angles) => Quaternion.Euler(-angles.y, angles.x, 0f);

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            foreach (var eye in new[] { m_LeftEye, m_RightEye })
            {
                if (eye != null)
                {
                    Gizmos.DrawLine(eye.position, eye.position + eye.forward * 0.6f);
                }
            }
        }
    }
}
