using UnityEngine;

namespace Game.TestScenes.EnemyShowcase
{
    /// <summary>
    /// Сцена просмотра противников: дверь перед монстром, который её ломает. Створка вздрагивает (приоткрывается
    /// и захлопывается) в моменты ударов клипа, а если у клипа есть момент пролома - распахивается настежь и остаётся
    /// так до конца клипа. Если включено «Срывается с петель», в момент пролома створка не распахивается, а отлетает
    /// от монстра, опрокидывается через нижний край и падает плашмя на пол. Время берётся из состояния Animator
    /// монстра, поэтому дверь всегда в такт анимации; при новом проигрывании клипа створка снова на месте.
    /// Только для показа: в игре реакцию двери делает её собственная логика.
    /// </summary>
    public class EnemyShowcaseDoor : MonoBehaviour
    {
        [Tooltip("Animator монстра, который ломает эту дверь.")]
        [SerializeField] private Animator m_Animator;

        [Tooltip("Имя состояния (клипа), в такт которому вздрагивает дверь.")]
        [SerializeField] private string m_State = "Door_Bash";

        [Tooltip("Длина клипа, с.")]
        [SerializeField] private float m_ClipLength = 3f;

        [Tooltip("Моменты ударов от начала клипа, с.")]
        [SerializeField] private float[] m_Hits = new float[0];

        [Tooltip("Момент пролома от начала клипа, с (меньше 0 - двери не проламывают).")]
        [SerializeField] private float m_BreakAt = -1f;

        [Tooltip("Петля двери (объект, вокруг которого поворачивается створка).")]
        [SerializeField] private Transform m_Hinge;

        [Tooltip("Ось петли в её локальных осях.")]
        [SerializeField] private Vector3 m_Axis = Vector3.up;

        [Tooltip("Знак поворота: +1 или -1 - так, чтобы створка уходила от монстра.")]
        [SerializeField] private float m_Sign = 1f;

        [Tooltip("На сколько градусов створка приоткрывается от удара.")]
        [SerializeField] private float m_HitAngle = 5f;

        [Tooltip("На сколько градусов створка распахивается при проломе.")]
        [SerializeField] private float m_BreakAngle = 100f;

        [Tooltip("При проломе створка срывается с петель и падает плашмя (иначе распахивается на петле).")]
        [SerializeField] private bool m_TearOff;

        [Tooltip("Сорванная створка: за сколько секунд она падает на пол.")]
        [SerializeField] private float m_TearFall = 0.5f;

        [Tooltip("Сорванная створка: на сколько метров она отлетает от монстра.")]
        [SerializeField] private float m_TearSlide = 1f;

        [Tooltip("Сорванная створка: на сколько градусов её разворачивает, пока она падает.")]
        [SerializeField] private float m_TearYaw = 9f;

        private Quaternion m_Rest;
        private Vector3 m_RestLocalPos;
        private Vector3 m_RestPos;
        private Quaternion m_RestRot;
        private Vector3 m_Pivot;
        private Vector3 m_Away;
        private Vector3 m_TipAxis;

        private void Start()
        {
            if (m_Hinge == null)
            {
                return;
            }

            m_Rest = m_Hinge.localRotation;
            m_RestLocalPos = m_Hinge.localPosition;
            m_RestPos = m_Hinge.position;
            m_RestRot = m_Hinge.rotation;

            // куда падает сорванная створка: от монстра; опрокидывается через свой нижний край на дальней от него стороне
            m_Away = m_Animator != null ? m_Animator.transform.forward : transform.forward;
            m_Away.y = 0f;
            m_Away = m_Away.sqrMagnitude > 1e-6f ? m_Away.normalized : Vector3.forward;
            m_TipAxis = Vector3.Cross(Vector3.up, m_Away);
            var renderers = m_Hinge.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                Bounds b = renderers[0].bounds;
                foreach (Renderer r in renderers)
                {
                    b.Encapsulate(r.bounds);
                }

                float depth = Mathf.Abs(m_Away.x) * b.extents.x + Mathf.Abs(m_Away.z) * b.extents.z;
                m_Pivot = new Vector3(b.center.x, b.min.y, b.center.z) + m_Away * depth;
            }
            else
            {
                m_Pivot = m_RestPos;
            }
        }

        private void Update()
        {
            if (m_Hinge == null || m_Animator == null)
            {
                return;
            }

            AnimatorStateInfo info = m_Animator.GetCurrentAnimatorStateInfo(0);
            float angle = 0f;
            float t = -1f;
            if (info.IsName(m_State))
            {
                t = info.loop ? (info.normalizedTime % 1f) * m_ClipLength : Mathf.Min(info.normalizedTime, 1f) * m_ClipLength;
                foreach (float hit in m_Hits)
                {
                    float dt = t - hit;
                    if (dt >= 0f && dt < 0.5f)
                    {
                        // толчок: быстро приоткрывается и захлопывается с отскоком
                        angle += m_HitAngle * Mathf.Sin(dt / 0.5f * Mathf.PI * 3f) * (1f - dt / 0.5f);
                    }
                }

                if (!m_TearOff && m_BreakAt >= 0f && t >= m_BreakAt)
                {
                    float k = Mathf.Clamp01((t - m_BreakAt) / 0.3f);
                    angle = m_BreakAngle * (1f - (1f - k) * (1f - k));
                }
            }

            if (m_TearOff && m_BreakAt >= 0f && t >= m_BreakAt)
            {
                Tear(t - m_BreakAt);
                return;
            }

            m_Hinge.localPosition = m_RestLocalPos;
            m_Hinge.localRotation = m_Rest * Quaternion.AngleAxis(angle * m_Sign, m_Axis);
        }

        /// <summary>Створка через k секунд после пролома: отлетает от монстра (быстро, потом всё медленнее),
        /// опрокидывается через нижний край с ускорением, как падающая плита, и ложится плашмя с небольшим отскоком,
        /// немного разворачиваясь. Та же формула - в превью клипов (clips.py, tear_pose).</summary>
        private void Tear(float k)
        {
            float x = Mathf.Clamp01(k / Mathf.Max(0.05f, m_TearFall));
            float slide = m_TearSlide * (1f - (1f - x) * (1f - x) * (1f - x));
            float tip = 90f * x * x;
            if (k > m_TearFall)
            {
                tip = 90f - 4f * Mathf.Sin(Mathf.PI * Mathf.Clamp01((k - m_TearFall) / 0.15f));
            }

            float yaw = m_TearYaw * x * x * (3f - 2f * x);
            Quaternion r = Quaternion.AngleAxis(yaw, Vector3.up) * Quaternion.AngleAxis(tip, m_TipAxis);
            m_Hinge.SetPositionAndRotation(m_Pivot + m_Away * slide + r * (m_RestPos - m_Pivot), r * m_RestRot);
        }
    }
}
