using System.Collections.Generic;
using Game.Scripts.InteractionObjects.Interfaces;
using UnityEngine;

namespace Game.Scripts.NPC.Runtime.Controllers
{
    /// <summary>
    /// Дверной проём для ботов. Бот, у которого в профиле анимаций и звуков есть анимации прохода в проём
    /// (NpcAnimationSoundProfileSO, "Проход в дверной проём"), проходит его этой анимацией: его ведут прямо через
    /// проём по кривой клипа, а клип показывает, как он проходит, протискивается или пробегает.
    /// Центр проёма - позиция объекта (на полу), ось Z объекта (forward) - поперёк проёма (из комнаты в комнату).
    /// Стоит на префабе двери HousingDoor; можно поставить и на проём без двери.
    /// </summary>
    public class NpcDoorway : MonoBehaviour
    {
        private static readonly List<NpcDoorway> s_All = new();

        [Tooltip("Ширина проёма (м).")]
        [SerializeField, Min(0.3f)] private float m_Width = 1.0f;

        [Tooltip("Высота проёма (м).")]
        [SerializeField, Min(0.5f)] private float m_Height = 2.1f;

        [Tooltip("Дверь в проёме (IInteractionDoor): пока она закрыта, бот проходом не идёт - сначала её открывает. " +
                 "Пусто - проём без двери.")]
        [SerializeField] private MonoBehaviour m_Door;

        /// <summary>Все включённые проёмы сцены.</summary>
        public static IReadOnlyList<NpcDoorway> All => s_All;

        public float Width => m_Width;
        public float Height => m_Height;
        public Vector3 Centre => transform.position;

        /// <summary>Поперёк проёма, по горизонтали.</summary>
        public Vector3 Normal
        {
            get
            {
                var n = transform.forward;
                n.y = 0f;
                return n.sqrMagnitude > 1e-6f ? n.normalized : Vector3.forward;
            }
        }

        /// <summary>Можно идти: двери нет или она открыта.</summary>
        public bool IsPassable => !(m_Door is IInteractionDoor door) || door.IsOpen.CurrentValue;

        private void OnEnable()
        {
            s_All.Add(this);
        }

        private void OnDisable()
        {
            s_All.Remove(this);
        }

        private void OnDrawGizmosSelected()
        {
            var right = Vector3.Cross(Vector3.up, Normal);
            var c = Centre;
            Gizmos.color = new Color(0.4f, 0.9f, 1f);
            var a = c - right * m_Width / 2f;
            var b = c + right * m_Width / 2f;
            Gizmos.DrawLine(a, b);
            Gizmos.DrawLine(a, a + Vector3.up * m_Height);
            Gizmos.DrawLine(b, b + Vector3.up * m_Height);
            Gizmos.DrawLine(a + Vector3.up * m_Height, b + Vector3.up * m_Height);
            Gizmos.DrawLine(c, c + Normal * 0.5f);
        }
    }
}
