using Game.Scripts.NPC.Runtime.Core;
using Game.Scripts.Story;

namespace Game.Scripts.NPC.Runtime.States
{
    /// <summary>
    /// Патрулирование: бот ходит по точкам своего текущего NpcPatrolRoute
    /// (2 и более точек, может зациклиться). Маршрут можно целиком подменить
    /// через сюжет (NpcController.SetPatrolRoute), не выходя из состояния.
    ///
    /// Если на пути к точке назначена дверь (NpcPatrolPoint.DoorToPass) и бот
    /// умеет открывать двери - пытается её открыть; если заперта - "стучит"
    /// (проигрывает анимацию/звук Knock) и ждёт, периодически повторяя попытку,
    /// пока дверь не откроют. Мирные боты не реагируют на игрока и продолжают
    /// патрулировать независимо от того, видят его или нет. Агрессивные боты
    /// при появлении игрока в поле зрения уходят в Chase.
    ///
    /// Если маршрут НЕ зациклен (NpcPatrolRoute.Loop == false) - по достижении
    /// последней точки бот один раз поднимает StoryEventChannelSO
    /// (NpcController.RaisePatrolRouteCompleted) и просто останавливается,
    /// больше не пытаясь двигаться дальше по маршруту.
    /// </summary>
    public class NpcPatrolState : NpcState
    {
        public override NpcStateId Id => NpcStateId.Patrol;

        private bool m_HasRaisedRouteCompleted;

        public override void Enter(NpcBlackboard blackboard)
        {
            blackboard.Agent.speed = blackboard.Definition.PatrolSpeed;
            blackboard.Agent.isStopped = false;
            blackboard.DoorKnockTimer = 0f;
            blackboard.CurrentPatrolIndex = 0;
            m_HasRaisedRouteCompleted = false;
            MoveToCurrentPoint(blackboard);
            blackboard.Controller.PlayAnimationSound("Patrol");
        }

        public override NpcStateId? Tick(NpcBlackboard blackboard, float deltaTime)
        {
            if (blackboard.AggressionSource != null
                && blackboard.AggressionSource.IsAggressive.CurrentValue
                && blackboard.CanSeePlayer)
            {
                return NpcStateId.Chase;
            }

            // Незацикленный маршрут уже пройден до конца - просто стоим,
            // событие о завершении уже поднято один раз в AdvanceToNextPoint.
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
            // валиден для текущего маршрута, просто перескакиваем на первую точку
            // вместо падения с ArgumentOutOfRangeException.
            if (blackboard.CurrentPatrolIndex >= route.Points.Count)
            {
                blackboard.CurrentPatrolIndex = 0;
                MoveToCurrentPoint(blackboard);
                return null;
            }

            var currentPoint = route.Points[blackboard.CurrentPatrolIndex];
            var door = currentPoint.ResolveDoor();

            if (door != null && blackboard.Definition.CanOpenDoors)
            {
                if (door.IsLocked.CurrentValue)
                {
                    HandleLockedDoor(blackboard, deltaTime);
                    return null;
                }

                // Дверь не заперта, но ещё закрыта - открываем на ходу (не
                // останавливаясь, коллайдер сам отключается на время анимации
                // открытия во View), затем продолжаем идти к точке как обычно.
                if (!door.IsOpen.CurrentValue)
                {
                    door.TryOpenBy(blackboard.Definition.PatrolDoorOpenerKind);
                }
            }

            if (!blackboard.Agent.pathPending && blackboard.Agent.remainingDistance <= blackboard.Agent.stoppingDistance)
            {
                AdvanceToNextPoint(blackboard);
            }

            return null;
        }

        private void HandleLockedDoor(NpcBlackboard blackboard, float deltaTime)
        {
            blackboard.Agent.isStopped = true;
            blackboard.DoorKnockTimer -= deltaTime;

            if (blackboard.DoorKnockTimer <= 0f)
            {
                blackboard.DoorKnockTimer = blackboard.Definition.KnockInterval;
                blackboard.Controller.PlayAnimationSound("Knock");
            }
        }

        private void AdvanceToNextPoint(NpcBlackboard blackboard)
        {
            var routeBeforeEvent = blackboard.PatrolRoute;

            if (routeBeforeEvent == null || blackboard.CurrentPatrolIndex >= routeBeforeEvent.Points.Count)
            {
                return;
            }

            // Точка, которую бот только что достиг - если у неё задан
            // ArrivalEventChannel, раздаём его ровно в момент достижения, до того
            // как перейти к следующей точке (или остановиться, если маршрут не
            // зациклен и это была последняя точка).
            //
            // ВАЖНО: Raise() может СИНХРОННО вызвать NpcController.SetPatrolRoute
            // (например, через StoryEventChannelListener), который подменит
            // blackboard.PatrolRoute и сбросит blackboard.CurrentPatrolIndex прямо
            // во время этого вызова. Поэтому нельзя переиспользовать захваченные до
            // Raise() значения route/index ниже - нужно перечитать их заново.
            var reachedPoint = routeBeforeEvent.Points[blackboard.CurrentPatrolIndex];
            StorySignals.Raise(reachedPoint.ArrivalSignal);
            reachedPoint.ArrivalEventChannel?.Raise();

            // Перечитываем маршрут и индекс ПОСЛЕ события - если Raise() подменил
            // маршрут (например, сюжет сократил список точек), дальше работаем уже
            // с актуальными данными, а не с теми, что были до вызова.
            var route = blackboard.PatrolRoute;

            if (route == null || route.Points.Count == 0)
            {
                return;
            }

            bool routeWasReplacedDuringEvent = !ReferenceEquals(route, routeBeforeEvent);

            // Если маршрут подменили прямо во время Raise() - SetPatrolRoute уже
            // сбросил CurrentPatrolIndex на 0 сам, поэтому просто идём к первой
            // точке нового маршрута и не трогаем индекс дальше.
            if (routeWasReplacedDuringEvent)
            {
                blackboard.Agent.isStopped = false;
                MoveToCurrentPoint(blackboard);
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
                    // поднимаем событие завершения маршрута один раз и останавливаем бота.
                    m_HasRaisedRouteCompleted = true;
                    blackboard.Agent.isStopped = true;
                    blackboard.Controller.RaisePatrolRouteCompleted();
                    return;
                }
            }

            blackboard.CurrentPatrolIndex = nextIndex;
            blackboard.Agent.isStopped = false;
            MoveToCurrentPoint(blackboard);
        }

        private void MoveToCurrentPoint(NpcBlackboard blackboard)
        {
            var route = blackboard.PatrolRoute;

            if (route == null || route.Points.Count == 0)
            {
                return;
            }

            if (blackboard.CurrentPatrolIndex >= route.Points.Count)
            {
                blackboard.CurrentPatrolIndex = 0;
            }

            var point = route.Points[blackboard.CurrentPatrolIndex];

            if (point.Point != null)
            {
                blackboard.Agent.SetDestination(point.Point.position);
            }
        }
    }
}