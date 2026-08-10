using Game.Scripts.NPC.Runtime.Core;
using UnityEngine;
using UnityEngine.AI;

namespace Game.Scripts.NPC.Runtime.States
{
    /// <summary>
    /// Паника: бот убегает от игрока (используется для мирных ботов, которых
    /// напугали, или как реакция на непосредственную опасность). Пересчитывает
    /// точку бегства - в противоположную от игрока сторону, спроецированную на
    /// NavMesh - раз в FleeRecalcInterval секунд. Выход - только принудительно
    /// извне (сюжет/скрипт-триггер), автоматических переходов нет, т.к.
    /// "когда именно бот успокоится" - решение геймдизайна конкретной сцены.
    /// </summary>
    public class NpcPanicState : NpcState
    {
        private const float FleeDistance = 10f;
        private const float FleeRecalcInterval = 1.5f;

        private float m_RecalcTimer;

        public override NpcStateId Id => NpcStateId.Panic;

        public override void Enter(NpcBlackboard blackboard)
        {
            blackboard.Agent.speed = blackboard.Definition.PanicSpeed;
            blackboard.Agent.isStopped = false;
            m_RecalcTimer = 0f;
            blackboard.Controller.PlayAnimationSound("Panic");
        }

        public override NpcStateId? Tick(NpcBlackboard blackboard, float deltaTime)
        {
            m_RecalcTimer -= deltaTime;

            if (m_RecalcTimer <= 0f)
            {
                m_RecalcTimer = FleeRecalcInterval;
                RecalculateFleeDestination(blackboard);
            }

            return null;
        }

        private void RecalculateFleeDestination(NpcBlackboard blackboard)
        {
            if (blackboard.PlayerTransform == null)
            {
                return;
            }

            var away = blackboard.Self.position - blackboard.PlayerTransform.position;

            if (away.sqrMagnitude < 0.01f)
            {
                away = blackboard.Self.forward;
            }

            var fleeTarget = blackboard.Self.position + away.normalized * FleeDistance;

            if (NavMesh.SamplePosition(fleeTarget, out var hit, FleeDistance, NavMesh.AllAreas))
            {
                blackboard.Agent.SetDestination(hit.position);
            }
        }
    }
}

