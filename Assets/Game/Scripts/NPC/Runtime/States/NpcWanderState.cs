using Game.Scripts.NPC.Runtime.Core;
using UnityEngine;
using UnityEngine.AI;

namespace Game.Scripts.NPC.Runtime.States
{
    /// <summary>
    /// Бесцельное блуждание по окрестностям текущей позиции - бот выбирает
    /// случайные достижимые точки на NavMesh в радиусе Definition.WanderRadius
    /// и идёт по ним одна за другой, периодически выбирая новую.
    ///
    /// Основной сценарий попадания сюда - агрессивный бот во время Chase упёрся
    /// в запертую дверь, не смог её открыть и "сдался" по истечении
    /// LockedDoorGiveUpDelay (см. NpcChaseState). Как и Patrol - если бот
    /// агрессивен и снова видит игрока, уходит в Chase; принудительно выйти в
    /// любое другое состояние можно из сюжета через NpcController.Force*.
    ///
    /// В отличие от Chase/Patrol, во время Wander бот НИКОГДА не пытается
    /// открывать двери (даже если Definition.CanOpenDoors = true) - если на
    /// пути к очередной случайной точке оказалась дверь (см. NpcDoorNavigator),
    /// бот просто отходит от неё в противоположную сторону и выбирает новую
    /// случайную точку - как будто "сдался" и не хочет больше связываться с
    /// этой дверью.
    /// </summary>
    public class NpcWanderState : NpcState
    {
        public override NpcStateId Id => NpcStateId.Wander;

        private float m_RepickTimer;

        public override void Enter(NpcBlackboard blackboard)
        {
            blackboard.Agent.speed = blackboard.Definition.PatrolSpeed;
            blackboard.Agent.isStopped = false;
            m_RepickTimer = 0f;
            PickNewDestination(blackboard);
            blackboard.Controller.PlayAnimationSound("Wander");
        }

        public override NpcStateId? Tick(NpcBlackboard blackboard, float deltaTime)
        {
            if (blackboard.ChaseReentrySuppressTimer > 0f)
            {
                blackboard.ChaseReentrySuppressTimer -= deltaTime;
            }
            else if (blackboard.AggressionSource != null
                && blackboard.AggressionSource.IsAggressive.CurrentValue
                && blackboard.CanSeePlayer)
            {
                return NpcStateId.Chase;
            }

            var doorAhead = NpcDoorNavigator.DetectDoorAhead(blackboard);

            if (doorAhead != null)
            {
                RetreatFromDoor(blackboard);
                return null;
            }

            m_RepickTimer -= deltaTime;

            bool reachedDestination = !blackboard.Agent.pathPending
                && blackboard.Agent.remainingDistance <= blackboard.Agent.stoppingDistance;

            if (reachedDestination || m_RepickTimer <= 0f)
            {
                PickNewDestination(blackboard);
            }

            return null;
        }

        /// <summary>
        /// Отход в сторону, противоположную обнаруженной двери, вместо
        /// продолжения движения к ней. Следующая случайная точка выбирается
        /// сразу же после того, как бот успеет отойти (см. m_RepickTimer).
        /// </summary>
        private void RetreatFromDoor(NpcBlackboard blackboard)
        {
            var definition = blackboard.Definition;
            Vector3 origin = blackboard.Self.position;
            Vector3 awayFromDoor = -blackboard.Self.forward;

            Vector3 retreatCandidate = origin + awayFromDoor * Mathf.Max(1f, definition.DoorDetectionDistance * 2f);

            if (NavMesh.SamplePosition(retreatCandidate, out var hit, definition.WanderRadius, NavMesh.AllAreas))
            {
                blackboard.Agent.SetDestination(hit.position);
            }

            m_RepickTimer = Random.Range(definition.WanderMinInterval, definition.WanderMaxInterval);
        }

        private void PickNewDestination(NpcBlackboard blackboard)
        {
            var definition = blackboard.Definition;
            Vector3 origin = blackboard.Self.position;

            for (int attempt = 0; attempt < 5; attempt++)
            {
                Vector2 randomCircle = Random.insideUnitCircle * definition.WanderRadius;
                Vector3 candidate = origin + new Vector3(randomCircle.x, 0f, randomCircle.y);

                if (NavMesh.SamplePosition(candidate, out var hit, definition.WanderRadius, NavMesh.AllAreas))
                {
                    blackboard.Agent.SetDestination(hit.position);
                    m_RepickTimer = Random.Range(definition.WanderMinInterval, definition.WanderMaxInterval);
                    return;
                }
            }

            // Не нашли валидную точку рядом - попробуем ещё раз чуть позже.
            m_RepickTimer = 1f;
        }
    }
}



