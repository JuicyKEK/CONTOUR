using Game.Scripts.InteractionObjects.Interfaces;
using UnityEngine;

namespace Game.Scripts.NPC.Runtime.Core
{
    /// <summary>
    /// Живой (не заранее расставленный, в отличие от NpcPatrolPoint.DoorToPass)
    /// поиск двери прямо по курсу движения бота - нужен состояниям вроде Chase,
    /// где маршрут динамический (за игроком) и заранее привязать дверь нельзя.
    /// </summary>
    public static class NpcDoorNavigator
    {
        // Дверь ищется до поворота пути и чуть дальше; но не ближе этого, чтобы дверь сразу за поворотом не терялась.
        private const float MinCastDistance = 0.6f;
        private const float CornerMargin = 0.3f;

        private static readonly RaycastHit[] s_Hits = new RaycastHit[16];

        /// <summary>
        /// Ищет ЗАКРЫТУЮ дверь (IInteractionDoor) впереди бота по его пути: к следующему повороту пути агента и
        /// не дальше этого поворота (дверь за поворотом, куда путь не ведёт, - не на пути). Пути нет - по
        /// desiredVelocity агента, а если он стоит - по forward. Открытые двери не мешают и не возвращаются.
        /// </summary>
        public static IInteractionDoor DetectDoorAhead(NpcBlackboard blackboard)
        {
            var definition = blackboard.Definition;
            var agent = blackboard.Agent;

            if (agent == null)
            {
                return null;
            }

            Vector3 origin = blackboard.Self.position + Vector3.up * 0.9f;
            Vector3 direction;
            float distance = definition.DoorDetectionDistance;

            if (TryGetPathDirection(agent, blackboard.Self, out var toCorner, out float cornerDistance))
            {
                direction = toCorner;
                distance = Mathf.Min(distance, Mathf.Max(MinCastDistance, cornerDistance + CornerMargin));
            }
            else
            {
                Vector3 desired = agent.isActiveAndEnabled ? agent.desiredVelocity : Vector3.zero;
                desired.y = 0f;
                direction = desired.sqrMagnitude > 0.01f ? desired.normalized : blackboard.Self.forward;
            }

            int mask = definition.DoorDetectionMask.value != 0 ? definition.DoorDetectionMask.value : Physics.AllLayers;

            int count = Physics.SphereCastNonAlloc(
                origin,
                definition.DoorDetectionRadius,
                direction,
                s_Hits,
                distance,
                mask,
                QueryTriggerInteraction.Collide);

            IInteractionDoor nearestDoor = null;
            float nearestDistance = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                var hit = s_Hits[i];

                if (hit.collider == null || hit.collider.transform.IsChildOf(blackboard.Self))
                {
                    continue;
                }

                var door = hit.collider.GetComponentInParent<IInteractionDoor>();

                if (door == null || door.IsOpen.CurrentValue)
                {
                    continue;
                }

                // Дистанция 0 - каст начался внутри коллайдера (бот уже в проёме): такая дверь не впереди.
                if (hit.distance <= 0f || hit.distance >= nearestDistance)
                {
                    continue;
                }

                nearestDistance = hit.distance;
                nearestDoor = door;
            }

            return nearestDoor;
        }

        /// <summary>
        /// Направление и расстояние до следующего поворота пути агента. Так "впереди" - туда, куда ведёт путь, а не
        /// куда бот смотрит или куда проскочит прямой луч: иначе бот у поворота (или закончивший поиск лицом к запертой
        /// двери) считал бы своей дверь за поворотом и бился в неё, хотя путь идёт мимо.
        /// </summary>
        private static bool TryGetPathDirection(UnityEngine.AI.NavMeshAgent agent, Transform self, out Vector3 direction, out float distance)
        {
            direction = default;
            distance = 0f;

            if (!agent.isActiveAndEnabled || !agent.isOnNavMesh || !agent.hasPath)
            {
                return false;
            }

            // The first corner of the path that is not right under the bot (standing on a corner, the next one counts).
            foreach (var corner in agent.path.corners)
            {
                Vector3 toCorner = corner - self.position;
                toCorner.y = 0f;
                distance = toCorner.magnitude;

                if (distance >= 0.1f)
                {
                    direction = toCorner / distance;
                    return true;
                }
            }

            return false;
        }
    }
}
