namespace Game.Scripts.NPC.Runtime.Core
{
    /// <summary>
    /// Базовый класс одного состояния FSM бота. Состояния не знают друг о друге -
    /// только о NpcBlackboard. Автоматический переход между состояниями (например
    /// Patrol -> Chase при появлении игрока) возвращается из Tick(); принудительный
    /// переход "по требованию" (сюжет, триггер) идёт в обход - через
    /// NpcStateMachine.ChangeState(), которую дёргает NpcController.
    /// </summary>
    public abstract class NpcState
    {
        public abstract NpcStateId Id { get; }

        public virtual void Enter(NpcBlackboard blackboard)
        {
        }

        public virtual void Exit(NpcBlackboard blackboard)
        {
        }

        /// <summary>
        /// Возвращает id состояния, в которое нужно перейти, либо null, чтобы
        /// остаться в текущем.
        /// </summary>
        public abstract NpcStateId? Tick(NpcBlackboard blackboard, float deltaTime);
    }
}

