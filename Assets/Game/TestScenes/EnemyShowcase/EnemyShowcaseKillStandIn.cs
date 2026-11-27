using Game.Scripts.NPC.Runtime.Data;
using UnityEngine;

namespace Game.TestScenes.EnemyShowcase
{
    /// <summary>
    /// Сцена просмотра противников: манекен игрока в анимации убийства от первого лица. В игре у игрока нет тела - камера
    /// идёт за костью глаз игрока (PlayerEye) в скелете монстра; здесь на этих костях висит манекен (голова, туловище,
    /// ноги), чтобы со стороны было видно, кого и как монстр держит. Голова - у PlayerEye, туловище - от PlayerHips до шеи,
    /// ноги - от PlayerHips вниз по его оси. EnemyShowcaseKillView показывает вид из PlayerEye (глазами игрока).
    /// Только для показа.
    /// </summary>
    public class EnemyShowcaseKillStandIn : MonoBehaviour
    {
        [Tooltip("Animator монстра, который убивает (в его скелете кости PlayerEye и PlayerHips).")]
        [SerializeField] private Animator m_Animator;

        [Tooltip("Профиль анимаций и звуков монстра: из него время затемнения этой анимации, как в игре.")]
        [SerializeField] private NpcAnimationSoundProfileSO m_Profile;

        [Tooltip("Если у монстра ещё нет профиля (в игре он пока не бот): длина и момент затемнения этой анимации здесь.")]
        [SerializeField] private NpcKillClip m_KillWithoutProfile;

        [Tooltip("Голова манекена (сфера).")]
        [SerializeField] private Transform m_Head;

        [Tooltip("Туловище манекена (капсула).")]
        [SerializeField] private Transform m_Torso;

        [Tooltip("Ноги манекена (капсула).")]
        [SerializeField] private Transform m_Legs;

        [SerializeField] private float m_TorsoRadius = 0.15f;
        [SerializeField] private float m_LegsRadius = 0.12f;
        [SerializeField] private float m_LegsLength = 0.9f;

        private Transform m_Eye;
        private Transform m_Hips;

        /// <summary>Глаза игрока в анимации (камера вида глазами игрока).</summary>
        public Transform Eye => m_Eye;

        /// <summary>Аниматор монстра этого ряда.</summary>
        public Animator Animator => m_Animator;

        /// <summary>Повтор анимации монстра этого ряда.</summary>
        public EnemyShowcaseClip Clip => m_Animator != null ? m_Animator.GetComponent<EnemyShowcaseClip>() : null;

        /// <summary>Анимация убийства этого ряда из профиля (длина, когда в игре начинается затемнение).</summary>
        public bool TryGetKill(out NpcKillClip kill)
        {
            kill = default;
            var clip = Clip;

            if (clip == null)
            {
                return false;
            }

            if (m_Profile == null)
            {
                kill = m_KillWithoutProfile;
                return kill.AnimatorState == clip.State;
            }

            if (!m_Profile.HasKillClips)
            {
                return false;
            }

            return m_Profile.TryGetRandomKillClip(k => k.AnimatorState == clip.State, out kill);
        }

        /// <summary>Показать или спрятать манекен (в виде глазами игрока его нет - в игре у игрока нет тела).</summary>
        public void SetVisible(bool isVisible)
        {
            foreach (var part in new[] { m_Head, m_Torso, m_Legs })
            {
                if (part != null && part.TryGetComponent<Renderer>(out var renderer))
                {
                    renderer.enabled = isVisible;
                }
            }
        }

        private void Start()
        {
            if (m_Animator == null)
            {
                return;
            }

            foreach (var t in m_Animator.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "PlayerEye")
                {
                    m_Eye = t;
                }
                else if (t.name == "PlayerHips")
                {
                    m_Hips = t;
                }
            }
        }

        private void LateUpdate()
        {
            if (m_Eye == null || m_Hips == null)
            {
                return;
            }

            Vector3 eye = m_Eye.position;
            Vector3 neck = eye - m_Eye.up * 0.14f - m_Eye.forward * 0.06f;
            m_Head.position = eye - m_Eye.forward * 0.07f + m_Eye.up * 0.03f;
            m_Head.rotation = m_Eye.rotation;
            PlaceCapsule(m_Torso, m_Hips.position, neck, m_TorsoRadius);
            PlaceCapsule(m_Legs, m_Hips.position, m_Hips.position - m_Hips.up * m_LegsLength, m_LegsRadius);
        }

        /// <summary>Капсула (примитив Unity: высота 2, радиус 0.5 по оси Y) от a до b.</summary>
        private static void PlaceCapsule(Transform capsule, Vector3 a, Vector3 b, float radius)
        {
            Vector3 d = b - a;
            float length = Mathf.Max(d.magnitude, 2f * radius);
            capsule.position = (a + b) * 0.5f;
            capsule.rotation = d.sqrMagnitude > 1e-6f ? Quaternion.FromToRotation(Vector3.up, d) : Quaternion.identity;
            capsule.localScale = new Vector3(2f * radius, length * 0.5f, 2f * radius);
        }
    }
}
