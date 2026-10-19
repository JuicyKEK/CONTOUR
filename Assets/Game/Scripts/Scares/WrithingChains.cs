using System.Collections.Generic;
using UnityEngine;

namespace Game.Scripts.Scares
{
    /// <summary>
    /// Шевелит цепочки костей - руки из перепонки, пальцы вместо перил, щупальца: каждая цепочка изгибается
    /// бегущей от основания к кончику волной, иногда подёргивается и поджимается. У каждой цепочки своя скорость
    /// и фаза, поэтому они не двигаются в такт.
    /// Цепочка - кость из списка оснований и её дочерние кости до кончика (дочерние объекты с мешем пропускаются).
    /// Повороты считаются от положения костей при запуске.
    /// </summary>
    public class WrithingChains : MonoBehaviour
    {
        [Tooltip("Первые кости цепочек (основания рук, пальцев, щупалец).")]
        [SerializeField] private List<Transform> m_Roots = new();

        [Tooltip("Наибольший изгиб в суставе у кончика, градусы (у основания меньше).")]
        [SerializeField] private float m_Bend = 14f;

        [Tooltip("Скорость шевеления, колебаний в секунду: от и до (у каждой цепочки своя).")]
        [SerializeField] private Vector2 m_Speed = new(0.25f, 0.6f);

        [Tooltip("Сдвиг волны между соседними суставами, доли колебания - волна бежит к кончику.")]
        [SerializeField] private float m_Wave = 0.18f;

        [Tooltip("Сколько раз в секунду (в среднем) цепочка дёргается.")]
        [SerializeField] private float m_TwitchRate = 0.35f;

        [Tooltip("Сила рывка, градусы.")]
        [SerializeField] private float m_TwitchStrength = 12f;

        [Tooltip("Медленное поджимание в одну сторону (как сжимаются пальцы), градусы.")]
        [SerializeField] private float m_Curl = 10f;

        private sealed class Chain
        {
            public Transform[] Bones;
            public Quaternion[] Rest;
            public Vector3[] AxisA, AxisB;
            public float Speed, Phase, NextTwitch, Twitch;
        }

        private readonly List<Chain> m_Chains = new();

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
                    NextTwitch = Random.Range(0.5f, 3f),
                };
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
            foreach (var chain in m_Chains)
            {
                chain.NextTwitch -= dt;
                if (chain.NextTwitch <= 0f)
                {
                    chain.Twitch = Random.Range(-1f, 1f) * m_TwitchStrength;
                    chain.NextTwitch = Random.Range(0.3f, 2f) / Mathf.Max(0.05f, m_TwitchRate);
                }

                chain.Twitch = Mathf.MoveTowards(chain.Twitch, 0f, m_TwitchStrength * 2.5f * dt);
                int n = chain.Bones.Length;
                float cycle = time * chain.Speed + chain.Phase;
                float curl = m_Curl * (0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * (cycle * 0.37f + 0.3f)));
                for (int i = 0; i < n; i++)
                {
                    float weight = (i + 1f) / n;
                    float wave = 2f * Mathf.PI * (cycle - i * m_Wave);
                    float bendA = m_Bend * weight * Mathf.Sin(wave) + curl * weight + chain.Twitch * weight;
                    float bendB = m_Bend * 0.6f * weight * Mathf.Sin(wave * 0.73f + 1.1f);
                    chain.Bones[i].localRotation = chain.Rest[i] * Quaternion.AngleAxis(bendA, chain.AxisA[i]) * Quaternion.AngleAxis(bendB, chain.AxisB[i]);
                }
            }
        }
    }
}
