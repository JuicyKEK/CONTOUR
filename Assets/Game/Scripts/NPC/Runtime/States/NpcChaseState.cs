using Game.Scripts.NPC.Runtime.Core;
using UnityEngine;

namespace Game.Scripts.NPC.Runtime.States
{
    /// <summary>
    /// Погоня: агент через NavMesh бежит к игроку (к месту, где видел его последним).
    ///  - Игрок в досягаемости - атака (NpcStateId.Attack).
    ///  - Игрок пропал из виду (забежал за угол) - бот добегает до места, где видел его последним, и если игрока
    ///    там не видно - начинает поиск (NpcStateId.Search). Не добежал за LoseSightGiveUpDelay - тоже поиск.
    ///  - Сюжетная погоня (Blackboard.IsHunting: ForceHunt или ChaseStopsOnlyByStory) - бот всегда знает, где
    ///    игрок, не ищет и сам не останавливается: только по команде сюжета.
    ///  - Агрессию сняли извне (сюжет) - бросает погоню и возвращается к спокойному поведению.
    ///
    /// Двери на пути (NpcDoorNavigator - поиск двери по курсу движения): незапертую бот, если умеет,
    /// открывает на ходу (анимация открытия - человек или монстр, по облику). Запертую (или если
    /// открывать не умеет) - стоит и периодически дёргает ("DoorLockedTry"), а через LockedDoorGiveUpDelay
    /// переходит к поиску игрока по эту сторону двери. В сюжетной погоне от двери не отходит.
    /// </summary>
    public class NpcChaseState : NpcState
    {
        private const float RepathInterval = 0.25f;
        private const float RepathTargetShift = 0.5f;
        private const float AttackVerticalTolerance = 1.5f;

        private float m_RepathTimer;
        private Vector3 m_LastDestination;

        public override NpcStateId Id => NpcStateId.Chase;

        public override void Enter(NpcBlackboard blackboard)
        {
            if (blackboard.Definition.ChaseStopsOnlyByStory)
            {
                blackboard.IsHunting = true;
            }

            NpcNavigation.SetSpeed(blackboard.Agent, blackboard.Definition.ChaseSpeed);
            NpcNavigation.SetStopped(blackboard.Agent, false);

            if (blackboard.Agent != null)
            {
                blackboard.Agent.angularSpeed = Mathf.Max(blackboard.Agent.angularSpeed, 360f);
            }

            blackboard.DoorKnockTimer = 0f;
            blackboard.DoorBlockedTimer = 0f;
            // Отсчёт "потерял из виду" - с начала погони: иначе погоня по команде сюжета у бота, который давно
            // не видел игрока, сразу сменялась бы поиском.
            blackboard.TimeSincePlayerLastSeen = 0f;
            m_RepathTimer = 0f;

            if (blackboard.IsHunting || blackboard.CanSeePlayer || !blackboard.HasLastKnownPlayerPosition)
            {
                UpdatePlayerPosition(blackboard);
            }

            blackboard.Controller.PlayAnimationSound("Chase");
            blackboard.Controller.RaiseReachedChaseStart();
        }

        public override NpcStateId? Tick(NpcBlackboard blackboard, float deltaTime)
        {
            // Агрессию выключили извне (сюжет/граф) - бросаем погоню. Сюжетную погоню снимает только сюжет.
            if (!blackboard.IsHunting && !blackboard.IsAggressive)
            {
                blackboard.Controller.PrepareReturnToCalm();
                return blackboard.CalmState;
            }

            if (blackboard.IsHunting)
            {
                UpdatePlayerPosition(blackboard);
            }

            var doorResult = HandleDoorsAhead(blackboard, deltaTime);

            if (doorResult.HasValue)
            {
                return doorResult.Value == NpcStateId.Chase ? null : doorResult;
            }

            bool knowsWherePlayerIs = blackboard.IsHunting || blackboard.CanSeePlayer;

            if (knowsWherePlayerIs && CanAttack(blackboard))
            {
                return NpcStateId.Attack;
            }

            MoveTowards(blackboard, blackboard.LastKnownPlayerPosition, deltaTime);

            if (!knowsWherePlayerIs)
            {
                bool reachedLastSeen = NpcNavigation.HasArrived(blackboard.Agent, 0.6f);

                if (reachedLastSeen || blackboard.TimeSincePlayerLastSeen >= blackboard.Definition.LoseSightGiveUpDelay)
                {
                    blackboard.SearchCenter = blackboard.LastKnownPlayerPosition;
                    return NpcStateId.Search;
                }
            }

            return null;
        }

        public override void Exit(NpcBlackboard blackboard)
        {
            NpcNavigation.SetStopped(blackboard.Agent, false);
        }

        private static void UpdatePlayerPosition(NpcBlackboard blackboard)
        {
            if (blackboard.PlayerTransform == null)
            {
                return;
            }

            blackboard.LastKnownPlayerPosition = blackboard.PlayerTransform.position;
            blackboard.HasLastKnownPlayerPosition = true;
        }

        private static bool CanAttack(NpcBlackboard blackboard)
        {
            // Игрока уже схватил другой бот - не нападаем.
            if (blackboard.AttackCooldownTimer > 0f || blackboard.PlayerTransform == null || blackboard.Controller.IsPlayerCaptured)
            {
                return false;
            }

            if (blackboard.DistanceToPlayer > blackboard.Definition.CatchDistance)
            {
                return false;
            }

            float verticalGap = Mathf.Abs(blackboard.PlayerTransform.position.y - blackboard.Self.position.y);
            return verticalGap <= AttackVerticalTolerance;
        }

        private void MoveTowards(NpcBlackboard blackboard, Vector3 target, float deltaTime)
        {
            m_RepathTimer -= deltaTime;

            if (m_RepathTimer > 0f && (target - m_LastDestination).sqrMagnitude < RepathTargetShift * RepathTargetShift)
            {
                return;
            }

            m_RepathTimer = RepathInterval;
            m_LastDestination = target;
            NpcNavigation.TrySetDestination(blackboard.Agent, target);
        }

        /// <summary>
        /// null - двери нет или она уже не мешает; Chase - бот стоит перед дверью (остаться в погоне);
        /// Search - пора бросить дверь и искать игрока.
        /// </summary>
        private NpcStateId? HandleDoorsAhead(NpcBlackboard blackboard, float deltaTime)
        {
            var door = NpcDoorNavigator.DetectDoorAhead(blackboard);

            if (door == null)
            {
                blackboard.DoorBlockedTimer = 0f;
                blackboard.DoorKnockTimer = 0f;
                NpcNavigation.SetStopped(blackboard.Agent, false);
                return null;
            }

            if (!door.IsLocked.CurrentValue && blackboard.Definition.CanOpenDoors)
            {
                door.TryOpenBy(blackboard.Controller.GetDoorOpener(true));
                blackboard.DoorBlockedTimer = 0f;
                blackboard.DoorKnockTimer = 0f;
                NpcNavigation.SetStopped(blackboard.Agent, false);
                return null;
            }

            // Дверь не открыть - стоим, периодически дёргаем, а потом ищем игрока по эту сторону двери.
            NpcNavigation.SetStopped(blackboard.Agent, true);
            blackboard.DoorBlockedTimer += deltaTime;
            blackboard.DoorKnockTimer -= deltaTime;

            if (blackboard.DoorKnockTimer <= 0f)
            {
                blackboard.DoorKnockTimer = blackboard.Definition.KnockInterval;

                if (blackboard.Definition.CanOpenDoors)
                {
                    door.TryOpenBy(blackboard.Controller.GetDoorOpener(true));
                }

                blackboard.Controller.PlayAnimationSound("DoorLockedTry");
            }

            if (!blackboard.IsHunting && blackboard.DoorBlockedTimer >= blackboard.Definition.LockedDoorGiveUpDelay)
            {
                // Дальше - поиск по эту сторону: дверь помечается непроходимой, точки поиска за ней не выбираются.
                Controllers.NpcDoorNavBlocker.Block(door);
                blackboard.DoorBlockedTimer = 0f;
                blackboard.DoorKnockTimer = 0f;
                NpcNavigation.SetStopped(blackboard.Agent, false);
                blackboard.ChaseReentrySuppressTimer = blackboard.Definition.WanderChaseSuppressDuration;
                blackboard.SearchCenter = blackboard.Self.position;
                return NpcStateId.Search;
            }

            return NpcStateId.Chase;
        }
    }
}
