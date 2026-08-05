using Game.Scripts.NPC.Runtime.Core;
using UnityEngine;

namespace Game.Scripts.NPC.Runtime.States
{
    /// <summary>
    /// Ожидание: бот стоит на месте (агент остановлен) и ничего не делает сам
    /// по себе - используется как "пауза" перед сюжетным событием (например
    /// сотрудник газовой службы ждёт, пока игрок проверит документы) или как
    /// состояние по умолчанию до первой команды. Автоматических переходов нет -
    /// выход только по внешней команде (ForcePatrol/ForceObserve/ForceChase...).
    /// </summary>
    public class NpcIdleState : NpcState
    {
        public override NpcStateId Id => NpcStateId.Idle;

        public override void Enter(NpcBlackboard blackboard)
        {
            if (blackboard.Agent.isOnNavMesh)
            {
                blackboard.Agent.isStopped = true;
            }

            blackboard.Controller.PlayAnimationSound("Idle");
        }

        public override NpcStateId? Tick(NpcBlackboard blackboard, float deltaTime)
        {
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

