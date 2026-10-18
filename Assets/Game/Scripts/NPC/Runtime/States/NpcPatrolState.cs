using Game.Scripts.InteractionObjects.Interfaces;
using Game.Scripts.NPC.Runtime.Controllers;
using Game.Scripts.NPC.Runtime.Core;
using Game.Scripts.Story;
using UnityEngine;

namespace Game.Scripts.NPC.Runtime.States
{
    /// <summary>
    /// Патрулирование - спокойное поведение: бот ходит по точкам своего текущего NpcPatrolRoute
    /// (может зациклиться). Маршрут можно целиком подменить через сюжет
    /// (NpcController.SetPatrolRoute), не выходя из состояния.
    ///
    /// На точке с задержкой (NpcPatrolPoint.WaitSeconds) бот стоит и играет действие точки
    /// (ActionKey) - так мимик изображает свою роль: "чинит механизм", "моет посуду".
    ///
    /// Двери: назначенная точке (NpcPatrolPoint.DoorToPass) или закрытая дверь прямо по курсу.
    /// Незапертую бот, если умеет, открывает на ходу; запертую (или если открывать не умеет) -
    /// "стучит" (Knock) и ждёт, периодически повторяя, пока дверь не откроют.
    /// Мирные боты не реагируют на игрока; агрессивные при виде игрока уходят в Chase.
    ///
    /// Если маршрут НЕ зациклен (NpcPatrolRoute.Loop == false) - по достижении
    /// последней точки бот один раз сообщает об этом (NpcController.RaisePatrolRouteCompleted)
    /// и останавливается, больше не пытаясь двигаться дальше по маршруту.
    /// </summary>
    public class NpcPatrolState : NpcState
    {
        private const float FaceDegreesPerSecond = 240f;

        public override NpcStateId Id => NpcStateId.Patrol;

        private const float RerouteDelay = 0.3f;
        private const float UnreachableRetryDelay = 3f;
        private const float ReachableEndDistance = 1.5f;

        private readonly NpcLockedDoorAttempts m_DoorAttempts = new();

        private bool m_HasRaisedRouteCompleted;
        private bool m_IsDwelling;
        private float m_DwellTimer;
        private float m_RerouteTimer = -1f;
        private float m_UnreachableRetryTimer = -1f;
        private bool m_IsCheckingReachability;
        private int m_SkippedPoints;

        public override void Enter(NpcBlackboard blackboard)
        {
            NpcNavigation.SetSpeed(blackboard.Agent, blackboard.Definition.PatrolSpeed);
            NpcNavigation.SetStopped(blackboard.Agent, false);
            blackboard.DoorKnockTimer = 0f;
            m_HasRaisedRouteCompleted = false;
            m_IsDwelling = false;
            m_RerouteTimer = -1f;
            m_UnreachableRetryTimer = -1f;
            m_SkippedPoints = 0;
            m_DoorAttempts.Reset();

            if (blackboard.PatrolResumeNearest)
            {
                blackboard.PatrolResumeNearest = false;
                blackboard.CurrentPatrolIndex = FindNearestPointIndex(blackboard);
            }
            else
            {
                blackboard.CurrentPatrolIndex = 0;
            }

            MoveToCurrentPoint(blackboard);
            blackboard.Controller.PlayAnimationSound("Patrol");
        }

        public override NpcStateId? Tick(NpcBlackboard blackboard, float deltaTime)
        {
            if (blackboard.IsAggressive && blackboard.CanSeePlayer)
            {
                return NpcStateId.Chase;
            }

            // Незацикленный маршрут уже пройден до конца - просто стоим,
            // событие о завершении уже поднято один раз.
            if (m_HasRaisedRouteCompleted)
            {
                return null;
            }

            var route = blackboard.PatrolRoute;

            if (route == null || route.Points.Count == 0)
            {
                return null;
            }

            // Защита от рассинхронизации: маршрут/индекс могли быть подменены
            // снаружи (SetPatrolRoute) между тиками - если индекс больше не
            // валиден для текущего маршрута, просто перескакиваем на первую точку.
            if (blackboard.CurrentPatrolIndex >= route.Points.Count)
            {
                blackboard.CurrentPatrolIndex = 0;
                m_IsDwelling = false;
                MoveToCurrentPoint(blackboard);
                return null;
            }

            var currentPoint = route.Points[blackboard.CurrentPatrolIndex];

            if (m_IsDwelling)
            {
                TickDwell(blackboard, currentPoint, deltaTime);
                return null;
            }

            // Бот сдался у запертой двери: ждём, пока NavMesh вырежет проём, и строим путь заново.
            if (m_RerouteTimer >= 0f)
            {
                m_RerouteTimer -= deltaTime;

                if (m_RerouteTimer < 0f)
                {
                    m_SkippedPoints = 0;
                    MoveToCurrentPoint(blackboard);
                }

                return null;
            }

            if (CheckReachability(blackboard, deltaTime))
            {
                return null;
            }

            if (HandleDoor(blackboard, currentPoint, deltaTime))
            {
                return null;
            }

            if (NpcNavigation.HasArrived(blackboard.Agent))
            {
                OnArrived(blackboard);
            }

            return null;
        }

        /// <summary>
        /// Двери на пути. true - бот стоит перед дверью, которую не может открыть (ждёт).
        /// </summary>
        private bool HandleDoor(NpcBlackboard blackboard, NpcPatrolPoint point, float deltaTime)
        {
            var assignedDoor = point.ResolveDoor();
            bool isAssignedDoor = assignedDoor != null && !assignedDoor.IsOpen.CurrentValue;
            var door = isAssignedDoor ? assignedDoor : NpcDoorNavigator.DetectDoorAhead(blackboard);

            if (door == null || door.IsOpen.CurrentValue)
            {
                m_DoorAttempts.Reset();
                NpcNavigation.SetStopped(blackboard.Agent, false);
                return false;
            }

            if (!door.IsLocked.CurrentValue && blackboard.Definition.CanOpenDoors)
            {
                // Дверь не заперта, но ещё закрыта - открываем на ходу (коллайдер сам
                // отключается на время анимации открытия во View) и идём дальше.
                door.TryOpenBy(blackboard.Controller.GetDoorOpener(false));
                m_DoorAttempts.Reset();
                NpcNavigation.SetStopped(blackboard.Agent, false);
                return false;
            }

            // Дверь, заданная точке маршрута, - часть сценария: бот стучит и ждёт, пока её откроют.
            if (isAssignedDoor)
            {
                HandleLockedDoor(blackboard, door, deltaTime);
                return true;
            }

            // Запертая дверь на пути: несколько попыток, потом - обход.
            if (m_DoorAttempts.Tick(blackboard, door, deltaTime))
            {
                m_RerouteTimer = RerouteDelay;
            }

            return true;
        }

        /// <summary>
        /// Точка маршрута стала недостижимой (за дверью, которую пометили запертой) - пропускаем её.
        /// true - бот занят этим (ждёт или идёт к другой точке).
        /// </summary>
        private bool CheckReachability(NpcBlackboard blackboard, float deltaTime)
        {
            if (m_UnreachableRetryTimer >= 0f)
            {
                m_UnreachableRetryTimer -= deltaTime;

                if (m_UnreachableRetryTimer < 0f)
                {
                    m_SkippedPoints = 0;
                    MoveToCurrentPoint(blackboard);
                }

                return true;
            }

            var agent = blackboard.Agent;

            if (!m_IsCheckingReachability || !NpcNavigation.IsReady(agent) || agent.pathPending)
            {
                return false;
            }

            m_IsCheckingReachability = false;
            var route = blackboard.PatrolRoute;
            var target = route.Points[blackboard.CurrentPatrolIndex].Point;

            bool isReachable = target == null
                               || agent.pathStatus == UnityEngine.AI.NavMeshPathStatus.PathComplete
                               || (agent.hasPath && (agent.pathEndPosition - target.position).sqrMagnitude <= ReachableEndDistance * ReachableEndDistance);

            if (isReachable)
            {
                m_SkippedPoints = 0;
                return false;
            }

            m_SkippedPoints++;

            if (m_SkippedPoints >= route.Points.Count)
            {
                // Ни одна точка не достижима (всё за запертыми дверями) - стоим и пробуем снова.
                NpcNavigation.Halt(agent);
                m_UnreachableRetryTimer = UnreachableRetryDelay;
                blackboard.Controller.LogDebug("ни одна точка маршрута сейчас недостижима - жду");
                return true;
            }

            int next = blackboard.CurrentPatrolIndex + 1;

            if (next >= route.Points.Count)
            {
                if (!route.Loop)
                {
                    m_HasRaisedRouteCompleted = true;
                    NpcNavigation.Halt(agent);
                    return true;
                }

                next = 0;
            }

            blackboard.Controller.LogDebug($"точка маршрута {blackboard.CurrentPatrolIndex} недостижима - иду к следующей");
            blackboard.CurrentPatrolIndex = next;
            MoveToCurrentPoint(blackboard);
            return true;
        }

        private void HandleLockedDoor(NpcBlackboard blackboard, IInteractionDoor door, float deltaTime)
        {
            NpcNavigation.SetStopped(blackboard.Agent, true);
            blackboard.DoorKnockTimer -= deltaTime;

            if (blackboard.DoorKnockTimer <= 0f)
            {
                blackboard.DoorKnockTimer = blackboard.Definition.KnockInterval;
                blackboard.Controller.PlayAnimationSound("Knock");
            }
        }

        private void TickDwell(NpcBlackboard blackboard, NpcPatrolPoint point, float deltaTime)
        {
            if (point.FacePointDirection && point.Point != null)
            {
                var target = blackboard.Self.position + point.Point.forward;
                NpcNavigation.FaceTowards(blackboard.Self, target, FaceDegreesPerSecond, deltaTime);
            }

            m_DwellTimer -= deltaTime;

            if (m_DwellTimer > 0f)
            {
                return;
            }

            m_IsDwelling = false;
            MoveToNextPoint(blackboard, true);
        }

        /// <summary>
        /// Бот дошёл до текущей точки: сигналы точки, затем задержка с действием или сразу следующая точка.
        /// </summary>
        private void OnArrived(NpcBlackboard blackboard)
        {
            var routeBeforeEvent = blackboard.PatrolRoute;

            if (routeBeforeEvent == null || blackboard.CurrentPatrolIndex >= routeBeforeEvent.Points.Count)
            {
                return;
            }

            // ВАЖНО: сигнал/канал точки может СИНХРОННО вызвать NpcController.SetPatrolRoute
            // (например, через StoryEventChannelListener), который подменит маршрут и сбросит
            // индекс прямо во время этого вызова. Поэтому после Raise маршрут и индекс перечитываются.
            var reachedPoint = routeBeforeEvent.Points[blackboard.CurrentPatrolIndex];
            StorySignals.Raise(reachedPoint.ArrivalSignal);
            reachedPoint.ArrivalEventChannel?.Raise();

            var route = blackboard.PatrolRoute;

            if (route == null || route.Points.Count == 0)
            {
                return;
            }

            // Маршрут подменили прямо во время Raise() - SetPatrolRoute уже сбросил индекс на 0,
            // идём к первой точке нового маршрута.
            if (!ReferenceEquals(route, routeBeforeEvent))
            {
                NpcNavigation.SetStopped(blackboard.Agent, false);
                MoveToCurrentPoint(blackboard);
                return;
            }

            if (reachedPoint.WaitSeconds > 0f)
            {
                m_IsDwelling = true;
                m_DwellTimer = reachedPoint.WaitSeconds;
                NpcNavigation.Halt(blackboard.Agent);

                if (!string.IsNullOrEmpty(reachedPoint.ActionKey))
                {
                    blackboard.Controller.PlayAnimationSound(reachedPoint.ActionKey);
                }

                return;
            }

            MoveToNextPoint(blackboard, false);
        }

        private void MoveToNextPoint(NpcBlackboard blackboard, bool isAfterAction)
        {
            var route = blackboard.PatrolRoute;

            if (route == null || route.Points.Count == 0)
            {
                return;
            }

            int nextIndex = blackboard.CurrentPatrolIndex + 1;

            if (nextIndex >= route.Points.Count)
            {
                if (route.Loop)
                {
                    nextIndex = 0;
                }
                else
                {
                    // Последняя точка незацикленного маршрута достигнута -
                    // сообщаем об этом один раз и останавливаем бота.
                    m_HasRaisedRouteCompleted = true;
                    NpcNavigation.Halt(blackboard.Agent);
                    blackboard.Controller.RaisePatrolRouteCompleted();
                    return;
                }
            }

            blackboard.CurrentPatrolIndex = nextIndex;
            NpcNavigation.SetStopped(blackboard.Agent, false);
            MoveToCurrentPoint(blackboard);

            // После действия на точке - снова походка (а не застывшая анимация действия).
            if (isAfterAction)
            {
                blackboard.Controller.PlayAnimationSound("Patrol");
            }
        }

        private void MoveToCurrentPoint(NpcBlackboard blackboard)
        {
            var route = blackboard.PatrolRoute;

            if (route == null || route.Points.Count == 0)
            {
                NpcNavigation.Halt(blackboard.Agent);
                return;
            }

            if (blackboard.CurrentPatrolIndex >= route.Points.Count)
            {
                blackboard.CurrentPatrolIndex = 0;
            }

            var point = route.Points[blackboard.CurrentPatrolIndex];

            if (point.Point != null)
            {
                NpcNavigation.SetStopped(blackboard.Agent, false);
                m_IsCheckingReachability = NpcNavigation.TrySetDestination(blackboard.Agent, point.Point.position);
            }
        }

        private static int FindNearestPointIndex(NpcBlackboard blackboard)
        {
            var route = blackboard.PatrolRoute;

            if (route == null || route.Points.Count == 0)
            {
                return 0;
            }

            int nearest = 0;
            float nearestDistance = float.MaxValue;
            var position = blackboard.Self.position;

            for (int i = 0; i < route.Points.Count; i++)
            {
                var point = route.Points[i].Point;

                if (point == null)
                {
                    continue;
                }

                float distance = (point.position - position).sqrMagnitude;

                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = i;
                }
            }

            return nearest;
        }
    }
}
