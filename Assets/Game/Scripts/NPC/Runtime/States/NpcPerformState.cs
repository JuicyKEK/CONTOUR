using Game.Scripts.NPC.Runtime.Core;

namespace Game.Scripts.NPC.Runtime.States
{
    /// <summary>
    /// Действие на месте по ключу анимации/звука (Blackboard.PerformKey): превращение мимика, злость у будки,
    /// "отдаёт посылку". Длится PerformDuration секунд, потом бот переходит в PerformNextState;
    /// длительность 0 - стоит с этим действием, пока не придёт другая команда. Задаётся NpcController.PlayAction
    /// (шаги графа "NPC/Play Action", "NPC/Change Form").
    /// </summary>
    public class NpcPerformState : NpcState
    {
        private float m_Time;
        private bool m_IsFinished;

        public override NpcStateId Id => NpcStateId.Perform;

        public override void Enter(NpcBlackboard blackboard)
        {
            m_Time = 0f;
            m_IsFinished = false;
            NpcNavigation.Halt(blackboard.Agent);
            blackboard.Controller.PlayAnimationSound(blackboard.PerformKey);
        }

        public override NpcStateId? Tick(NpcBlackboard blackboard, float deltaTime)
        {
            if (m_IsFinished || blackboard.PerformDuration <= 0f)
            {
                return null;
            }

            m_Time += deltaTime;

            if (m_Time < blackboard.PerformDuration)
            {
                return null;
            }

            m_IsFinished = true;
            blackboard.Controller.NotifyActionFinished();

            if (blackboard.PerformNextState == NpcStateId.Perform)
            {
                return null;
            }

            if (blackboard.PerformNextState == blackboard.CalmState)
            {
                blackboard.Controller.PrepareReturnToCalm();
            }

            return blackboard.PerformNextState;
        }

        public override void Exit(NpcBlackboard blackboard)
        {
            NpcNavigation.SetStopped(blackboard.Agent, false);
        }
    }
}
