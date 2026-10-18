using System.Collections.Generic;
using Game.Scripts.InfectionZone.External.Controllers;
using Game.Scripts.InfectionZone.Runtime.Interfaces;
using UnityEngine;
using UnityEngine.AI;

namespace Game.Scripts.NPC.Runtime.Core
{
    /// <summary>
    /// Зона бота: в какой зоне заражения он стоит (по триггерам зон, InfectionZoneAreaTrigger) и случайные
    /// точки внутри зоны или области поиска - для поиска игрока по всей зоне.
    /// </summary>
    public static class NpcZoneUtility
    {
        private const float InsideTolerance = 0.01f;

        /// <summary>
        /// Зона заражения, триггер которой накрывает точку, или null.
        /// </summary>
        public static IInfectionZone FindZoneAt(Vector3 position)
        {
            var triggers = Object.FindObjectsByType<InfectionZoneAreaTrigger>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            var colliders = new List<Collider>();

            foreach (var trigger in triggers)
            {
                if (trigger.Zone == null)
                {
                    continue;
                }

                colliders.Clear();
                trigger.GetComponents(colliders);

                foreach (var area in colliders)
                {
                    if (area.enabled && IsInsideXZ(area, position))
                    {
                        return trigger.Zone;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Все коллайдеры триггеров зоны (её территория).
        /// </summary>
        public static void CollectZoneColliders(IInfectionZone zone, List<Collider> result)
        {
            result.Clear();

            if (zone == null)
            {
                return;
            }

            var triggers = Object.FindObjectsByType<InfectionZoneAreaTrigger>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            var colliders = new List<Collider>();

            foreach (var trigger in triggers)
            {
                if (!ReferenceEquals(trigger.Zone, zone))
                {
                    continue;
                }

                colliders.Clear();
                trigger.GetComponents(colliders);

                foreach (var area in colliders)
                {
                    if (area.enabled)
                    {
                        result.Add(area);
                    }
                }
            }
        }

        /// <summary>
        /// Случайная достижимая точка NavMesh внутри одного из коллайдеров области (крупные области выпадают чаще).
        /// </summary>
        public static bool TryGetRandomPointInAreas(NavMeshAgent agent, IReadOnlyList<Collider> areas, out Vector3 point)
        {
            point = default;

            if (areas == null || areas.Count == 0)
            {
                return false;
            }

            float totalWeight = 0f;

            for (int i = 0; i < areas.Count; i++)
            {
                totalWeight += AreaWeight(areas[i]);
            }

            for (int attempt = 0; attempt < 12; attempt++)
            {
                var area = PickWeighted(areas, totalWeight);

                if (area == null)
                {
                    return false;
                }

                var bounds = area.bounds;
                var candidate = new Vector3(
                    Random.Range(bounds.min.x, bounds.max.x),
                    agent.transform.position.y,
                    Random.Range(bounds.min.z, bounds.max.z));

                if (NpcNavigation.TryGetReachable(agent, candidate, 2f, position => IsInsideXZ(area, position), out point))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsInside(Collider area, Vector3 position)
        {
            return (area.ClosestPoint(position) - position).sqrMagnitude <= InsideTolerance * InsideTolerance;
        }

        /// <summary>
        /// Точка внутри области по горизонтали (высоту берём из середины области - точки NavMesh лежат на полу,
        /// а триггер зоны может начинаться выше или ниже пола).
        /// </summary>
        private static bool IsInsideXZ(Collider area, Vector3 position)
        {
            var bounds = area.bounds;
            position.y = Mathf.Clamp(position.y + 0.5f, bounds.min.y, bounds.max.y);
            return IsInside(area, position);
        }

        private static float AreaWeight(Collider area)
        {
            var size = area.bounds.size;
            return Mathf.Max(0.01f, size.x * size.z);
        }

        private static Collider PickWeighted(IReadOnlyList<Collider> areas, float totalWeight)
        {
            float roll = Random.value * totalWeight;

            for (int i = 0; i < areas.Count; i++)
            {
                roll -= AreaWeight(areas[i]);

                if (roll <= 0f)
                {
                    return areas[i];
                }
            }

            return areas.Count > 0 ? areas[areas.Count - 1] : null;
        }
    }
}
