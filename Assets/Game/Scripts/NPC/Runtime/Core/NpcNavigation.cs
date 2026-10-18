using System;
using UnityEngine;
using UnityEngine.AI;

namespace Game.Scripts.NPC.Runtime.Core
{
    /// <summary>
    /// Общие операции с NavMeshAgent для состояний: безопасная постановка цели (агент выключен или ещё
    /// не стоит на NavMesh - ничего не делаем, а не сыплем ошибками), "дошёл ли", случайная достижимая точка.
    /// </summary>
    public static class NpcNavigation
    {
        private const int RandomPointAttempts = 12;

        private static readonly NavMeshPath s_Path = new();

        public static bool IsReady(NavMeshAgent agent)
        {
            return agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh;
        }

        public static bool TrySetDestination(NavMeshAgent agent, Vector3 target)
        {
            if (!IsReady(agent))
            {
                return false;
            }

            return agent.SetDestination(target);
        }

        public static void SetStopped(NavMeshAgent agent, bool isStopped)
        {
            if (IsReady(agent))
            {
                agent.isStopped = isStopped;
            }
        }

        /// <summary>
        /// Остановиться на месте сразу (без доезда по инерции до старой цели).
        /// </summary>
        public static void Halt(NavMeshAgent agent)
        {
            if (!IsReady(agent))
            {
                return;
            }

            agent.isStopped = true;
            agent.ResetPath();
            agent.velocity = Vector3.zero;
        }

        public static void SetSpeed(NavMeshAgent agent, float speed)
        {
            if (agent != null)
            {
                agent.speed = speed;
            }
        }

        /// <summary>
        /// Агент дошёл до цели (с небольшим запасом к stoppingDistance).
        /// </summary>
        public static bool HasArrived(NavMeshAgent agent, float extraDistance = 0.15f)
        {
            if (!IsReady(agent) || agent.pathPending)
            {
                return false;
            }

            if (!agent.hasPath)
            {
                return true;
            }

            return agent.remainingDistance <= agent.stoppingDistance + extraDistance;
        }

        /// <summary>
        /// Случайная точка на NavMesh в кольце [minRadius, radius] вокруг center, до которой агент может дойти
        /// целиком (путь Complete), и которая проходит фильтр (если он задан).
        /// </summary>
        public static bool TryGetRandomReachablePoint(NavMeshAgent agent, Vector3 center, float radius, float minRadius,
            Func<Vector3, bool> filter, out Vector3 point)
        {
            for (int attempt = 0; attempt < RandomPointAttempts; attempt++)
            {
                Vector2 direction = UnityEngine.Random.insideUnitCircle.normalized;
                float distance = UnityEngine.Random.Range(Mathf.Min(minRadius, radius), radius);
                Vector3 candidate = center + new Vector3(direction.x, 0f, direction.y) * distance;

                if (TryGetReachable(agent, candidate, 2f, filter, out point))
                {
                    return true;
                }
            }

            point = center;
            return false;
        }

        /// <summary>
        /// Ближайшая к candidate точка NavMesh, до которой агент может дойти целиком.
        /// </summary>
        public static bool TryGetReachable(NavMeshAgent agent, Vector3 candidate, float sampleDistance,
            Func<Vector3, bool> filter, out Vector3 point)
        {
            point = candidate;

            if (!IsReady(agent))
            {
                return false;
            }

            if (!NavMesh.SamplePosition(candidate, out var hit, sampleDistance, agent.areaMask))
            {
                return false;
            }

            if (filter != null && !filter(hit.position))
            {
                return false;
            }

            if (!agent.CalculatePath(hit.position, s_Path) || s_Path.status != NavMeshPathStatus.PathComplete)
            {
                return false;
            }

            point = hit.position;
            return true;
        }

        /// <summary>
        /// Плавно поворачивает бота лицом к точке (по горизонтали).
        /// </summary>
        public static void FaceTowards(Transform self, Vector3 target, float degreesPerSecond, float deltaTime)
        {
            var direction = target - self.position;
            direction.y = 0f;

            if (direction.sqrMagnitude < 0.0001f)
            {
                return;
            }

            var targetRotation = Quaternion.LookRotation(direction.normalized);
            self.rotation = Quaternion.RotateTowards(self.rotation, targetRotation, degreesPerSecond * deltaTime);
        }
    }
}
