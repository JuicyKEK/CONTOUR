using UnityEngine;
using UnityEngine.Events;

namespace Game.Scripts.Scares
{
    /// <summary>
    /// Люк мусоропровода, который сам откидывается, когда игрок на него смотрит (центр экрана наведён на крышку
    /// и между камерой и крышкой ничего нет). Крышка поворачивается на петле - объекте <see cref="m_Hatch"/>
    /// (его точка стоит на нижнем крае крышки): падает вперёд с ускорением и пару раз отскакивает.
    /// По умолчанию остаётся открытым; можно закрывать, когда игрок отвёл взгляд.
    /// Open / Close можно звать и по сюжету (UnityEvent, StorySceneReactions).
    /// </summary>
    public class GarbageChuteHatch : MonoBehaviour
    {
        [Header("Крышка")]
        [Tooltip("Петля крышки: поворачивается вместе с крышкой вокруг своей точки.")]
        [SerializeField] private Transform m_Hatch;

        [Tooltip("Крышка - по её размерам определяется, куда надо смотреть, чтобы люк открылся.")]
        [SerializeField] private Renderer m_Lid;

        [Tooltip("Ось петли в осях объекта петли; знак выбран так, что поворот на плюс откидывает крышку наружу.")]
        [SerializeField] private Vector3 m_HingeAxis = Vector3.forward;

        [Tooltip("На сколько градусов откидывается крышка.")]
        [SerializeField] private float m_OpenAngle = 100f;

        [Tooltip("За сколько секунд крышка падает до открытого положения (потом ещё отскакивает).")]
        [SerializeField] private float m_OpenTime = 0.45f;

        [Tooltip("Насколько крышка отскакивает, ударившись в открытом положении, градусы (0 - без отскока).")]
        [SerializeField] private float m_Bounce = 9f;

        [Tooltip("За сколько секунд крышка закрывается.")]
        [SerializeField] private float m_CloseTime = 0.6f;

        [Header("Взгляд игрока")]
        [Tooltip("С какого расстояния взгляд на крышку открывает люк, метры.")]
        [SerializeField] private float m_LookDistance = 2.5f;

        [Tooltip("Насколько можно промахнуться мимо крышки и всё равно считать, что игрок смотрит на неё, метры.")]
        [SerializeField] private float m_LookPadding = 0.04f;

        [Tooltip("Камера игрока. Пусто - Camera.main.")]
        [SerializeField] private Camera m_PlayerCamera;

        [Tooltip("Коллайдеры внутри этого объекта не загораживают крышку (например, коллайдер самого мусоропровода). Пусто - этот объект.")]
        [SerializeField] private Transform m_IgnoreCollidersUnder;

        [Header("Поведение")]
        [Tooltip("Открытый люк остаётся открытым. Если выключено - закрывается, когда игрок отвёл взгляд.")]
        [SerializeField] private bool m_StayOpen = true;

        [Tooltip("Через сколько секунд после того, как игрок отвёл взгляд, люк закрывается (если не остаётся открытым).")]
        [SerializeField] private float m_CloseDelay = 1.5f;

        [Tooltip("Звук открывания (скрип, лязг). Запускается, когда люк начинает открываться.")]
        [SerializeField] private AudioSource m_OpenSound;

        [Tooltip("Звук закрывания.")]
        [SerializeField] private AudioSource m_CloseSound;

        [Tooltip("Что сделать, когда люк начал открываться (например, сигнал сюжету).")]
        [SerializeField] private UnityEvent m_OnOpened = new();

        private static readonly RaycastHit[] s_Hits = new RaycastHit[16];

        private Quaternion m_HatchRest;
        private Matrix4x4 m_LidZone;      // крышка в закрытом положении, в осях этого объекта
        private Bounds m_LidBounds;
        private bool m_IsOpen;
        private float m_Angle, m_Timer, m_FromAngle, m_LookAwayTime;

        /// <summary>Люк открыт (или открывается).</summary>
        public bool IsOpen => m_IsOpen;

        /// <summary>Текущий угол крышки, градусы (для отладки).</summary>
        public float Angle => m_Angle;

        /// <summary>Смотрит ли игрок на крышку прямо сейчас (для отладки).</summary>
        public bool IsLookedAt { get; private set; }

        private void Awake()
        {
            if (m_Hatch != null) m_HatchRest = m_Hatch.localRotation;
            if (m_IgnoreCollidersUnder == null) m_IgnoreCollidersUnder = transform;
            if (m_Lid != null)
            {
                var filter = m_Lid.GetComponent<MeshFilter>();
                m_LidBounds = filter != null && filter.sharedMesh != null ? filter.sharedMesh.bounds : new Bounds(Vector3.zero, Vector3.one * 0.2f);
                m_LidZone = transform.worldToLocalMatrix * m_Lid.transform.localToWorldMatrix;
            }
        }

        private void Update()
        {
            IsLookedAt = PlayerLooksAtLid();
            if (IsLookedAt)
            {
                m_LookAwayTime = 0f;
                if (!m_IsOpen) Open();
            }
            else if (m_IsOpen && !m_StayOpen)
            {
                m_LookAwayTime += Time.deltaTime;
                if (m_LookAwayTime >= m_CloseDelay) Close();
            }

            Animate(Time.deltaTime);
        }

        /// <summary>Откинуть крышку.</summary>
        [ContextMenu("Открыть")]
        public void Open()
        {
            if (m_IsOpen) return;
            m_IsOpen = true;
            m_Timer = 0f;
            m_FromAngle = m_Angle;
            if (m_OpenSound != null && m_OpenSound.clip != null) m_OpenSound.Play();
            m_OnOpened.Invoke();
        }

        /// <summary>Закрыть крышку.</summary>
        [ContextMenu("Закрыть")]
        public void Close()
        {
            if (!m_IsOpen) return;
            m_IsOpen = false;
            m_Timer = 0f;
            m_FromAngle = m_Angle;
            if (m_CloseSound != null && m_CloseSound.clip != null) m_CloseSound.Play();
        }

        private void Animate(float dt)
        {
            m_Timer += dt;
            if (m_IsOpen)
            {
                // Падает с ускорением, ударяется и отскакивает с затуханием.
                float fall = Mathf.Max(0.01f, m_OpenTime);
                if (m_Timer < fall)
                {
                    float u = m_Timer / fall;
                    m_Angle = Mathf.Lerp(m_FromAngle, m_OpenAngle, u * u);
                }
                else
                {
                    float b = m_Timer - fall;
                    float decay = Mathf.Exp(-b * 6f);
                    m_Angle = m_OpenAngle - m_Bounce * Mathf.Abs(Mathf.Sin(b * Mathf.PI * 3.2f)) * decay;
                }
            }
            else
            {
                float u = Mathf.Clamp01(m_Timer / Mathf.Max(0.01f, m_CloseTime));
                m_Angle = Mathf.Lerp(m_FromAngle, 0f, u * u * (3f - 2f * u));
            }

            if (m_Hatch != null) m_Hatch.localRotation = m_HatchRest * Quaternion.AngleAxis(m_Angle, m_HingeAxis);
        }

        private bool PlayerLooksAtLid()
        {
            if (m_Lid == null) return false;
            var camera = m_PlayerCamera != null ? m_PlayerCamera : Camera.main;
            if (camera == null) return false;

            // Луч из центра экрана - в оси крышки (в закрытом положении), там она - коробка m_LidBounds.
            var origin = camera.transform.position;
            var direction = camera.transform.forward;
            var toLocal = (transform.localToWorldMatrix * m_LidZone).inverse;
            var localOrigin = toLocal.MultiplyPoint3x4(origin);
            var localDirection = toLocal.MultiplyVector(direction);
            var bounds = m_LidBounds;
            float scale = Mathf.Max(1e-4f, m_Lid.transform.lossyScale.x);
            bounds.Expand(2f * m_LookPadding / scale);
            if (!bounds.IntersectRay(new Ray(localOrigin, localDirection.normalized), out float localDistance)) return false;
            var hitLocal = localOrigin + localDirection.normalized * localDistance;
            float distance = Vector3.Distance(origin, toLocal.inverse.MultiplyPoint3x4(hitLocal));
            if (distance > m_LookDistance) return false;

            // Ничего не загораживает: всё, во что луч попадает раньше крышки, - сам мусоропровод или сам игрок.
            int count = Physics.RaycastNonAlloc(origin, direction, s_Hits, Mathf.Max(0f, distance - 0.01f), Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                var other = s_Hits[i].collider.transform;
                if (!other.IsChildOf(m_IgnoreCollidersUnder) && !other.IsChildOf(transform) && !other.IsChildOf(camera.transform.root) && !other.root.CompareTag("Player"))
                {
                    return false;
                }
            }

            return true;
        }

        private void OnDrawGizmosSelected()
        {
            if (m_Hatch == null) return;
            Gizmos.color = Color.yellow;
            var axis = m_Hatch.TransformDirection(m_HingeAxis.normalized) * 0.2f;
            Gizmos.DrawLine(m_Hatch.position - axis, m_Hatch.position + axis);
        }
    }
}
