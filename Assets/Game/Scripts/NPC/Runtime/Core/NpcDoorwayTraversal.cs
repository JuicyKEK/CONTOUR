using Game.Scripts.NPC.Runtime.Controllers;
using Game.Scripts.NPC.Runtime.Data;
using UnityEngine;
using UnityEngine.AI;

namespace Game.Scripts.NPC.Runtime.Core
{
    /// <summary>
    /// Проход бота через дверной проём своей анимацией (NpcDoorway + NpcAnimationSoundProfileSO.DoorwayClips).
    /// Когда путь агента пересекает открытый проём и до проёма осталось StartDistance клипа, бот перестаёт идти по
    /// NavMesh: его ведут прямо через проём по кривой клипа (Progress: доля пути от доли времени), выравнивая по
    /// центру проёма, а аниматор играет клип прохода (шагом или бегом - по скорости). После клипа агент снова ставится
    /// на NavMesh и продолжает свой путь. Пока бот в проёме, состояние поведения не тикает.
    /// </summary>
    public sealed class NpcDoorwayTraversal
    {
        private const float SearchRadius = 3.5f;
        private const float MinSpeed = 0.15f;
        private const float AlignSeconds = 0.3f;

        private NpcDoorway m_Doorway;
        private NpcDoorwayClip m_Clip;
        private float m_Time;
        private float m_StartTime;
        private Vector3 m_Dir;
        private Vector3 m_Right;
        private float m_Lateral0;
        private float m_Y;
        private Quaternion m_Rotation0;
        private Vector3 m_Destination;

        public bool IsActive { get; private set; }

        /// <summary>
        /// Начать проход, если бот идёт через проём и его профиль умеет проходить проёмы. true - проход начался.
        /// </summary>
        public bool TryStart(NpcBlackboard blackboard, NpcAnimationSoundProfileSO profile, NpcAnimatorDriver animator)
        {
            var agent = blackboard.Agent;

            if (profile == null || !profile.HasDoorwayClips || !NpcNavigation.IsReady(agent) || !agent.hasPath ||
                agent.isStopped || blackboard.Controller.IsPosed)
            {
                return false;
            }

            var velocity = agent.velocity;
            velocity.y = 0f;
            float speed = velocity.magnitude;

            if (speed < MinSpeed)
            {
                return false;
            }

            var definition = blackboard.Definition;
            bool run = speed >= (definition.PatrolSpeed + definition.ChaseSpeed) * 0.5f;

            if (!profile.TryGetDoorwayClip(run, out var clip))
            {
                return false;
            }

            var position = blackboard.Self.position;

            foreach (var doorway in NpcDoorway.All)
            {
                var rel = doorway.Centre - position;
                rel.y = 0f;

                if (rel.sqrMagnitude > SearchRadius * SearchRadius || !doorway.IsPassable)
                {
                    continue;
                }

                var n = doorway.Normal;
                float dn = Vector3.Dot(position - doorway.Centre, n);       // how far in front of the plane (signed)
                var dir = dn > 0f ? -n : n;                                  // across, toward the other side
                float distance = Mathf.Abs(dn);
                var right = Vector3.Cross(Vector3.up, dir);
                float lateral = Vector3.Dot(position - doorway.Centre, right);

                if (distance > clip.StartDistance + 0.05f || distance < 0.15f || Mathf.Abs(lateral) > doorway.Width / 2f ||
                    Vector3.Dot(velocity, dir) < MinSpeed * 0.5f || !PathCrosses(agent, doorway, n))
                {
                    continue;
                }

                Begin(blackboard, animator, doorway, clip, dir, right, lateral, distance);
                return true;
            }

            return false;
        }

        public void Tick(NpcBlackboard blackboard, float deltaTime)
        {
            if (!IsActive)
            {
                return;
            }

            m_Time += deltaTime;
            float u = m_Clip.Duration > 0f ? Mathf.Clamp01(m_Time / m_Clip.Duration) : 1f;
            float along = m_Clip.Travel * Progress(u) - m_Clip.StartDistance;   // metres past the plane
            float align = Mathf.SmoothStep(0f, 1f, (m_Time - m_StartTime) / AlignSeconds);
            var position = m_Doorway.Centre + m_Dir * along + m_Right * (m_Lateral0 * (1f - align));
            position.y = m_Y;
            var self = blackboard.Self;
            self.SetPositionAndRotation(position, Quaternion.Slerp(m_Rotation0, Quaternion.LookRotation(m_Dir), align));

            if (m_Time >= m_Clip.Duration)
            {
                Finish(blackboard);
            }
        }

        /// <summary>
        /// Закончить проход сейчас (конец клипа или внешняя команда): агент снова на NavMesh там, где бот стоит.
        /// </summary>
        public void Finish(NpcBlackboard blackboard)
        {
            if (!IsActive)
            {
                return;
            }

            IsActive = false;
            var agent = blackboard.Agent;

            if (agent == null)
            {
                return;
            }

            var position = blackboard.Self.position;

            if (NavMesh.SamplePosition(position, out var hit, 0.6f, agent.areaMask))
            {
                agent.Warp(hit.position);
            }

            agent.updatePosition = true;
            agent.updateRotation = true;
            agent.isStopped = false;

            if (agent.isOnNavMesh)
            {
                agent.SetDestination(m_Destination);
            }

            blackboard.Controller.LogDebug("прошёл проём");
        }

        private void Begin(NpcBlackboard blackboard, NpcAnimatorDriver animator, NpcDoorway doorway, NpcDoorwayClip clip,
            Vector3 dir, Vector3 right, float lateral, float distance)
        {
            var agent = blackboard.Agent;
            m_Doorway = doorway;
            m_Clip = clip;
            m_Dir = dir;
            m_Right = right;
            m_Lateral0 = lateral;
            m_Y = blackboard.Self.position.y;
            m_Rotation0 = blackboard.Self.rotation;
            m_Destination = agent.destination;
            // Closer than the clip's start: the clip is entered where its root would be now.
            m_StartTime = TimeAt(clip, clip.StartDistance - distance);
            m_Time = m_StartTime;
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            agent.updatePosition = false;
            agent.updateRotation = false;
            IsActive = true;
            animator.TrySetTrigger(clip.AnimatorState);
            blackboard.Controller.LogDebug($"проходит проём ({clip.AnimatorState}), до проёма {distance:0.00} м");
        }

        private float Progress(float u)
        {
            return m_Clip.Progress != null && m_Clip.Progress.length > 0 ? m_Clip.Progress.Evaluate(u) : u;
        }

        private static float TimeAt(NpcDoorwayClip clip, float travelled)
        {
            if (travelled <= 0f || clip.Travel <= 0f)
            {
                return 0f;
            }

            const int steps = 60;

            for (int k = 1; k <= steps; k++)
            {
                float u = k / (float)steps;
                float s = clip.Travel * (clip.Progress != null && clip.Progress.length > 0 ? clip.Progress.Evaluate(u) : u);

                if (s >= travelled)
                {
                    return u * clip.Duration;
                }
            }

            return clip.Duration;
        }

        /// <summary>Путь агента проходит через плоскость проёма в пределах его ширины.</summary>
        private static bool PathCrosses(NavMeshAgent agent, NpcDoorway doorway, Vector3 n)
        {
            var corners = agent.path.corners;
            var right = Vector3.Cross(Vector3.up, n);
            var prev = agent.transform.position;

            for (int i = 0; i <= corners.Length; i++)
            {
                var next = i < corners.Length ? corners[i] : agent.destination;
                float a = Vector3.Dot(prev - doorway.Centre, n);
                float b = Vector3.Dot(next - doorway.Centre, n);

                if (a * b < 0f)
                {
                    var hit = Vector3.Lerp(prev, next, a / (a - b));
                    return Mathf.Abs(Vector3.Dot(hit - doorway.Centre, right)) < doorway.Width / 2f + 0.25f;
                }

                prev = next;
            }

            return false;
        }
    }
}
