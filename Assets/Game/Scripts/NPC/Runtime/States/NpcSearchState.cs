using System.Collections.Generic;
using Game.Scripts.NPC.Runtime.Core;
using UnityEngine;

namespace Game.Scripts.NPC.Runtime.States
{
    /// <summary>
    /// Поиск потерянного игрока. Скорость - между спокойной и погоней (SearchSpeed).
    ///  1) Сначала бот ходит по случайным точкам в радиусе SearchRadius вокруг места, где видел игрока
    ///     последним (Blackboard.SearchCenter), и в каждой точке осматривается.
    ///  2) Через LocalSearchDuration: если заражение зоны бота не ниже ZoneSearchInfectionLevel - ищет по всей
    ///     зоне (её триггерам InfectionZoneAreaTrigger или NpcSearchArea бота) ZoneSearchDuration секунд
    ///     (0 - пока не найдёт); иначе - возвращается к спокойному поведению (мимик - к своей роли,
    ///     монстр - к обходу).
    /// Увидел игрока (и агрессивен) - снова погоня. Двери: незапертые открывает (если умеет), от запертых
    /// отворачивает к другой точке.
    /// </summary>
    public class NpcSearchState : NpcState
    {
        private const float StuckCheckInterval = 2f;
        private const float StuckMinProgress = 0.3f;

        private enum Phase
        {
            Local,
            Zone
        }

        private readonly List<Collider> m_ZoneAreas = new();

        private Phase m_Phase;
        private float m_PhaseTime;
        private float m_LookAroundTimer;
        private bool m_IsLookingAround;
        private float m_StuckTimer;
        private Vector3 m_StuckCheckPosition;

        public override NpcStateId Id => NpcStateId.Search;

        public override void Enter(NpcBlackboard blackboard)
        {
            NpcNavigation.SetSpeed(blackboard.Agent, blackboard.Definition.SearchSpeed);
            NpcNavigation.SetStopped(blackboard.Agent, false);
            m_Phase = Phase.Local;
            m_PhaseTime = 0f;
            m_IsLookingAround = false;
            m_StuckTimer = 0f;
            m_StuckCheckPosition = blackboard.Self.position;
            blackboard.Controller.PlayAnimationSound("Search");
            PickNextPoint(blackboard);
        }

        public override NpcStateId? Tick(NpcBlackboard blackboard, float deltaTime)
        {
            if (blackboard.ChaseReentrySuppressTimer > 0f)
            {
                blackboard.ChaseReentrySuppressTimer -= deltaTime;
            }
            else if ((blackboard.IsAggressive || blackboard.IsHunting) && blackboard.CanSeePlayer)
            {
                return NpcStateId.Chase;
            }

            // Агрессию сняли (сюжет) - искать незачем.
            if (!blackboard.IsAggressive && !blackboard.IsHunting)
            {
                blackboard.Controller.PrepareReturnToCalm();
                return blackboard.CalmState;
            }

            m_PhaseTime += deltaTime;

            var phaseResult = UpdatePhase(blackboard);

            if (phaseResult.HasValue)
            {
                return phaseResult;
            }

            if (m_IsLookingAround)
            {
                m_LookAroundTimer -= deltaTime;

                if (m_LookAroundTimer <= 0f)
                {
                    m_IsLookingAround = false;
                    PickNextPoint(blackboard);
                }

                return null;
            }

            var door = NpcDoorNavigator.DetectDoorAhead(blackboard);

            if (door != null)
            {
                if (!door.IsLocked.CurrentValue && blackboard.Definition.CanOpenDoors)
                {
                    door.TryOpenBy(blackboard.Controller.GetDoorOpener(false));
                }
                else
                {
                    // За запертую дверь не пройти - ищем в другом месте.
                    PickNextPoint(blackboard);
                    return null;
                }
            }

            if (NpcNavigation.HasArrived(blackboard.Agent))
            {
                StartLookAround(blackboard);
                return null;
            }

            TickStuck(blackboard, deltaTime);
            return null;
        }

        public override void Exit(NpcBlackboard blackboard)
        {
            NpcNavigation.SetStopped(blackboard.Agent, false);
        }

        private NpcStateId? UpdatePhase(NpcBlackboard blackboard)
        {
            var definition = blackboard.Definition;

            if (m_Phase == Phase.Local && m_PhaseTime >= definition.LocalSearchDuration)
            {
                if (blackboard.ZoneInfectionLevel >= definition.ZoneSearchInfectionLevel)
                {
                    m_Phase = Phase.Zone;
                    m_PhaseTime = 0f;
                    CollectZoneAreas(blackboard);
                    m_IsLookingAround = false;
                    blackboard.Controller.LogDebug($"ищет по всей зоне (заражение {blackboard.ZoneInfectionLevel:F0})");
                    PickNextPoint(blackboard);
                    return null;
                }

                blackboard.Controller.PrepareReturnToCalm();
                return blackboard.CalmState;
            }

            if (m_Phase == Phase.Zone && definition.ZoneSearchDuration > 0f && m_PhaseTime >= definition.ZoneSearchDuration)
            {
                blackboard.Controller.PrepareReturnToCalm();
                return blackboard.CalmState;
            }

            return null;
        }

        private void StartLookAround(NpcBlackboard blackboard)
        {
            var definition = blackboard.Definition;
            m_IsLookingAround = true;
            m_LookAroundTimer = Random.Range(definition.SearchLookAroundMin, definition.SearchLookAroundMax);
            blackboard.Controller.PlayAnimationSound("SearchLook");
        }

        private void TickStuck(NpcBlackboard blackboard, float deltaTime)
        {
            m_StuckTimer += deltaTime;

            if (m_StuckTimer < StuckCheckInterval)
            {
                return;
            }

            m_StuckTimer = 0f;
            var position = blackboard.Self.position;

            if ((position - m_StuckCheckPosition).sqrMagnitude < StuckMinProgress * StuckMinProgress)
            {
                PickNextPoint(blackboard);
            }

            m_StuckCheckPosition = position;
        }

        private void PickNextPoint(NpcBlackboard blackboard)
        {
            var definition = blackboard.Definition;
            var agent = blackboard.Agent;
            bool hasPoint;
            Vector3 point;

            if (m_Phase == Phase.Zone && m_ZoneAreas.Count > 0)
            {
                hasPoint = NpcZoneUtility.TryGetRandomPointInAreas(agent, m_ZoneAreas, out point);
            }
            else
            {
                float radius = m_Phase == Phase.Zone ? definition.ZoneSearchFallbackRadius : definition.SearchRadius;
                var center = m_Phase == Phase.Zone ? blackboard.Self.position : blackboard.SearchCenter;
                hasPoint = NpcNavigation.TryGetRandomReachablePoint(agent, center, radius, radius * 0.25f, null, out point);
            }

            m_StuckTimer = 0f;
            m_StuckCheckPosition = blackboard.Self.position;

            if (hasPoint)
            {
                NpcNavigation.SetStopped(agent, false);
                NpcNavigation.TrySetDestination(agent, point);
                return;
            }

            // Точку не нашли (вокруг всё перекрыто) - постоять, осмотреться и попробовать снова.
            StartLookAround(blackboard);
        }

        private void CollectZoneAreas(NpcBlackboard blackboard)
        {
            m_ZoneAreas.Clear();
            var searchArea = blackboard.Controller.SearchArea;

            if (searchArea != null)
            {
                m_ZoneAreas.AddRange(searchArea.Colliders);
                return;
            }

            NpcZoneUtility.CollectZoneColliders(blackboard.Zone, m_ZoneAreas);
        }
    }
}
