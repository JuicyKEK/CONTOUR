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
        private static readonly RaycastHit[] s_Hits = new RaycastHit[16];

        /// <summary>
        /// Ищет ЗАКРЫТУЮ дверь (IInteractionDoor) впереди бота в направлении его текущего
        /// движения (desiredVelocity агента, либо forward - если агент почти стоит).
        /// Открытые двери не мешают и не возвращаются.
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
            Vector3 desired = agent.isActiveAndEnabled ? agent.desiredVelocity : Vector3.zero;
            desired.y = 0f;

            Vector3 direction = desired.sqrMagnitude > 0.01f
                ? desired.normalized
                : blackboard.Self.forward;

            int mask = definition.DoorDetectionMask.value != 0 ? definition.DoorDetectionMask.value : Physics.AllLayers;

            int count = Physics.SphereCastNonAlloc(
                origin,
                definition.DoorDetectionRadius,
                direction,
                s_Hits,
                definition.DoorDetectionDistance,
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
    }
}
