using System;
using System.Collections.Generic;

namespace Game.Scripts.NPC.Runtime.Core
{
    /// <summary>
    /// Простой FSM-раннер: хранит все зарегистрированные состояния бота,
    /// текущее состояние и каждый кадр вызывает его Tick(), обрабатывая как
    /// автоматические переходы (возврат из Tick), так и принудительные
    /// (ChangeState вызванный извне - сюжетом/триггером).
    /// </summary>
    public class NpcStateMachine
    {
        private readonly Dictionary<NpcStateId, NpcState> m_States = new();
        private readonly NpcBlackboard m_Blackboard;

        public NpcStateId CurrentStateId { get; private set; }
        public event Action<NpcStateId, NpcStateId> StateChanged; // (from, to)

        public NpcStateMachine(NpcBlackboard blackboard)
        {
            m_Blackboard = blackboard;
        }

        public void Register(NpcState state)
        {
            m_States[state.Id] = state;
        }

        public void Start(NpcStateId startId)
        {
            CurrentStateId = startId;
            m_States[CurrentStateId].Enter(m_Blackboard);
        }

        public void Tick(float deltaTime)
        {
            var current = m_States[CurrentStateId];
            var next = current.Tick(m_Blackboard, deltaTime);

            if (next.HasValue && next.Value != CurrentStateId)
            {
                ChangeState(next.Value);
            }
        }

        /// <summary>
        /// Принудительный переход состояния (используется и автоматическими
        /// переходами внутри Tick, и внешними вызовами из NpcController по
        /// команде сюжета/триггера).
        /// </summary>
        public void ChangeState(NpcStateId newStateId)
        {
            if (!m_States.TryGetValue(newStateId, out var newState))
            {
                return;
            }

            var previous = CurrentStateId;
            m_States[CurrentStateId].Exit(m_Blackboard);
            CurrentStateId = newStateId;
            newState.Enter(m_Blackboard);
            StateChanged?.Invoke(previous, newStateId);
        }
    }
}

