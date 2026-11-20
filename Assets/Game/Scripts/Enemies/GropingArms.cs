using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Game.Scripts.Enemies
{
    /// <summary>
    /// Процедурные руки сидящего монстра: он по очереди и в случайном порядке ощупывает предметы вокруг себя,
    /// а иногда обвивает одну или обе руки вокруг себя.
    ///
    /// Каждая рука - цепочка костей от плеча до кончика пальцев (последний дочерний объект цепочки - метка кончика).
    /// Рука тянется к случайной точке на поверхности выбранного предмета (дугой, сверху), потом несколько секунд
    /// водит пальцами по его поверхности и шевелит ими, затем выбирает следующий предмет - не тот, что трогала только
    /// что, и не тот, что трогает другая рука. После ощупывания рука иногда (с заданной вероятностью) обвивается
    /// вокруг тела, бывает - вместе со второй рукой, держится так и разматывается.
    ///
    /// Работает поверх анимации: в LateUpdate поворачивает кости рук, плечи берёт из анимации. Суставы не проходят
    /// сквозь тело (капсулы из костей), пол и ощупываемые предметы. Голова поворачивается к руке, которая последней
    /// дотянулась до предмета.
    /// </summary>
    public class GropingArms : MonoBehaviour
    {
        /// <summary>
        /// Часть тела, сквозь которую не проходят руки: капсула от одной кости до другой.
        /// </summary>
        [Serializable]
        public struct BodyPart
        {
            [Tooltip("Начало капсулы (кость).")]
            public Transform From;

            [Tooltip("Конец капсулы (кость или метка).")]
            public Transform To;

            [Tooltip("Толщина: радиус капсулы, м.")]
            public float Radius;
        }

        private enum Mode
        {
            Move,
            Feel,
            Wrap,
            Hold,
            Unwrap,
        }

        private sealed class Arm
        {
            public Transform[] Bones;
            public float[] Lengths;
            public Vector3[] P;
            public float Side;
            public float Reach;
            public Mode Mode;
            public float Time;
            public float Duration;
            public Collider Target;
            public Collider Last;
            public Vector3 From;
            public Vector3 FromOffset;
            public Vector3 Touch;
            public Vector3 Normal;
            public Vector3 Wander;
            public float NextWander;
            public float CurlPhase;
            public float Wrap;
            public bool Started;
            public Quaternion[] Rot;
            public Vector3[] Axis;
            public Vector3[] Prev;
        }

        [Header("Руки")]
        [Tooltip("Первые кости рук (плечевые кости). От каждой цепочка идёт по первым дочерним объектам до метки кончика пальцев.")]
        [SerializeField] private List<Transform> m_Arms = new();

        [Tooltip("Предметы, которые монстр ощупывает (их коллайдеры). Берутся только те, до которых дотягивается рука.")]
        [SerializeField] private List<Collider> m_Targets = new();

        [Tooltip("Части тела, сквозь которые не проходят руки (туловище, голова, ноги).")]
        [SerializeField] private List<BodyPart> m_Body = new();

        [Tooltip("Толщина руки: на сколько суставы держатся от тела, пола и предметов, м.")]
        [SerializeField] private float m_ArmRadius = 0.06f;

        [Header("Ощупывание")]
        [Tooltip("Скорость, с которой рука тянется к предмету, м/с.")]
        [SerializeField] private float m_ReachSpeed = 1.6f;

        [Tooltip("На сколько рука поднимается дугой, пока тянется к предмету, м.")]
        [SerializeField] private float m_ReachLift = 0.6f;

        [Tooltip("Сколько секунд рука ощупывает один предмет: от и до.")]
        [SerializeField] private Vector2 m_FeelTime = new(2.5f, 5.5f);

        [Tooltip("Как быстро пальцы водят по поверхности предмета, м/с.")]
        [SerializeField] private float m_FeelSpeed = 0.22f;

        [Tooltip("Как далеко пальцы уходят по поверхности от первой точки касания, м.")]
        [SerializeField] private float m_FeelSpread = 0.35f;

        [Tooltip("Как часто пальцы сжимаются и разжимаются, раз в секунду.")]
        [SerializeField] private float m_CurlRate = 1.7f;

        [Tooltip("Насколько суставы между плечом и кистью выгибаются вверх, как лапа паука (0 - не выгибаются).")]
        [SerializeField] private float m_Arch = 0.35f;

        [Header("Обвивание вокруг себя")]
        [Tooltip("Вероятность, что после ощупывания рука обовьётся вокруг тела (0..1).")]
        [SerializeField] [Range(0f, 1f)] private float m_WrapChance = 0.12f;

        [Tooltip("Вероятность, что вторая рука обовьётся вместе с первой (0..1).")]
        [SerializeField] [Range(0f, 1f)] private float m_BothWrapChance = 0.4f;

        [Tooltip("Сколько секунд рука держится обвитой: от и до.")]
        [SerializeField] private Vector2 m_WrapHold = new(2f, 4f);

        [Tooltip("За сколько секунд рука обвивается (и столько же разматывается).")]
        [SerializeField] private float m_WrapTime = 1.8f;

        [Tooltip("Наибольшая скорость любого сустава руки, м/с: быстрее рука не дёргается, даже если цель прыгнула.")]
        [SerializeField] private float m_JointSpeed = 3.5f;

        [Tooltip("Наибольшая скорость сустава, пока рука обвивается и разматывается, м/с.")]
        [SerializeField] private float m_WrapSpeed = 2.5f;

        [Tooltip("Ось тела для обвивания: от нижней точки к верхней (обычно таз и шея).")]
        [SerializeField] private Transform m_WrapBottom;

        [SerializeField] private Transform m_WrapTop;

        [Tooltip("Радиус витка вокруг оси тела, м.")]
        [SerializeField] private float m_WrapRadius = 0.6f;

        [Tooltip("Сколько оборотов делает обвившаяся рука (сколько хватит длины).")]
        [SerializeField] private float m_WrapTurns = 1.2f;

        [Header("Голова")]
        [Tooltip("Кости, которые поворачиваются к руке, ощупывающей предмет (шея, голова), и доля поворота каждой.")]
        [SerializeField] private List<Transform> m_LookBones = new();

        [SerializeField] private List<float> m_LookShare = new();

        [Tooltip("Наибольший поворот головы к руке, градусы.")]
        [SerializeField] private float m_LookLimit = 55f;

        [Tooltip("За сколько секунд руки плавно перенимают движение у анимации при включении.")]
        [SerializeField] private float m_BlendIn = 1f;

        private readonly List<Arm> m_State = new();
        private float m_Weight;
        private float m_LookYaw;
        private Vector3 m_LookAt;
        private bool m_HasLook;

        private void OnEnable()
        {
            m_Weight = 0f;
            m_State.Clear();
            for (int i = 0; i < m_Arms.Count; i++)
            {
                var arm = BuildArm(m_Arms[i]);
                if (arm != null)
                {
                    m_State.Add(arm);
                }
            }
        }

        private Arm BuildArm(Transform root)
        {
            if (root == null)
            {
                return null;
            }

            var bones = new List<Transform> { root };
            var t = root;
            while (t.childCount > 0)
            {
                t = t.GetChild(0);
                bones.Add(t);
            }

            if (bones.Count < 3)
            {
                Debug.LogWarning($"[GropingArms] У руки {root.name} меньше двух костей - пропущена.", this);
                return null;
            }

            int n = bones.Count;
            var arm = new Arm
            {
                Bones = bones.ToArray(),
                Lengths = new float[n - 1],
                P = new Vector3[n],
                Side = transform.InverseTransformPoint(root.position).x >= 0f ? 1f : -1f,
                CurlPhase = Random.value * 10f,
            };
            for (int i = 0; i < n - 1; i++)
            {
                arm.Lengths[i] = Vector3.Distance(transform.InverseTransformPoint(bones[i].position), transform.InverseTransformPoint(bones[i + 1].position));
                arm.Reach += arm.Lengths[i];
            }

            for (int i = 0; i < n; i++)
            {
                arm.P[i] = transform.InverseTransformPoint(bones[i].position);
            }

            return arm;
        }

        private void LateUpdate()
        {
            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            if (dt <= 0f || m_State.Count == 0)
            {
                return;
            }

            m_Weight = m_BlendIn > 0f ? Mathf.MoveTowards(m_Weight, 1f, dt / m_BlendIn) : 1f;
            foreach (var arm in m_State)
            {
                if (!arm.Started)
                {
                    arm.Started = true;
                    NextAction(arm, true);
                }

                Step(arm, dt);
                Apply(arm);
            }

            Look(dt);
        }

        // ------------------------------------------------------------------ what each arm does next
        private void NextAction(Arm arm, bool first)
        {
            if (!first && arm.Mode == Mode.Feel && Random.value < m_WrapChance && CanWrap())
            {
                StartWrap(arm);
                var other = Other(arm);
                if (other != null && other.Mode is Mode.Feel or Mode.Move && Random.value < m_BothWrapChance)
                {
                    StartWrap(other);
                }

                return;
            }

            var target = PickTarget(arm);
            if (target == null)
            {
                // ощупывать нечего - рука обвивается вокруг себя или лежит
                if (CanWrap())
                {
                    StartWrap(arm);
                }
                else
                {
                    arm.Mode = Mode.Hold;
                    arm.Time = 0f;
                    arm.Duration = 1f;
                }

                return;
            }

            arm.Target = target;
            arm.Touch = SurfacePoint(target, arm, out arm.Normal);
            arm.From = arm.P[arm.P.Length - 1];
            arm.FromOffset = arm.P[arm.P.Length - 2] - arm.P[arm.P.Length - 1];
            arm.Mode = Mode.Move;
            arm.Time = 0f;
            float distance = Vector3.Distance(arm.From, arm.Touch);
            arm.Duration = Mathf.Clamp(distance / Mathf.Max(m_ReachSpeed, 0.05f), 0.7f, 3f);
        }

        private bool CanWrap()
        {
            return m_WrapBottom != null && m_WrapTop != null;
        }

        private Arm Other(Arm arm)
        {
            foreach (var a in m_State)
            {
                if (a != arm)
                {
                    return a;
                }
            }

            return null;
        }

        private void StartWrap(Arm arm)
        {
            arm.Mode = Mode.Wrap;
            arm.Time = 0f;
            arm.Duration = m_WrapTime;
            if (arm.Target != null)
            {
                arm.Last = arm.Target;
            }

            arm.Target = null;
        }

        private Collider PickTarget(Arm arm)
        {
            var shoulder = arm.Bones[0].position;
            var busy = Other(arm)?.Target;
            float total = 0f;
            var weights = new List<(Collider, float)>();
            foreach (var c in m_Targets)
            {
                if (c == null || !c.gameObject.activeInHierarchy || c == busy || c == arm.Target || c == arm.Last)
                {
                    continue;
                }

                var near = c.ClosestPoint(shoulder);
                if (Vector3.Distance(near, shoulder) > 0.85f * arm.Reach)
                {
                    continue;
                }

                // рука охотнее тянется к предметам на своей стороне, через себя - редко
                float x = transform.InverseTransformPoint(c.bounds.center).x * arm.Side;
                float w = x < -0.4f ? 0.15f : 1f + 2.5f * Mathf.Clamp01(x);
                weights.Add((c, w));
                total += w;
            }

            if (weights.Count == 0)
            {
                // все заняты - можно вернуться к тому, что трогала раньше
                if (arm.Last != null && arm.Last != busy && arm.Target != arm.Last)
                {
                    var keep = arm.Last;
                    arm.Last = arm.Target;
                    return keep;
                }

                return null;
            }

            float r = Random.value * total;
            foreach (var (c, w) in weights)
            {
                r -= w;
                if (r <= 0f)
                {
                    arm.Last = arm.Target;
                    return c;
                }
            }

            arm.Last = arm.Target;
            return weights[^1].Item1;
        }

        /// <summary>Случайная точка на поверхности предмета - сверху или с бока, обращённого к монстру (в осях монстра).</summary>
        private Vector3 SurfacePoint(Collider c, Arm arm, out Vector3 normal)
        {
            var b = c.bounds;
            var toMe = transform.position - b.center;
            toMe.y = 0f;
            toMe = toMe.sqrMagnitude > 1e-6f ? toMe.normalized : -transform.forward;
            var probe = b.center + Vector3.Scale(Random.insideUnitSphere, b.extents) * 0.8f
                        + Vector3.up * (b.extents.y + 0.3f) + toMe * (0.4f * b.extents.magnitude);
            var p = c.ClosestPoint(probe);
            var n = probe - p;
            normal = transform.InverseTransformDirection(n.sqrMagnitude > 1e-8f ? n.normalized : Vector3.up);
            return transform.InverseTransformPoint(p);
        }

        private Vector3 WanderPoint(Arm arm)
        {
            var c = arm.Target;
            var here = transform.TransformPoint(arm.Touch);
            var n = transform.TransformDirection(arm.Normal);
            var probe = here + Random.insideUnitSphere * m_FeelSpread + n * 0.25f;
            var p = c.ClosestPoint(probe);
            return transform.InverseTransformPoint(p);
        }

        // ------------------------------------------------------------------ one frame of one arm
        private void Step(Arm arm, float dt)
        {
            int n = arm.P.Length;
            arm.Prev ??= new Vector3[n];
            Array.Copy(arm.P, arm.Prev, n);
            arm.P[0] = transform.InverseTransformPoint(arm.Bones[0].position);
            arm.Time += dt;
            arm.CurlPhase += dt * m_CurlRate * Mathf.PI * 2f;
            switch (arm.Mode)
            {
                case Mode.Move:
                {
                    float u = Smooth(arm.Time / arm.Duration);
                    var tip = Arc(arm.From, arm.Touch, u);
                    Reach(arm, tip, arm.Normal, 0.15f * Mathf.Sin(Mathf.PI * u), dt, Smooth(2f * arm.Time / arm.Duration));
                    if (arm.Time >= arm.Duration)
                    {
                        arm.Mode = Mode.Feel;
                        arm.Time = 0f;
                        arm.Duration = Random.Range(m_FeelTime.x, m_FeelTime.y);
                        arm.Wander = arm.Touch;
                        arm.NextWander = 0f;
                        m_LookAt = arm.Touch;
                        m_HasLook = true;
                    }

                    break;
                }
                case Mode.Feel:
                {
                    if (arm.Target == null)
                    {
                        NextAction(arm, false);
                        break;
                    }

                    arm.NextWander -= dt;
                    if (arm.NextWander <= 0f)
                    {
                        arm.Wander = WanderPoint(arm);
                        arm.NextWander = Random.Range(0.5f, 1.3f);
                    }

                    arm.Touch = Vector3.MoveTowards(arm.Touch, arm.Wander, m_FeelSpeed * dt);
                    var touchWorld = arm.Target.ClosestPoint(transform.TransformPoint(arm.Touch));
                    arm.Touch = transform.InverseTransformPoint(touchWorld);
                    float curl = 0.5f + 0.5f * Mathf.Sin(arm.CurlPhase);
                    Reach(arm, arm.Touch, arm.Normal, 0.05f * curl, dt, 1f);
                    if (arm.Time >= arm.Duration)
                    {
                        NextAction(arm, false);
                    }

                    break;
                }
                case Mode.Wrap:
                case Mode.Hold:
                case Mode.Unwrap:
                {
                    float a = arm.Mode switch
                    {
                        Mode.Wrap => Smooth(arm.Time / arm.Duration),
                        Mode.Unwrap => 1f - Smooth(arm.Time / arm.Duration),
                        _ => 1f,
                    };
                    arm.Wrap = a;
                    if (CanWrap())
                    {
                        Coil(arm, a, dt);
                    }
                    else
                    {
                        Relax(arm, dt);
                    }
                    if (arm.Time >= arm.Duration)
                    {
                        arm.Time = 0f;
                        if (arm.Mode == Mode.Wrap)
                        {
                            arm.Mode = Mode.Hold;
                            arm.Duration = Random.Range(m_WrapHold.x, m_WrapHold.y);
                        }
                        else if (arm.Mode == Mode.Hold && CanWrap())
                        {
                            arm.Mode = Mode.Unwrap;
                            arm.Duration = m_WrapTime;
                        }
                        else
                        {
                            arm.Mode = Mode.Feel;          // NextAction picks a new object (not a wrap again)
                            arm.Target = null;
                            arm.Wrap = 0f;
                            NextAction(arm, true);
                        }
                    }

                    break;
                }
            }

            Limit(arm, dt);
        }

        /// <summary>Не даёт суставу сдвинуться за кадр дальше, чем позволяет наибольшая скорость, и сохраняет длины костей.</summary>
        private void Limit(Arm arm, float dt)
        {
            int n = arm.P.Length;
            float step = m_JointSpeed * dt;
            for (int i = 1; i < n; i++)
            {
                arm.P[i] = arm.Prev[i] + Vector3.ClampMagnitude(arm.P[i] - arm.Prev[i], step);
            }

            for (int i = 1; i < n; i++)
            {
                arm.P[i] = arm.P[i - 1] + Dir(arm.P[i] - arm.P[i - 1]) * arm.Lengths[i - 1];
            }
        }

        /// <summary>Путь кончика к предмету: дугой вокруг тела (по углу, а не напрямую сквозь него) и сверху.</summary>
        private Vector3 Arc(Vector3 from, Vector3 to, float u)
        {
            var c = Centre();
            var a = from - c;
            var b = to - c;
            float ra = new Vector2(a.x, a.z).magnitude;
            float rb = new Vector2(b.x, b.z).magnitude;
            float aa = Mathf.Atan2(a.z, a.x);
            float ab = Mathf.Atan2(b.z, b.x);
            float da = Mathf.DeltaAngle(aa * Mathf.Rad2Deg, ab * Mathf.Rad2Deg) * Mathf.Deg2Rad;
            float ang = aa + da * u;
            float r = Mathf.Lerp(ra, rb, u);
            float y = Mathf.Lerp(from.y, to.y, u) + m_ReachLift * Mathf.Sin(Mathf.PI * u) * Mathf.Clamp01(Vector3.Distance(from, to));
            return c + new Vector3(Mathf.Cos(ang) * r, y - c.y, Mathf.Sin(ang) * r);
        }

        private Vector3 Centre()
        {
            if (m_WrapBottom != null)
            {
                var p = transform.InverseTransformPoint(m_WrapBottom.position);
                return new Vector3(p.x, 0f, p.z);
            }

            return Vector3.zero;
        }

        /// <summary>
        /// Тянет кончик пальцев к точке: суставы до костяшек - решателем FABRIK, пальцы - в точку касания.
        /// settle 0..1 - насколько кисть уже повернулась к поверхности (в начале движения она как была).
        /// </summary>
        private void Reach(Arm arm, Vector3 touch, Vector3 normal, float lift, float dt, float settle)
        {
            int n = arm.P.Length;
            int knuckle = n - 2;
            float lf = arm.Lengths[n - 2];
            var toShoulder = arm.P[0] - touch;
            toShoulder.y = 0f;
            toShoulder = toShoulder.sqrMagnitude > 1e-6f ? toShoulder.normalized : Vector3.zero;
            var offset = normal * (0.55f * lf + lift) + toShoulder * (0.75f * lf);
            var goal = touch + Vector3.Lerp(arm.FromOffset, offset, settle);

            // суставы между плечом и кистью понемногу поднимаются - рука выгибается дугой, а не лежит на полу
            float top = arm.P[0].y + 0.3f;
            for (int i = 1; i < knuckle; i++)
            {
                if (arm.P[i].y < top)
                {
                    arm.P[i].y += m_Arch * dt;
                }
            }

            var root = arm.P[0];
            for (int it = 0; it < 10; it++)
            {
                arm.P[knuckle] = goal;
                for (int i = knuckle - 1; i >= 0; i--)
                {
                    arm.P[i] = arm.P[i + 1] + Dir(arm.P[i] - arm.P[i + 1]) * arm.Lengths[i];
                }

                arm.P[0] = root;
                for (int i = 1; i <= knuckle; i++)
                {
                    arm.P[i] = arm.P[i - 1] + Dir(arm.P[i] - arm.P[i - 1]) * arm.Lengths[i - 1];
                }

                Collide(arm, 1, knuckle - 1, true);
            }

            for (int i = 1; i <= knuckle; i++)
            {
                arm.P[i] = arm.P[i - 1] + Dir(arm.P[i] - arm.P[i - 1]) * arm.Lengths[i - 1];
            }

            arm.P[n - 1] = arm.P[knuckle] + Dir(touch - arm.P[knuckle]) * lf;
        }

        /// <summary>Рука обвивается вокруг тела: суставы по очереди ложатся на виток вокруг оси тела. a - доля витка (0..1).</summary>
        private void Coil(Arm arm, float a, float dt)
        {
            int n = arm.P.Length;
            var bottom = transform.InverseTransformPoint(m_WrapBottom.position);
            var top = transform.InverseTransformPoint(m_WrapTop.position);
            var c = new Vector3(0.5f * (bottom.x + top.x), 0f, 0.5f * (bottom.z + top.z));
            var s = arm.P[0];
            float a0 = Mathf.Atan2(s.z - c.z, s.x - c.x);
            float dir = arm.Side;                                      // левая рука идёт через грудь вправо, правая - влево
            bool upper = arm.Side > 0f;
            float h0 = upper ? s.y - 0.04f : s.y - 0.30f;
            float fall = upper ? 0.10f : 0.12f;
            float hMin = bottom.y + 0.05f;
            float span = a * m_WrapTurns * Mathf.PI * 2f;
            float k = 1f - Mathf.Exp(-dt * 5f);

            Vector3 Helix(float t)
            {
                float y = Mathf.Max(hMin, h0 - fall * t / (Mathf.PI * 2f));
                return new Vector3(c.x + Mathf.Cos(a0 + dir * t) * m_WrapRadius, y, c.z + Mathf.Sin(a0 + dir * t) * m_WrapRadius);
            }

            float t0 = 0f;
            var prev = s;
            for (int i = 1; i < n; i++)
            {
                float len = arm.Lengths[i - 1];
                // следующий сустав - на витке на расстоянии длины кости от предыдущего (поиск делением пополам)
                float lo = t0, hi = t0 + Mathf.PI;
                for (int it = 0; it < 24; it++)
                {
                    float mid = 0.5f * (lo + hi);
                    if (Vector3.Distance(Helix(mid), prev) < len)
                    {
                        lo = mid;
                    }
                    else
                    {
                        hi = mid;
                    }
                }

                float ti = 0.5f * (lo + hi);
                if (ti > span)
                {
                    // эта часть руки ещё не на витке: висит свободно и тянется за остальной рукой
                    for (int j = i; j < n; j++)
                    {
                        arm.P[j].y -= 0.6f * dt;
                    }

                    break;
                }

                arm.P[i] = Vector3.MoveTowards(arm.P[i], Vector3.Lerp(arm.P[i], Helix(ti), k), m_WrapSpeed * dt);
                prev = Helix(ti);
                t0 = ti;
            }

            for (int i = 1; i < n; i++)
            {
                arm.P[i] = arm.P[i - 1] + Dir(arm.P[i] - arm.P[i - 1]) * arm.Lengths[i - 1];
            }

            Collide(arm, 1, n - 1, false);
            for (int i = 1; i < n; i++)
            {
                arm.P[i] = arm.P[i - 1] + Dir(arm.P[i] - arm.P[i - 1]) * arm.Lengths[i - 1];
            }
        }

        /// <summary>Рука без дела: свободно провисает к полу.</summary>
        private void Relax(Arm arm, float dt)
        {
            int n = arm.P.Length;
            for (int i = 1; i < n; i++)
            {
                arm.P[i].y -= 0.6f * dt;
            }

            for (int i = 1; i < n; i++)
            {
                arm.P[i] = arm.P[i - 1] + Dir(arm.P[i] - arm.P[i - 1]) * arm.Lengths[i - 1];
            }

            Collide(arm, 1, n - 1, true);
        }

        /// <summary>Выталкивает суставы (и середины костей) из тела, из пола и, если нужно, из ощупываемых предметов.</summary>
        private void Collide(Arm arm, int first, int last, bool targets)
        {
            float floor = m_ArmRadius;
            for (int i = first; i <= last; i++)
            {
                arm.P[i] = PushOut(arm.P[i], targets);
                if (arm.P[i].y < floor)
                {
                    arm.P[i].y = floor;
                }

                if (i > 0)
                {
                    var mid = 0.5f * (arm.P[i] + arm.P[i - 1]);
                    var moved = PushOut(mid, targets) - mid;
                    if (moved.sqrMagnitude > 1e-8f)
                    {
                        arm.P[i] += moved;
                        if (i - 1 >= first)
                        {
                            arm.P[i - 1] += moved;
                        }
                    }
                }
            }
        }

        private Vector3 PushOut(Vector3 local, bool targets)
        {
            var p = transform.TransformPoint(local);
            foreach (var part in m_Body)
            {
                if (part.From == null || part.To == null)
                {
                    continue;
                }

                var a = part.From.position;
                var b = part.To.position;
                var ab = b - a;
                float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-6f));
                var closest = a + ab * t;
                var d = p - closest;
                float min = part.Radius + m_ArmRadius;
                if (d.sqrMagnitude < min * min)
                {
                    var away = d.sqrMagnitude > 1e-8f ? d.normalized : transform.right;
                    p = closest + away * min;
                }
            }

            if (targets)
            {
                foreach (var c in m_Targets)
                {
                    if (c == null || !c.enabled)
                    {
                        continue;
                    }

                    var b = c.bounds;
                    b.Expand(2f * m_ArmRadius);
                    if (!b.Contains(p))
                    {
                        continue;
                    }

                    p = OutOfBox(c, p);
                }
            }

            return transform.InverseTransformPoint(p);
        }

        private Vector3 OutOfBox(Collider c, Vector3 p)
        {
            if (c is BoxCollider box)
            {
                var t = box.transform;
                var lp = t.InverseTransformPoint(p) - box.center;
                var half = 0.5f * box.size;
                var scale = t.lossyScale;
                var pad = new Vector3(m_ArmRadius / Mathf.Max(Mathf.Abs(scale.x), 1e-4f), m_ArmRadius / Mathf.Max(Mathf.Abs(scale.y), 1e-4f),
                    m_ArmRadius / Mathf.Max(Mathf.Abs(scale.z), 1e-4f));
                var gap = new Vector3(half.x + pad.x - Mathf.Abs(lp.x), half.y + pad.y - Mathf.Abs(lp.y), half.z + pad.z - Mathf.Abs(lp.z));
                if (gap.x <= 0f || gap.y <= 0f || gap.z <= 0f)
                {
                    return p;
                }

                // наружу через ближайшую грань (вниз - никогда, там пол)
                float gx = gap.x * Mathf.Abs(scale.x), gy = gap.y * Mathf.Abs(scale.y), gz = gap.z * Mathf.Abs(scale.z);
                if (lp.y < 0f)
                {
                    gy = float.MaxValue;
                }

                if (gx <= gy && gx <= gz)
                {
                    lp.x += Mathf.Sign(lp.x == 0f ? 1f : lp.x) * gap.x;
                }
                else if (gy <= gz)
                {
                    lp.y += gap.y;
                }
                else
                {
                    lp.z += Mathf.Sign(lp.z == 0f ? 1f : lp.z) * gap.z;
                }

                return t.TransformPoint(lp + box.center);
            }

            var near = c.ClosestPoint(p);
            if (near != p)
            {
                var d = p - near;
                return d.magnitude < m_ArmRadius ? near + d.normalized * m_ArmRadius : p;
            }

            var bounds = c.bounds;
            return new Vector3(p.x, bounds.max.y + m_ArmRadius, p.z);
        }

        // ------------------------------------------------------------------ bones
        /// <summary>
        /// Поворачивает кости руки так, чтобы каждая смотрела на следующий сустав. Поворот каждой кости меняется
        /// понемногу от кадра к кадру (а не считается заново от анимации), поэтому кисть не перекручивается рывком,
        /// когда рука развёрнута почти против анимации; к закрутке из анимации кость медленно подтягивается.
        /// </summary>
        private void Apply(Arm arm)
        {
            int n = arm.Bones.Length;
            var inv = Quaternion.Inverse(transform.rotation);
            if (arm.Rot == null)
            {
                arm.Rot = new Quaternion[n - 1];
                arm.Axis = new Vector3[n - 1];
                for (int i = 0; i < n - 1; i++)
                {
                    arm.Rot[i] = inv * arm.Bones[i].rotation;
                    arm.Axis[i] = arm.Bones[i + 1].localPosition.normalized;
                }
            }

            for (int i = 0; i < n - 1; i++)
            {
                var bone = arm.Bones[i];
                var wanted = arm.P[i + 1] - arm.P[i];
                if (wanted.sqrMagnitude < 1e-10f)
                {
                    continue;
                }

                var animated = inv * bone.rotation;
                var animatedDir = animated * arm.Axis[i];
                if (Vector3.Angle(animatedDir, wanted) < 120f)
                {
                    var fromAnimation = Quaternion.FromToRotation(animatedDir, wanted) * animated;
                    arm.Rot[i] = Quaternion.Slerp(arm.Rot[i], fromAnimation, 0.03f);
                }

                arm.Rot[i] = Quaternion.FromToRotation(arm.Rot[i] * arm.Axis[i], wanted) * arm.Rot[i];
                bone.rotation = Quaternion.Slerp(bone.rotation, transform.rotation * arm.Rot[i], m_Weight);
            }
        }

        private void Look(float dt)
        {
            if (m_LookBones.Count == 0)
            {
                return;
            }

            float wanted = 0f;
            if (m_HasLook)
            {
                var d = m_LookAt;
                d.y = 0f;
                if (d.sqrMagnitude > 1e-4f)
                {
                    wanted = Mathf.Clamp(Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg, -m_LookLimit, m_LookLimit);
                }
            }

            m_LookYaw = Mathf.Lerp(m_LookYaw, wanted, 1f - Mathf.Exp(-dt * 3f));
            var up = transform.up;
            for (int i = 0; i < m_LookBones.Count; i++)
            {
                var b = m_LookBones[i];
                if (b == null)
                {
                    continue;
                }

                float share = i < m_LookShare.Count ? m_LookShare[i] : 1f / m_LookBones.Count;
                b.rotation = Quaternion.AngleAxis(m_LookYaw * share * m_Weight, up) * b.rotation;
            }
        }

        private static Vector3 Dir(Vector3 v)
        {
            return v.sqrMagnitude > 1e-10f ? v.normalized : Vector3.down;
        }

        private static float Smooth(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }

        private void OnDrawGizmosSelected()
        {
            foreach (var arm in m_State)
            {
                Gizmos.color = arm.Mode is Mode.Wrap or Mode.Hold or Mode.Unwrap ? Color.magenta : Color.yellow;
                for (int i = 1; i < arm.P.Length; i++)
                {
                    Gizmos.DrawLine(transform.TransformPoint(arm.P[i - 1]), transform.TransformPoint(arm.P[i]));
                }

                if (arm.Mode is Mode.Move or Mode.Feel)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawSphere(transform.TransformPoint(arm.Touch), 0.03f);
                }
            }

            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.5f);
            foreach (var part in m_Body)
            {
                if (part.From != null && part.To != null)
                {
                    Gizmos.DrawLine(part.From.position, part.To.position);
                }
            }
        }
    }
}
