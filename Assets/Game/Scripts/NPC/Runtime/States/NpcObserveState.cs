using Game.Scripts.NPC.Runtime.Core;
using UnityEngine;

namespace Game.Scripts.NPC.Runtime.States
{
    /// <summary>
    /// Наблюдение: бот стоит на месте, но поворачивается лицом к игроку, пока
    /// видит его (например охранник/симулякр, который просто следит взглядом,
    /// не приближаясь). Если у бота включена агрессия и игрок виден - автоматом
    /// уходит в Chase. Для мирных ботов (агрессия выключена) это чисто
    /// декоративное состояние - переход в Chase никогда не произойдёт.
    /// </summary>
    public class NpcObserveState : NpcState
    {
        private const float RotationSpeedDegPerSec = 180f;

        public override NpcStateId Id => NpcStateId.Observe;

        public override void Enter(NpcBlackboard blackboard)
        {
            if (blackboard.Agent.isOnNavMesh)
            {
                blackboard.Agent.isStopped = true;
            }

            blackboard.Controller.PlayAnimationSound("Observe");
        }

        public override NpcStateId? Tick(NpcBlackboard blackboard, float deltaTime)
        {
            if (blackboard.CanSeePlayer)
            {
                var toPlayer = blackboard.PlayerTransform.position - blackboard.Self.position;
                toPlayer.y = 0f;

                if (toPlayer.sqrMagnitude > 0.001f)
                {
                    var targetRotation = Quaternion.LookRotation(toPlayer.normalized);
                    blackboard.Self.rotation = Quaternion.RotateTowards(
                        blackboard.Self.rotation, targetRotation, RotationSpeedDegPerSec * deltaTime);
                }
            }

            if (blackboard.AggressionSource != null
                && blackboard.AggressionSource.IsAggressive.CurrentValue
                && blackboard.CanSeePlayer)
            {
                return NpcStateId.Chase;
            }

            return null;
        }

        public override void Exit(NpcBlackboard blackboard)
        {
            if (blackboard.Agent.isOnNavMesh)
            {
                blackboard.Agent.isStopped = false;
            }
        }
    }
}

