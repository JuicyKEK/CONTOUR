using Game.Scripts.NPC.Runtime.Core;

namespace Game.Scripts.NPC.Runtime.States
{
    /// <summary>
    /// Ожидание: бот стоит на месте (агент остановлен) и ничего не делает сам
    /// по себе - используется как "пауза" перед сюжетным событием (например
    /// сотрудник газовой службы ждёт, пока игрок проверит документы) или как
    /// состояние по умолчанию до первой команды. Автоматических переходов нет -
    /// выход только по внешней команде (ForcePatrol/ForceObserve/ForceChase, шаги графа...).
    /// </summary>
    public class NpcIdleState : NpcState
    {
        public override NpcStateId Id => NpcStateId.Idle;

        public override void Enter(NpcBlackboard blackboard)
        {
            NpcNavigation.Halt(blackboard.Agent);
            blackboard.Controller.PlayAnimationSound("Idle");
        }

        public override NpcStateId? Tick(NpcBlackboard blackboard, float deltaTime)
        {
            return null;
        }

        public override void Exit(NpcBlackboard blackboard)
        {
            NpcNavigation.SetStopped(blackboard.Agent, false);
        }
    }
}
