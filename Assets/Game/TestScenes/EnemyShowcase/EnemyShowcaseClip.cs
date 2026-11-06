using UnityEngine;

namespace Game.TestScenes.EnemyShowcase
{
    /// <summary>
    /// Сцена просмотра противников: держит на Animator этого объекта одно состояние контроллера.
    /// Зацикленные анимации (Idle, Walk, Run, Grope) играют непрерывно; разовые (Hit, Scream) запускаются заново
    /// каждые m_Repeat секунд. Для ходьбы и бега параметр Speed ставится в скорость клипа, а флаг с именем
    /// состояния (Grope) включается - иначе контроллер сам вернул бы монстра в стойку.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class EnemyShowcaseClip : MonoBehaviour
    {
        private static readonly int SpeedId = Animator.StringToHash("Speed");

        [Tooltip("Имя состояния в контроллере анимации (Idle, Walk, Run, Hit, Scream, Idle_Sit, Grope).")]
        [SerializeField] private string m_State = "Idle";

        [Tooltip("Через сколько секунд запускать состояние заново (для разовых анимаций). 0 - не перезапускать.")]
        [SerializeField] private float m_Repeat;

        [Tooltip("Значение параметра Speed, пока играет это состояние (м/с). Для ходьбы и бега - скорость, при которой ноги не скользят.")]
        [SerializeField] private float m_Speed;

        private Animator m_Animator;
        private float m_Next;

        private void Start()
        {
            m_Animator = GetComponent<Animator>();
            Play();
        }

        private void Update()
        {
            if (m_Repeat > 0f && Time.time >= m_Next)
            {
                Play();
            }
        }

        private void Play()
        {
            foreach (var parameter in m_Animator.parameters)
            {
                if (parameter.nameHash == SpeedId)
                {
                    m_Animator.SetFloat(SpeedId, m_Speed);
                }

                // флаг с именем состояния (например Grope у сидящего рабочего) держит контроллер в этом состоянии
                if (parameter.type == AnimatorControllerParameterType.Bool && parameter.name == m_State)
                {
                    m_Animator.SetBool(parameter.nameHash, true);
                }
            }

            m_Animator.Play(m_State, 0, 0f);
            m_Next = Time.time + m_Repeat;
        }
    }
}
