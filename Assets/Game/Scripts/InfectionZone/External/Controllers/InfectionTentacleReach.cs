using UnityEngine;

namespace Game.Scripts.InfectionZone.External.Controllers
{
    /// <summary>
    /// Щупальца тянутся к игроку. Передаёт шейдеру "Custom/InfectedConcrete" (материал щупалец с включённым Sway)
    /// точку камеры игрока, и свободные концы щупалец (цвет вершины R) вытягиваются к ней, когда игрок подходит
    /// ближе <see cref="m_StartDistance"/>. Насколько далеко тянутся - свойство материала Reach toward the Player.
    /// Вешается на объект с MeshRenderer щупалец. Меняет только MaterialPropertyBlock - материал общий,
    /// у остальных щупалец ничего не меняется.
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public class InfectionTentacleReach : MonoBehaviour
    {
        private static readonly int ReachTargetId = Shader.PropertyToID("_ReachTarget");

        [Tooltip("За кем тянуться. Если пусто - за основной камерой (камерой игрока).")]
        [SerializeField] private Transform m_Target;

        [Tooltip("С какого расстояния до щупалец они начинают тянуться, м.")]
        [SerializeField] private float m_StartDistance = 6f;

        [Tooltip("С какого расстояния тянутся в полную силу, м.")]
        [SerializeField] private float m_FullDistance = 1.8f;

        [Tooltip("Плавность: примерно за сколько секунд щупальца подстраиваются под игрока.")]
        [SerializeField] private float m_SmoothTime = 0.8f;

        private Renderer m_Renderer;
        private MaterialPropertyBlock m_PropertyBlock;
        private Vector3 m_Point;
        private Vector3 m_PointVelocity;
        private float m_Strength;
        private float m_StrengthVelocity;
        private bool m_HasPoint;

        private void Awake()
        {
            m_Renderer = GetComponent<Renderer>();
            m_PropertyBlock = new MaterialPropertyBlock();
        }

        private void LateUpdate()
        {
            Transform target = m_Target;
            if (target == null && Camera.main != null)
            {
                target = Camera.main.transform;
            }

            float wanted = 0f;
            if (target != null)
            {
                Vector3 position = target.position;
                if (!m_HasPoint)
                {
                    m_Point = position;
                    m_HasPoint = true;
                }

                // Точку тоже сглаживаем - щупальца поворачиваются за игроком, а не дёргаются.
                m_Point = Vector3.SmoothDamp(m_Point, position, ref m_PointVelocity, m_SmoothTime * 0.5f);
                float distance = Vector3.Distance(m_Renderer.bounds.ClosestPoint(position), position);
                wanted = 1f - Mathf.InverseLerp(m_FullDistance, m_StartDistance, distance);
            }

            m_Strength = Mathf.SmoothDamp(m_Strength, wanted, ref m_StrengthVelocity, m_SmoothTime);
            m_Renderer.GetPropertyBlock(m_PropertyBlock);
            m_PropertyBlock.SetVector(ReachTargetId, new Vector4(m_Point.x, m_Point.y, m_Point.z, m_Strength));
            m_Renderer.SetPropertyBlock(m_PropertyBlock);
        }
    }
}
