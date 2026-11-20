using UnityEngine;

namespace Game.TestScenes.EnemyShowcase
{
    /// <summary>
    /// Сцена просмотра противников: жидкость, которая льётся из руки монстра в такт его клипу (Flat5Bathtub пьёт воду из
    /// ванны: из горсти над раскрытым ртом льётся чёрная вода). Струя - эффект BlackWater_Pour; она выходит из точки на
    /// руке и течёт только в заданные окна клипа, время берётся из состояния Animator монстра. Струя не длиннее, чем до
    /// рта: так вода не проходит сквозь голову, а брызги эффекта появляются у рта (там стоит маленький коллайдер).
    /// Только для показа: в игре это делает логика самого монстра.
    /// </summary>
    public class EnemyShowcasePour : MonoBehaviour
    {
        [Tooltip("Animator монстра.")]
        [SerializeField] private Animator m_Animator;

        [Tooltip("Имя состояния (клипа), в такт которому льётся струя.")]
        [SerializeField] private string m_State = "Drink_Water";

        [Tooltip("Длина клипа, с.")]
        [SerializeField] private float m_ClipLength = 1f;

        [Tooltip("Когда льётся: пары «начало, конец» от начала клипа, с.")]
        [SerializeField] private float[] m_Windows = new float[0];

        [Tooltip("Струя (система частиц эффекта BlackWater_Pour).")]
        [SerializeField] private ParticleSystem m_Pour;

        [Tooltip("Кость руки, из которой льётся.")]
        [SerializeField] private Transform m_Hand;

        [Tooltip("Точка на руке, откуда льётся, в осях кости.")]
        [SerializeField] private Vector3 m_HandOffset;

        [Tooltip("Куда льётся (рот): объект на кости головы.")]
        [SerializeField] private Transform m_Target;

        private ParticleSystem.EmissionModule m_Emission;
        private ParticleSystem.MainModule m_Main;
        private float m_Speed;

        private void Start()
        {
            if (m_Pour == null)
            {
                return;
            }

            m_Emission = m_Pour.emission;
            m_Main = m_Pour.main;
            m_Speed = m_Main.startSpeed.constantMax;
            m_Emission.enabled = false;
        }

        private void LateUpdate()
        {
            if (m_Pour == null || m_Hand == null || m_Animator == null)
            {
                return;
            }

            // струя падает отвесно из точки на руке
            Transform pour = m_Pour.transform;
            pour.SetPositionAndRotation(m_Hand.TransformPoint(m_HandOffset), Quaternion.identity);
            bool on = false;
            AnimatorStateInfo info = m_Animator.GetCurrentAnimatorStateInfo(0);
            if (info.IsName(m_State))
            {
                float t = info.loop ? (info.normalizedTime % 1f) * m_ClipLength : Mathf.Min(info.normalizedTime, 1f) * m_ClipLength;
                for (int i = 0; i + 1 < m_Windows.Length; i += 2)
                {
                    if (t >= m_Windows[i] && t <= m_Windows[i + 1])
                    {
                        on = true;
                    }
                }
            }

            if (on && m_Target != null)
            {
                // время падения от руки до рта (+10 %): частица живёт ровно столько
                float h = Mathf.Max(0.02f, pour.position.y - m_Target.position.y);
                float g = Physics.gravity.magnitude * Mathf.Max(0.01f, m_Main.gravityModifierMultiplier);
                float life = (-m_Speed + Mathf.Sqrt(m_Speed * m_Speed + 2f * g * h)) / g;
                m_Main.startLifetime = life * 1.1f;
            }

            m_Emission.enabled = on;
        }
    }
}
