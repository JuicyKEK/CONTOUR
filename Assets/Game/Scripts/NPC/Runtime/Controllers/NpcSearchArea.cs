using System.Collections.Generic;
using UnityEngine;

namespace Game.Scripts.NPC.Runtime.Controllers
{
    /// <summary>
    /// Область, по которой бот ищет игрока "по всей зоне" (см. NpcSearchState). Нужна, только если область
    /// поиска должна отличаться от зоны заражения бота: без неё бот ищет по триггерам своей зоны
    /// (InfectionZoneAreaTrigger). Задаётся коллайдерами на этом объекте и его детях (обычно BoxCollider-триггеры);
    /// при добавлении компонента коллайдер создаётся сам.
    /// </summary>
    public class NpcSearchArea : MonoBehaviour
    {
        private const int IgnoreRaycastLayer = 2;

        private readonly List<Collider> m_Colliders = new();

        public IReadOnlyList<Collider> Colliders
        {
            get
            {
                m_Colliders.Clear();
                GetComponentsInChildren(true, m_Colliders);
                return m_Colliders;
            }
        }

        private void Reset()
        {
            var area = GetComponent<Collider>();

            if (area == null)
            {
                area = gameObject.AddComponent<BoxCollider>();
            }

            area.isTrigger = true;
            gameObject.layer = IgnoreRaycastLayer;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.35f);

            foreach (var area in Colliders)
            {
                Gizmos.DrawCube(area.bounds.center, area.bounds.size);
            }
        }
    }
}
