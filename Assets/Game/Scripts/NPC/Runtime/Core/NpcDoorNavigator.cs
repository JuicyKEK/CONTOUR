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
        /// <summary>
        /// Ищет дверь (IInteractionDoor) впереди бота в направлении его текущего
        /// движения (desiredVelocity агента, либо forward - если агент почти стоит).
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
            Vector3 desired = agent.desiredVelocity;

            Vector3 direction = desired.sqrMagnitude > 0.01f
                ? desired.normalized
                : blackboard.Self.forward;

            if (Physics.SphereCast(
                    origin,
                    definition.DoorDetectionRadius,
                    direction,
                    out var hit,
                    definition.DoorDetectionDistance,
                    definition.DoorDetectionMask,
                    QueryTriggerInteraction.Collide))
            {
                return hit.collider.GetComponentInParent<IInteractionDoor>();
            }

            return null;
        }
    }
}

