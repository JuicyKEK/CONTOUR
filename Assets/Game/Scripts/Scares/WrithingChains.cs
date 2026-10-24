using System.Collections.Generic;
using UnityEngine;

namespace Game.Scripts.Scares
{
    /// <summary>
    /// Шевелит цепочки костей - руки из перепонки, пальцы вместо перил, щупальца: каждая цепочка изгибается
    /// бегущей от основания к кончику волной, иногда вздрагивает и поджимается. У каждой цепочки своя скорость
    /// и фаза, поэтому они не двигаются в такт. Движение непрерывное, без начала и конца - повторов и скачков нет;
    /// вздрагивание тоже плавное (быстро набирается и медленно отпускает).
    /// Цепочка - кость из списка оснований и её дочерние кости до кончика (дочерние объекты с мешем пропускаются).
    /// Повороты считаются от положения костей при запуске.
    ///
    /// Если задана голова (<see cref="m_Face"/>), цепочки отвечают на её режимы: пока голова кричит - хаотично
    /// и резко машут, пока стучит зубами - тянутся к игроку. В новое поведение и обратно переходят плавно.
    /// </summary>
    public class WrithingChains : MonoBehaviour
    {
        [Tooltip("Первые кости цепочек (основания рук, пальцев, щупалец).")]
        [SerializeField] private List<Transform> m_Roots = new();

        [Tooltip("Наибольший изгиб в суставе у кончика, градусы (у основания меньше).")]
        [SerializeField] private float m_Bend = 14f;

        [Tooltip("Скорость шевеления, колебаний в секунду: от и до (у каждой цепочки своя).")]
        [SerializeField] private Vector2 m_Speed = new(0.25f, 0.6f);

        [Tooltip("Насколько скорость каждой цепочки сама плавно меняется со временем: 0 - постоянная, " +
                 "0.5 - от половины до полуторной, 1 - от почти остановки до двойной.")]
        [SerializeField] [Range(0f, 1f)] private float m_SpeedWander;

        [Tooltip("Разброс размаха между цепочками: 0 - у всех одинаковый, 1 - от трети до 1.7 размаха.")]
        [SerializeField] [Range(0f, 1f)] private float m_BendSpread;

        [Tooltip("Сдвиг волны между соседними суставами, доли колебания - волна бежит к кончику.")]
        [SerializeField] private float m_Wave = 0.18f;

        [Tooltip("Сколько раз в секунду (в среднем) цепочка вздрагивает.")]
        [SerializeField] private float m_TwitchRate = 0.35f;

        [Tooltip("Сила вздрагивания, градусы (0 - без вздрагиваний).")]
        [SerializeField] private float m_TwitchStrength = 12f;

        [Tooltip("За сколько секунд вздрагивание набирается и за сколько отпускает.")]
        [SerializeField] private Vector2 m_TwitchSmooth = new(0.12f, 0.35f);

        [Tooltip("Медленное поджимание в одну сторону (как сжимаются пальцы), градусы.")]
        [SerializeField] private float m_Curl = 10f;

        [Header("Ответ на голову (крик и стук зубами)")]
        [Tooltip("Голова, на режимы которой отвечают цепочки: крик - машут, стук зубами - тянутся к игроку. Пусто - не отвечают.")]
        [SerializeField] private StaringFaceAnimator m_Face;

        [Tooltip("Крик: размах взмаха в каждом суставе, градусы (взмахи суставов складываются).")]
        [SerializeField] private float m_FlailBend = 20f;

        [Tooltip("Крик: сколько раз в секунду рука меняет направление взмаха: от и до (у каждого взмаха своё).")]
        [SerializeField] private Vector2 m_FlailRate = new(2.5f, 5.5f);

        [Tooltip("Крик: за сколько секунд рука доходит до нового взмаха - чем меньше, тем резче.")]
        [SerializeField] private float m_FlailSnap = 0.07f;

        [Tooltip("Стук зубами: насколько цепочка поворачивается к игроку: 0 - никак, 1 - прямо на него.")]
        [SerializeField] [Range(0f, 1f)] private float m_ReachStrength = 0.85f;

        [Tooltip("Стук зубами: наибольший поворот к игроку в одном суставе, градусы.")]
        [SerializeField] private float m_ReachMaxAngle = 35f;

        [Tooltip("Стук зубами: насколько стихает обычное шевеление, пока руки тянутся (0 - не стихает, 1 - замирает).")]
        [SerializeField] [Range(0f, 1f)] private float m_ReachCalm = 0.6f;

        [Tooltip("Стук зубами: дрожь тянущихся рук, градусы.")]
        [SerializeField] private float m_ReachTremble = 3f;

        [Tooltip("К чему тянуться. Пусто - к камере игрока (Camera.main).")]
        [SerializeField] private Transform m_ReachTarget;

        [Tooltip("За сколько секунд цепочки переходят в новое поведение и обратно.")]
        [SerializeField] private float m_ReactionBlend = 0.3f;

        private sealed class Chain
        {
            public Transform[] Bones;
            public Quaternion[] Rest;
            public Vector3[] AxisA, AxisB;
            public float Speed, Phase, NextTwitch, Twitch, TwitchTarget, TwitchHold, TwitchVelocity, Cycle, Seed, BendScale;
            public Vector2 Flail, FlailTarget, FlailVelocity;
            public float NextFlail;
        }

        private readonly List<Chain> m_Chains = new();
        private float m_FlailWeight, m_ReachWeight;
        private Vector3 m_ReachPoint, m_ReachVelocity;
        private bool m_HasReachPoint;

        /// <summary>Насколько цепочки сейчас машут (0..1, для отладки).</summary>
        public float FlailWeight => m_FlailWeight;

        /// <summary>Насколько цепочки сейчас тянутся к игроку (0..1, для отладки).</summary>
        public float ReachWeight => m_ReachWeight;

        private void Awake()
        {
            foreach (var root in m_Roots)
            {
                if (root == null)
                {
                    continue;
                }

                var bones = new List<Transform> { root };
                for (var current = root; ;)
                {
                    Transform next = null;
                    foreach (Transform child in current)
                    {
                        if (child.GetComponent<Renderer>() == null)
                        {
                            next = child;
                            break;
                        }
                    }

                    if (next == null) break;
                    bones.Add(next);
                    current = next;
                }

                var chain = new Chain
                {
                    Bones = bones.ToArray(),
                    Rest = new Quaternion[bones.Count],
                    AxisA = new Vector3[bones.Count],
                    AxisB = new Vector3[bones.Count],
                    Speed = Random.Range(m_Speed.x, m_Speed.y),
                    Phase = Random.value,
                    Seed = Random.Range(0f, 100f),
                    BendScale = 1f + Random.Range(-1f, 1f) * m_BendSpread * 0.7f,
                    NextTwitch = Random.Range(0.5f, 3f),
                };
                chain.Cycle = chain.Phase;
                for (int i = 0; i < bones.Count; i++)
                {
                    var bone = bones[i];
                    chain.Rest[i] = bone.localRotation;
                    // Направление кости - к следующей (у последней - как у предыдущей), в её собственных осях.
                    var along = i + 1 < bones.Count ? bones[i + 1].position - bone.position : bone.position - bones[i - 1 >= 0 ? i - 1 : 0].position;
                    var dir = along.sqrMagnitude > 1e-8f ? bone.InverseTransformDirection(along).normalized : Vector3.up;
                    var a = Vector3.Cross(dir, Mathf.Abs(dir.y) < 0.9f ? Vector3.up : Vector3.right).normalized;
                    chain.AxisA[i] = a;
                    chain.AxisB[i] = Vector3.Cross(dir, a).normalized;
                }

                m_Chains.Add(chain);
            }
        }

        private void Update()
        {
            float time = Time.time;
            float dt = Time.deltaTime;
            UpdateReactionWeights(dt);
            float flail = Smooth(m_FlailWeight);
            float reach = Smooth(m_ReachWeight);
            var reachTarget = reach > 0f ? SmoothedReachPoint(dt) : null;
            if (reach <= 0f) m_HasReachPoint = false;
            foreach (var chain in m_Chains)
            {
                chain.NextTwitch -= dt;
                if (chain.NextTwitch <= 0f)
                {
                    chain.TwitchTarget = Random.Range(-1f, 1f) * m_TwitchStrength;
                    chain.TwitchHold = Random.Range(0.15f, 0.4f);
                    chain.NextTwitch = Random.Range(0.3f, 2f) / Mathf.Max(0.05f, m_TwitchRate);
                }

                // Вздрагивание плавно набирается, держится и плавно отпускает - без скачка за один кадр.
                if (chain.TwitchHold > 0f) chain.TwitchHold -= dt;
                else chain.TwitchTarget = 0f;
                float smooth = Mathf.Max(0.01f, chain.TwitchTarget != 0f ? m_TwitchSmooth.x : m_TwitchSmooth.y);
                if (dt > 0f) chain.Twitch = Mathf.SmoothDamp(chain.Twitch, chain.TwitchTarget, ref chain.TwitchVelocity, smooth, Mathf.Infinity, dt);
                UpdateFlail(chain, dt, flail);
                int n = chain.Bones.Length;
                // Фаза копится по шагам: скорость может плавно меняться - без скачков.
                float speed = chain.Speed;
                if (m_SpeedWander > 0f) speed *= 1f + m_SpeedWander * (Mathf.PerlinNoise(time * 0.15f, chain.Seed) - 0.5f) * 2f;
                chain.Cycle += dt * Mathf.Max(0f, speed);
                float cycle = chain.Cycle;
                float bend = m_Bend * chain.BendScale * (1f - m_ReachCalm * reach);
                float curl = m_Curl * (0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * (cycle * 0.37f + 0.3f))) * (1f - m_ReachCalm * reach);
                float tremble = m_ReachTremble * reach;
                for (int i = 0; i < n; i++)
                {
                    float weight = (i + 1f) / n;
                    float wave = 2f * Mathf.PI * (cycle - i * m_Wave);
                    float bendA = bend * weight * Mathf.Sin(wave) + curl * weight + chain.Twitch * weight;
                    float bendB = bend * 0.6f * weight * Mathf.Sin(wave * 0.73f + 1.1f);
                    // Взмахи при крике: у основания тоже сильные - рука машет целиком.
                    float flailWeight = 0.6f + 0.4f * weight;
                    bendA += chain.Flail.x * flailWeight;
                    bendB += chain.Flail.y * flailWeight;
                    if (tremble > 0f)
                    {
                        bendA += tremble * (Mathf.PerlinNoise(time * 9f, chain.Seed + i) - 0.5f) * 2f;
                        bendB += tremble * (Mathf.PerlinNoise(chain.Seed - i, time * 8f) - 0.5f) * 2f;
                    }

                    chain.Bones[i].localRotation = chain.Rest[i] * Quaternion.AngleAxis(bendA, chain.AxisA[i]) * Quaternion.AngleAxis(bendB, chain.AxisB[i]);
                }

                if (reachTarget.HasValue)
                {
                    Reach(chain, reachTarget.Value, reach);
                }
            }
        }

        /// <summary>Веса поведения по режиму головы: плавно набираются и плавно спадают.</summary>
        private void UpdateReactionWeights(float dt)
        {
            float flailGoal = 0f, reachGoal = 0f;
            if (m_Face != null)
            {
                if (m_Face.Mode == StaringFaceAnimator.FaceMode.Scream) flailGoal = 1f;
                else if (m_Face.Mode == StaringFaceAnimator.FaceMode.Chatter) reachGoal = 1f;
            }

            float step = dt / Mathf.Max(0.01f, m_ReactionBlend);
            m_FlailWeight = Mathf.MoveTowards(m_FlailWeight, flailGoal, step);
            m_ReachWeight = Mathf.MoveTowards(m_ReachWeight, reachGoal, step);
        }

        /// <summary>Крик: каждая цепочка рывками (но без скачков за кадр) бросается к новому случайному взмаху.</summary>
        private void UpdateFlail(Chain chain, float dt, float weight)
        {
            if (weight <= 0f && chain.Flail.sqrMagnitude < 0.01f)
            {
                chain.Flail = Vector2.zero;
                chain.FlailVelocity = Vector2.zero;
                chain.NextFlail = 0f;
                return;
            }

            chain.NextFlail -= dt;
            if (chain.NextFlail <= 0f)
            {
                // Новый взмах - в другую сторону и почти во весь размах.
                var direction = Random.insideUnitCircle.normalized;
                if (Vector2.Dot(direction, chain.FlailTarget) > 0f) direction = -direction;
                chain.FlailTarget = direction * Random.Range(0.6f, 1f);
                chain.NextFlail = 1f / Mathf.Max(0.1f, Random.Range(m_FlailRate.x, Mathf.Max(m_FlailRate.x, m_FlailRate.y)));
            }

            if (dt > 0f)
            {
                chain.Flail = Vector2.SmoothDamp(chain.Flail, chain.FlailTarget * (m_FlailBend * weight), ref chain.FlailVelocity, Mathf.Max(0.01f, m_FlailSnap), Mathf.Infinity, dt);
            }
        }

        /// <summary>
        /// Стук зубами: от основания к кончику каждый сустав доворачивает цепочку так, чтобы кончик смотрел на игрока
        /// (не больше m_ReachMaxAngle в суставе).
        /// </summary>
        private void Reach(Chain chain, Vector3 target, float weight)
        {
            int n = chain.Bones.Length;
            if (n < 2) return;
            var tip = chain.Bones[n - 1];
            for (int i = 0; i < n - 1; i++)
            {
                var bone = chain.Bones[i];
                var current = tip.position - bone.position;
                var wanted = target - bone.position;
                if (current.sqrMagnitude < 1e-8f || wanted.sqrMagnitude < 1e-8f) continue;
                // Если игрок почти точно позади цепочки, сторона поворота не определена - там поворот плавно сходит на нет.
                float away = Vector3.Angle(current, wanted);
                float certain = 1f - Mathf.Clamp01((away - 140f) / 30f);
                var turn = Quaternion.RotateTowards(Quaternion.identity, Quaternion.FromToRotation(current, wanted), m_ReachMaxAngle);
                float share = i == 0 ? 0.55f : 0.35f;
                bone.rotation = Quaternion.Slerp(Quaternion.identity, turn, m_ReachStrength * weight * share * certain) * bone.rotation;
            }
        }

        /// <summary>Цель тянущихся рук догоняет игрока плавно: покачивание головы и мелкие сдвиги камеры не дёргают руки.</summary>
        private Vector3? SmoothedReachPoint(float dt)
        {
            var point = ReachPoint();
            if (!point.HasValue) return null;
            if (!m_HasReachPoint)
            {
                m_ReachPoint = point.Value;
                m_ReachVelocity = Vector3.zero;
                m_HasReachPoint = true;
            }
            else if (dt > 0f)
            {
                m_ReachPoint = Vector3.SmoothDamp(m_ReachPoint, point.Value, ref m_ReachVelocity, 0.25f, Mathf.Infinity, dt);
            }

            return m_ReachPoint;
        }

        private Vector3? ReachPoint()
        {
            if (m_ReachTarget != null) return m_ReachTarget.position;
            var camera = Camera.main;
            return camera != null ? camera.transform.position : null;
        }

        private static float Smooth(float t) => t * t * (3f - 2f * t);
    }
}
