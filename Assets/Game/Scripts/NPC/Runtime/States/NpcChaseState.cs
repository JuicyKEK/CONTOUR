using Game.Scripts.NPC.Runtime.Core;
using UnityEngine;

namespace Game.Scripts.NPC.Runtime.States
{
    /// <summary>
    /// Преследование: агент через NavMesh ищет путь до игрока и пытается его
    /// догнать. Останавливается тремя способами (см. NpcTypeDefinitionSO):
    ///  - сама, потеряв игрока из виду дольше LoseSightGiveUpDelay секунд;
    ///  - никогда сама не останавливается, если ChaseStopsOnlyByStory = true
    ///    (тогда выйти можно только принудительно из сюжета - ForcePatrol/ForceIdle);
    ///  - мгновенно, если у бота отключили агрессию (ManualAggressionSource.SetAggressive(false)).
    /// Догнав игрока на CatchDistance - сообщает контроллеру о поимке (тот сам
    /// решает что дальше: анимация "Eat", событие для сюжета и т.д.).
    ///
    /// Двери на пути (см. NpcDoorNavigator - живой поиск двери по курсу
    /// движения, т.к. маршрут погони не расставлен заранее, в отличие от
    /// Patrol): если CanOpenDoors и дверь не заперта - бот открывает её на
    /// ходу (TryOpenBy, анимация открытия зависит от ChaseDoorOpenerKind -
    /// человек или монстр) и продолжает погоню. Если дверь заперта - бот
    /// останавливается, периодически пытается её открыть (что проигрывает
    /// анимацию "не поддаётся"), и если за LockedDoorGiveUpDelay секунд дверь
    /// так и не открыли - сдаётся и уходит блуждать (NpcStateId.Wander).
    /// </summary>
    public class NpcChaseState : NpcState
    {
        public override NpcStateId Id => NpcStateId.Chase;

        public override void Enter(NpcBlackboard blackboard)
        {
            blackboard.Agent.speed = blackboard.Definition.ChaseSpeed;
            blackboard.Agent.isStopped = false;
            blackboard.Agent.angularSpeed = 180f; 
            blackboard.TimeSincePlayerLastSeen = 0f;
            blackboard.DoorKnockTimer = 0f;
            blackboard.DoorBlockedTimer = 0f;
            blackboard.Controller.PlayAnimationSound("Chase");
            blackboard.Controller.RaiseReachedChaseStart();
        }

        public override NpcStateId? Tick(NpcBlackboard blackboard, float deltaTime)
        {
            // Агрессию выключили извне (сюжет/деактивация мимика и т.п.) - бросаем погоню сразу.
            if (blackboard.AggressionSource != null && !blackboard.AggressionSource.IsAggressive.CurrentValue)
            {
                return NpcStateId.Patrol;
            }

            if (blackboard.CanSeePlayer)
            {
                blackboard.TimeSincePlayerLastSeen = 0f;
                blackboard.LastKnownPlayerPosition = blackboard.PlayerTransform.position;
            }
            else
            {
                blackboard.TimeSincePlayerLastSeen += deltaTime;
            }

            if (blackboard.Definition.CanOpenDoors)
            {
                var doorResult = HandleDoorsAhead(blackboard, deltaTime);

                if (doorResult.HasValue)
                {
                    return doorResult;
                }
            }

            blackboard.Agent.SetDestination(blackboard.LastKnownPlayerPosition);

            if (blackboard.DistanceToPlayer <= blackboard.Definition.CatchDistance)
            {
                blackboard.Controller.CatchPlayer();
            }

            if (!blackboard.Definition.ChaseStopsOnlyByStory
                && blackboard.TimeSincePlayerLastSeen >= blackboard.Definition.LoseSightGiveUpDelay)
            {
                return NpcStateId.Patrol;
            }

            return null;
        }

        /// <summary>
        /// Возвращает NpcStateId.Wander, если пора сдаться перед запертой
        /// дверью; null, если можно продолжать (движение к игроку выполняется
        /// в Tick() уже после этого вызова).
        /// </summary>
        private NpcStateId? HandleDoorsAhead(NpcBlackboard blackboard, float deltaTime)
        {
            var door = NpcDoorNavigator.DetectDoorAhead(blackboard);

            if (door == null || door.IsOpen.CurrentValue)
            {
                blackboard.DoorBlockedTimer = 0f;
                blackboard.DoorKnockTimer = 0f;
                blackboard.Agent.isStopped = false;
                return null;
            }

            if (!door.IsLocked.CurrentValue)
            {
                door.TryOpenBy(blackboard.Definition.ChaseDoorOpenerKind);
                blackboard.DoorBlockedTimer = 0f;
                blackboard.DoorKnockTimer = 0f;
                return null;
            }

            // Дверь заперта - стоим, периодически пытаемся, и по истечении
            // лимита времени сдаёмся и уходим блуждать.
            blackboard.Agent.isStopped = true;
            blackboard.DoorBlockedTimer += deltaTime;
            blackboard.DoorKnockTimer -= deltaTime;

            if (blackboard.DoorKnockTimer <= 0f)
            {
                blackboard.DoorKnockTimer = blackboard.Definition.KnockInterval;
                door.TryOpenBy(blackboard.Definition.ChaseDoorOpenerKind);
                blackboard.Controller.PlayAnimationSound("DoorLockedTry");
            }
            
            if (blackboard.DoorBlockedTimer >= blackboard.Definition.LockedDoorGiveUpDelay)
            {
                blackboard.DoorBlockedTimer = 0f;
                blackboard.DoorKnockTimer = 0f;
                blackboard.Agent.isStopped = false;
                blackboard.ChaseReentrySuppressTimer = blackboard.Definition.WanderChaseSuppressDuration;
                return NpcStateId.Wander;
            }

            return NpcStateId.Chase;
        }
    }
}


