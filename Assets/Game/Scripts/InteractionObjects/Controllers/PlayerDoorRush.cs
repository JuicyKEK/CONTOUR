using AZE.AdvancedFirstPerson;
using Game.Scripts.InteractionObjects.Interfaces;
using UnityEngine;

namespace Game.Scripts.InteractionObjects.Controllers
{
    /// <summary>
    /// Игрок бежит (спринт) в закрытую дверь - она распахивается в момент удара (камера почти вплотную к двери),
    /// нажимать не нужно.
    /// Створка уходит от игрока (настройки "С разбега" у вида двери). Запертая дверь дёргается - один раз, пока игрок
    /// бежит в неё. Шагом дверь, как и раньше, открывается только нажатием.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerDoorRush : MonoBehaviour
    {
        [SerializeField] private PlayerMovementStateMachine m_Movement;
        [Tooltip("Выключено - двери на бегу сами не открываются.")]
        [SerializeField] private bool m_IsEnabled = true;
        [Tooltip("Дверь распахивается, только пока игрок бежит (спринт) и не медленнее этого, м/с.")]
        [SerializeField, Min(0f)] private float m_MinSpeed = 3f;
        [Tooltip("Дверь распахивается в момент удара: когда камера игрока подходит к ней на это расстояние, м. " +
                 "Упёршись в дверь, камера стоит примерно в 0,45 м от неё (радиус капсулы 0,4), ближе не подойдёт.")]
        [SerializeField, Min(0f)] private float m_OpenDistance = 0.55f;
        [Tooltip("Насколько наискось можно вбежать в дверь, чтобы она распахнулась (0 - строго в лоб). Больше - " +
                 "двери начнут открываться, когда игрок пробегает вдоль них.")]
        [SerializeField, Range(0f, 89f)] private float m_MaxAngle = 60f;
        [Tooltip("Дверцы шкафов и щитков с разбега не открываются: дверь должна быть не ниже этого (м) и начинаться " +
                 "не выше колен игрока.")]
        [SerializeField, Min(0f)] private float m_MinDoorHeight = 1.5f;
        [Tooltip("Запертая дверь на пути дёргается (как при нажатии) - один раз, пока игрок бежит в неё.")]
        [SerializeField] private bool m_ShakeLockedDoor = true;
        [Tooltip("На каких слоях искать двери.")]
        [SerializeField] private LayerMask m_DoorMask = Physics.DefaultRaycastLayers;

        // Низ проходной двери - не выше колен игрока (от его ног), м.
        private const float MaxDoorBottomAboveFeet = 0.5f;
        // Капсула ищет дверь хотя бы на столько вперёд, м (упёршись в дверь, игрок стоит от неё в паре сантиметров).
        private const float MinCastDistance = 0.05f;

        private readonly RaycastHit[] m_Hits = new RaycastHit[16];
        private CharacterController m_Controller;
        private IInteractionDoor m_ShakenDoor;

        // Для гизмо в окне Scene: последний поиск двери.
        private Vector3 m_LastCastStart;
        private Vector3 m_LastCastDirection;
        private float m_LastCastDistance;
        private bool m_LastCastFoundDoor;

        private void Awake()
        {
            m_Controller = GetComponent<CharacterController>();

            if (m_Movement == null)
            {
                m_Movement = GetComponent<PlayerMovementStateMachine>();
            }
        }

        private void Update()
        {
            IInteractionDoor door = m_IsEnabled ? FindDoorAhead() : null;
            m_LastCastFoundDoor = door != null;

            if (door == null)
            {
                m_ShakenDoor = null;
                return;
            }

            if (door.IsLocked.CurrentValue)
            {
                if (m_ShakeLockedDoor && door != m_ShakenDoor)
                {
                    door.TryRushOpen(DoorOpenerKind.Human, transform.position);
                    m_ShakenDoor = door;
                }

                return;
            }

            m_ShakenDoor = null;
            door.TryRushOpen(DoorOpenerKind.Human, transform.position);
        }

        /// <summary>
        /// Ближайшая закрытая дверь, в которую игрок сейчас бежит. Двери ищутся капсулой игрока по направлению бега.
        /// </summary>
        private IInteractionDoor FindDoorAhead()
        {
            m_LastCastDistance = 0f;

            if (m_Movement == null || !m_Movement.isActiveAndEnabled || !m_Movement.IsSprinting
                || !m_Movement.IsCanMove || m_Movement.IsUseMouse)
            {
                return null;
            }

            Vector3 velocity = m_Movement.CurrentMoveVelocity;
            velocity.y = 0f;
            float speed = velocity.magnitude;

            if (speed < m_MinSpeed || speed < 0.01f)
            {
                return null;
            }

            Vector3 direction = velocity / speed;
            float radius = m_Controller.radius;
            Vector3 center = transform.TransformPoint(m_Controller.center);
            float halfLine = Mathf.Max(0f, m_Controller.height * 0.5f - radius);
            Vector3 top = center + Vector3.up * halfLine;
            Vector3 bottom = center - Vector3.up * halfLine;
            // Капсула ищет дверь на столько, сколько остаётся от её передней стенки до точки "камера в m_OpenDistance от двери".
            float cameraToFront = radius;

            if (m_Movement.CameraTransform != null)
            {
                Vector3 toCamera = m_Movement.CameraTransform.position - center;
                toCamera.y = 0f;
                cameraToFront = radius - Vector3.Dot(toCamera, direction);
            }

            float distance = Mathf.Max(MinCastDistance, m_OpenDistance - cameraToFront);

            m_LastCastStart = center;
            m_LastCastDirection = direction;
            m_LastCastDistance = distance;

            int count = Physics.CapsuleCastNonAlloc(bottom, top, radius, direction, m_Hits, distance, m_DoorMask,
                QueryTriggerInteraction.Ignore);
            float minFacing = Mathf.Cos(m_MaxAngle * Mathf.Deg2Rad);
            IInteractionDoor nearestDoor = null;
            float nearestDistance = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = m_Hits[i];

                if (hit.collider == null || hit.collider.transform.IsChildOf(transform) || hit.distance >= nearestDistance)
                {
                    continue;
                }

                var door = hit.collider.GetComponentInParent<IInteractionDoor>();

                if (door == null || door.IsOpen.CurrentValue)
                {
                    continue;
                }

                Bounds leaf = hit.collider.bounds;

                if (leaf.size.y < m_MinDoorHeight || leaf.min.y > transform.position.y + MaxDoorBottomAboveFeet)
                {
                    // Дверца шкафа или щитка, а не проход.
                    continue;
                }

                if (hit.distance <= 0f)
                {
                    // Капсула уже касается двери (игрок упёрся в неё): дверь должна быть впереди, а не сбоку.
                    Vector3 toDoor = hit.collider.ClosestPoint(center) - center;
                    toDoor.y = 0f;

                    if (toDoor.sqrMagnitude > 1e-6f && Vector3.Dot(toDoor.normalized, direction) < minFacing)
                    {
                        continue;
                    }
                }
                else
                {
                    Vector3 normal = hit.normal;
                    normal.y = 0f;

                    if (normal.sqrMagnitude < 1e-6f || Vector3.Dot(-normal.normalized, direction) < minFacing)
                    {
                        // Игрок пробегает вдоль двери, а не в неё (или задел её край сверху/снизу).
                        continue;
                    }
                }

                nearestDistance = hit.distance;
                nearestDoor = door;
            }

            return nearestDoor;
        }

        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying || m_LastCastDistance <= 0f)
            {
                return;
            }

            Gizmos.color = m_LastCastFoundDoor ? Color.green : Color.yellow;
            Vector3 end = m_LastCastStart + m_LastCastDirection * m_LastCastDistance;
            Gizmos.DrawLine(m_LastCastStart, end);
            Gizmos.DrawWireSphere(end, m_Controller != null ? m_Controller.radius : 0.3f);
        }
    }
}
