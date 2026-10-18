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
        public bool IsStarted { get; private set; }
        public event Action<NpcStateId, NpcStateId> StateChanged; // (from, to)

        public NpcStateMachine(NpcBlackboard blackboard)
        {
            m_Blackboard = blackboard;
        }

        public void Register(NpcState state)
        {
            m_States[state.Id] = state;
        }

        public bool IsRegistered(NpcStateId id)
        {
            return m_States.ContainsKey(id);
        }

        public void Start(NpcStateId startId)
        {
            CurrentStateId = m_States.ContainsKey(startId) ? startId : NpcStateId.Idle;
            IsStarted = true;
            m_States[CurrentStateId].Enter(m_Blackboard);
            StateChanged?.Invoke(CurrentStateId, CurrentStateId);
        }

        public void Tick(float deltaTime)
        {
            if (!IsStarted)
            {
                return;
            }

            var current = m_States[CurrentStateId];
            var next = current.Tick(m_Blackboard, deltaTime);

            // Пока состояние тикало, его могла сменить внешняя команда (например событие удара вызвало
            // LoseInterest) - тогда переход, который вернуло старое состояние, уже не актуален.
            if (!ReferenceEquals(current, m_States[CurrentStateId]))
            {
                return;
            }

            if (next.HasValue && next.Value != CurrentStateId)
            {
                ChangeState(next.Value);
            }
        }

        /// <summary>
        /// Принудительный переход состояния (используется и автоматическими
        /// переходами внутри Tick, и внешними вызовами из NpcController по
        /// команде сюжета/триггера). Переход в то же состояние перезапускает его
        /// (например новая точка для MoveTo или новый маршрут для Patrol).
        /// </summary>
        public void ChangeState(NpcStateId newStateId)
        {
            if (!m_States.TryGetValue(newStateId, out var newState))
            {
                return;
            }

            if (!IsStarted)
            {
                Start(newStateId);
                return;
            }

            var previous = CurrentStateId;
            m_States[CurrentStateId].Exit(m_Blackboard);
            CurrentStateId = newStateId;
            newState.Enter(m_Blackboard);
            StateChanged?.Invoke(previous, newStateId);
        }

        /// <summary>
        /// Повторный вход в текущее состояние - когда выключенный бот снова включился (агенту нужно заново
        /// выставить цель, аниматору - текущую анимацию).
        /// </summary>
        public void Reenter()
        {
            if (!IsStarted)
            {
                return;
            }

            var state = m_States[CurrentStateId];
            state.Exit(m_Blackboard);
            state.Enter(m_Blackboard);
            StateChanged?.Invoke(CurrentStateId, CurrentStateId);
        }
    }
}
