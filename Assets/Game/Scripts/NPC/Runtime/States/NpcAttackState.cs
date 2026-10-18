using Game.Scripts.NPC.Runtime.Core;
using Game.Scripts.NPC.Runtime.Data;

namespace Game.Scripts.NPC.Runtime.States
{
    /// <summary>
    /// Атака: бот останавливается, поворачивается к игроку и играет "Attack".
    ///  - Убийство (NpcTypeDefinitionSO.AttackKind): бот хватает игрока сразу, как подошёл на расстояние удара -
    ///    убежать нельзя; дальше NpcController: управление у игрока отбирается, поедание, затемнение, загрузка.
    ///  - Удар: через AttackHitDelay (замах) удар попадает, если игрок всё ещё рядом; отбежал - промах, погоня дальше.
    ///    После удара бот по AfterHit стоит и ждёт команды, возвращается к спокойному поведению или гонится дальше.
    /// </summary>
    public class NpcAttackState : NpcState
    {
        private const float FaceDegreesPerSecond = 540f;
        private const float ReachTolerance = 0.6f;

        private float m_Time;
        private bool m_IsResolved;
        private bool m_IsLanded;

        public override NpcStateId Id => NpcStateId.Attack;

        public override void Enter(NpcBlackboard blackboard)
        {
            m_Time = 0f;
            m_IsResolved = false;
            m_IsLanded = false;
            NpcNavigation.Halt(blackboard.Agent);
            blackboard.Controller.PlayAnimationSound("Attack");
        }

        public override NpcStateId? Tick(NpcBlackboard blackboard, float deltaTime)
        {
            var definition = blackboard.Definition;

            // Убийство - хватает сразу: игрок уже на расстоянии удара (поэтому погоня и перешла в атаку).
            if (definition.AttackKind == NpcAttackKind.Kill && !m_IsResolved)
            {
                m_IsResolved = true;

                if (blackboard.DistanceToPlayer <= definition.CatchDistance + ReachTolerance && !blackboard.Controller.IsPlayerCaptured)
                {
                    m_IsLanded = true;
                    // Бот переходит к поеданию (Perform 'Kill') прямо отсюда.
                    blackboard.Controller.ApplyAttack();
                    return null;
                }

                return NpcStateId.Chase;
            }

            m_Time += deltaTime;

            if (blackboard.PlayerTransform != null && !m_IsLanded)
            {
                NpcNavigation.FaceTowards(blackboard.Self, blackboard.PlayerTransform.position, FaceDegreesPerSecond, deltaTime);
            }

            if (!m_IsResolved && m_Time >= definition.AttackHitDelay)
            {
                m_IsResolved = true;
                bool isInReach = blackboard.DistanceToPlayer <= definition.CatchDistance + ReachTolerance;
                bool knowsWherePlayerIs = blackboard.CanSeePlayer || blackboard.IsHunting;

                if (isInReach && knowsWherePlayerIs)
                {
                    m_IsLanded = true;
                    blackboard.Controller.ApplyAttack();
                }
                else
                {
                    blackboard.Controller.LogDebug("промах - игрок отбежал");
                }
            }

            if (m_Time < definition.AttackDuration)
            {
                return null;
            }

            blackboard.AttackCooldownTimer = definition.AttackCooldown;

            if (!m_IsLanded)
            {
                return NpcStateId.Chase;
            }

            if (definition.AttackKind == NpcAttackKind.Kill)
            {
                return NpcStateId.Idle;
            }

            switch (definition.AfterHit)
            {
                case NpcAfterHitBehaviour.ReturnToCalm:
                    blackboard.Controller.PrepareReturnToCalm();
                    return blackboard.CalmState;

                case NpcAfterHitBehaviour.KeepChasing:
                    return NpcStateId.Chase;

                default:
                    return NpcStateId.Idle;
            }
        }

        public override void Exit(NpcBlackboard blackboard)
        {
            NpcNavigation.SetStopped(blackboard.Agent, false);
        }
    }
}
