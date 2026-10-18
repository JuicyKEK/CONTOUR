using AZE.AdvancedFirstPerson;
using Game.Scripts.Story;
using JuicyDI;
using JuicyDI.Attributes;
using UnityEngine;

namespace Game.Scripts.Player.External.Controllers
{
    /// <summary>
    /// Поимка игрока: монстр подошёл на расстояние удара - игрок больше не двигается, не крутит камеру и не
    /// взаимодействует с предметами, а камера плавно поворачивается к монстру (тело - по горизонтали, камера - по вертикали).
    /// Дальше смерть показывает тот, кто схватил (NpcController: поедание, затемнение, возврат к сохранению).
    /// Вешается на корень игрока. Release - вернуть управление (если сцену после поимки не перезагружают).
    /// </summary>
    [JDIMonoController]
    public class PlayerCaptureController : MonoBehaviour, IPlayerCapture
    {
        [SerializeField] private PlayerMovementStateMachine m_Movement;
        [SerializeField] private PlayerCameraController m_CameraController;
        [Tooltip("Куда смотрит камера: на этой высоте над опорой того, кто схватил (голова монстра).")]
        [SerializeField] private float m_LookAtHeight = 1.6f;
        [Tooltip("За сколько секунд камера поворачивается к схватившему.")]
        [SerializeField, Min(0.01f)] private float m_TurnDuration = 0.35f;

        private Transform m_Attacker;
        private float m_TurnTime;
        private Quaternion m_StartYaw;
        private Quaternion m_StartPitch;

        public bool IsCaptured { get; private set; }

        private void Awake()
        {
            if (m_Movement == null)
            {
                m_Movement = GetComponent<PlayerMovementStateMachine>();
            }

            if (m_CameraController == null)
            {
                m_CameraController = GetComponent<PlayerCameraController>();
            }
        }

        public void Capture(Transform attacker)
        {
            if (IsCaptured)
            {
                return;
            }

            IsCaptured = true;
            m_Attacker = attacker;
            m_TurnTime = 0f;
            m_StartYaw = transform.rotation;
            m_StartPitch = CameraTransform != null ? CameraTransform.localRotation : Quaternion.identity;

            if (m_Movement != null)
            {
                m_Movement.SetCanMovement(false);
            }

            SetInteractionEnabled(false);
        }

        public void Release()
        {
            if (!IsCaptured)
            {
                return;
            }

            IsCaptured = false;
            m_Attacker = null;

            if (m_Movement != null)
            {
                m_Movement.SetCanMovement(true);
            }

            SetInteractionEnabled(true);
        }

        private Transform CameraTransform => m_CameraController != null ? m_CameraController.CameraTransform : null;

        private void LateUpdate()
        {
            if (!IsCaptured || m_Attacker == null)
            {
                return;
            }

            // Камера игрока выключена (SetCanMovement(false)) - поворачиваем сами: тело к монстру, взгляд - на его голову.
            m_TurnTime += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(m_TurnTime / m_TurnDuration));
            var target = m_Attacker.position + Vector3.up * m_LookAtHeight;
            var flat = target - transform.position;
            flat.y = 0f;

            if (flat.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.Slerp(m_StartYaw, Quaternion.LookRotation(flat.normalized), t);
            }

            var cameraTransform = CameraTransform;

            if (cameraTransform != null)
            {
                var toTarget = target - cameraTransform.position;
                float pitch = -Mathf.Atan2(toTarget.y, new Vector2(toTarget.x, toTarget.z).magnitude) * Mathf.Rad2Deg;
                cameraTransform.localRotation = Quaternion.Slerp(m_StartPitch, Quaternion.Euler(pitch, 0f, 0f), t);
            }
        }

        private static void SetInteractionEnabled(bool isEnabled)
        {
            var handles = BinController.GetContext()?.GetBeans<IPlayerControlHandle>();

            if (handles == null)
            {
                return;
            }

            foreach (var handle in handles)
            {
                handle?.SetControlEnabled(isEnabled);
            }
        }
    }
}
