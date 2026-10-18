using Game.Scripts.LocationScene.Runtime.Data;
using Game.Scripts.LocationScene.Runtime.Interfaces;
using JuicyDI.Attributes;
using UnityEngine;

namespace Game.Scripts.LocationScene.Runtime.Controllers
{
    /// <summary>
    /// Триггер на конце переходной сцены. У перехода их два - по одному у каждой из двух локаций.
    /// Для игрока, который идёт из своей локации, ближний триггер - вход в переход (начинает грузиться
    /// соседняя локация), дальний - выход (соседняя локация становится текущей, прежняя выгружается).
    /// Если игрок, задев входной триггер, развернулся и вышел обратно в свою локацию, соседняя выгружается.
    ///
    /// Настройка: коллайдер-триггер поперёк прохода на конце перехода, ссылка на переход и на локацию,
    /// к которой ведёт этот конец. Синяя стрелка объекта (ось Z) должна смотреть наружу, в сторону этой
    /// локации: по ней триггер понимает, куда вышел игрок - в локацию или внутрь перехода.
    /// При добавлении компонента коллайдер сам становится триггером, а объект - слоем Ignore Raycast
    /// (чтобы триггер не перехватывал лучи взаимодействия игрока).
    /// </summary>
    [JDIMonoController]
    public class LocationTransitionGate : MonoBehaviour
    {
        private const int IgnoreRaycastLayer = 2;

        [Header("Переход")]
        [Tooltip("Переход, в сцене которого стоит этот триггер.")]
        [SerializeField] private LocationTransitionDefinitionSO m_Transition;
        [Tooltip("Локация, к которой ведёт этот конец перехода. Синяя стрелка объекта (ось Z) должна " +
                 "смотреть в её сторону.")]
        [SerializeField] private LocationDefinitionSO m_Location;

        [Header("Игрок")]
        [Tooltip("Тег объекта игрока.")]
        [SerializeField] private string m_PlayerTag = "Player";

        [Inject] private ILocationSceneService m_LocationService;

        private void Reset()
        {
            var trigger = GetComponent<Collider>();

            if (trigger == null)
            {
                trigger = gameObject.AddComponent<BoxCollider>();
            }

            trigger.isTrigger = true;
            gameObject.layer = IgnoreRaycastLayer;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(m_PlayerTag))
            {
                return;
            }

            m_LocationService?.PlayerTouchedGate(m_Transition, m_Location);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag(m_PlayerTag))
            {
                return;
            }

            // Игрок вышел из триггера со стороны, куда смотрит ось Z, - значит, в локацию; иначе - внутрь перехода.
            bool isInLocation = Vector3.Dot(other.transform.position - transform.position, transform.forward) > 0f;

            m_LocationService?.PlayerLeftGate(m_Transition, m_Location, isInLocation);
        }

        private void OnDrawGizmos()
        {
            // Стрелка в сторону локации, к которой ведёт этот конец перехода.
            var start = transform.position;
            var end = start + transform.forward * 1.5f;

            Gizmos.color = new Color(0.2f, 0.75f, 1f);
            Gizmos.DrawLine(start, end);
            Gizmos.DrawSphere(end, 0.1f);

#if UNITY_EDITOR
            UnityEditor.Handles.Label(end, m_Location != null ? $"к локации {m_Location.name}" : "локация не назначена");
#endif
        }
    }
}
