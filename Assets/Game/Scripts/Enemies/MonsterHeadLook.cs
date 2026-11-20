using System;
using UnityEngine;

namespace Game.Scripts.Enemies
{
    /// <summary>
    /// Процедурный поворот головы монстра к игроку поверх любой анимации: монстр продолжает делать то, что делал
    /// (идёт, ест, лижет стену, ломает дверь), а голова (и шея) следят за целью.
    ///
    /// Работает в LateUpdate, после того как Animator выставил позу кадра: для каждой цепочки (шея..голова) считает
    /// поворот, который направит лицо на цель, плавно догоняет его и делит между костями цепочки по долям. Анимация
    /// при этом не прерывается и не перезаписывается - поворот каждый кадр накладывается заново на её позу.
    ///
    /// Цель - Target, а если она не задана, главная камера (глаза игрока). Насколько сильно голова следит, задаёт
    /// Weight (0 - чистая анимация, 1 - смотрит на цель); его меняет ИИ, когда монстр замечает или теряет игрока.
    /// Если цель дальше предельного угла (например, за спиной), голова плавно возвращается к анимации.
    /// </summary>
    [DisallowMultipleComponent]
    public class MonsterHeadLook : MonoBehaviour
    {
        /// <summary>
        /// Шея с головой: кости от основания шеи к голове, доли поворота каждой кости и направление лица.
        /// </summary>
        [Serializable]
        public struct Chain
        {
            [Tooltip("Кости от основания шеи к голове (последняя - голова).")]
            public Transform[] Bones;

            [Tooltip("Какую долю поворота берёт каждая кость (по порядку костей, в сумме 1).")]
            public float[] Shares;

            [Tooltip("Куда смотрит лицо в локальных осях головы (последней кости).")]
            public Vector3 LocalForward;
        }

        [Tooltip("За кем следить. Если пусто - за главной камерой (глаза игрока).")]
        [SerializeField] private Transform m_Target;

        [Tooltip("Насколько голова следит за целью: 0 - только анимация, 1 - смотрит на цель.")]
        [SerializeField, Range(0f, 1f)] private float m_Weight;

        [Tooltip("Наибольший угол, на который голова отворачивается от позы анимации, градусы.")]
        [SerializeField, Range(10f, 170f)] private float m_MaxAngle = 80f;

        [Tooltip("Как быстро голова догоняет цель (больше - резче, по-звериному).")]
        [SerializeField, Range(0.5f, 30f)] private float m_Speed = 6f;

        [Tooltip("Если цель дальше предельного угла: включено - голова отпускает её и возвращается к анимации; выключено - держит на пределе.")]
        [SerializeField] private bool m_ReleaseOutsideAngle = true;

        [Tooltip("Цепочки шея-голова (у двухголового монстра их две).")]
        [SerializeField] private Chain[] m_Chains = Array.Empty<Chain>();

        private Quaternion[] m_Current = Array.Empty<Quaternion>();

        /// <summary>За кем следить; null - за главной камерой.</summary>
        public Transform Target
        {
            get => m_Target;
            set => m_Target = value;
        }

        /// <summary>Насколько голова следит за целью (0..1). Меняется плавно - голова не дёргается.</summary>
        public float Weight
        {
            get => m_Weight;
            set => m_Weight = Mathf.Clamp01(value);
        }

        private void OnEnable()
        {
            m_Current = new Quaternion[m_Chains.Length];
            for (int i = 0; i < m_Current.Length; i++)
            {
                m_Current[i] = Quaternion.identity;
            }
        }

        private void LateUpdate()
        {
            if (m_Current.Length != m_Chains.Length)
            {
                OnEnable();
            }

            bool hasTarget = TryGetTarget(out Vector3 target);
            float follow = 1f - Mathf.Exp(-m_Speed * Time.deltaTime);
            for (int i = 0; i < m_Chains.Length; i++)
            {
                Chain chain = m_Chains[i];
                if (chain.Bones == null || chain.Bones.Length == 0)
                {
                    continue;
                }

                Transform head = chain.Bones[chain.Bones.Length - 1];
                Quaternion want = Quaternion.identity;
                if (hasTarget && m_Weight > 0f)
                {
                    Vector3 face = head.TransformDirection(chain.LocalForward).normalized;
                    Vector3 toTarget = (target - head.position).normalized;
                    float angle = Vector3.Angle(face, toTarget);
                    if (angle <= m_MaxAngle)
                    {
                        want = Quaternion.FromToRotation(face, toTarget);
                    }
                    else if (!m_ReleaseOutsideAngle)
                    {
                        Vector3 limited = Vector3.RotateTowards(face, toTarget, m_MaxAngle * Mathf.Deg2Rad, 0f);
                        want = Quaternion.FromToRotation(face, limited);
                    }

                    want = Quaternion.Slerp(Quaternion.identity, want, m_Weight);
                }

                m_Current[i] = Quaternion.Slerp(m_Current[i], want, follow);
                Apply(chain, m_Current[i]);
            }
        }

        private static void Apply(Chain chain, Quaternion turn)
        {
            for (int k = 0; k < chain.Bones.Length; k++)
            {
                float share = chain.Shares != null && k < chain.Shares.Length ? chain.Shares[k] : 1f / chain.Bones.Length;
                Transform bone = chain.Bones[k];
                if (bone == null)
                {
                    continue;
                }

                bone.rotation = Quaternion.Slerp(Quaternion.identity, turn, share) * bone.rotation;
            }
        }

        private bool TryGetTarget(out Vector3 position)
        {
            if (m_Target != null)
            {
                position = m_Target.position;
                return true;
            }

            Camera main = Camera.main;
            if (main != null)
            {
                position = main.transform.position;
                return true;
            }

            position = default;
            return false;
        }
    }
}
