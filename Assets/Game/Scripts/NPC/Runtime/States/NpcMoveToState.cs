using Game.Scripts.NPC.Runtime.Core;
using UnityEngine;

namespace Game.Scripts.NPC.Runtime.States
{
    /// <summary>
    /// Идёт в точку по команде (NpcController.MoveTo, шаг графа "NPC/Move To Point") спокойно, быстро или бегом
    /// и там останавливается (дальше - Idle), сообщив о прибытии (условие графа "NPC/Arrived").
    /// Двери на пути: незапертые открывает (если умеет), у запертых стучит и ждёт, пока откроют.
    /// На игрока сам не реагирует - поведением управляет тот, кто дал команду.
    /// </summary>
    public class NpcMoveToState : NpcState
    {
        private const float FaceDegreesPerSecond = 360f;

        private bool m_HasArrived;
        private bool m_HasDestination;

        public override NpcStateId Id => NpcStateId.MoveTo;

        public override void Enter(NpcBlackboard blackboard)
        {
            m_HasArrived = false;
            blackboard.DoorKnockTimer = 0f;
            NpcNavigation.SetSpeed(blackboard.Agent, blackboard.GetSpeed(blackboard.MovePace));
            NpcNavigation.SetStopped(blackboard.Agent, false);
            m_HasDestination = NpcNavigation.TrySetDestination(blackboard.Agent, blackboard.MoveTarget);

            switch (blackboard.MovePace)
            {
                case NpcPace.Chase:
                    blackboard.Controller.PlayAnimationSound("Chase");
                    break;
                case NpcPace.Search:
                    blackboard.Controller.PlayAnimationSound("Search");
                    break;
                default:
                    blackboard.Controller.PlayAnimationSound("Patrol");
                    break;
            }
        }

        public override NpcStateId? Tick(NpcBlackboard blackboard, float deltaTime)
        {
            if (m_HasArrived)
            {
                return TickArrived(blackboard, deltaTime);
            }

            var door = NpcDoorNavigator.DetectDoorAhead(blackboard);

            if (door != null)
            {
                if (!door.IsLocked.CurrentValue && blackboard.Definition.CanOpenDoors)
                {
                    door.TryOpenBy(blackboard.Controller.GetDoorOpener(blackboard.MovePace == NpcPace.Chase));
                    NpcNavigation.SetStopped(blackboard.Agent, false);
                }
                else
                {
                    NpcNavigation.SetStopped(blackboard.Agent, true);
                    blackboard.DoorKnockTimer -= deltaTime;

                    if (blackboard.DoorKnockTimer <= 0f)
                    {
                        blackboard.DoorKnockTimer = blackboard.Definition.KnockInterval;
                        blackboard.Controller.PlayAnimationSound("Knock");
                    }

                    return null;
                }
            }
            else
            {
                NpcNavigation.SetStopped(blackboard.Agent, false);
            }

            // Команда пришла, когда агент ещё не стоял на NavMesh (бот только что включился), - повторяем.
            if (!m_HasDestination)
            {
                m_HasDestination = NpcNavigation.TrySetDestination(blackboard.Agent, blackboard.MoveTarget);
                return null;
            }

            // Встаём точно в точку (у окна, у ворот): допуск - только stoppingDistance агента и 5 см.
            if (!NpcNavigation.HasArrived(blackboard.Agent, 0.05f) || !IsNearTarget(blackboard))
            {
                return null;
            }

            m_HasArrived = true;

            if (blackboard.Agent != null && blackboard.Agent.pathStatus != UnityEngine.AI.NavMeshPathStatus.PathComplete)
            {
                Debug.LogWarning($"[NPC] {blackboard.Self.name}: точка '{blackboard.MoveTargetKey}' недостижима по NavMesh - бот дошёл " +
                                 "до ближайшего места. Проверьте NavMesh: проёмы дверей, высоту над проходом.", blackboard.Self);
            }

            NpcNavigation.Halt(blackboard.Agent);
            blackboard.Controller.NotifyArrived(blackboard.MoveTargetKey);

            if (!blackboard.MoveTargetRotation.HasValue)
            {
                return NpcStateId.Idle;
            }

            return null;
        }

        private static NpcStateId? TickArrived(NpcBlackboard blackboard, float deltaTime)
        {
            // Доворачиваемся, как повёрнута точка (лицом к окну, к игроку у ворот), и встаём.
            var rotation = blackboard.MoveTargetRotation.GetValueOrDefault(blackboard.Self.rotation);
            blackboard.Self.rotation = Quaternion.RotateTowards(blackboard.Self.rotation, rotation, FaceDegreesPerSecond * deltaTime);

            if (Quaternion.Angle(blackboard.Self.rotation, rotation) < 1f)
            {
                return NpcStateId.Idle;
            }

            return null;
        }

        private static bool IsNearTarget(NpcBlackboard blackboard)
        {
            var offset = blackboard.MoveTarget - blackboard.Self.position;
            offset.y = 0f;

            // Цель вне NavMesh или за непроходимым местом: агент дошёл до ближайшей возможной точки - тоже прибытие.
            return offset.magnitude <= 1f
                   || (blackboard.Agent != null && blackboard.Agent.pathStatus != UnityEngine.AI.NavMeshPathStatus.PathComplete);
        }

        public override void Exit(NpcBlackboard blackboard)
        {
            NpcNavigation.SetStopped(blackboard.Agent, false);
        }
    }
}
