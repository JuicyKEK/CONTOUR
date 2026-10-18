using Game.Scripts.NPC.Runtime.Core;

namespace Game.Scripts.NPC.Runtime.States
{
    /// <summary>
    /// Наблюдение: бот стоит на месте, но поворачивается лицом к игроку, пока
    /// видит его (например охранник/мимик, который просто следит взглядом,
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
            NpcNavigation.Halt(blackboard.Agent);
            blackboard.Controller.PlayAnimationSound("Observe");
        }

        public override NpcStateId? Tick(NpcBlackboard blackboard, float deltaTime)
        {
            if (blackboard.CanSeePlayer && blackboard.PlayerTransform != null)
            {
                NpcNavigation.FaceTowards(blackboard.Self, blackboard.PlayerTransform.position, RotationSpeedDegPerSec, deltaTime);
            }

            if (blackboard.IsAggressive && blackboard.CanSeePlayer)
            {
                return NpcStateId.Chase;
            }

            return null;
        }

        public override void Exit(NpcBlackboard blackboard)
        {
            NpcNavigation.SetStopped(blackboard.Agent, false);
        }
    }
}
